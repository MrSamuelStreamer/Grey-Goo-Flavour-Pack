using Verse;

namespace Grey_Goo;

public class Projectile_EMP : Projectile_Explosive
{
    protected override void Explode()
    {
        Map map = Map;
        ThingDef Def = def;

        Destroy(DestroyMode.Vanish);
        if (Def.projectile.explosionEffect != null)
        {
            Effecter eff = Def.projectile.explosionEffect.Spawn();
            if (Def.projectile.explosionEffectLifetimeTicks != 0)
            {
                Map.effecterMaintainer.AddEffecterToMaintain(eff, Position.ToVector3().ToIntVec3(), Def.projectile.explosionEffectLifetimeTicks);
            }
            else
            {
                eff.Trigger(new TargetInfo(Position, map), new TargetInfo(Position, map));
                eff.Cleanup();
            }
        }

        IntVec3 position = Position;
        double explosionRadius = Def.projectile.explosionRadius;

        if (Def.HasModExtension<ProjectileModExtension>())
        {
            explosionRadius *= Def.GetModExtension<ProjectileModExtension>().explosionSizeMultiplier.RandomInRange;
        }

        GenExplosion.DoExplosion(position, map, (float) explosionRadius, Def.projectile.damageDef, launcher, DamageAmount, ArmorPenetration, Def.projectile.soundExplode,
            equipmentDef, Def, intendedTarget.Thing, Def.projectile.postExplosionSpawnThingDef ?? Def.projectile.filth, Def.projectile.postExplosionSpawnChance,
            Def.projectile.postExplosionSpawnThingCount, Def.projectile.postExplosionGasType,
            Def.projectile.applyDamageToExplosionCellsNeighbors, Def.projectile.preExplosionSpawnThingDef, Def.projectile.preExplosionSpawnChance,
            Def.projectile.preExplosionSpawnThingCount, Def.projectile.explosionChanceToStartFire,
            Def.projectile.explosionDamageFalloff, origin.AngleToFlat(destination), affectedAngle: new FloatRange?(), doVisualEffects: Def.projectile.doExplosionVFX,
            propagationSpeed: Def.projectile.damageDef.expolosionPropagationSpeed, postExplosionSpawnThingDefWater: Def.projectile.postExplosionSpawnThingDefWater,
            screenShakeFactor: Def.projectile.screenShakeFactor);
    }
}
