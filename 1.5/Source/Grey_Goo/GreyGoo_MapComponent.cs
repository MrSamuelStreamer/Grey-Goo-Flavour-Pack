using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grey_Goo.Buildings;
using RimWorld;
using UnityEngine;
using Verse;
using Random = UnityEngine.Random;
using NetRandom = System.Random;

namespace Grey_Goo;

public class GreyGoo_MapComponent(Map map) : MapComponent(map)
{
    // keep this a list and allow duplicates to make it easier to handle removing overlapping protected tiles.
    public List<IntVec3> ProtectedCells = new List<IntVec3>();
    public HashSet<IntVec3> ProtectedCellsSet => ProtectedCells.ToHashSet();

    public NetRandom RandLocal = new NetRandom();
    public record struct CellInfo
    {
        public bool IsGooed = false;
        public bool IsActive = false;
        public TerrainDef Terrain = null;

        public CellInfo()
        {
        }
    }

    private ConcurrentDictionary<IntVec3, CellInfo> _allMapCells;
    public ConcurrentDictionary<IntVec3, CellInfo> AllMapCells =>
        _allMapCells ??= new ConcurrentDictionary<IntVec3, CellInfo>(
            Enumerable.Range(0, map.cellIndices.NumGridCells)
                .ToDictionary(x => map.cellIndices.IndexToCell(x), _ => new CellInfo()));

    public int TicksToUpdateGoo => Grey_GooMod.settings.MapGooUpdateFrequency;
    public int NextGooUpdateTick = Grey_GooMod.settings.MapGooUpdateFrequency;
    private Task CurrentGooUpdateTask;
    public ConcurrentQueue<Thing> ThingsToDamage = new ConcurrentQueue<Thing>();
    public ConcurrentQueue<IntVec3> CellsToChange = new ConcurrentQueue<IntVec3>();

    public int TicksToRecheckGoo => Grey_GooMod.settings.MapGooReevaluateFrequency;
    public int NextGooRecheckTick = Grey_GooMod.settings.MapGooReevaluateFrequency;
    private Task CurrentGooRecheckTask;

    public float ChanceToSpreadGoo => Grey_GooMod.settings.ChanceToSpreadGooToCell;
    public float ChanceToApplyDamage = Grey_GooMod.settings.ChanceForGooToDamagePercent;

    public GGWorldComponent ggWorldComponent => Find.World.GetComponent<GGWorldComponent>();

    public IntVec3 OriginPos => ggWorldComponent.GetDirection8WayToNearestController(map.Tile) switch
                {
                    Direction8Way.North => new IntVec3(map.Size.x / 2, 0, 0),
                    Direction8Way.NorthEast => new IntVec3(map.Size.x, 0, 0),
                    Direction8Way.East => new IntVec3(map.Size.x, 0, map.Size.z / 2),
                    Direction8Way.SouthEast => new IntVec3(map.Size.x, 0, map.Size.z),
                    Direction8Way.South => new IntVec3(map.Size.x / 2, 0, map.Size.z),
                    Direction8Way.SouthWest => new IntVec3(0, 0, map.Size.z),
                    Direction8Way.West => new IntVec3(0, 0, map.Size.z / 2),
                    Direction8Way.NorthWest => new IntVec3(0, 0, 0),
                    _ => IntVec3.Invalid
                };

    public FloatRange DamageRange = new FloatRange(0f,4f);

    public void UpdateGoo()
    {
        NextGooUpdateTick = TicksToUpdateGoo + Find.TickManager.TicksGame;

        Dictionary<IntVec3, CellInfo> ActiveCells = AllMapCells.Where(item => item.Value.IsActive).ToDictionary(item=>item.Key,item=>item.Value);
        Dictionary<IntVec3, CellInfo> GooedCells = AllMapCells.Where(item => item.Value.IsGooed).ToDictionary(item=>item.Key,item=>item.Value);

        // Primary update method for goo
        foreach ((IntVec3 cell, CellInfo cellInfo) in ActiveCells)
        {
            // Skip if there's a building here
            if (map.thingGrid.ThingsAt(cell).Any(t => t is Building)) return;

            // skip if protected
            if (ProtectedCells.Contains(cell)) return;

            // get neighbours that are valid and exclude tiles we already know about
            IEnumerable<IntVec3> neighboursAndSelf = GenAdj.AdjacentCellsAndInside.Select(d => cell + d).Where(c => c.InBounds(map)).Except(GooedCells.Keys).ToList();

            // all our neighbours are gooed, skip!
            if (!neighboursAndSelf.Any())
            {
                AllMapCells.TryUpdate(cell, cellInfo with { IsActive = false }, cellInfo);
                return;
            }

            // build a list of neighbours and things
            var neighboursWithThings = neighboursAndSelf
                .Select(c => new { Cell = c, ThingsAt = map.thingGrid.ThingsAt(c) }).ToList();

            // check neighbours can be gooed (e.g. no building on cell)
            // order by fertility to favour fertile tiles
            List<IntVec3> candidates = neighboursWithThings
                .Where(c=>!AllMapCells[c.Cell].IsGooed)
                .Where(ct => !ct.ThingsAt.Any(t => t is Building))
                .Select(ct => ct.Cell)
                .OrderByDescending(c=>map.fertilityGrid.FertilityAt(c))
                .ToList();

            // if gooed, add to GooedCells
            foreach (IntVec3 nCell in candidates)
            {
                // CellsToChange.Enqueue(nCell);
                float fertility = map.fertilityGrid.FertilityAt(nCell);

                float spreadChance = 0.01f + (Mathf.Max(0, (fertility - 0.5f)/100f) * ChanceToSpreadGoo); // can still spread on infertile terrain

                if(RandLocal.NextDouble() < spreadChance){
                    CellInfo info = AllMapCells[nCell];
                    map.terrainGrid.SetTerrain(nCell, Grey_GooDefOf.GG_Goo);
                    CellInfo newCI = new CellInfo { IsGooed = true, IsActive = true, Terrain = Grey_GooDefOf.GG_Goo };
                    AllMapCells.TryUpdate(nCell, newCI, info);

                }
            }

            // check things on self and all neighbours to apply damage to
            foreach (Thing thing in neighboursWithThings.SelectMany(ct => ct.ThingsAt))
            {
                ThingsToDamage.Enqueue(thing);
            }
        }

        // check all gooed cells for things to damage
        IEnumerable<Thing> gooedCellsThings = GooedCells.Select(kv=>kv.Key).Select(c => new { Cell = c, ThingsAt = map.thingGrid.ThingsAt(c) }).SelectMany(ct => ct.ThingsAt);

        foreach (Thing thing in gooedCellsThings)
        {
            ThingsToDamage.Enqueue(thing);
        }
    }

    public void GooRecheck()
    {
        // backup func to recheck over all goo at a slower rate
        float level = ggWorldComponent.GetTileGooLevelAt(map.Tile);

        if(!AllMapCells.Any(kv=>kv.Value.IsGooed) && !Mathf.Approximately(level, 0))
        {
            map.terrainGrid.SetTerrain(OriginPos, Grey_GooDefOf.GG_Goo);
            CellInfo info = AllMapCells[OriginPos];
            AllMapCells.TryUpdate(OriginPos, new CellInfo{IsGooed = true, IsActive = true}, info);
        }

        NextGooRecheckTick = TicksToRecheckGoo + Find.TickManager.TicksGame;

        foreach ((IntVec3 cell, CellInfo cellInfo) in AllMapCells)
        {
            CellInfo newCellInfo = new CellInfo();
            newCellInfo.Terrain = map.terrainGrid.TerrainAt(cell);

            //re-evaluate if tile is active
            // get neighbours
            IEnumerable<IntVec3> neighbours = GenAdj.AdjacentCellsAround.Select(d => cell + d);

            // check neighbours are valid
            neighbours = neighbours.Where(c => c.InBounds(map)).ToList();

            if (newCellInfo.Terrain == Grey_GooDefOf.GG_Goo)
            {
                newCellInfo.IsGooed = true;
                newCellInfo.IsActive = true;
            }

            // all our neighbours are gooed, remove from active!
            if (newCellInfo.Terrain == Grey_GooDefOf.GG_Goo &&
                neighbours.All(nCell => map.terrainGrid.TerrainAt(nCell) == Grey_GooDefOf.GG_Goo))
            {
                newCellInfo.IsActive = false;
            }
            // we should be active, add
            else if (newCellInfo.Terrain == Grey_GooDefOf.GG_Goo &&
                     neighbours.Any(nCell => map.terrainGrid.TerrainAt(nCell) != Grey_GooDefOf.GG_Goo) &&
                     !map.thingGrid.ThingsAt(cell).Any(t => t is Building)
            )
            {
                newCellInfo.IsActive = true;
            }

            // Ensure goo cell in protected tiles are "deactivated"
            if (newCellInfo.Terrain == Grey_GooDefOf.GG_Goo && ProtectedCellsSet.Contains(cell))
            {
                map.terrainGrid.SetTerrain(cell, Grey_GooDefOf.GG_Goo_Inactive);
                newCellInfo.Terrain = Grey_GooDefOf.GG_Goo_Inactive;
                newCellInfo.IsGooed = false;
                newCellInfo.IsActive = false;
            }
            // check if a tile is inactive goo, and not in protected tiles, and convert back to active goo
            else if (newCellInfo.Terrain == Grey_GooDefOf.GG_Goo_Inactive && !ProtectedCellsSet.Contains(cell))
            {
                map.terrainGrid.SetTerrain(cell, Grey_GooDefOf.GG_Goo);
                newCellInfo.Terrain = Grey_GooDefOf.GG_Goo;
                newCellInfo.IsGooed = true;
                newCellInfo.IsActive = true;
            }

            AllMapCells.TryUpdate(cell, newCellInfo, cellInfo);
        }
    }

    public override void MapComponentTick()
    {
        base.MapComponentTick();

        if ((CurrentGooUpdateTask is null || CurrentGooUpdateTask.IsCompleted) && Find.TickManager.TicksGame >= NextGooUpdateTick && ThingsToDamage.IsEmpty && CellsToChange.IsEmpty)
            CurrentGooUpdateTask = Task.Run(UpdateGoo);

        if ((CurrentGooRecheckTask is null || CurrentGooRecheckTask.IsCompleted) && Find.TickManager.TicksGame >= NextGooRecheckTick)
            CurrentGooRecheckTask = Task.Run(GooRecheck);

        for (int i = 0; i < Mathf.Max(1, Mathf.Min(10, ThingsToDamage.Count / 10)); i++)
        {
            if (ThingsToDamage.TryDequeue(out Thing thing) && thing != null)
            {
                if (thing is { Destroyed: false } && Random.value > (ChanceToApplyDamage/100) && thing is Pawn && Random.value < 0.1)
                {
                    ; // 10% chance to damage people
                    DamageInfo dinfo = new DamageInfo(
                        Grey_GooDefOf.GG_Goo_Burn,
                        Grey_GooMod.settings.GooDamageRange.RandomInRange,
                        1f);
                    thing.TakeDamage(dinfo);
                }
            }
            else
            {
                break;
            }
        }
    }

    public void NotifyCellsProtected(IMapCellProtector mapCellProtector)
    {
        List<IntVec3> newlyProtectedCells = mapCellProtector.CellsInRadius(map).ToList();
        ProtectedCells.AddRange(newlyProtectedCells);
        foreach (IntVec3 cell in newlyProtectedCells.Where(cell=>map.terrainGrid.TerrainAt(cell) == Grey_GooDefOf.GG_Goo))
        {
            CellInfo info = AllMapCells[cell];
            // Ensure goo cell in protected tiles are "deactivated"
            map.terrainGrid.SetTerrain(cell, Grey_GooDefOf.GG_Goo_Inactive);
            AllMapCells.TryUpdate(cell, new CellInfo{IsActive = false, IsGooed = false}, info);
        }
    }

    public void NotifyCellsUnprotected(IMapCellProtector mapCellProtector)
    {
        List<IntVec3> newlyUnprotectedCells = mapCellProtector.CellsInRadius(map).ToList();

        ProtectedCells.RemoveWhere(c=>newlyUnprotectedCells.Contains(c));

        // because it's a list, not a hashset, we can safely remove, as tiles covered by multiple protectors are dublicated, and we only remove once.
        foreach (IntVec3 cell in newlyUnprotectedCells.Where(cell=>map.terrainGrid.TerrainAt(cell) == Grey_GooDefOf.GG_Goo_Inactive))
        {
            CellInfo info = AllMapCells[cell];
            // check if a tile is unprotected goo, and not in protected tiles, and convert back to active goo
            map.terrainGrid.SetTerrain(cell, Grey_GooDefOf.GG_Goo);
            AllMapCells.TryUpdate(cell, new CellInfo{IsActive = true, IsGooed = true}, info);
        }

    }
}
