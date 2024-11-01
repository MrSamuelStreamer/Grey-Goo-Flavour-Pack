using Verse;

namespace Grey_Goo;

public static class TerrainGridExtensions
{
    public static bool TryGooTerrain(this TerrainGrid grid, IntVec3 c)
    {
        if(!Grey_GooMod.settings.EnableGoo) return false;
        if (!Grey_GooDefOf.GG_Goo.preventPlacingOnTerrain.NullOrEmpty() &&
            Grey_GooDefOf.GG_Goo.preventPlacingOnTerrain.Contains(grid.TerrainAt(c))) return false;

        grid.SetTerrain(c, Grey_GooDefOf.GG_Goo);
        return true;
    }

    public static bool TryDeactivateGooTerrain(this TerrainGrid grid, IntVec3 c)
    {
        if (grid.TerrainAt(c) != Grey_GooDefOf.GG_Goo) return false;

        grid.SetTerrain(c, Grey_GooDefOf.GG_Goo_Inactive);
        return true;
    }
}
