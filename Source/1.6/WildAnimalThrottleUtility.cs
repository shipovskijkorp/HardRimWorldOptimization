using RimWorld;
using Verse;

namespace MyRimWorldMod
{
    public static class WildAnimalThrottleUtility
    {
        public static bool ShouldThrottleBase(Pawn p)
        {
            if (p == null || p.Dead || !p.Spawned) return false;
            if (!p.RaceProps.Animal) return false;
            if (p.Faction != null) return false;              // only factionless (wild)
            if (p.Downed) return false;
            if (p.InContainerEnclosed) return false;
            if (p.questTags != null && p.questTags.Count > 0) return false; // quest-tagged pawns: never touch
            return true;
        }

        public static bool IsAggressiveNow(Pawn p)
        {
            if (p.InMentalState) return true;

            var ms = p.mindState;
            if (ms != null && (ms.enemyTarget != null || ms.meleeThreat != null))
                return true;

            var job = p.jobs?.curJob;
            if (job != null && (job.playerForced || job.def?.alwaysShowWeapon == true))
                return true;

            return false;
        }

        public static bool IsPredator(Pawn p)
        {
            return p?.RaceProps?.predator == true;
        }

        // NOTE: called only when a full tick is about to be allowed (to keep per-tick overhead tiny).
        public static bool IsNearColonists(Pawn animal, int radius)
        {
            if (animal?.Map == null) return false;
            if (radius <= 0) return false;

            var pawns = animal.Map.mapPawns?.FreeColonistsSpawned;
            if (pawns == null || pawns.Count == 0) return false;

            var pos = animal.Position;
            int r2 = radius * radius;

            for (int i = 0; i < pawns.Count; i++)
            {
                var c = pawns[i];
                if (c == null || c.Dead || !c.Spawned) continue;

                int dx = c.Position.x - pos.x;
                int dz = c.Position.z - pos.z;
                if (dx * dx + dz * dz <= r2)
                    return true;
            }

            return false;
        }
    }
}
