using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Grey_Goo;

public class GGWorldComponent(World world) : WorldComponent(world)
{
    public Dictionary<int, float> TileGooLevel = new Dictionary<int, float>();

    public List<GreyGooController> controllers = new List<GreyGooController>();

    public bool HasAlreadyStarted = false;

    public bool GooEnabled => Grey_GooMod.settings.EnableGoo;

    public void Setup()
    {
        foreach (int idx in Enumerable.Range(0, Find.World.grid.tiles.Count - 1).Except(TileGooLevel.Keys))
        {
            TileGooLevel[idx] = 0;
        }
    }

    public bool TrySpawnController(IncidentParms parms, int locationTile = -1, bool isDebug = false)
    {
        if(!GooEnabled) return false;
        if(!CanCreateNewController(isDebug)) return false;

        if (locationTile < 0)
        {
            for (int i = 0; i < 100; i++)
            {
                Tile tile = Find.World.grid.tiles.RandomElement();
                locationTile = Find.World.grid.tiles.IndexOf(tile);
                if (tile.WaterCovered) continue;
                if (Find.World.worldObjects.AnyWorldObjectAt(locationTile)) continue;

                break;
            }
        }

        Site wo = (Site) WorldObjectMaker.MakeWorldObject(Grey_GooDefOf.MSS_GG_GooControllerWorldDef);
        Faction fac = Find.FactionManager.FirstFactionOfDef(Grey_GooDefOf.GG_GreyGoo);
        wo.SetFaction(fac);
        wo.Tile = locationTile;
        wo.customLabel = "GG_ActiveGreyGoo".Translate().Colorize(Color.red);
        wo.AddPart(new SitePart(wo, Grey_GooDefOf.MSS_GG_GooControllerSitePart, new SitePartParams()));
        Find.WorldObjects.Add(wo);

        GreyGooController controller = new GreyGooController(wo);
        controller.Setup();

        controllers.Add(controller);

        return true;
    }

    public override void WorldComponentTick()
    {
        if (!HasAlreadyStarted && Current.ProgramState == ProgramState.Playing)
        {
            HasAlreadyStarted = true;
            Setup();
        }

        if (Find.TickManager.TicksGame % 60 == 0)
        {
            foreach (GreyGooController greyGooController in controllers)
            {
                greyGooController.Tick();
            }
        }

        if (Find.TickManager.TicksGame % 300 == 0)
        {
            foreach (GreyGooController greyGooController in controllers)
            {
                greyGooController.LongTick();
            }
        }
    }

    [CanBeNull]
    public GreyGooController ClosestController(int tile)
    {
        float distance = float.MaxValue;
        GreyGooController closest = null;

        foreach (GreyGooController greyGooController in controllers)
        {
            float dist = Find.World.grid.ApproxDistanceInTiles(Find.World.grid.tiles.IndexOf(greyGooController.tile), tile);

            if (dist < distance)
            {
                distance = dist;
                closest = greyGooController;
            }
        }

        return closest;
    }

    public Direction8Way GetDirection8WayToNearestController(int tile)
    {
        if (controllers.Count == 0) return Direction8Way.Invalid;
        GreyGooController closest = ClosestController(tile);
        if(closest == null) return Direction8Way.Invalid;

        int controllerIdx = Find.World.grid.tiles.IndexOf(closest.tile);

        return Find.World.grid.GetDirection8WayFromTo(tile, controllerIdx);
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref HasAlreadyStarted, "HasAlreadyStarted");
        Scribe_Collections.Look(ref TileGooLevel, "pollutedTiles", LookMode.Value);
        Scribe_Collections.Look(ref controllers, "controllers", LookMode.Deep);
    }

    public void GooifyTileAt(int tile, float level = 0.1f)
    {
        if (!TileGooLevel.ContainsKey(tile))
            TileGooLevel[tile] = 0;
        TileGooLevel[tile] = Mathf.Clamp01(TileGooLevel[tile]+ level);

        GGUtils.NotifyGooChanged(tile);
    }

    public float GetTileGooLevelAt(int tile)
    {
        return TileGooLevel.TryGetValue(tile, out float value) ? value : 0f;
    }

    public bool CanCreateNewController(bool isDebug = false)
    {
        if (isDebug) return true;
        return GooEnabled && controllers.Count < 3;
    }
}
