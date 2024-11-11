using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Grey_Goo;

public class SymbolResolver_Shamblers : SymbolResolver
{
    public override bool CanResolve(ResolveParams rp)
    {
        return base.CanResolve(rp) && rp.threatPoints.HasValue;
    }

    public override void Resolve(ResolveParams rp)
    {
        CellFinder.TryFindRandomCellNear(rp.rect.CenterCell, BaseGen.globalSettings.map, 25, (_)=>true, out IntVec3 end);

        Lord lord = LordMaker.MakeNewLord(
            Find.FactionManager.FirstFactionOfDef(Grey_GooDefOf.GG_GreyGoo),
            new LordJob_GooShamblerSwarm(rp.rect.CenterCell, end),
            BaseGen.globalSettings.map);

        float value = rp.threatPoints ?? 0f;

        PawnKindDef[] generatedPawns = PawnUtility.GetCombatPawnKindsForPoints((pk)=>pk == PawnKindDefOf.ShamblerSwarmer, value,
            pk => 1f / pk.combatPower).ToArray();

        float num = Math.Min(rp.rect.Width, rp.rect.Height) / 2f;

        List<CellRect> usedRects;
        if (!MapGenerator.TryGetVar("UsedRects", out usedRects))
        {
            usedRects = new List<CellRect>();
            MapGenerator.SetVar("UsedRects", usedRects);
        }

        for (int index = 0; index < generatedPawns.Length; ++index)
        {
            ResolveParams resolveParams = rp;

            float angle = 360f / generatedPawns.Length * index;
            Vector3 vect = IntVec3.North.ToVector3().RotatedBy(angle) * num;

            if (CellFinder.TryFindRandomCellNear(rp.rect.CenterCell + vect.ToIntVec3(), BaseGen.globalSettings.map, 10,
                    c => !usedRects.Any(r => r.Contains(c)), out IntVec3 result) &&
                SiteGenStepUtility.TryFindSpawnCellAroundOrNear(rp.rect, result, BaseGen.globalSettings.map, out IntVec3 spawnCell))
            {
                resolveParams.rect = CellRect.CenteredOn(spawnCell, 1, 1);
                resolveParams.singlePawnKindDef = generatedPawns[index];
                resolveParams.singlePawnLord = lord;
                resolveParams.faction = Find.FactionManager.FirstFactionOfDef(Grey_GooDefOf.GG_GreyGoo);
                BaseGen.symbolStack.Push("pawn", resolveParams);
            }
        }
    }
}
