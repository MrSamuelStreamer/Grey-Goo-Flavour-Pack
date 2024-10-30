using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Grey_Goo;

public class IncidentWorker_GooShamblerSwarm: IncidentWorker_ShamblerSwarm
{
    public static Dictionary<Map, LordJob> lordJobs = new Dictionary<Map, LordJob>();

    public static Lazy<FieldInfo> queuedIncidents = new Lazy<FieldInfo>(()=>AccessTools.Field(typeof(IncidentQueue), "queuedIncidents"));
    public int ShamblersOnMap(Map map)
    {
        return map.mapPawns.AllPawns.Count(p => p.mutant != null && p.mutant.Def == MutantDefOf.Shambler);
    }

    protected override LordJob GenerateLordJob(IntVec3 entry, IntVec3 dest)
    {
        return new LordJob_ShamblerSwarm(entry, dest);
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        if (!ModsConfig.AnomalyActive)
            return false;

        Map target = (Map) parms.target;
        Lord lord = target.lordManager.lords.FirstOrDefault(l => l.faction == parms.faction);

        RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 entryCell, target, CellFinder.EdgeRoadChance_Hostile);

        IntVec3 destCell = IntVec3.Invalid;

        // if there's a lord, grab one of the pawns positions as a target point
        if (lord is { AnyActivePawn: true })
        {
            destCell = lord.ownedPawns.Where(p=>p.mindState is { Active: true }).RandomElement().Position;
        }

        if (destCell == IntVec3.Invalid && !RCellFinder.TryFindTravelDestFrom(entryCell, target, out destCell))
            return false;

        parms.spawnCenter = entryCell;
        List<Pawn> entities = GenerateEntities(parms, TransformPoints(parms.points));

        if (entities.NullOrEmpty())
            return false;

        if (AnomalyIncidentUtility.IncidentShardChance(parms.points))
            AnomalyIncidentUtility.PawnShardOnDeath(entities.RandomElement());

        if (!entryCell.IsValid && !RCellFinder.TryFindRandomPawnEntryCell(out entryCell, target, CellFinder.EdgeRoadChance_Hostile))
            return false;

        Rot4 rot = Rot4.FromAngleFlat((target.Center - entryCell).AngleFlat);
        foreach (Pawn newThing in entities)
        {
            GenSpawn.Spawn(newThing, CellFinder.RandomClosewalkCellNear(entryCell, target, 10), target, rot);
            QuestUtility.AddQuestTag(newThing, parms.questTag);
        }

        // Don't send the letter or slow down
        // SendLetter(parms, entities);
        // Find.TickManager.slower.SignalForceNormalSpeedShort();

        if (lord != null)
        {
            lord.AddPawns(entities);
        }
        else
        {
            LordMaker.MakeNewLord(parms.faction, GenerateLordJob(parms.spawnCenter, destCell), target, entities);
        }

        return true;
    }

    public bool MapIsValid(Map map)
    {
        if (!map.IsPlayerHome) return false;
        if (ShamblersOnMap(map) > Grey_GooMod.settings.MaxShamblersOnMap) return false;

        return true;
    }

    public bool AlreadyInQueue()
    {
        if(Find.Storyteller.incidentQueue == null) return false;
        List<QueuedIncident> queue = (List<QueuedIncident>)queuedIncidents.Value.GetValue(Find.Storyteller.incidentQueue);

        return queue != null && queue.Any(i => i.FiringIncident.def == def);
    }

    protected override bool CanFireNowSub(IncidentParms parms)
    {
        return base.CanFireNowSub(parms) && !AlreadyInQueue() && parms.target is Map map && MapIsValid(map);
    }
    protected override List<Pawn> GenerateEntities(IncidentParms parms, float points)
    {
        Map target = (Map) parms.target;
        PawnGroupMakerParms parms1 = new PawnGroupMakerParms
        {
            groupKind = GroupKindDef,
            tile = target.Tile,
            faction = Faction.OfEntities,
            points = Faction.OfEntities.def.MinPointsToGeneratePawnGroup(GroupKindDef) * 1.05f
        };
        List<Pawn> entities = PawnGroupMakerUtility.GeneratePawns(parms1).Take(1).ToList();
        SetupShamblerHediffs(entities, ShamblerLifespanTicksRange);
        return entities;
    }
}
