using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Keyz_Misc_Resources;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Grey_Goo;

public class WorldLayer_GreyGoo : WorldLayer
{
    private static readonly Color DefaultTileColor = Color.white;
    private static readonly Color BordersUnpollutedTileColor = new Color(1f, 1f, 1f, 0.4f);
    private static List<int> tmpChangedNeighbours = new List<int>();

    public readonly IEnumerable<int> nums = Enumerable.Repeat(1, 20).Select((tr, ti) => tr + ti).ToList().AsReadOnly();
    private Queue<int> regionsToRegenerate = new Queue<int>();
    private Dictionary<int, List<LayerSubMesh>> subMeshesByRegion = new Dictionary<int, List<LayerSubMesh>>();
    private HashSet<Vector3> tmpBordersUnpollutedVerts = new HashSet<Vector3>();

    private List<int> tmpNeighbors = new List<int>();
    private List<Vector3> tmpVerts = new List<Vector3>();
    private List<Vector3> verts = new List<Vector3>();


    public static GGWorldComponent ggWorldComponent => Find.World.GetComponent<GGWorldComponent>();
    public List<Lazy<Material>> Materials => nums.Select(i => new Lazy<Material>(() => MaterialPool.MatFrom($"World/GG_Goo_{i}", Shaders.LiquidMetal, 3511))).ToList();

    private static int GetRegionIdForTile(int tileId) => Mathf.FloorToInt(tileId / 500f);

    public List<LayerSubMesh> GetSubMeshesForRegion(int regionId)
    {
        if (!subMeshesByRegion.ContainsKey(regionId))
        {
            subMeshesByRegion[regionId] = new List<LayerSubMesh>();
        }

        return subMeshesByRegion[regionId];
    }

    public LayerSubMesh GetSubMeshForMaterialAndRegion(Material material, int regionId)
    {
        List<LayerSubMesh> subMeshesForRegion = GetSubMeshesForRegion(regionId);
        foreach (LayerSubMesh t in subMeshesForRegion)
        {
            if (t.material == material)
            {
                return t;
            }
        }

        Mesh mesh = new Mesh();
        LayerSubMesh materialAndRegion = new LayerSubMesh(mesh, material);
        subMeshesForRegion.Add(materialAndRegion);
        subMeshes.Add(materialAndRegion);
        return materialAndRegion;
    }

    private void RegenerateRegion(int regionId)
    {
        List<LayerSubMesh> subMeshesForRegion = GetSubMeshesForRegion(regionId);
        foreach (LayerSubMesh t in subMeshesForRegion)
        {
            t.Clear(MeshParts.All);
        }

        int regionStartIdx = regionId * 500;
        int regionEndIdx = regionStartIdx + 500;
        for (int index = regionStartIdx; index < regionEndIdx && Find.World.grid.InBounds(index); ++index)
        {
            TryAddMeshForTile(index);
        }

        foreach (LayerSubMesh t in subMeshesForRegion)
        {
            if (t.verts.Count > 0)
            {
                t.FinalizeMesh(MeshParts.All);
            }
        }
    }

    public override IEnumerable Regenerate()
    {
        WorldLayer_GreyGoo wlGreyGoo = this;


        foreach (object obj in base.Regenerate())
        {
            yield return obj;
        }

        int tilesCount = Find.WorldGrid.TilesCount;
        int gooMeshPrinted = 0;
        wlGreyGoo.verts.Clear();
        wlGreyGoo.subMeshesByRegion.Clear();
        wlGreyGoo.regionsToRegenerate.Clear();
        for (int i = 0; i < tilesCount; ++i)
        {
            if (wlGreyGoo.TryAddMeshForTile(i))
            {
                ++gooMeshPrinted;
                if (gooMeshPrinted % 1000 == 0)
                {
                    yield return null;
                }
            }
        }

        wlGreyGoo.FinalizeMesh(MeshParts.All);
    }

    public bool TryAddMeshForTile(int tileId)
    {
        Material tileMat = GetMaterialForTile(tileId);

        if (tileMat == null)
        {
            return false;
        }

        int regionIdForTile = GetRegionIdForTile(tileId);
        LayerSubMesh materialAndRegion = GetSubMeshForMaterialAndRegion(tileMat, regionIdForTile);
        Find.WorldGrid.GetTileVertices(tileId, verts);
        Find.WorldGrid.GetTileNeighbors(tileId, tmpNeighbors);
        int numVerts = materialAndRegion.verts.Count;
        tmpBordersUnpollutedVerts.Clear();
        tmpVerts.Clear();
        foreach (int t in tmpNeighbors)
        {
            if (ggWorldComponent.GetTileGooLevelAt(tileId) >= 0.33f)
            {
                continue;
            }

            Vector3 center = Find.WorldGrid.GetTileCenter(t);
            tmpVerts.AddRange(verts);
            tmpVerts.SortBy(v => Vector2.Distance(center, v));
            for (int index2 = 0; index2 < 2; ++index2)
            {
                tmpBordersUnpollutedVerts.Add(tmpVerts[index2]);
            }
        }

        for (int i = 0; i < verts.Count; ++i)
        {
            Vector3 vector3 = verts[i] + verts[i].normalized * 0.012f;
            materialAndRegion.verts.Add(vector3);
            materialAndRegion.uvs.Add(vector3 * 0.1f);
            Color color = tmpBordersUnpollutedVerts.Contains(verts[i]) ? BordersUnpollutedTileColor : DefaultTileColor;
            materialAndRegion.colors.Add(color);
            if (i >= verts.Count - 2)
            {
                continue;
            }

            materialAndRegion.tris.Add(numVerts + i + 2);
            materialAndRegion.tris.Add(numVerts + i + 1);
            materialAndRegion.tris.Add(numVerts);
        }

        tmpBordersUnpollutedVerts.Clear();
        tmpVerts.Clear();
        return true;
    }

    public Material GetMaterialForTile(int tileId)
    {
        float gooLevel = ggWorldComponent.GetTileGooLevelAt(tileId);

        if (Mathf.Approximately(gooLevel, 0))
        {
            return null;
        }

        int idx = Mathf.FloorToInt(gooLevel * 20);
        idx = Math.Max(idx - 1, 0);

        return Materials[idx].Value;
    }

    public void Notify_TileGooChanged(int tileId)
    {
        int regionIdForTile1 = GetRegionIdForTile(tileId);
        if (!regionsToRegenerate.Contains(regionIdForTile1))
        {
            regionsToRegenerate.Enqueue(regionIdForTile1);
        }

        Find.WorldGrid.GetTileNeighbors(tileId, tmpChangedNeighbours);
        foreach (int t in tmpChangedNeighbours)
        {
            int regionIdForTile2 = GetRegionIdForTile(t);
            if (!regionsToRegenerate.Contains(regionIdForTile2))
            {
                regionsToRegenerate.Enqueue(regionIdForTile2);
            }
        }

        tmpChangedNeighbours.Clear();
    }

    public override void Render()
    {
        if (regionsToRegenerate.Count > 0)
        {
            RegenerateRegion(regionsToRegenerate.Dequeue());
        }

        base.Render();
    }
}
