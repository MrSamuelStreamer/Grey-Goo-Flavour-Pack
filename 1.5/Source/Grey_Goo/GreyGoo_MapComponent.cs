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
    private ConcurrentDictionary<IntVec3, CellInfo> _allMapCells;
    public ConcurrentQueue<IntVec3> CellsToChange = new ConcurrentQueue<IntVec3>();
    public float ChanceToApplyDamage = Grey_GooMod.settings.ChanceForGooToDamagePercent;
    private Task CurrentGooRecheckTask;
    private Task CurrentGooUpdateTask;

    public FloatRange DamageRange = new FloatRange(0f, 4f);

    public HediffDef InfectionHediff = DefDatabase<HediffDef>.GetNamed("Taggerung_SCP_GeneMutation");
    public int NextGooRecheckTick = 300;
    public int NextGooUpdateTick = 600;

    public int NextMortarSpawnTick = -1;

    // keep this a list and allow duplicates to make it easier to handle removing overlapping protected tiles.
    public List<IntVec3> ProtectedCells = new List<IntVec3>();

    public NetRandom RandLocal = new NetRandom();
    public ConcurrentQueue<Thing> ThingsToDamage = new ConcurrentQueue<Thing>();
    public HashSet<IntVec3> ProtectedCellsSet => ProtectedCells.ToHashSet();


    public static bool GooBoosted => Find.Maps.Any(m => m.GameConditionManager.ConditionIsActive(Grey_GooDefOf.MSS_GG_GooBoosted)) ||
                                     Find.World.GameConditionManager.ConditionIsActive(Grey_GooDefOf.MSS_GG_GooBoosted);

    public ConcurrentDictionary<IntVec3, CellInfo> AllMapCells =>
        _allMapCells ??= new ConcurrentDictionary<IntVec3, CellInfo>(
            Enumerable.Range(0, map.cellIndices.NumGridCells)
                .ToDictionary(x => map.cellIndices.IndexToCell(x), _ => new CellInfo()));

    public static int TicksToUpdateGoo => Grey_GooMod.settings.MapGooUpdateFrequency;

    public static int TicksToRecheckGoo => Grey_GooMod.settings.MapGooReevaluateFrequency;

    public static float ChanceToSpreadGoo => Grey_GooMod.settings.ChanceToSpreadGooToCell;

    public static GGWorldComponent ggWorldComponent => Find.World.GetComponent<GGWorldComponent>();

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

    public static IntRange MortarSpawnInterval => Grey_GooMod.settings.GooMortarSpawnTickRange;

    public void UpdateGoo()
    {
        NextGooUpdateTick = TicksToUpdateGoo + Find.TickManager.TicksGame;

        Dictionary<IntVec3, CellInfo> ActiveCells = AllMapCells.Where(item => item.Value.IsActive).ToDictionary(item => item.Key, item => item.Value);
        Dictionary<IntVec3, CellInfo> GooedCells = AllMapCells.Where(item => item.Value.IsGooed).ToDictionary(item => item.Key, item => item.Value);

        // Primary update method for goo
        foreach ((IntVec3 cell, CellInfo cellInfo) in ActiveCells)
        {
            // Skip if there's a building here
            if (map.thingGrid.ThingsAt(cell).Any(t => t is Building)) continue;

            // skip if protected
            if (ProtectedCells.Contains(cell)) continue;

            // get neighbours that are valid and exclude tiles we already know about
            IEnumerable<IntVec3> neighboursAndSelf = GenAdj.AdjacentCellsAndInside.Select(d => cell + d).Where(c => c.InBounds(map)).Except(GooedCells.Keys).ToList();

            // all our neighbours are gooed, skip!
            if (!neighboursAndSelf.Any())
            {
                AllMapCells.TryUpdate(cell, cellInfo with { IsActive = false }, cellInfo);
                continue;
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
                float fertility = map.fertilityGrid.FertilityAt(nCell);

                float spreadChance =
                    Mathf.Min(1f, 0.01f + Mathf.Max(0, (fertility - 0.5f) / 100f) * ChanceToSpreadGoo * WorldMapSiteCoverageMultiplier); // can still spread on infertile terrain

                if (GooBoosted)
                {
                    spreadChance *= 10;
                }

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
            .Where(t => t is not Building_GooController).Where(t => t.def != Grey_GooDefOf.MSS_GG_Goo_Mortar).Where(t => t.def != Grey_GooDefOf.MSS_GG_ArchotechPowerNode);

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

    public static bool PawnWearingGooProofApparel(Thing thing)
    {
        if (thing is not Pawn pawn) return false;
        return pawn.apparel?.WornApparel != null && pawn.apparel.WornApparel.Any(a => a.def == Grey_GooDefOf.MSS_GG_GooWaders);
    }

    public override void MapComponentTick()
    {
        base.MapComponentTick();
        if (!Grey_GooMod.settings.EnableGoo) return;

        StartGooUpdateIfNeeded();
        StartGooRecheckIfNeeded();
        ProcessThingsToDamage();
        ScheduleMortarSpawnIfNeeded();
        TrySpawnMortar();
    }

    public void StartGooUpdateIfNeeded()
    {
        if ((CurrentGooUpdateTask is null || CurrentGooUpdateTask.IsCompleted) && Find.TickManager.TicksGame >= NextGooUpdateTick && ThingsToDamage.IsEmpty &&
            CellsToChange.IsEmpty)
            CurrentGooUpdateTask = Task.Run(UpdateGoo);
    }

    public void StartGooRecheckIfNeeded()
    {
        if ((CurrentGooRecheckTask is null || CurrentGooRecheckTask.IsCompleted) && Find.TickManager.TicksGame >= NextGooRecheckTick)
            CurrentGooRecheckTask = Task.Run(GooRecheck);
    }

    public void ProcessThingsToDamage()
    {
        const int minIterations = 1;
        const int maxIterations = 10;

        for (int i = 0; i < Mathf.Max(minIterations, Mathf.Min(maxIterations, ThingsToDamage.Count / 10)); i++)
        {
            if (ThingsToDamage.TryDequeue(out Thing thing) && thing != null && thing is { Destroyed: false })
            {
                HandleThingDamage(thing);
            }
            else
            {
                break;
            }
        }
    }

    public void HandleThingDamage(Thing thing)
    {
        if (!PawnWearingGooProofApparel(thing))
        {
            ApplyGooEffects(thing);
        }
        else if (thing is Pawn pawn)
        {
            DamageGooProofApparel(pawn);
        }
    }

    public void ApplyGooEffects(Thing thing)
    {
        if (Grey_GooMod.settings.InfectOnGooTouch && InfectionHediff != null && thing is Pawn pawn)
        {
            pawn.health.GetOrAddHediff(InfectionHediff);
        }

        if (Random.value > ChanceToApplyDamage / 100)
        {
            DamageInfo dinfo = new DamageInfo(
                Grey_GooDefOf.GG_Goo_Burn,
                Grey_GooMod.settings.GooDamageRange.RandomInRange,
                1f);
            thing.TakeDamage(dinfo);
        }
    }

    public static void DamageGooProofApparel(Pawn pawn)
    {
        foreach (Apparel apparel in pawn.apparel.WornApparel.Where(a => a.def == Grey_GooDefOf.MSS_GG_GooWaders))
        {
            DamageInfo dinfo = new DamageInfo(
                Grey_GooDefOf.GG_Goo_Burn,
                2f,
                1f);
            DamageWorker.DamageResult res = apparel.TakeDamage(dinfo);
            ModLog.Log(res.ToString());
        }
    }

    public void ScheduleMortarSpawnIfNeeded()
    {
        if (NextMortarSpawnTick < 0)
        {
            NextMortarSpawnTick = MortarSpawnInterval.RandomInRange + Find.TickManager.TicksGame;
        }
    }

    public void TrySpawnMortar()
    {
        if (NextMortarSpawnTick < Find.TickManager.TicksGame)
        {
            if (Rand.Chance(Mathf.Max(CurrentGooCoverage, 0.15f)))
            {
                IntVec3 cell = AllMapCells.Where(c => c.Value.IsGooed).Select(c => c.Key).RandomElement();
                foreach (Thing thing in map.thingGrid.ThingsAt(cell))
                {
                    thing.Destroy();
                }

                Thing mortar = ThingMaker.MakeThing(Grey_GooDefOf.MSS_GG_Goo_Mortar);
                mortar.SetFactionDirect(Find.FactionManager.FirstFactionOfDef(Grey_GooDefOf.GG_GreyGoo));
                GenSpawn.Spawn(mortar, cell, map);
            }

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

    public record struct CellInfo
    {
        public bool IsActive = false;
        public bool IsGooed = false;
        public TerrainDef Terrain = null;

        public CellInfo()
        {
        }
    }
}
