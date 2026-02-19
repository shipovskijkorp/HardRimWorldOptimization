using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MyRimWorldMod
{
    /// <summary>
    /// Plant tick optimization:
    /// - Reduce how often Plant.TickLong() runs (policy based on home area + fully-grown interval)
    /// - Compensate skipped TickLong calls for GROWING plants (Growth < 1.0) by running extra TickLong steps in a batch.
    /// - For fully-grown plants (Growth >= 1.0), we do NOT compensate missed steps (growth is already capped).
    ///   This is where the real savings come from; other behavior (blight/fire/etc) may be delayed.
    /// </summary>
    [HarmonyPatch(typeof(Plant), nameof(Plant.TickLong))]
    public static class Patch_Plant_TickLong_Optimization
    {
        // thingIDNumber -> state
        private static Dictionary<int, PlantTickState> stateByPlant = new Dictionary<int, PlantTickState>();

        // simple cleanup so dict doesn't grow forever
        private static int _nextCleanupTick;

        // guard to avoid recursion when we manually invoke TickLong for compensation
        [ThreadStatic]
        private static bool _inCompensation;

        // per-call scratch
        [ThreadStatic]
        private static int _pendingExtraTickLongCalls;

        private struct PlantTickState
        {
            public int lastAllowedTick;
            public int nextAllowedTick;
        }

        public static bool Prefix(Plant __instance)
        {
            if (_inCompensation) return true;

            var settings = HardRimWorldOptimizationMod.Settings;
            if (settings == null || !settings.optimizePlants)
                return true;

            Plant plant = __instance;
            if (plant == null || plant.Destroyed || !plant.Spawned)
                return true;

            Map map = plant.Map;
            if (map == null)
                return true;

            // Vanilla TickLong runs on a fixed cadence.
            int baseInterval = GenTicks.TickLongInterval;

            // periodic cleanup (every ~1 in-game day)
            int now = Find.TickManager?.TicksGame ?? 0;
            if (now >= _nextCleanupTick)
            {
                Cleanup(now);
                _nextCleanupTick = now + 60000;
            }

            // Exceptions: keep vanilla frequency.
            // NOTE: As requested, we accept potential detection delay between allowed ticks.
            if (IsExceptionPlant(plant))
            {
                TouchState(plant.thingIDNumber, now, baseInterval);
                _pendingExtraTickLongCalls = 0;
                return true;
            }

            int id = plant.thingIDNumber;
            if (!stateByPlant.TryGetValue(id, out PlantTickState st))
            {
                st = new PlantTickState
                {
                    lastAllowedTick = now,
                    nextAllowedTick = now // allow immediately
                };
                stateByPlant[id] = st;
            }

            // Decide desired interval
            int desiredIntervalTicks = GetDesiredIntervalTicks(plant, map, settings, baseInterval);

            // If not yet allowed, skip this TickLong entirely.
            if (now < st.nextAllowedTick)
                return false;

            // Allowed: schedule next allowed tick.
            st.nextAllowedTick = now + desiredIntervalTicks;

            bool isGrowing = plant.Growth < 1f;

            // Compensation for growing plants only.
            // We batch missed TickLong steps since lastAllowedTick.
            if (isGrowing)
            {
                int deltaTicks = Mathf.Max(0, now - st.lastAllowedTick);
                int steps = Mathf.Max(1, deltaTicks / baseInterval);

                // We will run original TickLong once, then Postfix will run (steps-1) more times.
                _pendingExtraTickLongCalls = Mathf.Clamp(steps - 1, 0, 64);
            }
            else
            {
                // Fully-grown: no compensation (growth capped anyway). This is where we actually save time.
                _pendingExtraTickLongCalls = 0;
            }

            st.lastAllowedTick = now;
            stateByPlant[id] = st;

            return true;
        }

        public static void Postfix(Plant __instance)
        {
            if (_inCompensation) return;

            int extra = _pendingExtraTickLongCalls;
            _pendingExtraTickLongCalls = 0;

            if (extra <= 0) return;

            // Run extra TickLong steps in a tight batch.
            // Guard against recursion so our Prefix doesn't reapply scheduling.
            try
            {
                _inCompensation = true;
                for (int i = 0; i < extra; i++)
                {
                    // Direct call is fine; recursion is prevented by _inCompensation.
                    __instance.TickLong();
                }
            }
            catch (Exception e)
            {
                Log.Error($"[HardRimWorldOptimization] Plant TickLong compensation failed for '{__instance?.LabelShort ?? "<null>"}': {e}");
            }
            finally
            {
                _inCompensation = false;
            }
        }

        private static bool IsExceptionPlant(Plant plant)
        {
            if (plant == null) return true;

            // Blight
            if (plant.Blighted) return true;

            // Fire / burning
            if (plant.IsBurning()) return true;

            return false;
        }

        private static int GetDesiredIntervalTicks(Plant plant, Map map, OptimizationSettings settings, int baseInterval)
        {
            // Fully-grown interval (global)
            if (plant.Growth >= 1f)
            {
                int full = Mathf.Clamp(settings.plantFullyGrownIntervalTicks, baseInterval, 60000);
                // Align to TickLong cadence to keep the state predictable.
                return RoundUpToMultiple(full, baseInterval);
            }

            // Growing plants: slow down by multiplier depending on "importance".
            bool inHomeOrGrowingZone = IsInHomeOrGrowingZone(map, plant.Position);

            float mul = inHomeOrGrowingZone
                ? Mathf.Clamp(settings.plantHomeAreaGrowingMultiplier, 1f, 8f)
                : Mathf.Clamp(settings.plantWildGrowingMultiplier, 1f, 16f);

            int target = Mathf.RoundToInt(baseInterval * mul);
            target = Mathf.Clamp(target, baseInterval, 60000);

            // Align to TickLong cadence (important because Prefix only runs when vanilla calls TickLong).
            return RoundUpToMultiple(target, baseInterval);
        }

        private static bool IsInHomeOrGrowingZone(Map map, IntVec3 pos)
        {
            if (map == null) return false;

            // Home area
            if (map.areaManager?.Home != null && map.areaManager.Home[pos])
                return true;

            // Growing zone
            Zone z = pos.GetZone(map);
            return z is Zone_Growing;
        }

        private static int RoundUpToMultiple(int value, int multiple)
        {
            if (multiple <= 0) return value;
            int rem = value % multiple;
            return rem == 0 ? value : value + (multiple - rem);
        }

        private static void TouchState(int id, int now, int interval)
        {
            if (!stateByPlant.TryGetValue(id, out var st))
            {
                st = new PlantTickState { lastAllowedTick = now, nextAllowedTick = now + interval };
            }
            else
            {
                st.lastAllowedTick = now;
                st.nextAllowedTick = now + interval;
            }
            stateByPlant[id] = st;
        }

        private static void Cleanup(int currentTick)
        {
            if (stateByPlant == null || stateByPlant.Count == 0) return;

            int cutoff = currentTick - 60000;
            List<int> remove = null;

            foreach (var kv in stateByPlant)
            {
                if (kv.Value.nextAllowedTick < cutoff)
                {
                    if (remove == null) remove = new List<int>();
                    remove.Add(kv.Key);
                }
            }

            if (remove == null) return;
            for (int i = 0; i < remove.Count; i++)
                stateByPlant.Remove(remove[i]);
        }
    }
}
