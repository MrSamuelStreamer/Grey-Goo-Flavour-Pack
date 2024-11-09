using Grey_Goo.Buildings.Comps;
using HarmonyLib;
using Verse;

namespace Grey_Goo.HarmonyPatches;

[HarmonyPatch(typeof(ThingListGroupHelper))]
public static class ThingListGroupHelper_Patch
{
    [HarmonyPatch(nameof(ThingListGroupHelper.Includes))]
    [HarmonyPostfix]
    public static void Includes_Patch(ThingRequestGroup group, ThingDef def, ref bool __result)
    {
        if (group == ThingRequestGroup.ProjectileInterceptor && def.HasComp(typeof(CompGooShield))) __result = true;
    }
}
