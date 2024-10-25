using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Grey_Goo;

public class StorytellerComp_RefiringQuest: StorytellerComp
{
    private int IntervalsPassed => Find.TickManager.TicksGame / 1000;

    public StorytellerCompProperties_RefiringQuest Props => (StorytellerCompProperties_RefiringQuest) props;

    public int NextInterval = 0;

    public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
    {
        if (Props.incident.TargetAllowed(target))
        {
            if (IntervalsPassed >= NextInterval)
            {
                IncidentParms parms = GenerateParms(Props.incident.category, target);
                parms.forced = true;
                parms.silent = true;
                if (Props.incident.Worker.CanFireNow(parms))
                    yield return new FiringIncident(Props.incident, this, parms);
                NextInterval = IntervalsPassed + Mathf.CeilToInt(Props.minDaysPassed * 60.0f);
            }
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        NextInterval = IntervalsPassed + Mathf.CeilToInt(Props.minDaysPassed * 60.0f);
    }

    public override string ToString() => base.ToString() + " " + Props.incident;
}
