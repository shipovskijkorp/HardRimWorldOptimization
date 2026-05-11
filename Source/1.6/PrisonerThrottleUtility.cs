using RimWorld;
using Verse;

namespace MyRimWorldMod
{
    internal static class PrisonerThrottleUtility
    {
        public static bool ShouldThrottle(Pawn pawn, OptimizationSettings s)
        {
            if (pawn == null || s == null || !s.throttlePrisoners)
                return false;

            if (pawn.Dead || pawn.DestroyedOrNull() || !pawn.Spawned)
                return false;

            if (!pawn.IsPrisonerOfColony)
                return false;

            // don't throttle when the game expects responsiveness
            if (pawn.Downed)
                return false;

            if (pawn.InMentalState)
                return false;

            if (pawn.IsBurning())
                return false;

            // if drafted/combat (rare for prisoners but safe)
            if (pawn.Drafted)
                return false;

            // near-colonist exclusion (prevents "late reaction" to wardens, fights, etc.)
            if (s.prisonersExcludeNearColonists && pawn.Map != null)
            {
                int r = s.prisonersNearColonistRadius;
                if (r > 0 && HasColonistNearby(pawn, r))
                    return false;
            }

            // if actively moving, don't throttle (pathing & job progression)
            if (pawn.pather != null && pawn.pather.Moving)
                return false;

            // if being carried / in weird holders
            if (pawn.ParentHolder != null && pawn.ParentHolder is not Map)
                return false;

            return true;
        }

        private static bool HasColonistNearby(Pawn prisoner, int radius)
        {
            var map = prisoner.Map;
            if (map == null || radius <= 0) return false;

            // Avoid scanning every cell in a large radius every tick. The vanilla map already
            // keeps a spawned-colonist list, so this is O(colonists), not O(radius^2 cells).
            var colonists = map.mapPawns?.FreeColonistsSpawned;
            if (colonists == null || colonists.Count == 0) return false;

            IntVec3 pos = prisoner.Position;
            int r2 = radius * radius;

            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn c = colonists[i];
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
