using RimWorld;
using Verse;

namespace Grey_Goo;

[DefOf]
public static class Grey_GooDefOf
{
    public static readonly GGTerrainDef GG_Goo;
    public static readonly TerrainDef GG_Goo_Inactive;
    public static readonly DamageDef GG_Goo_Burn;
    public static readonly DamageDef MSS_GG_GooMortarBurn;
    public static readonly DamageDef GG_Goo_GooShieldBurn;
    public static readonly FactionDef GG_GreyGoo;
    public static readonly SitePartDef MSS_GG_GooControllerSitePart;
    public static readonly WorldObjectDef MSS_GG_GooControllerWorldDef;
    public static readonly ThingDef MSS_GG_Goo_Mortar;
    public static readonly ThingDef MSS_GG_ArchotechPowerNode;
    public static readonly ThingDef MSS_GG_GooWaders;
    public static readonly GameConditionDef MSS_GG_GooBoosted;
    public static readonly ThingDef MSS_GG_Frogge;

    static Grey_GooDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(Grey_GooDefOf));
}
