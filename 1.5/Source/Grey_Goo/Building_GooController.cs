using Verse;

namespace Grey_Goo;

public class Building_GooController: Building
{
    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        GGWorldComponent ggComp = Find.World.GetComponent<GGWorldComponent>();
        if(ggComp != null) ggComp.Notify_ControllerDestroyed(Map);
        base.Destroy(mode);
    }
}
