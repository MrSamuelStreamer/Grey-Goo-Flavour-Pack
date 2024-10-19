using System;
using System.Collections.Generic;
using System.Linq;
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
    public List<IntVec3> ProtectedTiles = new List<IntVec3>();

    public HashSet<IntVec3> ProtectedTilesSet => ProtectedTiles.ToHashSet();

    public GGWorldComponent ggWorldComponent => Find.World.GetComponent<GGWorldComponent>();

    public float mapGooLevel = 0f;

    public IntVec3 OriginPos => NearestControllerDirection switch
            {
                // TODO: Might have directions flipped, as getting incorrect origins
                Direction8Way.North => new IntVec3(map.Size.x/2,0 , 0),
                Direction8Way.NorthEast => new IntVec3(map.Size.x,0 , 0),
                Direction8Way.East => new IntVec3(map.Size.x,0 , map.Size.z/2),
                Direction8Way.SouthEast => new IntVec3(map.Size.x,0 , map.Size.z),
                Direction8Way.South => new IntVec3(map.Size.x/2,0 , map.Size.z),
                Direction8Way.SouthWest => new IntVec3(0,0 , map.Size.z),
                Direction8Way.West => new IntVec3(0,0 , map.Size.z/2),
                Direction8Way.NorthWest => new IntVec3(0,0 , 0),
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
            UpdateGoo();
        }
    }

    public FloatRange DamageRange = new FloatRange(0f,4f );

    public override void MapComponentTick()
    {
        base.MapComponentTick();

        MapGooLevel = ggWorldComponent.GetTileGooLevelAt(map.Tile);


        // If we miss it, we're fine
        if (Find.TickManager.TicksGame % Grey_GooMod.settings.GooDamageTickFrequency == 0 && _gooIndices is not null)
        {
            foreach (int gooIndex in _gooIndices)
            {
                List<Pawn> pawns = map.thingGrid.ThingsAt(map.cellIndices.IndexToCell(gooIndex)).OfType<Pawn>().InRandomOrder().ToList();
                if(pawns.Count == 0) continue;
                foreach (Pawn pawn in pawns.Take(Random.Range(1, pawns.Count)))
                {
                    DamageInfo dinfo = new DamageInfo(
                        Grey_GooDefOf.GG_Goo_Burn,
                        DamageRange.RandomInRange,
                        1f);
                    pawn.TakeDamage(dinfo);
                }
            }
        }
    }

    public List<IntVec3> _cellsOrderedByDistance;
    private List<int> _gooIndices;
    private List<IntVec3> _allMapCells;

    public List<IntVec3> cellsOrderedByDistance => _cellsOrderedByDistance ??= Enumerable.Range(0, map.cellIndices.NumGridCells).Select(idx => map.cellIndices.IndexToCell(idx))
        .Select(cell => (cell, cell.DistanceTo(OriginPos))).OrderBy(c => c.Item2).Select(t => t.Item1).ToList();
    public List<IntVec3> AllMapCells => _allMapCells ??= Enumerable.Range(0, map.cellIndices.NumGridCells).Select(idx => map.cellIndices.IndexToCell(idx)).ToList();
    public List<int> gooIndices => _gooIndices ??= Enumerable.Range(0, map.terrainGrid.topGrid.Length).Where(idx => map.terrainGrid.topGrid[idx] == Grey_GooDefOf.GG_Goo).ToList();

    public void UpdateGoo()
    {
        int expectedCoverage = Mathf.CeilToInt(map.terrainGrid.topGrid.Length * MapGooLevel);

        if (expectedCoverage > gooIndices.Count)
        {
            // add goo
            IEnumerable<IntVec3> cellsToTransform = cellsOrderedByDistance.Where(c => map.terrainGrid.TerrainAt(c) != Grey_GooDefOf.GG_Goo && !ProtectedTilesSet.Contains(c)).Take(expectedCoverage-gooIndices.Count);

            foreach (IntVec3 cell in cellsToTransform)
            {
                map.terrainGrid.SetTerrain(cell, Grey_GooDefOf.GG_Goo);
                gooIndices.Add(map.cellIndices.CellToIndex(cell));
            }
        }else if (expectedCoverage < gooIndices.Count)
        {
            // remove goo
            List<IntVec3> reversed = cellsOrderedByDistance;
            reversed.Reverse();

            IEnumerable<IntVec3> cellsToTransform = reversed.Where(c => map.terrainGrid.TerrainAt(c) == Grey_GooDefOf.GG_Goo).Take(gooIndices.Count-expectedCoverage);

            foreach (IntVec3 cell in cellsToTransform)
            {
                map.terrainGrid.SetTerrain(cell, Grey_GooDefOf.GG_Goo_Inactive);
                gooIndices.Remove(map.cellIndices.CellToIndex(cell));
            }
        }

        foreach (IntVec3 cell in AllMapCells)
        {
            if (map.terrainGrid.TerrainAt(cell) == Grey_GooDefOf.GG_Goo && ProtectedTiles.Contains(cell))
            {
                // Ensure goo cell in protected tiles are "deactivated"
                map.terrainGrid.SetTerrain(cell, Grey_GooDefOf.GG_Goo_Inactive);
            }
            else if (map.terrainGrid.TerrainAt(cell) == Grey_GooDefOf.GG_Goo_Inactive && !ProtectedTiles.Contains(cell))
            {
                // check if a tile is unprotected goo, and not in protected tiles, and convert back to active goo
                map.terrainGrid.SetTerrain(cell, Grey_GooDefOf.GG_Goo);
            }
        }
    }


    public void NotifyTilesProtected(IMapCellProtector mapCellProtector)
    {
        ProtectedTiles.AddRange(mapCellProtector.CellsInRadius(map));
        UpdateGoo();
    }

    public void NotifyTilesUnprotected(IMapCellProtector mapCellProtector)
    {
        // because it's a list, not a hashset, we can safely remove, as tiles covered by multiple protectors are dublicated, and we only remove once.
        foreach (IntVec3 tile in mapCellProtector.CellsInRadius(map))
        {
            ProtectedTiles.Remove(tile);
        }
        UpdateGoo();
    }
}
