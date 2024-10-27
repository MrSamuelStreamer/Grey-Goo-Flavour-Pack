using System.Linq;
using Verse;

namespace Grey_Goo;

public class PlaceWorker_ReclaimedGoo : PlaceWorker
{
    public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
    {
        bool isInactiveGoo = map.terrainGrid.TerrainAt(loc) == Grey_GooDefOf.GG_Goo_Inactive;
        bool isBlocked = map.thingGrid.ThingsAt(loc).Any(t=>t.def.passability == Traversability.Impassable);

        if (!isInactiveGoo || isBlocked)
            return "MSS_GG_MustBeInactiveGoo".Translate();

        return true;
    }
}
