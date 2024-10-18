using RimWorld;
using Verse;

namespace Grey_Goo;

[DefOf]
public static class Grey_GooDefOf
{
    // Remember to annotate any Defs that require a DLC as needed e.g.
    // [MayRequireBiotech]
    // public static GeneDef YourPrefix_YourGeneDefName;

    public static GG_ShaderTypeDef GG_LiquidMetal;
    public static GG_ShaderTypeDef GG_LiquidMetalSimplex;

    public static GGTerrainDef GG_Goo;

    public static FactionDef GG_GreyGoo;
    static Grey_GooDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(Grey_GooDefOf));
}
