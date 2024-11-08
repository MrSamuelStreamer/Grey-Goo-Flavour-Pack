using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Grey_Goo;

public class CompGooMortar : ThingComp, IAttackTargetSearcher, IVerbOwner
{
    public CompProperties_GooMortar Props => (CompProperties_GooMortar) props;

    // IAttackTargetSearcher
    public Thing Thing => parent;
    public Verb CurrentEffectiveVerb => AttackVerb;
    public LocalTargetInfo LastAttackedTarget => lastAttackedTarget;
    public int LastAttackTargetTick => lastSpitTick;

    // IVerbOwner
    public VerbTracker VerbTracker => verbTracker ??= new VerbTracker(this);
    public List<VerbProperties> VerbProperties => parent.def.Verbs;
    public List<Tool> Tools => parent.def.tools;
    public ImplementOwnerTypeDef ImplementOwnerTypeDef => ImplementOwnerTypeDefOf.NativeVerb;
    public string UniqueVerbOwnerID() => "CompGooMortar_" + parent.ThingID;
    public bool VerbsStillUsableBy(Pawn p) => false;
    public Thing ConstantCaster => parent;


    private int lastSpitTick = -99999;
    private int nextSpitDelay = -99999;
    private LocalTargetInfo lastAttackedTarget;
    private VerbTracker verbTracker;
    private Effecter progressBarEffecter;

    public bool CanFire(out string reason)
    {
        reason = "";
        if (parent.Map == null || parent.Position == IntVec3.Invalid)
        {
            reason = "MSS_GG_MortarNotOnMap";
            return false;
        }

        if (GenAdj.AdjacentCellsAndInside.Select(d => parent.Position + d).Where(c => c.InBounds(parent.Map)).All(c => parent.Map.terrainGrid.TerrainAt(c) != Grey_GooDefOf.GG_Goo))
        {
            reason = "MSS_GG_MortarNotOnGoo".Translate();
            return false;
        }

        return true;
    }

    private Verb AttackVerb => AllVerbs[0];

    private int TicksToNextSpit
    {
        get => lastSpitTick + nextSpitDelay - Find.TickManager.TicksGame;
    }

    private bool OnCooldown => TicksToNextSpit > 0;

    public List<Verb> AllVerbs => VerbTracker.AllVerbs;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        foreach (Verb allVerb in AllVerbs)
            allVerb.caster = parent;
        if (respawningAfterLoad)
            return;
        lastSpitTick = Find.TickManager.TicksGame;
        nextSpitDelay = Props.SpitIntervalRangeTicks.RandomInRange;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref nextSpitDelay, "nextSpitDelay");
        Scribe_Values.Look(ref lastSpitTick, "lastSpitTick");
        Scribe_TargetInfo.Look(ref lastAttackedTarget, "lastAttackedTarget");
        Scribe_Deep.Look(ref verbTracker, "verbTracker", this);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            yield return gizmo;

        if (DebugSettings.ShowDevGizmos)
        {
            Command_Action commandAction = new Command_Action
            {
                defaultDesc = "Dev: Cooldown Spit",
                defaultLabel = "Dev: Cooldown Spit",
                action = delegate
                {
                    nextSpitDelay = 0;
                }
            };

            yield return commandAction;
        }
    }

    public override void PostDeSpawn(Map map)
    {
        progressBarEffecter?.ForceEnd();
        progressBarEffecter = null;
    }

    public override void CompTick()
    {
        base.CompTick();
        if (!parent.Spawned)
            return;

        if (!CanFire(out string reason))
        {
            nextSpitDelay++;
            return;
        };

        if (OnCooldown)
        {
            if (progressBarEffecter == null) progressBarEffecter = EffecterDefOf.ProgressBar.Spawn();
            progressBarEffecter.EffectTick((TargetInfo) (Thing) parent, TargetInfo.Invalid);
            MoteProgressBar mote = ((SubEffecter_ProgressBar) progressBarEffecter.children[0]).mote;
            mote.progress = (float) (1.0 - Mathf.Max(TicksToNextSpit, 0) / (double) nextSpitDelay);
            mote.offsetZ = -0.8f;
        }

        if (TicksToNextSpit > 0 || !parent.IsHashIntervalTick(180))
            return;

        Thing castTarg = (Thing) AttackTargetFinder.BestShootTargetFromCurrentPosition(this, TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable, t => !t.Position.Roofed(t.Map));
        if (castTarg == null)
            return;

        AttackVerb.TryStartCastOn((LocalTargetInfo) castTarg);
        lastSpitTick = Find.TickManager.TicksGame;
        nextSpitDelay = Props.SpitIntervalRangeTicks.RandomInRange;
        Messages.Message("MSS_GG_MortarFiring".Translate(), (Thing) parent, MessageTypeDefOf.NegativeEvent);
    }

    public override void PostDrawExtraSelectionOverlays()
    {
        AttackVerb.DrawHighlight(LocalTargetInfo.Invalid);
    }

    public override string CompInspectStringExtra()
    {
        return CanFire(out string reason) ? (TicksToNextSpit > 0 ? "MSS_GG_MortarSyphoningEnergy".Translate() + ": " + TicksToNextSpit.ToStringTicksToPeriod() : (string) "MSS_GG_MortarReady".Translate()) : reason;
    }
}
