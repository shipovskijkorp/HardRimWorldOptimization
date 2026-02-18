using HarmonyLib;
using Verse;

namespace MyRimWorldMod
{
    [HarmonyPatch(typeof(Pawn), "Tick")]
    public static class Patch_Pawn_Tick_WildAnimalThrottle
    {
        static bool Prefix(Pawn __instance)
        {
            var settings = HardRimWorldOptimizationMod.Settings;
            if (settings == null || !settings.throttleWildAnimals)
                return true;

            Pawn p = __instance;

            if (!WildAnimalThrottleUtility.ShouldThrottleBase(p))
                return true;

            if (WildAnimalThrottleUtility.IsAggressiveNow(p))
                return true;

            if (settings.excludePredators && WildAnimalThrottleUtility.IsPredator(p))
                return true;

            if (Find.TickManager == null)
                return true;

            int now = Find.TickManager.TicksGame;
            var comp = WildAnimalThrottleComponent.Instance;
            if (comp == null)
                return true;

            // =========================
            // SAFETY GUARDS (critical)
            // =========================

            // Do not throttle if moving or executing a job
            if (WildAnimalThrottleUtility.IsBusyOrMoving(p))
                return true;

            // If hunger is emergency-level, do not throttle at all
            if (WildAnimalThrottleUtility.IsHungerEmergency(p))
                return true;

            int interval = settings.throttleIntervalTicks;

            // When skipping entire Pawn.Tick(), long intervals are dangerous.
            // Cap to safe maximum.
            if (interval < 60) interval = 60;
            if (interval > 300) interval = 300;

            // If hunger is getting low, reduce interval heavily
            if (WildAnimalThrottleUtility.IsHungerCritical(p))
                interval = 60;

            // Keep responsive near colonists
            if (settings.excludeNearColonists &&
                WildAnimalThrottleUtility.IsNearColonists(p, settings.excludeNearColonistsRadius))
            {
                comp.MarkDidFullTick(p, now, 60);
                return true;
            }

            // =========================
            // Core throttle logic
            // =========================

            if (!comp.CanDoFullTick(p, now))
                return false;

            comp.MarkDidFullTick(p, now, interval);
            return true;
        }
    }
}
