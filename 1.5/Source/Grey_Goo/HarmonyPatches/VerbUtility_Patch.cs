using HarmonyLib;
using Verse;

namespace Grey_Goo.HarmonyPatches;

[HarmonyPatch(typeof(VerbUtility))]
public static class VerbUtility_Patch
{
    [HarmonyPatch(nameof(VerbUtility.IsEMP))]
    [HarmonyPostfix]
    public static void IsEMP(Verb verb, ref bool __result)
    {
        if (verb.caster.def == Grey_GooDefOf.MSS_GG_Turret_EMPMiniTurret)
        {
            __result = false;
        }
    }
}
