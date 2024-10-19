using RimWorld;
using UnityEngine;
using Verse;

namespace Grey_Goo.BS.Shamblers;

public abstract class StatPart_Multiplier: StatPart
{
    public abstract string ExplanationString { get; }

    public abstract float GetMultiplier(Thing t);

    public override void TransformValue(StatRequest req, ref float val)
    {
        val *= GetMultiplier(req.Thing);
    }

    public override string ExplanationPart(StatRequest req)
    {
        float multiplier = GetMultiplier(req.Thing);

        return Mathf.Approximately(multiplier, 1f) ? null : ExplanationString.Translate(multiplier.ToString("0.00" + "%"));
    }
}
