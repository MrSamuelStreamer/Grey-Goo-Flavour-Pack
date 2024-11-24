using System;
using RimWorld;
using Verse;

namespace Grey_Goo;

public class CompEMPWeakness : ThingComp
{
    private Pawn pawn => parent as Pawn;

    public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
    {
        if (pawn == null)
        {
            return;
        }

        if (dinfo.Def == DamageDefOf.EMP)
        {
            pawn.health.GetOrAddHediff(DefDatabase<HediffDef>.GetNamed("BrainShock"));
        }
    }
}

public class Gene_EMP_Weakness : Gene
{
    public override void PostAdd()
    {
        base.PostAdd();

        if (pawn.GetComp<CompEMPWeakness>() != null)
        {
            return;
        }

        CompEMPWeakness comp = (CompEMPWeakness) Activator.CreateInstance(typeof(CompEMPWeakness));
        comp.parent = pawn;
        pawn.AllComps.Add(comp);
        comp.Initialize(new CompProperties());
    }
}
