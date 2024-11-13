using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Grey_Goo;

[StaticConstructorOnStartup]
public static class GGUtils
{
    public static readonly Texture2D MSS_GG_GooPlusOne = ContentFinder<Texture2D>.Get("UI/MSS_GG_GooPlusOne");
    public static readonly Texture2D MSS_GG_GooPlusTen = ContentFinder<Texture2D>.Get("UI/MSS_GG_GooPlusTen");
    public static readonly Texture2D MSS_GG_GooMax = ContentFinder<Texture2D>.Get("UI/MSS_GG_GooMax");
    public static readonly Texture2D MSS_GG_GooMinusOne = ContentFinder<Texture2D>.Get("UI/MSS_GG_GooMinusOne");
    public static readonly Texture2D MSS_GG_GooMinusTen = ContentFinder<Texture2D>.Get("UI/MSS_GG_GooMinusTen");
    public static readonly Texture2D MSS_GG_GooMin = ContentFinder<Texture2D>.Get("UI/MSS_GG_GooMin");
    public static readonly Texture2D GG_Faction = ContentFinder<Texture2D>.Get("World/GG_Faction");

    public static readonly Lazy<FieldInfo> worldRender_layers = new Lazy<FieldInfo>(() => AccessTools.Field(typeof(WorldRenderer), "layers"));

    public static void NotifyGooChanged(int tile)
    {
        if (worldRender_layers.Value.GetValue(Find.World.renderer) is not List<WorldLayer> layers)
        {
            return;
        }

        WorldLayer_GreyGoo gg = layers.First(wl => wl is WorldLayer_GreyGoo) as WorldLayer_GreyGoo;

        gg?.Notify_TileGooChanged(tile);
    }

    [DebugActionYielder]
    private static IEnumerable<DebugActionNode> IncidentsYielder()
    {
        int tile = Find.WorldSelector.selectedTile;

        if (Grey_GooMod.settings.EnableGoo && tile > -1)
        {
            ModLog.Log("Showing goo gizmos");
            DebugActionNode plusOne = new DebugActionNode();
            plusOne.category = "GreyGoo";
            plusOne.label = "MSS_GG_IncreaseGooOnePercent".Translate();
            plusOne.action = GooChangeAction(0.01f, tile);
            yield return plusOne;


            DebugActionNode plusTen = new DebugActionNode();
            plusTen.label = "MSS_GG_IncreaseGooTenPercent".Translate();
            plusTen.action = GooChangeAction(0.1f, tile);
            yield return plusTen;


            DebugActionNode max = new DebugActionNode();
            max.label = "MSS_GG_MaxGoo".Translate();
            max.action = GooChangeAction(1f, tile);
            yield return max;


            DebugActionNode minusOne = new DebugActionNode();
            minusOne.label = "MSS_GG_DecreaseGooOnePercent".Translate();
            minusOne.action = GooChangeAction(-0.01f, tile);
            yield return minusOne;


            DebugActionNode minusTen = new DebugActionNode();
            minusTen.label = "MSS_GG_DecreaseGooTenPercent".Translate();
            minusTen.action = GooChangeAction(-0.1f, tile);
            yield return minusTen;


            DebugActionNode min = new DebugActionNode();
            min.label = "MSS_GG_MinGoo".Translate();
            min.action = GooChangeAction(-1f, tile);
            yield return min;

            DebugActionNode createControllerHere = new DebugActionNode();
            createControllerHere.label = "MSS_GG_CreateControllerHere".Translate();
            createControllerHere.action = delegate
            {
                Find.World.GetComponent<GGWorldComponent>().TrySpawnController(new IncidentParms(), Find.WorldSelector.selectedTile, true);
            };

            yield return createControllerHere;
        }
    }

    public static Action GooChangeAction(float change, int tile)
    {
        return delegate()
        {
            Find.World.GetComponent<GGWorldComponent>().TileGooLevel[tile] += change;
        };
    }
}
