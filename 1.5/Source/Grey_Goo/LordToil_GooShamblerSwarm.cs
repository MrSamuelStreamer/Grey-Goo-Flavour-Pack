using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Grey_Goo;

public class LordToil_GooShamblerSwarm(IntVec3 start, IntVec3 dest) : LordToil_ShamblerSwarm(start, dest)
{
    public float ChanceToMerge => Grey_GooMod.settings.ChanceToMerge;
    public Lazy<FieldInfo> pathInfo = new Lazy<FieldInfo>(()=> AccessTools.Field(typeof(LordToil_EntitySwarm), "path"));
    public Lazy<MethodInfo> getPathInfo = new Lazy<MethodInfo>(()=> AccessTools.Method(typeof(LordToil_EntitySwarm), "GetPath"));
    public JobDef MSS_GG_Merge_Shamblers => DefDatabase<JobDef>.GetNamed("MSS_GG_Merge_Shamblers");

    public override void LordToilTick()
    {
        if (pathInfo.Value.GetValue(this) == null)
            getPathInfo.Value.Invoke(this, []);

        PawnPath path = (PawnPath)pathInfo.Value.GetValue(this);

        if (path.NodesLeftCount <= 1 || !path.Found || lord.ownedPawns.Count == 0)
        {
            lord.ReceiveMemo("TravelArrived");
        }
        else
        {
            if (Find.TickManager.TicksGame > Data.lastMoved + 600)
            {
                Data.pos = path.ConsumeNextNode();
                Data.lastMoved = Find.TickManager.TicksGame;
            }
            Pawn ownedPawn = lord.ownedPawns[Find.TickManager.TicksGame % lord.ownedPawns.Count];

            if (Find.TickManager.TicksGame % 60 == 0 && MSS_GG_Merge_Shamblers != null && !(ownedPawn.CurJob != null && ownedPawn.CurJob.def == MSS_GG_Merge_Shamblers) && Rand.Chance(ChanceToMerge))
            {
                Pawn target = lord.ownedPawns.Except(ownedPawn).Where(p=>p.CurJob.def != MSS_GG_Merge_Shamblers && p.mutant.Def == ownedPawn.mutant.Def).RandomElement();

                if(target != null){
                    ownedPawn.mindState.nextMoveOrderIsWait = false;

                    Job job = JobMaker.MakeJob(MSS_GG_Merge_Shamblers, ownedPawn, target);

                    job.reportStringOverride = "MSS_GG_Shambler_Merging".Translate(ownedPawn, target);

                    ownedPawn.jobs.StartJob(job);

                }
            }else if (!ownedPawn.Position.InHorDistOf(Data.pos, 7f) && !ownedPawn.pather.Moving)
            {
                IntVec3 result;
                CellFinder.TryFindRandomReachableCellNearPosition(ownedPawn.Position, Data.pos, lord.Map, 7f, TraverseParms.For(ownedPawn), x => x.Standable(lord.Map), null, out result);
                if (!result.IsValid)
                    lord.RemovePawn(ownedPawn);
                ownedPawn.mindState.duty = new PawnDuty(GetDutyDef(), result);
            }
        }
    }
}
