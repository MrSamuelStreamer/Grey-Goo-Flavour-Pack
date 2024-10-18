using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace Grey_Goo;

public static class GGUtils
{
    public static Lazy<FieldInfo> worldRender_layers = new(() => AccessTools.Field(typeof(WorldRenderer), "layers"));

    public static void NotifyGooChanged(int tile)
    {
        if (worldRender_layers.Value.GetValue(Find.World.renderer) is not List<WorldLayer> layers)
        {
            return;
        }

        WorldLayer_GreyGoo gg = layers.First(wl => wl is WorldLayer_GreyGoo) as WorldLayer_GreyGoo;

        gg?.Notify_TileGooChanged(tile);
    }

}
