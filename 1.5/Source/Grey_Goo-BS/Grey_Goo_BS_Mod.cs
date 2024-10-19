using HarmonyLib;
using Verse;

namespace Grey_Goo.BS;

public class Grey_Goo_BS_Mod : Mod
{
    public Grey_Goo_BS_Mod(ModContentPack content) : base(content)
    {
#if DEBUG
        ModLog.Log("Grey_Goo_BS_Mod");
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("Feldoh.rimworld.Grey_Goo.BS.main");
        harmony.PatchAll();
    }
}
