using HarmonyLib;
using UnityEngine;
using Verse;

namespace MyRimWorldMod
{
    public class HardRimWorldOptimizationMod : Mod
    {
        public static OptimizationSettings Settings;

        public HardRimWorldOptimizationMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<OptimizationSettings>();

            var harmony = new Harmony("myrimworldmod.hardrimworldoptimization");
            harmony.PatchAll();

            Log.Message("[HardRimWorldOptimization] Harmony patches applied.");
        }

        public override string SettingsCategory() => "Hard RimWorld Optimization";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var list = new Listing_Standard();
            list.Begin(inRect);

            // =========================
            // Wildlife Optimization
            // =========================
            Text.Font = GameFont.Medium;
            list.Label("Wildlife Optimization");
            Text.Font = GameFont.Small;
            list.Gap(6f);

            list.CheckboxLabeled(
                "Throttle factionless neutral animals (major TPS saver)",
                ref Settings.throttleWildAnimals);

            using (new GuiEnabledScope(Settings.throttleWildAnimals))
            {
                list.Gap(4f);

                list.Label($"Throttle interval: {Settings.throttleIntervalTicks} ticks (~{Settings.throttleIntervalTicks / 60f:0.0}s)");
                Settings.throttleIntervalTicks = (int)list.Slider(Settings.throttleIntervalTicks, 60, 7200);

                list.GapLine();

                list.CheckboxLabeled("Exclude predators (do not throttle predators)", ref Settings.excludePredators);
                list.Gap(2f);

                list.CheckboxLabeled("Exclude animals near colonists (keep local wildlife active)", ref Settings.excludeNearColonists);

                using (new GuiEnabledScope(Settings.excludeNearColonists))
                {
                    list.Label($"Near-colonist radius: {Settings.excludeNearColonistsRadius} cells");
                    Settings.excludeNearColonistsRadius = (int)list.Slider(Settings.excludeNearColonistsRadius, 0, 80);
                }

                list.Gap(4f);
                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                list.Label("Recommended interval: 1800–3600 ticks (30–60 sec).");
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }

            list.GapLine();

            // =========================
            // Turret Optimization
            // =========================
            Text.Font = GameFont.Medium;
            list.Label("Turret Optimization");
            Text.Font = GameFont.Small;
            list.Gap(6f);

            list.CheckboxLabeled(
                "Optimize player turrets (reduce target scans when no threats)",
                ref Settings.optimizePlayerTurrets);

            using (new GuiEnabledScope(Settings.optimizePlayerTurrets))
            {
                list.Gap(4f);

                list.Label($"Idle scan interval: {Settings.turretIdleScanIntervalTicks} ticks (~{Settings.turretIdleScanIntervalTicks / 60f:0.0}s)");
                Settings.turretIdleScanIntervalTicks = (int)list.Slider(Settings.turretIdleScanIntervalTicks, 60, 2000);

                list.Gap(4f);

                list.Label($"Danger refresh interval: {Settings.turretDangerRefreshIntervalTicks} ticks (~{Settings.turretDangerRefreshIntervalTicks / 60f:0.0}s)");
                Settings.turretDangerRefreshIntervalTicks = (int)list.Slider(Settings.turretDangerRefreshIntervalTicks, 60, 2000);

                list.Gap(4f);
                list.CheckboxLabeled("Verbose turret logging", ref Settings.turretVerboseLogging);
            }

            list.GapLine();

            // =========================
            // Prisoner Optimization
            // =========================
            Text.Font = GameFont.Medium;
            list.Label("Prisoner Optimization");
            Text.Font = GameFont.Small;
            list.Gap(6f);

            list.CheckboxLabeled(
                "Throttle prisoners when idle (huge TPS saver for prison-heavy colonies)",
                ref Settings.throttlePrisoners);

            using (new GuiEnabledScope(Settings.throttlePrisoners))
            {
                list.Gap(4f);

                list.Label($"Prisoner throttle interval: {Settings.prisonerThrottleIntervalTicks} ticks (~{Settings.prisonerThrottleIntervalTicks / 60f:0.0}s)");
                Settings.prisonerThrottleIntervalTicks = (int)list.Slider(Settings.prisonerThrottleIntervalTicks, 15, 600);

                list.Gap(4f);

                list.CheckboxLabeled(
                    "Exclude prisoners near colonists (keep interactions responsive)",
                    ref Settings.prisonersExcludeNearColonists);

                using (new GuiEnabledScope(Settings.prisonersExcludeNearColonists))
                {
                    list.Label($"Near-colonist radius: {Settings.prisonersNearColonistRadius} cells");
                    Settings.prisonersNearColonistRadius = (int)list.Slider(Settings.prisonersNearColonistRadius, 5, 80);
                }

                list.Gap(4f);
                list.CheckboxLabeled("Verbose prisoner logging", ref Settings.prisonerVerboseLogging);

                list.Gap(4f);
                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                list.Label("Tip: start with 120–250 ticks (2–4 sec). Increase if your prison is isolated.");
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }

            list.GapLine();

            // =========================
            // UI Tweaks
            // =========================
            Text.Font = GameFont.Medium;
            list.Label("UI Tweaks");
            Text.Font = GameFont.Small;
            list.Gap(6f);

            list.CheckboxLabeled(
                "Compact enemy faction icons in Factions tab (UI perf + cleaner layout)",
                ref Settings.compactEnemyIconsInFactionRow);

            using (new GuiEnabledScope(Settings.compactEnemyIconsInFactionRow))
            {
                list.Gap(4f);
                list.Label($"Max rows without scaling: {Settings.compactEnemyIconsMaxRowsWithoutScaling}");
                Settings.compactEnemyIconsMaxRowsWithoutScaling =
                    (int)list.Slider(Settings.compactEnemyIconsMaxRowsWithoutScaling, 1, 10);

                list.Gap(4f);
                list.CheckboxLabeled("Verbose faction-row logging", ref Settings.compactEnemyIconsVerboseLogging);
            }

            // reset font just in case
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            list.End();

            // Write after End, not mid-layout
            Settings.Write();
        }

        private readonly struct GuiEnabledScope : System.IDisposable
        {
            private readonly bool old;
            public GuiEnabledScope(bool enabled)
            {
                old = GUI.enabled;
                GUI.enabled = enabled;
            }
            public void Dispose() => GUI.enabled = old;
        }
    }
}
