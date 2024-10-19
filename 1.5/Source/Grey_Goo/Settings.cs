using UnityEngine;
using Verse;

namespace Grey_Goo;

public class Settings : ModSettings
{
    //Use Mod.settings.setting to refer to this setting.
    public bool EnableGoo = false;

    public float ChanceToMerge = 0.015f;
    public int ShamblerMergeHediffSeverityToTransform = 10;

    public void DoWindowContents(Rect wrect)
    {
        var options = new Listing_Standard();
        options.Begin(wrect);

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
        Scribe_Values.Look(ref ChanceToMerge, "ChanceToMerge", 0.015f);
        Scribe_Values.Look(ref ShamblerMergeHediffSeverityToTransform, "ShamblerMergeHediffSeverityToTransform", 10);
    }
}
