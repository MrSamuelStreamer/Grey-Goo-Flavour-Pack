using RimWorld;

namespace Grey_Goo.Verbs;

public class Verb_CastAbilityConsumeLeapFrogge : Verb_CastAbilityConsumeLeap
{
    // public override bool CanHitTarget(LocalTargetInfo targ)
    // {
    //     return base.CanHitTarget(targ) && targ.Pawn != null;
    // }
    // protected override bool TryCastShot()
    // {
    //     return this.currentTarget.Pawn != null && base.TryCastShot() && JumpUtility.DoJump(this.CasterPawn, this.currentTarget, this.ReloadableCompSource, this.verbProps, this.ability, this.CurrentTarget, this.JumpFlyerDef);
    // }
    //
    // public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
    // {
    //     return caster != null && target != null && target.Pawn != null && CanHitTarget(target) && JumpUtility.ValidJumpTarget(this.caster.Map, target.Cell) && ReloadableUtility.CanUseConsideringQueuedJobs(this.CasterPawn, this.EquipmentSource);
    // }
}
