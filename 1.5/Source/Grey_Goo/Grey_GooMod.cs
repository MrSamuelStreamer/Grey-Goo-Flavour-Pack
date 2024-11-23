using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Grey_Goo;

public class Grey_GooMod : Mod
{
    public static Settings settings;

    public static Grey_GooMod mod;

    public Grey_GooMod(ModContentPack content) : base(content)
    {
        ModLog.Debug("Hello world from Grey Goo");
        mod = this;

        // initialize settings
        settings = GetSettings<Settings>();
#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("mss.rimworld.Grey_Goo.main");
        harmony.PatchAll();
    }

    private static bool UpdateCachePatch(int key, StatWorker statWorker, StatRequest req)
    {
        ModLog.Debug($"Updating cache for {statWorker.GetType().Name} with key {key} via request for {req.Thing.LabelCap}");
        return true;
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        settings.DoWindowContents(inRect);
    }

    public override string SettingsCategory()
    {
        return "MSS_GG_SettingsCategory".Translate();
    }
}
