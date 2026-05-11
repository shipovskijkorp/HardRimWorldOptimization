using HarmonyLib;
using Verse;

namespace MyRimWorldMod
{
    [HarmonyPatch(typeof(Pawn), "Tick")]
    public static class Patch_Pawn_Tick_PrisonerThrottle
    {
        private static bool Prefix(Pawn __instance)
        {
            OptimizationSettings settings = HardRimWorldOptimizationMod.Settings;
            if (settings == null || !settings.throttlePrisoners)
                return true;

            Pawn pawn = __instance;
            if (!PrisonerThrottleUtility.ShouldThrottle(pawn, settings))
                return true;

            PrisonerThrottleComponent comp = PrisonerThrottleComponent.Instance;
            if (comp == null || Find.TickManager == null)
                return true;

            int now = Find.TickManager.TicksGame;
            int interval = Clamp(settings.prisonerThrottleIntervalTicks, 15, 300);

            if (!comp.CanDoFullTick(pawn, now))
                return false;

            comp.MarkDidFullTick(pawn, now, interval);
            return true;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
