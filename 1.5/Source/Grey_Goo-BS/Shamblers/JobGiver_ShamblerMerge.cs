using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Grey_Goo.BS.Shamblers;

public class JobGiver_ShamblerMerge: ThinkNode_JobGiver
{
    public float ChanceToMerge => Grey_GooMod.settings.ChanceToMerge;
    protected LocomotionUrgency locomotionUrgency = LocomotionUrgency.Jog;

    public override ThinkNode DeepCopy(bool resolve = true)
    {
        JobGiver_ShamblerMerge jobGiver = (JobGiver_ShamblerMerge) base.DeepCopy(resolve);
        jobGiver.locomotionUrgency = locomotionUrgency;
        return jobGiver;
    }

    public Pawn RandomShadeOnPawnsMap(Pawn pawn)
    {
        return pawn.Map.mapPawns.AllPawns.Where(p=>p!=pawn && p.kindDef == PawnKindDefOf.ShamblerSwarmer).RandomElementWithFallback();
    }


    protected override Job TryGiveJob(Pawn pawn)
    {
        if (!Rand.Chance(ChanceToMerge))
        {
            return null;
        }

        bool alreadyMerging = pawn.CurJob != null && pawn.CurJob.def == Grey_Goo_BS_DefOf.MSS_GG_Merge_Shamblers;

        if (alreadyMerging)
        {
            return null;
        }


        Pawn target = RandomShadeOnPawnsMap(pawn);

        if (target == null)
        {
            return null;
        }

        pawn.mindState.nextMoveOrderIsWait = false;

        Job job = JobMaker.MakeJob(Grey_Goo_BS_DefOf.MSS_GG_Merge_Shamblers, pawn, target);

        job.reportStringOverride = "MSS_GG_Shambler_Merging".Translate(pawn, target);

        return job;
    }
}
