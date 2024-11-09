using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Grey_Goo.Buildings.Comps;

public class CompProperties_GooShield : CompProperties_ProjectileInterceptor
{
    public List<HediffDef> gooInfectedHediffs;
    public List<XenotypeDef> gooEnemyXenotypes;

    public CompProperties_GooShield()
    {
        compClass = typeof (CompGooShield);
    }
}
