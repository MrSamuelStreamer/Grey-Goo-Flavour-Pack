using RimWorld;
using Verse;

namespace Grey_Goo;

public class CompCauseGameCondition_GooBoost : CompCauseGameCondition
{
    protected override GameCondition CreateConditionOn(Map map)
    {
        GameCondition conditionOn = base.CreateConditionOn(map);
        conditionOn.Duration = -1;
        conditionOn.Permanent = true;
        return conditionOn;
    }
}
