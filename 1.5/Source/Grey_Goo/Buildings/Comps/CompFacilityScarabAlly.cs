using System.Text;
using RimWorld;
using Verse;

namespace Grey_Goo.Buildings.Comps;

public class CompFacilityScarabAlly : CompFacility
{
    public bool AllyCheckActive = false;

    public override bool CanBeActive => base.CanBeActive && (!AllyCheckActive || Find.FactionManager.OfPlayer.RelationKindWith(Faction.OfEmpire) == FactionRelationKind.Ally);

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref AllyCheckActive, "AllyCheckActive", false);
    }

    public override string CompInspectStringExtra()
    {
        StringBuilder sb = new(base.CompInspectStringExtra());
        if (AllyCheckActive)
        {
            if (Find.FactionManager.OfPlayer.RelationKindWith(Faction.OfEmpire) == FactionRelationKind.Ally)
            {
                sb.Append("MSS_GG_ScarabDBAccessGranted".Translate());
            }
            else
            {
                sb.Append("MSS_GG_ScarabDBAccessDenied".Translate());
            }
        }
        else
        {
            sb.Append("MSS_GG_ScarabDBAccessHacked".Translate());
        }

        return sb.ToString();
    }

    public override string TransformLabel(string label)
    {
        if (AllyCheckActive)
        {
            if (Find.FactionManager.OfPlayer.RelationKindWith(Faction.OfEmpire) == FactionRelationKind.Ally)
            {
                return label;
            }

            return label + " (Disabled)";
        }

        return label + " (Hacked)";
    }
}
