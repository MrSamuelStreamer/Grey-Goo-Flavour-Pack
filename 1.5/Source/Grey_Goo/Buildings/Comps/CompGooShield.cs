using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;
using Random = UnityEngine.Random;

namespace Grey_Goo.Buildings.Comps;

[StaticConstructorOnStartup]
public class CompGooShield : CompProjectileInterceptor, IMapCellProtector
{
    public static readonly Lazy<MethodInfo> BreakShieldEmp = new Lazy<MethodInfo>(() => AccessTools.Method(typeof(CompProjectileInterceptor), "BreakShieldEmp"));
    public static readonly Lazy<MethodInfo> GetCurrentAlpha = new Lazy<MethodInfo>(() => AccessTools.Method(typeof(CompProjectileInterceptor), "GetCurrentAlpha"));

    public bool HaveMap = false;

    public CompPowerTrader PowerTrader => parent.GetComp<CompPowerTrader>();
    public CompRefuelable Refuelable => parent.GetComp<CompRefuelable>();

    public bool IsPowered => (Refuelable?.HasFuel ?? true) && (PowerTrader?.PowerOn ?? true);

    public CompProperties_GooShield GooShieldProps
    {
        get => (CompProperties_GooShield) props;
    }

    public IEnumerable<IntVec3> CellsInRadius(Map map)
    {
        List<IntVec3> cells = GenRadial.RadialCellsAround(parent.Position, Props.radius, true).ToList();
        return cells;
    }

    public override void PostPostMake()
    {
        base.PostPostMake();

        if (parent.Map != null)
        {
            GreyGoo_MapComponent comp = parent.Map.GetComponent<GreyGoo_MapComponent>();
            comp?.NotifyCellsProtected(this);
            HaveMap = true;
            comp?.TriggerGooRecheck();
        }
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        GreyGoo_MapComponent comp = previousMap.GetComponent<GreyGoo_MapComponent>();
        comp?.NotifyCellsUnprotected(this);
    }


    public override void CompTick()
    {
        if (currentHitPoints < 0 && HitPointsMax > 0)
            currentHitPoints = HitPointsMax;

        if (!HaveMap && parent.Map != null)
        {
            GreyGoo_MapComponent comp = parent.Map.GetComponent<GreyGoo_MapComponent>();
            comp?.NotifyCellsProtected(this);
            HaveMap = true;
        }

        base.CompTick();

        if (Find.TickManager.TicksGame % 3600 == 0)
        {
            TickLong();
        }
    }

    /// <summary>
    /// Executes long-tick logic for the Goo Shield component, applying relevant hediffs and damage effects to entities within its radius.
    /// </summary>
    /// <remarks>
    /// The method performs two main operations:
    /// 1. It identifies and heals pawns within the shield's radius that have specific infected hediffs defined by <see cref="GooShieldProps.gooInfectedHediffs"/>.
    /// 2. It identifies and damages pawns within the shield's radius that have specific enemy xenotypes defined by <see cref="GooShieldProps.gooEnemyXenotypes"/>.
    /// </remarks>
    /// <seealso cref="CompGooShield"/>
    public void TickLong()
    {
        if (GooShieldProps.gooInfectedHediffs.NullOrEmpty())
        {
            return;
        }

        IEnumerable<IntVec3> protectedCells = CellsInRadius(parent.Map);

        List<Pawn> pawns = protectedCells.SelectMany(c => c.GetThingList(parent.Map)).OfType<Pawn>().ToList();

        IEnumerable<Pawn> pawnsWithRelevantXenotypeToDamage = pawns.Where(p =>
            (p.genes != null && GooShieldProps.gooEnemyXenotypes.Contains(p.genes.Xenotype)) || (p.mutant != null && p.mutant.Def == MutantDefOf.Shambler)).ToList();

        foreach (Pawn p in pawnsWithRelevantXenotypeToDamage)
        {
            DamageInfo dinfo = new DamageInfo(
                Grey_GooDefOf.GG_Goo_GooShieldBurn,
                Grey_GooMod.settings.GooDamageRange.RandomInRange,
                1f);

            p.TakeDamage(dinfo);
        }

        // heal up a random pawn 10% of the time
        if (Random.value > 0.1)
        {
            return;
        }

        Pawn pawn = pawns
            .Except(pawnsWithRelevantXenotypeToDamage) // Don't heal enemies
            .Where(p => p.health.hediffSet.hediffs.Any(hediff => GooShieldProps.gooInfectedHediffs.Contains(hediff.def))).RandomElement();

        if (pawn == null)
        {
            return;
        }

        Hediff hediff = pawn.health.hediffSet.hediffs
            .Where(hediff => hediff.pawn.health.HasHediffsNeedingTend() && GooShieldProps.gooInfectedHediffs.Contains(hediff.def) && hediff.TendableNow()).RandomElement();
        if (hediff == null)
        {
            return;
        }

        float baseTendQuality = TendUtility.CalculateBaseTendQuality(null, pawn, null);
        hediff.Tended(baseTendQuality, 0.7f, 0);
        pawn.records.Increment(RecordDefOf.TimesTendedTo);

        QuestUtility.SendQuestTargetSignals(pawn.questTags, "PlayerTended", pawn.Named("SUBJECT"));
    }

    public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
    {
        absorbed = false;
        if (dinfo.Def != DamageDefOf.EMP && dinfo.Def != Grey_GooDefOf.MSS_GG_GooMortarBurn || Props.disarmedByEmpForTicks <= 0)
            return;
        BreakShieldEmp.Value.Invoke(this, [dinfo]);
    }


    public override string CompInspectStringExtra()
    {
        StringBuilder sb = new StringBuilder(base.CompInspectStringExtra());
        if (sb.Length != 0)
            sb.AppendLine();
        sb.AppendTagged("MSS_GG_CompGooShieldEnergy".Translate(currentHitPoints, HitPointsMax));

        return sb.ToString();
    }
}
