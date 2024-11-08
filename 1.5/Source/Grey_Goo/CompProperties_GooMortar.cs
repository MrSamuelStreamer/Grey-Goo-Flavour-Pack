using Verse;

namespace Grey_Goo;

public class CompProperties_GooMortar: CompProperties
{
    public IntRange SpitIntervalRangeTicks = new IntRange(5000, 7500);
    public CompProperties_GooMortar()
    {
        compClass = typeof(CompGooMortar);
    }
}
