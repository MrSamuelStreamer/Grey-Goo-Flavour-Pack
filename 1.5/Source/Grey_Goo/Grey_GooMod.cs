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

    public AssetBundle MainBundle
    {
        get
        {
            string text = "";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                text = "StandaloneOSX";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                text = "StandaloneWindows64";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                text = "StandaloneLinux64";
            }

            string bundlePath = Path.Combine(Content.RootDir, $@"Materials\Bundles\{text}\liquidmetal");
            Log.Message("Bundle Path: " + bundlePath);

            AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);

            if (bundle == null)
            {
                Log.Error("Failed to load bundle at path: " + bundlePath);
            }

            foreach (string allAssetName in bundle.GetAllAssetNames())
            {
                Log.Message($"[{allAssetName}]");
            }

            return bundle;
        }
    }

    public static void ShaderFromAssetBundle(ShaderTypeDef __instance, ref Shader ___shaderInt)
    {
        if (__instance is GG_ShaderTypeDef)
        {
            ___shaderInt = GG_Shaders.AssetBundle.LoadAsset<Shader>(__instance.shaderPath);
            if (___shaderInt is null)
            {
                Log.Error($"Failed to load Shader from path <text>\"{__instance.shaderPath}\"</text>");
            }
        }
    }
}
