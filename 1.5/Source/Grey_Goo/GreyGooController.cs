using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Random = UnityEngine.Random;

namespace Grey_Goo;

public class GreyGooController : IExposable
{
    public Tile _tile;

    public List<int> _tilesOrderedByDistance;

    public int NextTileToGooify = 0;
    public WorldObject wo;

    public GreyGooController() { }

    public GreyGooController(WorldObject wo)
    {
        this.wo = wo;
    }

    public int TileIdx => Find.World.grid.tiles.IndexOf(tile);

    public Tile tile
    {
        get
        {
            if (_tile == null)
            {
                _tile = Find.WorldGrid[wo.Tile];
            }

            return _tile;
        }
    }

    public List<int> tilesOrderedByDistance => _tilesOrderedByDistance ??= GetTilesOrderedByDistance();

    public static GGWorldComponent ggWorldComponent => Find.World.GetComponent<GGWorldComponent>();

    public static bool GooBoosted => Find.Maps.Any(map => map.GameConditionManager.ConditionIsActive(Grey_GooDefOf.MSS_GG_GooBoosted)) ||
                                     Find.World.GameConditionManager.ConditionIsActive(Grey_GooDefOf.MSS_GG_GooBoosted);

    public void ExposeData()
    {
        Scribe_Values.Look(ref NextTileToGooify, "NextTileToGooify");
        Scribe_Collections.Look(ref _tilesOrderedByDistance, "_tilesOrderedByDistance");
    }

    public List<int> GetTilesOrderedByDistance()
    {
        WorldGrid worldGrid = Find.World.grid;
        int tileIndex = worldGrid.tiles.IndexOf(tile);

        return worldGrid.tiles
            .Where(t => t != tile && !t.WaterCovered)
            .Select(t => new { Tile = t, Distance = worldGrid.ApproxDistanceInTiles(tileIndex, worldGrid.tiles.IndexOf(t)) })
            .OrderBy(t => t.Distance)
            .Select(t => worldGrid.tiles.IndexOf(t.Tile))
            .ToList();
    }

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
            settlement.Name = $"GG_ActiveGreyGoo".Translate().Colorize(Color.red);
        }

        ggWorldComponent.GooifyTileAt(TileIdx, 1f);
    }

    public void Tick()
    {
        if (wo == null || wo.Destroyed || wo.def == WorldObjectDefOf.DestroyedSettlement || wo.Faction == Find.FactionManager.OfPlayer)
        {
            return;
        }

        float gooIncrement = Grey_GooMod.settings.WorldMapGooIncrementPercentPerTick / 100;

        if (GooBoosted)
        {
            gooIncrement *= 100;
        }


        for (int i = 0; i < Math.Min(tilesOrderedByDistance.Count, NextTileToGooify); i++)
        {
            ggWorldComponent.GooifyTileAt(tilesOrderedByDistance[i], gooIncrement);
        }
    }

    public void LongTick()
    {
        if (NextTileToGooify >= tilesOrderedByDistance.Count)
        {
            return;
        }

        float spreadChance = Grey_GooMod.settings.GooSpreadChance;

        if (GooBoosted)
        {
            spreadChance *= 10;
        }

        if (Random.value > spreadChance / 100)
        {
            NextTileToGooify++;
        }
    }
}
