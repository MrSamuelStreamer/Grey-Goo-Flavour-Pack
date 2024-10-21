using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace Grey_Goo;

[StaticConstructorOnStartup]
public static class GG_Fonts
{
    private static Dictionary<string, Font> lookupFonts;

    public static readonly Font OptimusPrinceps = LoadFont(Path.Combine("Assets", "Fonts", "OptimusPrinceps.ttf"));
    public static readonly Font OptimusPrincepsSemiBold = LoadFont(Path.Combine("Assets", "Fonts", "OptimusPrinceps.ttf"));


    public static Font LoadFont(string fontName)
    {
        lookupFonts ??= new Dictionary<string, Font>();
        if (!lookupFonts.ContainsKey(fontName))
        {
            Log.Message($"lookupFonts: {lookupFonts.ToList().Count}");
            lookupFonts[fontName] = GG_Shaders.AssetBundle.LoadAsset<Font>(fontName);
        }

        Font font = lookupFonts[fontName];
        if (font == null)
        {
            Log.Warning($"Could not load font: {fontName}");
            return (UnityEngine.Font) Resources.Load("Fonts/Arial_medium");
        }

        if (font != null)
        {
            Log.Message($"Loaded font: {lookupFonts.Count}");
        }

        return font;
    }
}
