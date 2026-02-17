using HarmonyLib;
using RimWorld;
using Verse;

namespace MyRimWorldMod
{
    /// <summary>
    /// Player turret optimization:
    /// - If danger is present on map: vanilla behavior.
    /// - If no danger: reuse current target if still valid; otherwise allow a full scan only once per configured interval.
    /// Storage is per-map (MapComponent_TurretOptimizer) with periodic cleanup.
    /// </summary>
    [HarmonyPatch(typeof(Building_TurretGun), "TryFindNewTarget")]
    internal static class Patch_Building_TurretGun_TryFindNewTarget_IdleThrottle
    {
        [HarmonyPrefix]
        private static bool Prefix(Building_TurretGun __instance, ref LocalTargetInfo __result)
        {
            var settings = HardRimWorldOptimizationMod.Settings;
            if (settings == null || !settings.optimizePlayerTurrets)
                return true;

            if (__instance == null)
                return true;

            // Don't touch non-player turrets (leave vanilla for raiders, caravans, etc.)
            if (!TurretOptimizerUtility.IsPlayerTurret(__instance))
                return true;

            // Forced target should always work instantly
            LocalTargetInfo forcedTarget = ((Building_Turret)__instance).ForcedTarget;
            if (forcedTarget.IsValid)
                return true;

            Map map = __instance.Map;
            if (map == null)
                return true;

            // In danger: full vanilla logic (maximum responsiveness)
            if (TurretOptimizerUtility.IsDangerPresent(map))
                return true;

            // No danger: if turret already has a valid target, keep it without doing a full expensive scan
            LocalTargetInfo currentTarget = __instance.CurrentTarget;
            if (TurretOptimizerUtility.IsTargetStillValid(__instance, currentTarget))
            {
                if (settings.turretVerboseLogging && Gen.IsHashIntervalTick(__instance, 250))
                    Log.Message($"[HRWO] Turret {__instance.ThingID} reusing target (idle mode). ");

                __result = currentTarget;
                return false;
            }

            int now = Find.TickManager.TicksGame;
            var comp = map.GetComponent<MapComponent_TurretOptimizer>();
            if (comp == null)
                return true; // shouldn't happen, but safest

            int interval = Clamp(settings.turretIdleScanIntervalTicks, 60, 2000);
            int last = comp.GetLastFullScanTick(__instance.thingIDNumber);

            if (now - last < interval)
            {
                if (settings.turretVerboseLogging && Gen.IsHashIntervalTick(__instance, 250))
                    Log.Message($"[HRWO] Turret {__instance.ThingID} full scan skipped (idle throttle). ");

                __result = LocalTargetInfo.Invalid;
                return false;
            }

            comp.SetLastFullScanTick(__instance.thingIDNumber, now);
            return true; // allow vanilla full scan
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
