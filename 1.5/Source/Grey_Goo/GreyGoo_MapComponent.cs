using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grey_Goo.Buildings;
using RimWorld;
using UnityEngine;
using Verse;
using Random = UnityEngine.Random;

namespace Grey_Goo;

public class GreyGoo_MapComponent(Map map) : MapComponent(map)
{
    public Direction8Way NearestControllerDirection = Direction8Way.Invalid;

    // keep this a list and allow duplicates to make it easier to handle removing overlapping protected tiles.
    public List<IntVec3> ProtectedCells = new List<IntVec3>();
    public HashSet<IntVec3> ProtectedCellsSet => ProtectedCells.ToHashSet();

    public HashSet<IntVec3> GooedTiles = new HashSet<IntVec3>();
    public HashSet<IntVec3> ActiveGooTiles = new HashSet<IntVec3>();

    private List<IntVec3> _allMapCells;
    public List<IntVec3> AllMapCells => _allMapCells ??= Enumerable.Range(0, map.cellIndices.NumGridCells).Select(idx => map.cellIndices.IndexToCell(idx)).ToList();

    public int TicksToReprocessCells = 600;
    public int NextGooTick = TicksToReprocessCells;
    public int TicksToUpdateGoo => Grey_GooMod.settings.GooDamageTickFrequency;

    public float ChanceToSpreadGoo => Grey_GooMod.settings.ChanceToSpreadGooToCell;
    public float ChanceToApplyDamage = Grey_GooMod.settings.ChanceForGooToDamage;

    private ConcurrentQueue<Thing> thingsToDamage = new ConcurrentQueue<Thing>();
    private Task CurrentGooCycleTask;

    public GGWorldComponent ggWorldComponent => Find.World.GetComponent<GGWorldComponent>();

    public float mapGooLevel = 0f;

    public IntVec3 OriginPos => NearestControllerDirection switch
                {
                    // TODO: Might have directions flipped, as getting incorrect origins
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

    public float MapGooLevel
    {
        get => mapGooLevel;
        set
        {
            if(Mathf.Approximately(mapGooLevel, value)) return;
            NearestControllerDirection = ggWorldComponent.GetDirection8WayToNearestController(map.Tile);
            mapGooLevel = value;
            if (!GooedTiles.Any() && !Mathf.Approximately(mapGooLevel, 0))
            {
                map.terrainGrid.SetTerrain(OriginPos, Grey_GooDefOf.GG_Goo);
                GooedTiles.Add(OriginPos);
                ActiveGooTiles.Add(OriginPos);
            }
        }
    }

    public FloatRange DamageRange = new FloatRange(0f,4f);

    public void DoGooCycle()
    {
        IEnumerable<IntVec3> tilesToCheck = ActiveGooTiles.Where(c => c.InBounds(map)).InRandomOrder().ToList();
        List<IntVec3> tilesToDeactivate = new List<IntVec3>();
        List<IntVec3> tilesToActivate = new List<IntVec3>();

        foreach (IntVec3 cell in tilesToCheck)
        {
            // Skip if there's a building here
            if(map.thingGrid.ThingsAt(cell).Any(t=>t is Building)) return;

            // skip if protected
            if(ProtectedCells.Contains(cell)) return;

            // get neighbours that are valid and exclude tiles we already know about
            IEnumerable<IntVec3> neighboursAndSelf = GenAdj.CardinalDirectionsAndInside.Select(d => cell + d).Where(c => c.InBounds(map)).Except(tilesToCheck).ToList();

            // all our neighbours are gooed, skip!
            if (!neighboursAndSelf.Any())
            {
                tilesToDeactivate.Add(cell);
                continue;
            }

            // build a list of neighbours and things
            var neighboursWithThings = neighboursAndSelf.Select(c => new { Cell = c, ThingsAt = map.thingGrid.ThingsAt(c) }).ToList();

            // check neighbours can be gooed (e.g. no building on cell)
            List<IntVec3> candidates = neighboursWithThings.Where(ct => !ct.ThingsAt.Any(t => t is not Building)).Select(ct => ct.Cell).ToList();
            foreach (IntVec3 nCell in candidates.InRandomOrder().Take(Mathf.Max(1, Mathf.CeilToInt(candidates.Count * ChanceToSpreadGoo))))
            {
                // if gooed, add to GooedTiles
                map.terrainGrid.SetTerrain(nCell, Grey_GooDefOf.GG_Goo);
                tilesToActivate.Add(nCell);
            }

            // check things on self and all neighbours to apply damage to
            foreach (Thing thing in neighboursWithThings.SelectMany(ct => ct.ThingsAt))
                thingsToDamage.Enqueue(thing);
        }

        GooedTiles = GooedTiles.Except(tilesToDeactivate).Union(tilesToActivate).ToHashSet();
    }

    public override void MapComponentTick()
    {
        base.MapComponentTick();

        MapGooLevel = ggWorldComponent.GetTileGooLevelAt(map.Tile);

        if ((CurrentGooCycleTask is null || CurrentGooCycleTask.IsCompleted) && NextGooTick <= Find.TickManager.TicksGame)
            CurrentGooCycleTask = Task.Run(DoGooCycle);

        // apply damage - outside a thread, as some things do main-thread only in `TakeDamage`
        foreach (Thing thing in thingsToDamage.Where(_ => Random.value < ChanceToApplyDamage))
        {
            if (thing is null) return; //shouldn't be needed, but got occasional null ref
            DamageInfo dinfo = new DamageInfo(
                Grey_GooDefOf.GG_Goo_Burn,
                DamageRange.RandomInRange,
                1f);
            thing.TakeDamage(dinfo);
        }

        // recheck cells
        // spread the update over `TicksToReprocessCells` ticks
        int tilesToProcess = Mathf.CeilToInt(map.cellIndices.NumGridCells / (float)TicksToReprocessCells);

        GenThreading.ParallelForEach(AllMapCells.InRandomOrder().Take(tilesToProcess).ToList(), (cell) =>
        {
            //re-evaluate if tile is active
            // get neighbours
            IEnumerable<IntVec3> neighbours = GenAdj.AdjacentCellsAround.Select(d => cell + d);

            // check neighbours are valid
            neighbours = neighbours.Where(c => c.InBounds(map)).ToList();

            // all our neighbours are gooed, remove from active!
            if (neighbours.All(nCell => map.terrainGrid.TerrainAt(nCell) != Grey_GooDefOf.GG_Goo) && ActiveGooTiles.Contains(cell))
            {
                ActiveGooTiles.Remove(cell);
            }
            // we should be active, add
            else if (neighbours.Any(nCell => map.terrainGrid.TerrainAt(nCell) == Grey_GooDefOf.GG_Goo) && !ActiveGooTiles.Contains(cell))
            {
                ActiveGooTiles.Add(cell);
            }

            // Ensure goo cell in protected tiles are "deactivated"
            if (map.terrainGrid.TerrainAt(cell) == Grey_GooDefOf.GG_Goo && ProtectedCellsSet.Contains(cell))
            {
                map.terrainGrid.SetTerrain(cell, Grey_GooDefOf.GG_Goo_Inactive);
            }
            // check if a tile is inactive goo, and not in protected tiles, and convert back to active goo
            else if (map.terrainGrid.TerrainAt(cell) == Grey_GooDefOf.GG_Goo_Inactive && !ProtectedCellsSet.Contains(cell))
            {
                map.terrainGrid.SetTerrain(cell, Grey_GooDefOf.GG_Goo);
            }
        });
    }

    public void NotifyTilesProtected(IMapCellProtector mapCellProtector)
    {
        List<IntVec3> newlyProtectedCells = mapCellProtector.CellsInRadius(map).ToList();
        ProtectedCells.AddRange(newlyProtectedCells);
        foreach (IntVec3 cell in newlyProtectedCells.Where(cell=>map.terrainGrid.TerrainAt(cell) == Grey_GooDefOf.GG_Goo))
        {
            // Ensure goo cell in protected tiles are "deactivated"
            map.terrainGrid.SetTerrain(cell, Grey_GooDefOf.GG_Goo_Inactive);
        }

        GooedTiles.RemoveWhere(c=>newlyProtectedCells.Contains(c));
    }

    public void NotifyTilesUnprotected(IMapCellProtector mapCellProtector)
    {
        List<IntVec3> newlyUnprotectedCells = mapCellProtector.CellsInRadius(map).ToList();

        ProtectedCells.RemoveWhere(c=>newlyUnprotectedCells.Contains(c));

        // because it's a list, not a hashset, we can safely remove, as tiles covered by multiple protectors are dublicated, and we only remove once.
        foreach (IntVec3 cell in newlyUnprotectedCells.Where(cell=>map.terrainGrid.TerrainAt(cell) == Grey_GooDefOf.GG_Goo_Inactive))
        {
            // check if a tile is unprotected goo, and not in protected tiles, and convert back to active goo
            map.terrainGrid.SetTerrain(cell, Grey_GooDefOf.GG_Goo);
            GooedTiles.Add(cell);
        }

    }
}
