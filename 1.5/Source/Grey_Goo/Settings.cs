using UnityEngine;
using Verse;

namespace Grey_Goo;

public class Settings : ModSettings
{
    //Use Mod.settings.setting to refer to this setting.
    public bool EnableGoo = false;
    public float GooSpreadIncrement = 0.0001f;
    public float GooSpreadScale = 0.01f;
    public int GooRecheckTickFrequency = 600;
    public int TicksToSpreadGooUpdateOver = 120;
    public float ChanceToSpreadGooToCell = 0.001f;
    public float ChanceForGooToDamage = 0.001f;
    public FloatRange GooDamageRange = new FloatRange(0f,4f);

    public float ChanceToMerge = 0.015f;
    public int ShamblerMergeHediffSeverityToTransform = 10;

    public void DoWindowContents(Rect wrect)
    {
        var options = new Listing_Standard();
        options.Begin(wrect);

        options.CheckboxLabeled("MSS_GG_Setting_EnableGoo".Translate(), ref EnableGoo);
        options.Gap();

        GooSpreadIncrement = Widgets.HorizontalSlider(options.GetRect(40f), GooSpreadIncrement, 0f, 0.01f, label: "MSS_GG_Setting_GooSpreadIncrement".Translate(GooSpreadIncrement.ToString("0.0000")));
        options.Gap();

        GooSpreadScale = Widgets.HorizontalSlider(options.GetRect(40f), GooSpreadScale, 0f, 1f, label: "MSS_GG_Setting_GooSpreadScale".Translate(GooSpreadScale.ToString("0.000")));
        options.Gap();

        options.Label("MSS_GG_Setting_TicksToSpreadGooUpdateOver".Translate(TicksToSpreadGooUpdateOver));
        options.IntAdjuster(ref TicksToSpreadGooUpdateOver, 1);

        options.Gap();

        ChanceToSpreadGooToCell = Widgets.HorizontalSlider(options.GetRect(40f), ChanceToSpreadGooToCell, 0f, 0.25f, label: "MSS_GG_Setting_ChanceToSpreadGooToCell".Translate(ChanceToSpreadGooToCell.ToString("0.000")));
        options.Gap();

        ChanceForGooToDamage = Widgets.HorizontalSlider(options.GetRect(40f), ChanceForGooToDamage, 0f, 0.1f, label: "MSS_GG_Setting_ChanceForGooToDamage".Translate(ChanceForGooToDamage.ToString("0.000")));
        options.Gap();

        Widgets.FloatRange(options.GetRect(40), 1, ref GooDamageRange, 0f, 10f, "MSS_GG_Setting_GooDamageRange");
        options.Gap();

        options.Label("MSS_GG_Setting_GooRecheckTickFrequency".Translate(GooRecheckTickFrequency));
        options.IntAdjuster(ref GooRecheckTickFrequency, 60);

        options.Gap();
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
        Scribe_Values.Look(ref TicksToSpreadGooUpdateOver, "TicksToSpreadGooUpdateOver", 120);
        Scribe_Values.Look(ref ChanceToSpreadGooToCell, "ChanceToSpreadGooToCell", 0.001f);
        Scribe_Values.Look(ref ChanceForGooToDamage, "ChanceForGooToDamage", 0.001f);
        Scribe_Values.Look(ref GooDamageRange, "GooDamageRange", new FloatRange(0f, 4f));
        Scribe_Values.Look(ref GooRecheckTickFrequency, "GooRecheckTickFrequency", 600);
        Scribe_Values.Look(ref ChanceToMerge, "ChanceToMerge", 0.015f);
        Scribe_Values.Look(ref ShamblerMergeHediffSeverityToTransform, "ShamblerMergeHediffSeverityToTransform", 10);
    }
}
