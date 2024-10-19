using System.Collections.Generic;
using System.Linq;
using Grey_Goo.Buildings;
using RimWorld;
using UnityEngine;
using Verse;

namespace Grey_Goo;

public class GreyGoo_MapComponent(Map map) : MapComponent(map)
{
    public Direction8Way NearestControllerDirection = Direction8Way.Invalid;

    // keep this a list and allow duplicates to make it easier to handle removing overlapping protected tiles.
    public List<IntVec3> ProtectedCells = new List<IntVec3>();
    public HashSet<IntVec3> ProtectedCellsSet => ProtectedCells.ToHashSet();
    public int TicksToReprocessCells = 600;

    public int TicksToUpdateGoo = 60;

    public GGWorldComponent ggWorldComponent => Find.World.GetComponent<GGWorldComponent>();

    public float mapGooLevel = 0f;

    public IntVec3 _OriginPos = IntVec3.Invalid;

    public IntVec3 OriginPos
    {
        get
        {
            if (_OriginPos == IntVec3.Invalid)
            {
                _OriginPos = NearestControllerDirection switch
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
            }

            return _OriginPos;
        }
    }

    public float MapGooLevel
    {
        get => mapGooLevel;
        set
        {
            if(Mathf.Approximately(mapGooLevel, value)) return;
            NearestControllerDirection = ggWorldComponent.GetDirection8WayToNearestController(map.Tile);
            mapGooLevel = value;
        }
    }

    public FloatRange DamageRange = new FloatRange(0f,4f);

    public override void MapComponentTick()
    {
        base.MapComponentTick();

        MapGooLevel = ggWorldComponent.GetTileGooLevelAt(map.Tile);

        // If we miss it, we're fine
        if (Find.TickManager.TicksGame % 60 == 0)
        {
            UpdateGoo();
        }

        // If we miss it, we're fine
        if (Find.TickManager.TicksGame % Grey_GooMod.settings.GooDamageTickFrequency == 0 && AllGooCells is not null)
        {

            List<Thing> toDamage = new List<Thing>();

            GenThreading.ParallelForEach(AllGooCells.ToList(), (cell) =>
            {
                List<Thing> things = map.thingGrid.ThingsAt(cell).InRandomOrder().ToList();
                // randomly damage between 1/4 and all things
                toDamage.AddRange(things);
            });

            foreach (Thing thing in toDamage) //.Take(Random.Range(Mathf.CeilToInt(things.Count / 4f),things.Count)))
            {
                if (thing is null) continue;
                DamageInfo dinfo = new DamageInfo(
                    Grey_GooDefOf.GG_Goo_Burn,
                    DamageRange.RandomInRange,
                    1f);
                thing.TakeDamage(dinfo);
            }
        }

        // spread the update over `TicksToReprocessCells` ticks
        int chunkIdx = Find.TickManager.TicksGame % TicksToReprocessCells;

        List<IntVec3> CellsToCheck = [];
        for (
            int idx = chunkIdx;
            idx < map.cellIndices.NumGridCells - TicksToReprocessCells;
            idx += TicksToReprocessCells
        )
        {
            CellsToCheck.Add(map.cellIndices.IndexToCell(idx));
        }

        GenThreading.ParallelForEach(CellsToCheck, (cell) =>
        {
            if (map.terrainGrid.TerrainAt(cell) == Grey_GooDefOf.GG_Goo && ProtectedCellsSet.Contains(cell))
            {
                // Ensure goo cell in protected tiles are "deactivated"
                map.terrainGrid.SetTerrain(cell, Grey_GooDefOf.GG_Goo_Inactive);
            }
            else if (map.terrainGrid.TerrainAt(cell) == Grey_GooDefOf.GG_Goo_Inactive && !ProtectedCellsSet.Contains(cell))
            {
                // check if a tile is inactive goo, and not in protected tiles, and convert back to active goo
                map.terrainGrid.SetTerrain(cell, Grey_GooDefOf.GG_Goo);
            }
        });
    }

    public List<IntVec3> _cellsOrderedByDistance;
    private List<IntVec3> _allMapCells;
    private HashSet<IntVec3> _allGooCells;

    public List<IntVec3> cellsOrderedByDistance => _cellsOrderedByDistance ??= Enumerable.Range(0, map.cellIndices.NumGridCells).Select(idx => map.cellIndices.IndexToCell(idx))
        .Select(cell => (cell, cell.DistanceTo(OriginPos))).OrderBy(c => c.Item2).Select(t => t.Item1).ToList();
    public List<IntVec3> AllMapCells => _allMapCells ??= Enumerable.Range(0, map.cellIndices.NumGridCells).Select(idx => map.cellIndices.IndexToCell(idx)).ToList();
    public HashSet<IntVec3> AllGooCells => _allGooCells ??= AllMapCells.Where(cell => map.terrainGrid.TerrainAt(cell) == Grey_GooDefOf.GG_Goo).ToHashSet();


    public void UpdateGoo()
    {
        int expectedCoverage = Mathf.CeilToInt(map.terrainGrid.topGrid.Length * MapGooLevel);

        if (expectedCoverage > AllGooCells.Count)
        {
            // add goo
            int cellsToTake = Mathf.Min(expectedCoverage - AllGooCells.Count, cellsOrderedByDistance.Count - AllGooCells.Count);

            // Only update some of the cells this tick
            if (cellsToTake > 1)
            {
                cellsToTake = Mathf.CeilToInt(cellsToTake / (float)TicksToUpdateGoo);
            }

            List<IntVec3> cellsToGoo = cellsOrderedByDistance.Except(AllGooCells).Except(ProtectedCellsSet).Take(cellsToTake).ToList();

            foreach (IntVec3 cell in cellsToGoo)
            {
                map.terrainGrid.SetTerrain(cell, Grey_GooDefOf.GG_Goo);
            }

            AllGooCells.AddRange(cellsToGoo);
        }
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

        AllGooCells.RemoveWhere(c=>newlyProtectedCells.Contains(c));
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
            AllGooCells.Add(cell);
        }

    }
}
