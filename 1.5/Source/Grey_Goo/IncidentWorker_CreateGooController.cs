using RimWorld;
using Verse;

namespace Grey_Goo;

public class IncidentWorker_CreateGooController : IncidentWorker
{
    public static GGWorldComponent ggWorldComponent => Find.World.GetComponent<GGWorldComponent>();

    protected override bool CanFireNowSub(IncidentParms parms) => ggWorldComponent.CanCreateNewController();

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        return ggWorldComponent.TrySpawnController(parms);
    }
}
