using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Grey_Goo;

public class IncidentWorker_GooShamblerSwarm: IncidentWorker_ShamblerSwarm
{
    protected override List<Pawn> GenerateEntities(IncidentParms parms, float points)
    {
        return [base.GenerateEntities(parms, points).RandomElement()];
    }
}
