using RimWorld;

namespace Grey_Goo;

public class StorytellerCompProperties_RefiringQuest: StorytellerCompProperties
{
    public IncidentDef incident;
    public float refireEveryDays = -1f;

    public StorytellerCompProperties_RefiringQuest()
    {
        compClass = typeof(StorytellerComp_RefiringQuest);
    }
}
