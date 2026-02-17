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
            if (map == null) return false;

            // cheap radial scan for colonists
            foreach (var cell in GenRadial.RadialCellsAround(prisoner.Position, radius, true))
            {
                if (!cell.InBounds(map)) continue;
                var thingList = cell.GetThingList(map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    if (thingList[i] is Pawn p && p.IsColonist && !p.Dead && p.Spawned)
                        return true;
                }
            }
            return false;
        }
    }
}
