using UnityEngine;
using Verse;

namespace Grey_Goo;

public class Settings : ModSettings
{
    //Use Mod.settings.setting to refer to this setting.
    public bool EnableGoo = false;
    public float WorldMapGooIncrementPercentPerTick = 0.01f;
    public float GooSpreadChance = 0.01f;
    public int MapGooReevaluateFrequency = 6000;
    public int MapGooUpdateFrequency = 600;
    public float ChanceToSpreadGooToCell = 0.01f;
    public float ChanceForGooToDamagePercent = 0.001f;
    public FloatRange GooDamageRange = new FloatRange(0f,4f);
    public bool InfectOnGooTouch = false;
    public Vector2 scrollPosition = Vector2.zero;

    public int MaxShamblersOnMap = 40;

    public float ChanceToMerge = 0.015f;
    public int ShamblerMergeHediffSeverityToTransform = 10;

    public void DoWindowContents(Rect wrect)
    {
        float scrollViewHeigh = 650f;

        Rect viewRect = new Rect(0f, 0f, wrect.width- 20, scrollViewHeigh);
        scrollPosition = GUI.BeginScrollView(new Rect(0, 50, wrect.width, wrect.height - 50), scrollPosition, viewRect);

        Listing_Standard options = new Listing_Standard();

        options.Begin(viewRect);
        try
        {
            options.CheckboxLabeled("MSS_GG_Setting_EnableGoo".Translate(), ref EnableGoo);
            options.Gap();

            WorldMapGooIncrementPercentPerTick = Widgets.HorizontalSlider(options.GetRect(40f), WorldMapGooIncrementPercentPerTick, 0f, 0.01f,
                label: "MSS_GG_Setting_GooSpreadIncrement".Translate(WorldMapGooIncrementPercentPerTick.ToString("0.000")));
            options.Gap();

            GooSpreadChance = Widgets.HorizontalSlider(options.GetRect(40f), GooSpreadChance, 0f, 10f,
                label: "MSS_GG_Setting_GooSpreadChance".Translate(GooSpreadChance.ToString("0.000")));
            options.Gap();

            options.Label("MSS_GG_Setting_MapGooUpdateFrequency".Translate(MapGooUpdateFrequency));
            options.IntAdjuster(ref MapGooUpdateFrequency, 1);

            options.Gap();

            ChanceToSpreadGooToCell = Widgets.HorizontalSlider(options.GetRect(40f), ChanceToSpreadGooToCell, 0f, 0.25f,
                label: "MSS_GG_Setting_ChanceToSpreadGooToCell".Translate(ChanceToSpreadGooToCell.ToString("0.000")));
            options.Gap();

            ChanceForGooToDamagePercent = Widgets.HorizontalSlider(options.GetRect(40f), ChanceForGooToDamagePercent, 0f, 0.1f,
                label: "MSS_GG_Setting_ChanceForGooToDamagePercent".Translate(ChanceForGooToDamagePercent.ToString("0.000")));
            options.Gap();

            Widgets.FloatRange(options.GetRect(40), 1, ref GooDamageRange, 0f, 10f, "MSS_GG_Setting_GooDamageRange");
            options.Gap();

            options.CheckboxLabeled("MSS_GG_InfectOnGooTouch".Translate(), ref InfectOnGooTouch);
            options.Gap();

            options.Label("MSS_GG_Setting_MapGooReevaluateFrequency".Translate(MapGooReevaluateFrequency));
            options.IntAdjuster(ref MapGooReevaluateFrequency, 60);

            options.Gap();

            options.Label("MSS_GG_Settings_ChanceToMerge".Translate((ChanceToMerge * 100f).ToString("0.0000")));
            ChanceToMerge = options.Slider(ChanceToMerge, 0.0001f, 1f);

            options.Label("MSS_GG_Settings_ShamblerMergeHediffSeverityToTransform".Translate(ShamblerMergeHediffSeverityToTransform));
            options.IntAdjuster(ref ShamblerMergeHediffSeverityToTransform, 1);

            options.Label("MSS_GG_Settings_MaxShamblersOnMap".Translate(MaxShamblersOnMap));
            options.IntAdjuster(ref MaxShamblersOnMap, 1);

            options.Gap();
        }
        finally
        {
            options.End();
            GUI.EndScrollView();
        }
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref EnableGoo, "EnableGoo", false);
        Scribe_Values.Look(ref WorldMapGooIncrementPercentPerTick, "WorldMapGooIncrementPercentPerTick", 0.01f);
        Scribe_Values.Look(ref GooSpreadChance, "GooSpreadChance", 1f);
        Scribe_Values.Look(ref MapGooUpdateFrequency, "MapGooUpdateFrequency", 600);
        Scribe_Values.Look(ref ChanceToSpreadGooToCell, "ChanceToSpreadGooToCell", 0.01f);
        Scribe_Values.Look(ref ChanceForGooToDamagePercent, "ChanceForGooToDamagePercent", 0.001f);
        Scribe_Values.Look(ref GooDamageRange, "GooDamageRange", new FloatRange(0f, 4f));
        Scribe_Values.Look(ref InfectOnGooTouch, "InfectOnGooTouch", false);
        Scribe_Values.Look(ref MapGooReevaluateFrequency, "MapGooReevaluateFrequency", 6000);
        Scribe_Values.Look(ref ChanceToMerge, "ChanceToMerge", 0.015f);
        Scribe_Values.Look(ref ShamblerMergeHediffSeverityToTransform, "ShamblerMergeHediffSeverityToTransform", 10);
        Scribe_Values.Look(ref MaxShamblersOnMap, "MaxShamblersOnMap", 40);
    }
}
