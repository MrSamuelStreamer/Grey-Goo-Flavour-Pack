using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Grey_Goo;

public class IncidentWorker_GooShamblerSwarm: IncidentWorker_ShamblerSwarm
{
    public static Lazy<FieldInfo> queuedIncidents = new Lazy<FieldInfo>(()=>AccessTools.Field(typeof(IncidentWorker), "queuedIncidents"));
    public int ShamblersOnMap(Map map)
    {
        return map.mapPawns.AllPawns.Count(p => p.mutant != null && p.mutant.Def == MutantDefOf.Shambler);
    }

    public bool MapIsValid(Map map)
    {
        if (!map.IsPlayerHome) return false;
        if (ShamblersOnMap(map) > Grey_GooMod.settings.MaxShamblersOnMap) return false;

        return true;
    }

    public bool AlreadyInQueue()
    {
        List<QueuedIncident> queue = (List<QueuedIncident>)queuedIncidents.Value.GetValue(Find.Storyteller.incidentQueue);

        return queue != null && queue.Any(i => i.FiringIncident.def == def);
    }

    protected override bool CanFireNowSub(IncidentParms parms)
    {
        return base.CanFireNowSub(parms) && !AlreadyInQueue() && parms.target is Map map && MapIsValid(map);
    }
    protected override List<Pawn> GenerateEntities(IncidentParms parms, float points)
    {
        return [base.GenerateEntities(parms, points).RandomElement()];
    }
}
