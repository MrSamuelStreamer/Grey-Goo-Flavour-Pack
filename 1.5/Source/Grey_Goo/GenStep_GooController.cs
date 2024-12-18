﻿using System.Collections.Generic;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using Verse;

namespace Grey_Goo;

public class GenStep_GooController : GenStep_Scatterer
{
    public override int SeedPart => 1948112838;

    protected override bool CanScatterAt(IntVec3 loc, Map map)
    {
        if (!base.CanScatterAt(loc, map) || !map.reachability.CanReachMapEdge(loc, TraverseParms.For(TraverseMode.PassDoors)))
        {
            return false;
        }

        CellRect rect = CellRect.CenteredOn(loc, 12, 12).ClipInsideMap(map);

        List<CellRect> var;
        if (MapGenerator.TryGetVar("UsedRects", out var) && var.Any(x => rect.Overlaps(x)))
            return false;

        return true;
    }

    public override void Generate(Map map, GenStepParams parms)
    {
        count = 1;
        base.Generate(map, parms);
    }

    protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int count = 1)
    {
        if (map.Parent is not Site site)
        {
            return;
        }

        SitePart sitePart = site.parts.FirstOrDefault(p => p.def == Grey_GooDefOf.MSS_GG_GooControllerSitePart);
        if (sitePart == null)
        {
            return;
        }

        if (sitePart.conditionCauser.Destroyed)
        {
            return;
        }

        BaseGen.globalSettings.map = map;
        ResolveParams resolveParams = new ResolveParams();
        resolveParams.rect = CellRect.CenteredOn(loc, 12, 12).ClipInsideMap(map);
        resolveParams.faction = Find.FactionManager.FirstFactionOfDef(Grey_GooDefOf.GG_GreyGoo);
        resolveParams.conditionCauser = sitePart.conditionCauser;
        BaseGen.symbolStack.Push("conditionCauserRoom", resolveParams);
        resolveParams.threatPoints = StorytellerUtility.DefaultSiteThreatPointsNow();
        BaseGen.symbolStack.Push("MSS_GG_Shamblers", resolveParams);
        BaseGen.Generate();
        MapGenerator.SetVar("RectOfInterest", resolveParams.rect);
    }
}
