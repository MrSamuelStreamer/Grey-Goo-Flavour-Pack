using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace Thirst_Flavour_Pack.HarmonyPatches;

[HarmonyPatch(typeof(MutantUtility), nameof(MutantUtility.GetShamblerColor))]
public class ShamblerColorPatch
{
    [HarmonyPostfix]
    public static Color GetShamblerColorPostfix(Color __result)
    {
        Color.RGBToHSV(__result, out float H, out float S, out float V);

        float BaseHue = 227.0f / 360.0f;

        H = Rand.RangeSeeded(BaseHue - 0.2f, BaseHue + 0.2f, __result.GetHashCode());
        S = Rand.RangeSeeded(0.1f, 0.25f, __result.GetHashCode());
        V = Rand.RangeSeeded(0.2f, 0.4f, __result.GetHashCode());
        return Color.HSVToRGB(H, S, V);
    }
}

[HarmonyPatch(typeof(Pawn), nameof(Pawn.ButcherProducts))]
public class ButcherProductsPatch
{
    [HarmonyPrefix]
    public static void ButcherProducts(Pawn butcher, float efficiency, Pawn __instance)
    {
        if (__instance.IsShambler)
        {
            __instance.RaceProps.meatDef = ThingDef.Named("Meat_Twisted");
        }
    }
}
