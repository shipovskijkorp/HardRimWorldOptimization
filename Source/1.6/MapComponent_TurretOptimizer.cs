using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MyRimWorldMod
{
    /// <summary>
    /// Per-map cache for turret optimization:
    /// - dangerPresent is refreshed periodically (cheap)
    /// - lastFullScanTickByTurretId stores last allowed vanilla full scan for each turret
    /// - cleanup keeps dictionary bounded on long saves
    /// </summary>
    public class MapComponent_TurretOptimizer : MapComponent
    {
        public bool dangerPresent;

        // turret thingIDNumber -> last full scan tick
        private Dictionary<int, int> lastFullScanTickByTurretId = new Dictionary<int, int>();

        private int _nextDangerCheckTick;
        private int _nextCleanupTick;

        public MapComponent_TurretOptimizer(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            var settings = HardRimWorldOptimizationMod.Settings;
            if (settings == null || !settings.optimizePlayerTurrets)
                return;

            int now = Find.TickManager.TicksGame;

            // Refresh danger cache
            if (now >= _nextDangerCheckTick)
            {
                _nextDangerCheckTick = now + Clamp(settings.turretDangerRefreshIntervalTicks, 60, 2000);

                bool newDanger = GenHostility.AnyHostileActiveThreatToPlayer(map);
                if (newDanger != dangerPresent && settings.turretVerboseLogging)
                    Log.Message($"[HRWO] Map {map.Index} dangerPresent: {dangerPresent} -> {newDanger}");

                dangerPresent = newDanger;
            }

            // Periodic cleanup (every ~1 day)
            if (now >= _nextCleanupTick)
            {
                _nextCleanupTick = now + 60000;
                Cleanup(now);
            }
        }

        public int GetLastFullScanTick(int turretThingId)
        {
            int last;
            return lastFullScanTickByTurretId != null && lastFullScanTickByTurretId.TryGetValue(turretThingId, out last)
                ? last
                : 0;
        }

        public void SetLastFullScanTick(int turretThingId, int tick)
        {
            if (lastFullScanTickByTurretId == null)
                lastFullScanTickByTurretId = new Dictionary<int, int>();

            lastFullScanTickByTurretId[turretThingId] = tick;
        }

        private void Cleanup(int now)
        {
            if (lastFullScanTickByTurretId == null || lastFullScanTickByTurretId.Count == 0)
                return;

            // Keep only recent entries.
            // If a turret is gone, its thingIDNumber won't be reused, so time-based eviction is safe.
            int cutoff = now - 60000; // 1 day ago

            List<int> toRemove = null;
            foreach (var kv in lastFullScanTickByTurretId)
            {
                if (kv.Value < cutoff)
                {
                    if (toRemove == null) toRemove = new List<int>();
                    toRemove.Add(kv.Key);
                }
            }

            if (toRemove == null)
                return;

            for (int i = 0; i < toRemove.Count; i++)
                lastFullScanTickByTurretId.Remove(toRemove[i]);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref dangerPresent, "dangerPresent", false);
            Scribe_Values.Look(ref _nextDangerCheckTick, "nextDangerCheckTick", 0);
            Scribe_Values.Look(ref _nextCleanupTick, "nextCleanupTick", 0);

            Scribe_Collections.Look(ref lastFullScanTickByTurretId, "lastFullScanTickByTurretId", LookMode.Value, LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && lastFullScanTickByTurretId == null)
                lastFullScanTickByTurretId = new Dictionary<int, int>();
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
