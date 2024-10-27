using System.IO;
using System.Runtime.InteropServices;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace Grey_Goo;

public class Grey_GooMod : Mod
{
    public static Settings settings;

    public static Grey_GooMod mod;

    public Grey_GooMod(ModContentPack content) : base(content)
    {
        Log.Message("Hello world from Grey Goo");
        mod = this;

        // initialize settings
        settings = GetSettings<Settings>();
#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("mss.rimworld.Grey_Goo.main");
        harmony.PatchAll();
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
