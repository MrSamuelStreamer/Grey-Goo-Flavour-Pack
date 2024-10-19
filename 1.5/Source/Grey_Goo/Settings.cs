using UnityEngine;
using Verse;

namespace Grey_Goo;

public class Settings : ModSettings
{
    //Use Mod.settings.setting to refer to this setting.
    public bool EnableGoo = false;
    public float GooSpreadIncrement = 0.0001f;
    public float GooSpreadScale = 0.01f;
    public int GooDamageTickFrequency = 3200;

    public float ChanceToMerge = 0.015f;
    public int ShamblerMergeHediffSeverityToTransform = 10;

    public void DoWindowContents(Rect wrect)
    {
        var options = new Listing_Standard();
        options.Begin(wrect);

        GooSpreadIncrement = Widgets.HorizontalSlider(options.GetRect(40f), GooSpreadIncrement, 0f, 0.01f, label: "MSS_GreyGoo_GooSpreadIncrement".Translate(GooSpreadIncrement.ToString("0.0000")));
        options.Gap();

        GooSpreadScale = Widgets.HorizontalSlider(options.GetRect(40f), GooSpreadScale, 0f, 1f, label: "MSS_GreyGoo_GooSpreadScale".Translate(GooSpreadScale.ToString("0.000")));
        options.Gap();

        options.Label("MSS_GG_Settings_GooDamageTickFrequency".Translate(GooDamageTickFrequency));
        options.IntAdjuster(ref GooDamageTickFrequency, 60);

        options.Gap();

        options.CheckboxLabeled("MSS_GreyGoo_EnableGoo".Translate(), ref EnableGoo);
        options.Gap();

        options.CheckboxLabeled("MSS_GreyGoo_EnableGoo".Translate(), ref EnableGoo);
        options.Gap();

        options.Label("MSS_GG_Settings_ChanceToMerge".Translate((ChanceToMerge * 100f).ToString("0.0000")));
        ChanceToMerge = options.Slider( ChanceToMerge, 0.0001f, 1f);

        options.Label("MSS_GG_Settings_ShamblerMergeHediffSeverityToTransform".Translate(ShamblerMergeHediffSeverityToTransform));
        options.IntAdjuster(ref ShamblerMergeHediffSeverityToTransform, 1);

        options.Gap();

        options.End();
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref EnableGoo, "EnableGoo", false);
        Scribe_Values.Look(ref GooSpreadIncrement, "GooSpreadIncrement", 0.001f);
        Scribe_Values.Look(ref GooSpreadScale, "GooSpreadScale", 1f);
        Scribe_Values.Look(ref GooDamageTickFrequency, "GooDamageTickFrequency", 3200);
        Scribe_Values.Look(ref ChanceToMerge, "ChanceToMerge", 0.015f);
        Scribe_Values.Look(ref ShamblerMergeHediffSeverityToTransform, "ShamblerMergeHediffSeverityToTransform", 10);
    }
}
