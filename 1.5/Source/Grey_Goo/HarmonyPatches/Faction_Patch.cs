using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Grey_Goo.HarmonyPatches;

[HarmonyPatch(typeof(Faction))]
public static class Faction_Patch
{
    public static bool HasAlliedWithScarab => Find.World.GetComponent<GGWorldComponent>()?.HasAlliedWithScarab ?? false;

    [HarmonyPatch(nameof(Faction.Notify_RelationKindChanged))]
    [HarmonyPostfix]
    public static void Faction_Notify_RelationKindChanged(Faction __instance, Faction other)
    {
        if (other.IsPlayer)
        {
            return;
        }

        if (!__instance.IsPlayer || HasAlliedWithScarab || other.def != FactionDefOf.Empire)
        {
            return;
        }

        FactionRelationKind newKind = __instance.RelationKindWith(other);

        if (newKind != FactionRelationKind.Ally)
        {
            return;
        }

        List<Thing> thingsToGive = new();

        thingsToGive.Add(ThingMaker.MakeThing(Grey_GooDefOf.MSS_Goo_Scarab_Database).MakeMinified());

        Thing plasteel = ThingMaker.MakeThing(ThingDefOf.Plasteel);
        plasteel.stackCount = 1000;
        thingsToGive.Add(plasteel);

        Thing steel = ThingMaker.MakeThing(ThingDefOf.Steel);
        steel.stackCount = 1000;
        thingsToGive.Add(steel);

        Thing compInd = ThingMaker.MakeThing(ThingDefOf.ComponentIndustrial);
        compInd.stackCount = 10;
        thingsToGive.Add(compInd);

        Thing compSpa = ThingMaker.MakeThing(ThingDefOf.ComponentSpacer);
        compSpa.stackCount = 5;
        thingsToGive.Add(compSpa);

        if (ModLister.AnomalyInstalled)
        {
            Thing bioferrite = ThingMaker.MakeThing(ThingDefOf.Bioferrite);
            bioferrite.stackCount = 250;
            thingsToGive.Add(bioferrite);

            Thing shard = ThingMaker.MakeThing(ThingDefOf.Shard);
            shard.stackCount = 10;
            thingsToGive.Add(shard);
        }

        DiaNode nodeRoot = new("MSS_GG_AlliedWithScarab".Translate(GenLabel.ThingsLabel(thingsToGive)));

        nodeRoot.options.Add(new DiaOption("OK".Translate()) { resolveTree = true });

        Dialog_NodeTreeWithFactionInfo treeWithFactionInfo = new(nodeRoot, other, true, true, "MSS_GG_AlliedWithScarab_Title".Translate());

        treeWithFactionInfo.closeAction = delegate
        {
            Map map = Find.AnyPlayerHomeMap;

            IntVec3 dropSpot = DropCellFinder.TradeDropSpot(map);

            DropPodUtility.DropThingsNear(dropSpot, map, thingsToGive, canRoofPunch: false, forbid: false, allowFogged: true, faction: Faction.OfPlayer);
        };

        Find.WindowStack.Add(treeWithFactionInfo);
    }
}
