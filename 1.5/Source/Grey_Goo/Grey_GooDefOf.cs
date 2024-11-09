using RimWorld;
using Verse;

namespace Grey_Goo;

[DefOf]
public static class Grey_GooDefOf
{
    public static GGTerrainDef GG_Goo;
    public static TerrainDef GG_Goo_Inactive;

    public static DamageDef GG_Goo_Burn;
    public static DamageDef MSS_GG_GooMortarBurn;
    public static DamageDef GG_Goo_GooShieldBurn;

    public static FactionDef GG_GreyGoo;

    public static SitePartDef MSS_GG_GooControllerSitePart;

    public static WorldObjectDef MSS_GG_GooControllerWorldDef;

    public static ThingDef MSS_GG_Goo_Mortar;

    static Grey_GooDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(Grey_GooDefOf));
}
