using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Grey_Goo;

public class Projectile_GreyGoo: Projectile_Explosive
{
    protected override void Explode()
    {
        IEnumerable<IntVec3> CellsToGo = GenAdj.CardinalDirectionsAndInside.Select(d => Position + d).Where(c => c.InBounds(Map));
        Map map = Map;
        base.Explode();
        foreach (IntVec3 cell in CellsToGo)
        {
            map.GetComponent<GreyGoo_MapComponent>()?.GooTileAt(cell);
        }
    }

}
