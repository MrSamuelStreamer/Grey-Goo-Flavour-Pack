using Verse;

namespace Grey_Goo;

public class GooSpreaderGeneModExtension : DefModExtension
{
    public float chanceToSpreadGoo = 0.1f;
}

public class GooSpreaderGene: Gene
{
    public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
    {
        if(!Grey_GooMod.settings.EnableGoo) return;
        if(!def.HasModExtension<GooSpreaderGeneModExtension>()) return;
        if(!Rand.Chance(def.GetModExtension<GooSpreaderGeneModExtension>().chanceToSpreadGoo) || pawn.Corpse.Map == null) return;

        IntVec3 cell = pawn.Position;
        pawn.Corpse.Map.terrainGrid.SetTerrain(cell, Grey_GooDefOf.GG_Goo);
    }
}
