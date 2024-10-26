using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Grey_Goo;

public class GeneRemovalRecipeModExtension : DefModExtension
{
    public GeneDef geneToRemove;
}

public class GeneRemovalRecipeWorker: RecipeWorker
{
    public GeneDef geneToRemove => recipe.GetModExtension<GeneRemovalRecipeModExtension>()?.geneToRemove;

    public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
    {
        if(thing is not Pawn pawn || pawn.genes == null) return false;

        return geneToRemove != null && pawn.genes.HasActiveGene(geneToRemove);
    }

    public override void ApplyOnPawn(
        Pawn pawn,
        BodyPartRecord part,
        Pawn billDoer,
        List<Thing> ingredients,
        Bill bill)
    {
        Gene gene = pawn.genes?.GetGene(geneToRemove);
        pawn.genes?.RemoveGene(gene);
    }
}
