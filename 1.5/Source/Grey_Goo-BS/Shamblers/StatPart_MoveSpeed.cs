using Verse;

namespace Grey_Goo.BS.Shamblers;

public class StatPart_MoveSpeed: StatPart_Multiplier
{
    public override string ExplanationString => "MSS_GG_MoveSpeed";

    public override float GetMultiplier(Thing t)
    {
        if(t is not Pawn pawn) return 1f;

        return !pawn.health.hediffSet.TryGetHediff(out Hediff_MergedShade hediff) ? 1f : 1/hediff.MergedMoveSpeedInverseMultiplier;
    }
}
