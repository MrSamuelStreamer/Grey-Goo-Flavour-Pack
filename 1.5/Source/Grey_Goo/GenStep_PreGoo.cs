using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Grey_Goo;

public class GenStep_PreGoo : GenStep_ConditionCauser
{
    public override int SeedPart => 1413581224;

    public override void Generate(Map map, GenStepParams parms)
    {
        foreach (int idx in Enumerable.Range(0, map.terrainGrid.topGrid.Length).Where(t => Random.value > 0.25f))
        {
            map.terrainGrid.TryGooTerrain(map.cellIndices.IndexToCell(idx));
        }

        base.Generate(map, parms);
    }
}
