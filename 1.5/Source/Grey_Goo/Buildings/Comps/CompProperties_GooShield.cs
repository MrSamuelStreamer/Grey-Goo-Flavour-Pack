using UnityEngine;
using Verse;

namespace Grey_Goo.Buildings.Comps;

public class CompProperties_GooShield : CompProperties
{

    public float radius;
    public int disarmedByEmpForTicks;
    public bool drawWithNoSelection;
    public float minAlpha;
    public float idlePulseSpeed = 0.7f;
    public float minIdleAlpha = -1.7f;
    public Color color = Color.white;
    public SoundDef activeSound;

    public CompProperties_GooShield()
    {
        compClass = typeof (CompGooShield);
    }
}
