using System;
using Verse;

namespace Grey_Goo;

public static class TerrainGridExtensions
{
    public static float ChanceToSpreadModifier(this TerrainGrid grid, IntVec3 c)
    {
        if (!Grey_GooDefOf.GG_Goo.reduceChanceOfPlacingOnTerrain.NullOrEmpty() &&
            Grey_GooDefOf.GG_Goo.reduceChanceOfPlacingOnTerrain.Contains(grid.TerrainAt(c))) return 0.001f;

        return 1f;
    }

    public static bool TryGooTerrain(this TerrainGrid grid, IntVec3 c)
    {
        try
        {
            if (!Grey_GooMod.settings.EnableGoo) return false;

            grid.SetTerrain(c, Grey_GooDefOf.GG_Goo);
            return true;
        }
        catch (ArgumentOutOfRangeException e)
        {
            ModLog.Debug($"Exception occured while trying goo terrain: {e}");
            return false;
        }
        catch (NullReferenceException e)
        {
            ModLog.Debug($"Exception occured while trying goo terrain: {e}");
            return false;
        }
    }

    public static bool TryDeactivateGooTerrain(this TerrainGrid grid, IntVec3 c)
    {
        if (grid.TerrainAt(c) != Grey_GooDefOf.GG_Goo) return false;

        grid.SetTerrain(c, Grey_GooDefOf.GG_Goo_Inactive);
        return true;
    }
}
