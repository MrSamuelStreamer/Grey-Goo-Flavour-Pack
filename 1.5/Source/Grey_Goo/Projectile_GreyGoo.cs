using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Grey_Goo;

public class Projectile_GreyGoo: Projectile_Explosive
{
    protected override void Explode()
    {
        IntVec3 position = Position;
        Map map = Map;
        IEnumerable<IntVec3> CellsToGo = GenAdj.CardinalDirectionsAndInside.Select(d => position + d).Where(c => c.InBounds(map));
        base.Explode();
        foreach (IntVec3 cell in CellsToGo)
        {
            map.GetComponent<GreyGoo_MapComponent>()?.GooTileAt(cell);
        }
    }

    public override void Tick()
    {
        base.Tick();
    }
}
