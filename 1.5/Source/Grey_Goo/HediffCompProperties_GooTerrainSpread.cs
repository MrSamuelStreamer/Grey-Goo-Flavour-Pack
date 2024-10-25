using Verse;

namespace Grey_Goo;

public class HediffCompProperties_GooTerrainSpread: HediffCompProperties
{
    public float chanceToDropGoo = 0.1f;

    public HediffCompProperties_GooTerrainSpread()
    {
        compClass = typeof(HediffCompGooTerrainSpread);
    }

}
