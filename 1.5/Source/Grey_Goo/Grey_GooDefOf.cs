using RimWorld;
using Verse;

namespace Grey_Goo;

[DefOf]
public static class Grey_GooDefOf
{
    public static GGTerrainDef GG_Goo;
    public static TerrainDef GG_Goo_Inactive;

    public static DamageDef GG_Goo_Burn;
    public static DamageDef GG_Goo_GooShieldBurn;

    public static FactionDef GG_GreyGoo;

    static Grey_GooDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(Grey_GooDefOf));
}
