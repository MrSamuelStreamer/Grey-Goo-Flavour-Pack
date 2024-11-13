using RimWorld;
using Verse;

namespace Grey_Goo;

public class ThoughtWorker_Frogge : ThoughtWorker
{
    private const float Radius = 15f;

    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        return p.Spawned && p.Map.listerThings.ThingsOfDef(Grey_GooDefOf.MSS_GG_Frogge).Any(thing => p.Position.InHorDistOf(thing.Position, Radius));
    }
}
