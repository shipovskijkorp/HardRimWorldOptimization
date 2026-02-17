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

            int interval = settings.throttleIntervalTicks;
            if (interval < 60) interval = 60;
            if (interval > 7200) interval = 7200;

            if (Find.TickManager == null)
                return true;

            int now = Find.TickManager.TicksGame;
            var comp = WildAnimalThrottleComponent.Instance;
            if (comp == null)
                return true;

            if (!comp.CanDoFullTick(p, now))
                return false;

            if (settings.excludeNearColonists &&
                WildAnimalThrottleUtility.IsNearColonists(p, settings.excludeNearColonistsRadius))
            {
                comp.MarkDidFullTick(p, now, 60);
                return true;
            }

            comp.MarkDidFullTick(p, now, interval);
            return true;
        }
    }
}
