using Verse;

namespace Grey_Goo;

public class HediffCompGooTerrainSpread: HediffComp
{
    public HediffCompProperties_GooTerrainSpread Props => (HediffCompProperties_GooTerrainSpread) props;
    public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
    {
        if(!Rand.Chance(Props.chanceToDropGoo)) return;

        IntVec3 cell = parent.pawn.Position;
        parent.pawn.Map.terrainGrid.SetTerrain(cell, Grey_GooDefOf.GG_Goo);
    }
}
