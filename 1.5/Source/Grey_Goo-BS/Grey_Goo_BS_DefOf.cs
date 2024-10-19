using RimWorld;
using Verse;
// ReSharper disable UnassignedField.Global

namespace Grey_Goo.BS;

[DefOf]
public static class Grey_Goo_BS_DefOf
{
    public static HediffDef MSS_GG_MergedShambler;
    public static PawnKindDef MSS_GG_ShamblerGorebeast;
    public static JobDef MSS_GG_Merge_Shamblers;

    static Grey_Goo_BS_DefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(Grey_Goo_BS_DefOf));
}
