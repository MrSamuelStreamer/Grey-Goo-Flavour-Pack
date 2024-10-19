using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Grey_Goo;

public class GreyGooController: IExposable
{
    public WorldObject wo;

    public int TileIdx => Find.World.grid.tiles.IndexOf(tile);
    public Tile _tile;
    public Tile tile{
        get{
            if (_tile == null) {
                _tile = Find.WorldGrid[wo.Tile];
            }
            return _tile;
        }
    }

    public GreyGooController(){}

    public GreyGooController(WorldObject wo)
    {
        this.wo = wo;
    }

    public List<int> _tilesOrderedByDistance;

    public List<int> tilesOrderedByDistance => _tilesOrderedByDistance ??= Find.World.grid.tiles.Where(t=>t!=tile).Where(t => !t.WaterCovered)
                .Select(t => (t, Find.World.grid.ApproxDistanceInTiles(Find.World.grid.tiles.IndexOf(tile), Find.World.grid.tiles.IndexOf(t))))
                .OrderBy(t => t.Item2)
                .Select(t => Find.World.grid.tiles.IndexOf(t.Item1)).ToList();

    public GGWorldComponent ggWorldComponent => Find.World.GetComponent<GGWorldComponent>();

    public int NextTileToGooify = 0;

    public void SetParent(WorldObject parent)
    {
        wo = parent;
    }

    public void Setup()
    {

        Find.LetterStack.ReceiveLetter(
            string.Format("GG_ControllerStartTitle".Translate()),
            string.Format("GG_ControllerStartDesc".Translate(), wo.Label.Colorize(wo.Faction.Color)),
            LetterDefOf.ThreatBig,
            new LookTargets(wo)
        );

        if (wo is Settlement settlement)
        {
            settlement.Name = $"{settlement.Name} GG_ActiveGreyGoo".Translate().Colorize(Color.red);
        }

        ggWorldComponent.GooifyTileAt(TileIdx, 1f);
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref NextTileToGooify, "NextTileToGooify");
        Scribe_Collections.Look(ref _tilesOrderedByDistance, "_tilesOrderedByDistance");
    }

    public void Tick()
    {
        if (wo == null || wo.Destroyed || wo.def == WorldObjectDefOf.DestroyedSettlement || wo.Faction == Find.FactionManager.OfPlayer)
        {
            return;
        }

        for (int i = 0; i < Math.Min(tilesOrderedByDistance.Count, NextTileToGooify); i++)
        {
            ggWorldComponent.GooifyTileAt(tilesOrderedByDistance[i], Grey_GooMod.settings.GooSpreadIncrement);
        }
    }

    public void LongTick()
    {
        if(NextTileToGooify >= tilesOrderedByDistance.Count) return;

        int tileIdx = tilesOrderedByDistance[NextTileToGooify];
        float distance = Find.World.grid.ApproxDistanceInTiles(tileIdx, Find.World.grid.tiles.IndexOf(tile));
        float hexCircumference = Mathf.FloorToInt(distance * 6);
        float scaled = Math.Max(1, hexCircumference) * Grey_GooMod.settings.GooSpreadScale;

        NextTileToGooify += Math.Min(Mathf.CeilToInt(scaled), tilesOrderedByDistance.Count);
    }
}
