using System.Collections.Generic;
using Verse;

namespace Grey_Goo.Buildings;

public interface IMapCellProtector
{
    public abstract IEnumerable<IntVec3> CellsInRadius(Map map);
}
