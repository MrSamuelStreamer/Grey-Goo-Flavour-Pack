using System;
using System.Collections.Generic;
using RimWorld.BaseGen;
using RimWorld.Planet;
using Verse;

namespace Grey_Goo;

public class GenStep_GooController: GenStep_Scatterer
{
    public override int SeedPart => 1948112838;
    private GenStepParams currentParams;

    protected override bool CanScatterAt(IntVec3 c, Map map)
    {
        if (!base.CanScatterAt(c, map) || !map.reachability.CanReachMapEdge(c, TraverseParms.For(TraverseMode.PassDoors)))
            return false;
        CellRect rect = CellRect.CenteredOn(c, 3, 3).ClipInsideMap(map);

        List<CellRect> var;
        if (MapGenerator.TryGetVar("UsedRects", out var) && var.Any(x => rect.Overlaps(x)))
            return false;

        return true;
    }

    public override void Generate(Map map, GenStepParams parms)
    {
        currentParams = parms;
        count = 1;
        base.Generate(map, parms);
    }

    protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int count = 1)
    {
        SitePart sitePart = currentParams.sitePart;
        BaseGen.globalSettings.map = map;
        ResolveParams resolveParams = new ResolveParams();
        resolveParams.rect = CellRect.CenteredOn(loc, 3, 3).ClipInsideMap(map);
        resolveParams.faction = Find.FactionManager.FirstFactionOfDef(Grey_GooDefOf.GG_GreyGoo);
        resolveParams.conditionCauser = sitePart.conditionCauser;
        BaseGen.symbolStack.Push("conditionCauserRoom", resolveParams);
        MapGenerator.SetVar<CellRect>("RectOfInterest", resolveParams.rect);
    }
}
