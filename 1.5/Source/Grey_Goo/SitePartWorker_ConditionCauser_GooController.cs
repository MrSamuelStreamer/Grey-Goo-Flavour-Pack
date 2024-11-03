using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Grey_Goo;

public class SitePartWorker_ConditionCauser_GooController: SitePartWorker_ConditionCauser
{
    public override string GetPostProcessedThreatLabel(Site site, SitePart sitePart)
    {
        return def.label;
    }

    public override void PostDrawExtraSelectionOverlays(SitePart sitePart)
    {
        return;
    }

    public override void Init(Site site, SitePart sitePart)
    {
        sitePart.conditionCauser = ThingMaker.MakeThing(sitePart.def.conditionCauserDef);
        // CompCauseGameCondition comp = sitePart.conditionCauser.TryGetComp<CompCauseGameCondition>();
        // comp.RandomizeSettings(site);
        // comp.LinkWithSite(sitePart.site);
    }
}
