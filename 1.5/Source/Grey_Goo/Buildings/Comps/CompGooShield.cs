using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Grey_Goo.Buildings.Comps;

[StaticConstructorOnStartup]
public class CompGooShield : ThingComp, IMapCellProtector
{
    private int nextChargeTick = -1;
    private bool shutDown;
    private StunHandler stunner;
    private Sustainer sustainer;
    public int currentHitPoints = -1;
    public int? maxHitPointsOverride;
    private static readonly Material ForceFieldMat = MaterialPool.MatFrom("Other/ForceField", ShaderDatabase.MoteGlow);
    private static readonly Material ForceFieldConeMat = MaterialPool.MatFrom("Other/ForceFieldCone", ShaderDatabase.MoteGlow);
    private static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();
    private const float TextureActualRingSizeFactor = 1.1601562f;
    private static readonly Color InactiveColor = new Color(0.2f, 0.2f, 0.2f);
    private const int NumInactiveDots = 7;

    public bool HaveMap = false;

    public CompPowerTrader PowerTrader => parent.GetComp<CompPowerTrader>();
    public CompRefuelable Refuelable => parent.GetComp<CompRefuelable>();

    public bool IsPowered => (Refuelable?.HasFuel ?? true) && (PowerTrader?.PowerOn ?? true);

    private static Material ShieldDotMat
    {
        get => MaterialPool.MatFrom("Things/Mote/ShieldDownDot", ShaderDatabase.MoteGlow);
    }

    public CompProperties_GooShield Props
    {
        get => (CompProperties_GooShield) props;
    }

    public bool Active
    {
        get
        {
            if (!IsPowered || stunner.Stunned || shutDown || currentHitPoints == 0 || !this.parent.Spawned || this.parent is Pawn pawn && (pawn.IsCharging() || pawn.IsSelfShutdown()))
                return false;
            CompCanBeDormant comp = parent.GetComp<CompCanBeDormant>();
            return comp == null || comp.Awake;
        }
    }

    public bool ShouldDisplayGizmo
    {
        get
        {
            return parent is Pawn pawn && (pawn.IsColonistPlayerControlled || pawn.RaceProps.IsMechanoid);
        }
    }

    public override void PostPostMake()
    {
        base.PostPostMake();
        stunner = new StunHandler(parent);

        if (parent.Map != null)
        {
            GreyGoo_MapComponent comp = parent.Map.GetComponent<GreyGoo_MapComponent>();
            comp?.NotifyCellsProtected(this);
            HaveMap = true;
        }
    }

    public override void PostDeSpawn(Map map) => sustainer?.End();

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        GreyGoo_MapComponent comp = previousMap.GetComponent<GreyGoo_MapComponent>();
        comp?.NotifyCellsUnprotected(this);
    }


    public override void CompTick()
    {
        if (!HaveMap && parent.Map != null)
        {
            GreyGoo_MapComponent comp = parent.Map.GetComponent<GreyGoo_MapComponent>();
            comp?.NotifyCellsProtected(this);
            HaveMap = true;
        }

        stunner.StunHandlerTick();
        if (Props.activeSound.NullOrUndefined())
            return;
        if (Active)
        {
            if (sustainer == null || sustainer.Ended)
                sustainer = Props.activeSound.TrySpawnSustainer(SoundInfo.InMap((TargetInfo) (Thing) parent));
            sustainer.Maintain();
        }
        else
        {
            if (sustainer == null || sustainer.Ended)
                return;
            sustainer.End();
        }

        if(Find.TickManager.TicksGame % 3600 == 0) TickLong();
    }

    /// <summary>
    /// Executes long-tick logic for the Goo Shield component, applying relevant hediffs and damage effects to entities within its radius.
    /// </summary>
    /// <remarks>
    /// The method performs two main operations:
    /// 1. It identifies and heals pawns within the shield's radius that have specific infected hediffs defined by <see cref="Props.gooInfectedHediffs"/>.
    /// 2. It identifies and damages pawns within the shield's radius that have specific enemy xenotypes defined by <see cref="Props.gooEnemyXenotypes"/>.
    /// </remarks>
    /// <seealso cref="CompGooShield"/>
    public void TickLong()
    {
        if(Props.gooInfectedHediffs.NullOrEmpty()) return;

        IEnumerable<IntVec3> protectedCells = CellsInRadius(this.parent.Map);

        List<Pawn> pawns = protectedCells.SelectMany(c=>c.GetThingList(this.parent.Map)).OfType<Pawn>().ToList();

        IEnumerable<Pawn> pawnsWithRelevantXenotypeToDamage = pawns.Where(p=>p.genes != null && Props.gooEnemyXenotypes.Contains(p.genes.Xenotype) || p.mutant != null && p.mutant.Def == MutantDefOf.Shambler).ToList();

        foreach (Pawn p in pawnsWithRelevantXenotypeToDamage)
        {
            DamageInfo dinfo = new DamageInfo(
                Grey_GooDefOf.GG_Goo_GooShieldBurn,
                Grey_GooMod.settings.GooDamageRange.RandomInRange,
                1f);

            p.TakeDamage(dinfo);
        }

        // heal up a random pawn 10% of the time
        if(Random.value > 0.1) return;
        Pawn pawn = pawns
            .Except(pawnsWithRelevantXenotypeToDamage) // Don't heal enemies
            .Where(p=>p.health.hediffSet.hediffs.Any(hediff=>Props.gooInfectedHediffs.Contains(hediff.def))).RandomElement();

        Hediff hediff = pawn.health.hediffSet.hediffs.Where(hediff => hediff.pawn.health.HasHediffsNeedingTend() && Props.gooInfectedHediffs.Contains(hediff.def) && hediff.TendableNow()).RandomElement();
        if(hediff == null) return;
        float baseTendQuality = TendUtility.CalculateBaseTendQuality(null, pawn, null);
        hediff.Tended(baseTendQuality, 0.7f, 0);
        pawn.records.Increment(RecordDefOf.TimesTendedTo);

        QuestUtility.SendQuestTargetSignals(pawn.questTags, "PlayerTended", pawn.Named("SUBJECT"));
    }

    public override void PostDrawExtraSelectionOverlays()
    {
        base.PostDrawExtraSelectionOverlays();
        if (Active)
            return;
        for (int index = 0; index < 7; ++index)
        {
            Vector3 vector3 = parent.DrawPos + new Vector3(0.0f, 0.0f, 1f).RotatedBy((float) (index / 7.0 * 360.0)) * (Props.radius * 0.966f);
            Graphics.DrawMesh(MeshPool.plane10, new Vector3(vector3.x, AltitudeLayer.MoteOverhead.AltitudeFor(), vector3.z), Quaternion.identity, ShieldDotMat, 0);
        }
    }

    public override void PostDraw()
    {
        base.PostDraw();
        Vector3 drawPos = parent.DrawPos with { y = AltitudeLayer.MoteOverhead.AltitudeFor() };
        float currentAlpha = GetCurrentAlpha();
        if (currentAlpha > 0.0)
        {
            Color color = Active || !Find.Selector.IsSelected(parent) ? Props.color : InactiveColor;
            color.a *= currentAlpha;
            MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, color);
            Matrix4x4 matrix = new Matrix4x4();
            matrix.SetTRS(drawPos, Quaternion.identity, new Vector3((float) (Props.radius * 2.0 * (297.0 / 256.0)), 1f, (float) (Props.radius * 2.0 * (297.0 / 256.0))));
            Graphics.DrawMesh(MeshPool.plane10, matrix, ForceFieldMat, 0, null, 0, MatPropertyBlock);
        }
    }

    private float GetCurrentAlpha()
    {
        return Mathf.Max(Mathf.Max(Mathf.Max(Mathf.Max(GetCurrentAlpha_Idle(), GetCurrentAlpha_Selected()))), Props.minAlpha);
    }

    private float GetCurrentAlpha_Idle()
    {
        float idlePulseSpeed = Props.idlePulseSpeed;
        float minIdleAlpha = Props.minIdleAlpha;
        return !Active || parent.Faction == Faction.OfPlayer && Find.Selector.IsSelected(parent)
            ? 0.0f
            : Mathf.Lerp(minIdleAlpha, 0.11f,
                (float) ((Mathf.Sin(Gen.HashCombineInt(parent.thingIDNumber, 96804938) % 100 + Time.realtimeSinceStartup * idlePulseSpeed) + 1.0) / 2.0));
    }

    private float GetCurrentAlpha_Selected()
    {
        float num = Mathf.Max(2f, Props.idlePulseSpeed);
        return !Find.Selector.IsSelected(parent) && !Props.drawWithNoSelection || !Active
            ? 0.0f
            : Mathf.Lerp(0.2f, 0.62f, (float) ((Mathf.Sin(Gen.HashCombineInt(parent.thingIDNumber, 35990913) % 100 + Time.realtimeSinceStartup * num) + 1.0) / 2.0));
    }

    public override string CompInspectStringExtra()
    {
        StringBuilder sb = new StringBuilder();
        if (stunner.Stunned)
        {
            if (sb.Length != 0)
                sb.AppendLine();
            sb.AppendTagged("DisarmedTime".Translate() + ": " + stunner.StunTicksLeft.ToStringTicksToPeriod());
        }

        if (shutDown)
        {
            if (sb.Length != 0)
                sb.AppendLine();
            sb.Append("ShutDown".Translate());
        }

        return sb.ToString();
    }

    public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
    {
        base.PostPreApplyDamage(ref dinfo, out absorbed);
        BreakShieldEmp(dinfo);
    }

    private void BreakShieldEmp(DamageInfo dinfo)
    {
        if (Active)
        {
            EffecterDefOf.Shield_Break.SpawnAttached(parent, parent.MapHeld, Props.radius);
            int num = Mathf.CeilToInt(Props.radius * 2f);
            float fTheta = 6.2831855f / num;
            Vector3 center = parent.TrueCenter();
            for (int index = 0; index < num; ++index)
                FleckMaker.ConnectingLine(PosAtIndex(index), PosAtIndex((index + 1) % num), FleckDefOf.LineEMP, parent.Map, 1.5f);

            Vector3 PosAtIndex(int index)
            {
                return new Vector3(Props.radius * Mathf.Cos(fTheta * index) + center.x, 0.0f, Props.radius * Mathf.Sin(fTheta * index) + center.z);
            }
        }

        dinfo.SetAmount(Props.disarmedByEmpForTicks / 30f);
        stunner.Notify_DamageApplied(dinfo);
    }

    private void BreakShieldHitpoints(DamageInfo dinfo)
    {
        EffecterDefOf.Shield_Break.SpawnAttached(parent, parent.MapHeld, Props.radius);
        stunner.Notify_DamageApplied(dinfo);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref shutDown, "shutDown");
        Scribe_Values.Look(ref nextChargeTick, "nextChargeTick", -1);
        Scribe_Deep.Look(ref stunner, "stunner", parent);
        Scribe_Values.Look(ref currentHitPoints, "currentHitPoints", -1);
        Scribe_Values.Look(ref maxHitPointsOverride, "maxHitPointsOverride");
        if (Scribe.mode != LoadSaveMode.PostLoadInit)
            return;
        if (stunner != null)
            return;
        stunner = new StunHandler(parent);
    }

    public IEnumerable<IntVec3> CellsInRadius(Map map)
    {
        List<IntVec3> cells = GenRadial.RadialCellsAround(parent.Position, Props.radius, true).ToList();
        return cells;
    }
}
