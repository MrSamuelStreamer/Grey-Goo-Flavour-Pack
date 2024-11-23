using System.Text;
using UnityEngine;
using Verse;

namespace Grey_Goo.BS.Shamblers;

public class Hediff_MergedShade : HediffWithComps
{
    public float MergedBodySizeMultiplier = 1f;
    public float MergedMeleeDamageFactorOffset = 0f;
    public float MergedMoveSpeedInverseMultiplier = 1f;

    public override string Label => base.Label.Replace("MSS_GG_MergedShambler_Label", "MSS_GG_MergedShambler_Label".Translate(Mathf.Floor(Severity)));

    public override string Description
    {
        get
        {
            StringBuilder stringBuilder = new(def.Description.Translate(Mathf.Floor(Severity)));
            int index = 0;
            while (true)
            {
                int num = index;
                int? count = comps?.Count;
                int valueOrDefault = count.GetValueOrDefault();
                if ((num < valueOrDefault) & count.HasValue)
                {
                    string descriptionExtra = comps[index].CompDescriptionExtra;
                    if (!descriptionExtra.NullOrEmpty())
                    {
                        stringBuilder.Append(" ");
                        stringBuilder.Append(descriptionExtra);
                    }

                    ++index;
                }
                else
                {
                    break;
                }
            }

            return stringBuilder.ToString();
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
