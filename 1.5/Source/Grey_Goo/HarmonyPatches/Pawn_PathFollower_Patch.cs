using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Grey_Goo.HarmonyPatches;

[HarmonyPatch(typeof(Pawn_PathFollower))]
public static class Pawn_PathFollower_Patch
{
    public static bool CellIsWater = false;
    public static List<TerrainDef> waterTerrain => Grey_GooDefOf.GG_Goo.reduceChanceOfPlacingOnTerrain;

    [HarmonyPatch("CostToMoveIntoCell")]
    [HarmonyPrefix]
    public static void CostToMoveIntoCell_Prefix(Pawn_PathFollower __instance, Pawn pawn, IntVec3 c, ref float __result)
    {
        CellIsWater = false;
        if (pawn?.mutant == null || pawn.mutant.Def != MutantDefOf.Shambler)
        {
            return;
        }

        if (!waterTerrain.Contains(pawn.Map.terrainGrid.TerrainAt(c)))
        {
            return;
        }

        CellIsWater = true;
    }

    [HarmonyPatch("CostToMoveIntoCell")]
    [HarmonyPostfix]
    public static void CostToMoveIntoCell_Postfix(ref float __result)
    {
        if (CellIsWater)
        {
            __result = Mathf.Min(__result * 40f, 800);
        }

        CellIsWater = false;
    }
}
