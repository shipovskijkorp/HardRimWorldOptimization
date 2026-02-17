using RimWorld;
using Verse;
using Verse.AI;

namespace MyRimWorldMod
{
    internal static class TurretOptimizerUtility
    {
        public static bool IsPlayerTurret(Building_TurretGun turret)
        {
            return turret != null && turret.Faction == Faction.OfPlayer;
        }

        public static bool IsDangerPresent(Map map)
        {
            if (map == null) return true; // safer default
            var comp = map.GetComponent<MapComponent_TurretOptimizer>();
            return comp != null ? comp.dangerPresent : true;
        }

        public static bool IsTargetStillValid(Building_TurretGun turret, LocalTargetInfo target)
        {
            if (turret == null || !target.IsValid || !target.HasThing)
                return false;

            Thing thing = target.Thing;
            if (thing == null || thing.Destroyed || !thing.Spawned)
                return false;

            Map map = turret.Map;
            if (map == null || thing.Map != map)
                return false;

            Verb attackVerb = ((Building_Turret)turret).AttackVerb;
            if (attackVerb == null || !attackVerb.Available() || !attackVerb.CanHitTargetFrom(turret.Position, target))
                return false;

            Faction faction = turret.Faction;
            if (faction != null)
            {
                var atk = thing as IAttackTarget;
                if (atk != null)
                {
                    if (!GenHostility.IsActiveThreatTo(atk, faction, true, false))
                        return false;
                }
                else if (!GenHostility.HostileTo(thing, faction))
                {
                    return false;
                }
            }

            // avoid incendiary spam on burning targets
            if (VerbUtility.IsIncendiary_Ranged(attackVerb) && FireUtility.IsBurning(thing))
                return false;

            // avoid overhead projectiles into thick roof targets
            if ((VerbUtility.ProjectileFliesOverhead(attackVerb) || (turret.def?.building?.IsMortar ?? false))
                && GridsUtility.Roofed(thing.Position, map))
            {
                RoofDef roof = GridsUtility.GetRoof(thing.Position, map);
                if (roof != null && roof.isThickRoof)
                    return false;
            }

            return true;
        }
    }
}
