using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

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
