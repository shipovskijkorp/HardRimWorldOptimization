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
            if (p == null) return false;

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

        /// <summary>
        /// True if pawn is moving or currently running any job.
        /// Skipping Pawn.Tick() while moving/doing a job can freeze jobdriver/pather progression.
        /// </summary>
        public static bool IsBusyOrMoving(Pawn p)
        {
            if (p == null) return false;

            if (p.pather != null && p.pather.Moving)
                return true;

            // If it has any active job, it's safer not to hard-throttle the whole Tick().
            if (p.jobs?.curJob != null)
                return true;

            return false;
        }

        /// <summary>
        /// "Critical" hunger: below this, reduce throttling heavily (e.g. force short intervals).
        /// </summary>
        public static bool IsHungerCritical(Pawn p)
        {
            var food = p?.needs?.food;
            if (food == null) return false;

            float pct = food.CurLevelPercentage;
            return pct < 0.35f;
        }

        /// <summary>
        /// "Emergency" hunger: below this, do not throttle at all.
        /// </summary>
        public static bool IsHungerEmergency(Pawn p)
        {
            var food = p?.needs?.food;
            if (food == null) return false;

            float pct = food.CurLevelPercentage;
            return pct < 0.20f;
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
