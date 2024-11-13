using System;
using System.Collections.Generic;
using BigAndSmall;
using RimWorld;
using Verse;
using Verse.AI;

namespace Grey_Goo.BS.Shamblers;

public class JobDriver_MergeShamblers : JobDriver
{
    public void MergeShamblers()
    {
        Hediff_MergedShade hediffA = (Hediff_MergedShade) pawn.health.GetOrAddHediff(Grey_Goo_BS_DefOf.MSS_GG_MergedShambler);

        if (!TargetPawnB.health.hediffSet.TryGetHediff(Grey_Goo_BS_DefOf.MSS_GG_MergedShambler, out Hediff hediffB))
        {
            hediffA.MergedBodySizeMultiplier += TargetPawnB.GetStatValue(BSDefs.SM_BodySizeMultiplier);
            hediffA.MergedMoveSpeedInverseMultiplier += TargetPawnB.GetStatValue(StatDefOf.MoveSpeed);
            hediffA.MergedMeleeDamageFactorOffset += TargetPawnB.GetStatValue(StatDefOf.MeleeDamageFactor);
            hediffA.Severity += 1;
        }
        else
        {
            hediffA.MergedBodySizeMultiplier += ((Hediff_MergedShade) hediffB).MergedBodySizeMultiplier;
            hediffA.MergedMoveSpeedInverseMultiplier += ((Hediff_MergedShade) hediffB).MergedMoveSpeedInverseMultiplier;
            hediffA.MergedMeleeDamageFactorOffset += ((Hediff_MergedShade) hediffB).MergedMeleeDamageFactorOffset;
            hediffA.Severity += hediffB.Severity;
        }

        string nameA = TargetPawnA.NameFullColored;

        string nameB = TargetPawnB.NameFullColored;
        TargetPawnB.Destroy(DestroyMode.Vanish);

        if (hediffA.Severity > Grey_GooMod.settings.ShamblerMergeHediffSeverityToTransform)
        {
            PawnGenerationRequest request = new PawnGenerationRequest(
                kind: Grey_Goo_BS_DefOf.MSS_GG_ShamblerGorebeast,
                forceGenerateNewPawn: true,
                allowDowned: false,
                allowDead: false,
                faction: pawn.Faction,
                tile: pawn.Map.Tile,
                fixedGender: pawn.gender,
                fixedBirthName: pawn.Name.ToStringFull,
                fixedBiologicalAge: pawn.ageTracker.AgeBiologicalYearsFloat,
                fixedChronologicalAge: pawn.ageTracker.AgeChronologicalYearsFloat);
            request.ForcedMutant = pawn.mutant.Def;

            Pawn newPawn = PawnGenerator.GeneratePawn(request);
            if (pawn.Name is NameTriple trip)
            {
                newPawn.Name = trip;
                newPawn.Name = new NameTriple(trip.First, "Gorebeast", trip.Last);
            }
            else
            {
                NameTriple plain = NameTriple.FromString(pawn.Name.ToStringFull);
                newPawn.Name = new NameTriple(plain.First, "Gorebeast", plain.Last);
            }

            SpawnRequest req = new SpawnRequest([newPawn], [pawn.Position], 1, 1) { spawnSound = SoundDefOf.FleshmassBirth };
            pawn.Map.deferredSpawner.AddRequest(req);
            pawn.Destroy(DestroyMode.Vanish);

            Messages.Message("MSS_GG_Shambler_Merge_Gorebeast".Translate(nameB, nameA), newPawn, MessageTypeDefOf.ThreatBig, true);
        }
        else
        {
            Messages.Message("MSS_GG_Shambler_Merge".Translate(nameB, nameA), TargetPawnA, MessageTypeDefOf.ThreatSmall, true);

            HumanoidPawnScaler.GetCache(pawn, forceRefresh: true, scheduleForce: 10);
        }
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return true;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        // fail if not pawn, and not shambler
        AddEndCondition((Func<JobCondition>) (() => TargetPawnA.Dead || TargetPawnA.kindDef != PawnKindDefOf.ShamblerSwarmer ? JobCondition.Incompletable : JobCondition.Ongoing));
        AddEndCondition((Func<JobCondition>) (() => TargetPawnB.Dead || TargetPawnB.kindDef != PawnKindDefOf.ShamblerSwarmer ? JobCondition.Incompletable : JobCondition.Ongoing));

        this.FailOnDespawnedOrNull(TargetIndex.A);
        this.FailOnDespawnedOrNull(TargetIndex.B);

        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch, false);
        Toil toil = ToilMaker.MakeToil("MakeNewToils");
        toil.initAction = MergeShamblers;
        yield return toil;
    }
}
