using UnityEngine;
using Verse;

namespace Grey_Goo;

public class Settings : ModSettings
{
    //Use Mod.settings.setting to refer to this setting.
    public bool EnableGoo = false;

    public void DoWindowContents(Rect wrect)
    {
        var options = new Listing_Standard();
        options.Begin(wrect);

        options.CheckboxLabeled("MSS_GreyGoo_EnableGoo".Translate(), ref EnableGoo);
        options.Gap();

        options.End();
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref EnableGoo, "EnableGoo", false);
    }
}
