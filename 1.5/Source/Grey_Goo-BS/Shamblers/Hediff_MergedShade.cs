using UnityEngine;
using Verse;

namespace Grey_Goo.BS.Shamblers;

public class Hediff_MergedShade: HediffWithComps
{
    public float MergedBodySizeMultiplier = 1f;
    public float MergedMoveSpeedInverseMultiplier = 1f;
    public float MergedMeleeDamageFactorOffset = 0f;

    public override string Description
    {
        get
        {
            string description = base.Description;
            return description.Replace(def.Description, def.Description.Translate(Mathf.Floor(Severity)));
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref MergedBodySizeMultiplier, "MergedBodySizeMultiplier", 1f);
        Scribe_Values.Look(ref MergedMoveSpeedInverseMultiplier, "MergedMoveSpeedMultiplier", 1f);
        Scribe_Values.Look(ref MergedMeleeDamageFactorOffset, "MergedMeleeDamageFactorOffset", 0f);
    }
}
