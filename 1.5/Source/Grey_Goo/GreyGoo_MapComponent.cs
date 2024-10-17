using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Grey_Goo;

public class GreyGoo_MapComponent(Map map) : MapComponent(map)
{
    public Direction8Way NearestControllerDirection = Direction8Way.Invalid;

    public GGWorldComponent ggWorldComponent => Find.World.GetComponent<GGWorldComponent>();

    public float mapGooLevel = 0f;

    public IntVec3 OriginPos => NearestControllerDirection switch
            {
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
            NearestControllerDirection = ggWorldComponent.GetDirection8WayToNearestController(map.Tile);
            mapGooLevel = value;
            UpdateGoo();
        }
    }

    public override void MapComponentTick()
    {
        base.MapComponentTick();

        MapGooLevel = ggWorldComponent.GetTileGooLevelAt(map.Tile);
    }

    public List<IntVec3> _cellsOrderedByDistance;
    private List<int> _gooIndices;

    public List<IntVec3> cellsOrderedByDistance => _cellsOrderedByDistance ??= Enumerable.Range(0, map.cellIndices.NumGridCells).Select(idx => map.cellIndices.IndexToCell(idx))
        .Select(cell => (cell, cell.DistanceTo(OriginPos))).OrderBy(c => c.Item2).Select(t => t.Item1).ToList();

    public List<int> gooIndices => _gooIndices ?? (_gooIndices = Enumerable.Range(0, map.terrainGrid.topGrid.Length).Where(idx => map.terrainGrid.topGrid[idx] == Grey_GooDefOf.GG_Goo).ToList());

    public void UpdateGoo()
    {
        int expectedCoverage = Mathf.CeilToInt(map.terrainGrid.topGrid.Length * MapGooLevel);

        if (expectedCoverage > gooIndices.Count)
        {
            IEnumerable<IntVec3> cellsToTransform = cellsOrderedByDistance.Where(c => map.terrainGrid.TerrainAt(c) != Grey_GooDefOf.GG_Goo).Take(expectedCoverage-gooIndices.Count);

            foreach (IntVec3 cell in cellsToTransform)
            {
                map.terrainGrid.SetTerrain(cell, Grey_GooDefOf.GG_Goo);
                gooIndices.Add(map.cellIndices.CellToIndex(cell));
            }
        }else if (expectedCoverage < gooIndices.Count)
        {
            //TODO: remove goo
        }
    }
}
