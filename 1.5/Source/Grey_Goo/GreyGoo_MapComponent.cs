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

    public HediffDef InfectionHediff = DefDatabase<HediffDef>.GetNamed("Taggerung_SCP_GeneMutation");

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
    public int NextGooUpdateTick = 600;
    private Task CurrentGooUpdateTask;
    public ConcurrentQueue<Thing> ThingsToDamage = new ConcurrentQueue<Thing>();
    public ConcurrentQueue<IntVec3> CellsToChange = new ConcurrentQueue<IntVec3>();

    public int TicksToRecheckGoo => Grey_GooMod.settings.MapGooReevaluateFrequency;
    public int NextGooRecheckTick = 300;
    private Task CurrentGooRecheckTask;

    public float ChanceToSpreadGoo => Grey_GooMod.settings.ChanceToSpreadGooToCell;
    public float ChanceToApplyDamage = Grey_GooMod.settings.ChanceForGooToDamagePercent;

    public GGWorldComponent ggWorldComponent => Find.World.GetComponent<GGWorldComponent>();

    public float CurrentGooCoverage => AllMapCells.Count(item => item.Value.IsGooed) / (float) map.cellIndices.NumGridCells;
    public float WorldMapSiteCoverage => ggWorldComponent.GetTileGooLevelAt(map.Tile);

    // If we're ahead of the world coverage, slow down, if we're behind, speed up
    public float WorldMapSiteCoverageMultiplier => CurrentGooCoverage > WorldMapSiteCoverage ? Mathf.Min(0.1f, WorldMapSiteCoverage) : WorldMapSiteCoverage * 10f;

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

    public FloatRange DamageRange = new FloatRange(0f, 4f);

    public IntRange MortarSpawnInterval => Grey_GooMod.settings.GooMortarSpawnTickRange;

    public int NextMortarSpawnTick = -1;

    public void UpdateGoo()
    {
        NextGooUpdateTick = TicksToUpdateGoo + Find.TickManager.TicksGame;

        Dictionary<IntVec3, CellInfo> ActiveCells = AllMapCells.Where(item => item.Value.IsActive).ToDictionary(item => item.Key, item => item.Value);
        Dictionary<IntVec3, CellInfo> GooedCells = AllMapCells.Where(item => item.Value.IsGooed).ToDictionary(item => item.Key, item => item.Value);

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
                .Where(c => !AllMapCells[c.Cell].IsGooed)
                .Where(ct => !ct.ThingsAt.Any(t => t is Building))
                .Select(ct => ct.Cell)
                .OrderByDescending(c => map.fertilityGrid.FertilityAt(c))
                .ToList();

            // if gooed, add to GooedCells
            foreach (IntVec3 nCell in candidates)
            {
                // CellsToChange.Enqueue(nCell);
                float fertility = map.fertilityGrid.FertilityAt(nCell);

                float spreadChance =
                    Mathf.Min(1f, 0.01f + Mathf.Max(0, (fertility - 0.5f) / 100f) * ChanceToSpreadGoo * WorldMapSiteCoverageMultiplier); // can still spread on infertile terrain

                if (RandLocal.NextDouble() < spreadChance * map.terrainGrid.ChanceToSpreadModifier(nCell))
                {
                    GooTileAt(nCell);
                }
            }

            // check things on self and all neighbours to apply damage to
            foreach (Thing thing in neighboursWithThings.SelectMany(ct => ct.ThingsAt).Where(t => t is not Building_GooController))
            {
                ThingsToDamage.Enqueue(thing);
            }
        }

        // check all gooed cells for things to damage
        IEnumerable<Thing> gooedCellsThings = GooedCells.Select(kv => kv.Key).Select(c => new { Cell = c, ThingsAt = map.thingGrid.ThingsAt(c) }).SelectMany(ct => ct.ThingsAt)
            .Where(t => t is not Building_GooController).Where(t=>t.def != Grey_GooDefOf.MSS_GG_Goo_Mortar);

        foreach (Thing thing in gooedCellsThings)
        {
            ThingsToDamage.Enqueue(thing);
        }
    }

    public void GooRecheck()
    {
        // backup func to recheck over all goo at a slower rate
        float level = ggWorldComponent.GetTileGooLevelAt(map.Tile);

        if (!AllMapCells.Any(kv => kv.Value.IsGooed) && !Mathf.Approximately(level, 0))
        {
            GooTileAt(OriginPos);
        }

        NextGooRecheckTick = TicksToRecheckGoo + Find.TickManager.TicksGame;

        foreach ((IntVec3 cell, CellInfo cellInfo) in AllMapCells)
        {
            CellInfo newCellInfo = new CellInfo { Terrain = map.terrainGrid.TerrainAt(cell) };

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
                map.terrainGrid.TryDeactivateGooTerrain(cell);
                newCellInfo.Terrain = Grey_GooDefOf.GG_Goo_Inactive;
                newCellInfo.IsGooed = false;
                newCellInfo.IsActive = false;
            }
            // check if a tile is inactive goo, and not in protected tiles, and convert back to active goo
            else if (newCellInfo.Terrain == Grey_GooDefOf.GG_Goo_Inactive && !ProtectedCellsSet.Contains(cell))
            {
                map.terrainGrid.TryGooTerrain(cell);
                newCellInfo.Terrain = Grey_GooDefOf.GG_Goo;
                newCellInfo.IsGooed = true;
                newCellInfo.IsActive = true;
            }

            AllMapCells.TryUpdate(cell, newCellInfo, cellInfo);
        }
    }

    public void TriggerGooRecheck()
    {
        NextGooRecheckTick = Find.TickManager.TicksGame;
    }

    public override void MapComponentTick()
    {
        base.MapComponentTick();
        if (!Grey_GooMod.settings.EnableGoo) return;

        if ((CurrentGooUpdateTask is null || CurrentGooUpdateTask.IsCompleted) && Find.TickManager.TicksGame >= NextGooUpdateTick && ThingsToDamage.IsEmpty &&
            CellsToChange.IsEmpty)
            CurrentGooUpdateTask = Task.Run(UpdateGoo);

        if ((CurrentGooRecheckTask is null || CurrentGooRecheckTask.IsCompleted) && Find.TickManager.TicksGame >= NextGooRecheckTick)
            CurrentGooRecheckTask = Task.Run(GooRecheck);

        for (int i = 0; i < Mathf.Max(1, Mathf.Min(10, ThingsToDamage.Count / 10)); i++)
        {
            if (ThingsToDamage.TryDequeue(out Thing thing) && thing != null)
            {
                if (Grey_GooMod.settings.InfectOnGooTouch && InfectionHediff != null && thing is { Destroyed: false } && thing is Pawn p)
                {
                    p.health.GetOrAddHediff(InfectionHediff);
                }

                if (thing is { Destroyed: false } && Random.value > (ChanceToApplyDamage/100) && thing is Pawn && Random.value < 0.1)
                {
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

        if (NextMortarSpawnTick < 0)
        {
            NextMortarSpawnTick = MortarSpawnInterval.RandomInRange + Find.TickManager.TicksGame;
        }

        // More chance to spawn a mortar as coverage increases
        if (NextMortarSpawnTick < Find.TickManager.TicksGame && Rand.Chance(CurrentGooCoverage))
        {
            IntVec3 cell = AllMapCells.Where(c => c.Value.IsGooed).Select(c => c.Key).RandomElement();
            foreach (Thing thing in map.thingGrid.ThingsAt(cell))
            {
                thing.Destroy();
            }

            Thing mortar = ThingMaker.MakeThing(Grey_GooDefOf.MSS_GG_Goo_Mortar);
            mortar.SetFactionDirect(Find.FactionManager.FirstFactionOfDef(Grey_GooDefOf.GG_GreyGoo));
            GenSpawn.Spawn(mortar, cell, map);
            NextMortarSpawnTick = MortarSpawnInterval.RandomInRange + Find.TickManager.TicksGame;
        }

    }

    public void GooTileAt(IntVec3 cell)
    {
        CellInfo info = AllMapCells[cell];
        map.terrainGrid.TryGooTerrain(cell);
        CellInfo newCI = new CellInfo { IsGooed = true, IsActive = true, Terrain = Grey_GooDefOf.GG_Goo };
        AllMapCells.TryUpdate(cell, newCI, info);
    }

    public void NotifyCellsProtected(IMapCellProtector mapCellProtector)
    {
        List<IntVec3> newlyProtectedCells = mapCellProtector.CellsInRadius(map).ToList();
        ProtectedCells.AddRange(newlyProtectedCells);
        GenThreading.ParallelForEach(newlyProtectedCells.Where(cell => map.terrainGrid.TerrainAt(cell) == Grey_GooDefOf.GG_Goo).ToList(), (cell) =>
        {
            CellInfo info = AllMapCells[cell];
            // Ensure goo cell in protected tiles are "deactivated"
            map.terrainGrid.TryDeactivateGooTerrain(cell);
            AllMapCells.TryUpdate(cell, new CellInfo { IsActive = false, IsGooed = false }, info);
        });
    }

    public void NotifyCellsUnprotected(IMapCellProtector mapCellProtector)
    {
        List<IntVec3> newlyUnprotectedCells = mapCellProtector.CellsInRadius(map).ToList();

        ProtectedCells.RemoveWhere(c => newlyUnprotectedCells.Contains(c));

        GenThreading.ParallelForEach(newlyUnprotectedCells.Where(cell => map.terrainGrid.TerrainAt(cell) == Grey_GooDefOf.GG_Goo_Inactive).ToList(), GooTileAt);
    }

    public override void MapGenerated()
    {
        foreach (IntVec3 cell in map.AllCells.InRandomOrder().Take(Mathf.CeilToInt(map.AllCells.Count() * WorldMapSiteCoverage)))
        {
            GooTileAt(cell);
        }
    }
}
