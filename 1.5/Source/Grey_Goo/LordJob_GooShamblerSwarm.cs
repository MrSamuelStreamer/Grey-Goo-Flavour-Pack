using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Grey_Goo;

public class LordJob_GooShamblerSwarm: LordJob_ShamblerSwarm
{
    public LordJob_GooShamblerSwarm()
    {
    }

    public LordJob_GooShamblerSwarm(IntVec3 startPos, IntVec3 destPos)
        : base(startPos, destPos)
    {
    }

    protected override LordToil CreateTravelingToil(IntVec3 start, IntVec3 dest)
    {
        return new LordToil_GooShamblerSwarm(start, dest);
    }

}
