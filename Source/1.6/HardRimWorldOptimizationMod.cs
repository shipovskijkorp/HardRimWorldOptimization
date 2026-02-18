using HarmonyLib;
using UnityEngine;
using Verse;

namespace MyRimWorldMod
{
    public class HardRimWorldOptimizationMod : Mod
    {
        public static OptimizationSettings Settings;

        // Scroll position for settings window
        private Vector2 scrollPosition;

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
            // A large enough view height so everything fits; scrollview will handle the rest.
            // If you add more sections later, feel free to increase this number.
            float viewHeight = 1400f;

            // Make the inner view a bit narrower so content doesn't hide behind the scrollbar.
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, viewHeight);

            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);

            var list = new Listing_Standard();
            list.Begin(viewRect);

            // =========================
            // Wildlife Optimization
            // =========================
            SectionHeader(list, "Wildlife Optimization");

            list.CheckboxLabeled(
                "Throttle factionless neutral animals (major TPS saver)",
                ref Settings.throttleWildAnimals);

            using (new GuiEnabledScope(Settings.throttleWildAnimals))
            {
                list.Gap(4f);

                LabelTicksSeconds(list, "Throttle interval", Settings.throttleIntervalTicks);
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
                TipText(list, "Recommended interval: 1800–3600 ticks (30–60 sec).");
            }

            list.GapLine();

            // =========================
            // Turret Optimization
            // =========================
            SectionHeader(list, "Turret Optimization");

            list.CheckboxLabeled(
                "Optimize player turrets (reduce target scans when no threats)",
                ref Settings.optimizePlayerTurrets);

            using (new GuiEnabledScope(Settings.optimizePlayerTurrets))
            {
                list.Gap(4f);

                LabelTicksSeconds(list, "Idle scan interval", Settings.turretIdleScanIntervalTicks);
                Settings.turretIdleScanIntervalTicks = (int)list.Slider(Settings.turretIdleScanIntervalTicks, 60, 2000);

                list.Gap(4f);

                LabelTicksSeconds(list, "Danger refresh interval", Settings.turretDangerRefreshIntervalTicks);
                Settings.turretDangerRefreshIntervalTicks = (int)list.Slider(Settings.turretDangerRefreshIntervalTicks, 60, 2000);

                list.Gap(4f);
                list.CheckboxLabeled("Verbose turret logging", ref Settings.turretVerboseLogging);
            }

            list.GapLine();

            // =========================
            // Prisoner Optimization
            // =========================
            SectionHeader(list, "Prisoner Optimization");

            list.CheckboxLabeled(
                "Throttle prisoners when idle (huge TPS saver for prison-heavy colonies)",
                ref Settings.throttlePrisoners);

            using (new GuiEnabledScope(Settings.throttlePrisoners))
            {
                list.Gap(4f);

                LabelTicksSeconds(list, "Prisoner throttle interval", Settings.prisonerThrottleIntervalTicks);
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
                TipText(list, "Tip: start with 120–250 ticks (2–4 sec). Increase if your prison is isolated.");
            }

            list.GapLine();

            // =========================
            // Quest Generation
            // =========================
            SectionHeader(list, "Quest Generation");

            list.CheckboxLabeled(
                "Tweak quest selection/generation (reduce stutters, fix points=0 calls)",
                ref Settings.tweakQuestGeneration);

            using (new GuiEnabledScope(Settings.tweakQuestGeneration))
            {
                list.Gap(4f);

                list.CheckboxLabeled(
                    "Use fast natural-random quest chooser (fewer CanRun() checks)",
                    ref Settings.questUseFastNaturalRandomChooser);

                using (new GuiEnabledScope(Settings.questUseFastNaturalRandomChooser))
                {
                    list.Gap(2f);
                    list.Label($"Max checks per roll: {Settings.questMaxCanRunChecksPerSelection}");
                    Settings.questMaxCanRunChecksPerSelection =
                        (int)list.Slider(Settings.questMaxCanRunChecksPerSelection, 1, 60);
                }

                list.Gap(4f);

                list.CheckboxLabeled(
                    "Fix quests when points <= 0",
                    ref Settings.questNormalizeZeroPointsForGeneration);

                list.Gap(2f);
                list.CheckboxLabeled(
                    "Ancient Complex like last attemp to control",
                    ref Settings.questUseAncientComplexFallback);

                list.Gap(4f);
                list.CheckboxLabeled("Verbose quest logging", ref Settings.questVerboseLogging);

                list.Gap(4f);
                TipText(list, "Note: fast chooser slightly changes probability distribution vs vanilla (usually unnoticeable).");
            }

            list.GapLine();

            // =========================
            // UI Tweaks
            // =========================
            SectionHeader(list, "UI Tweaks");

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

            // reset GUI just in case
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            list.End();
            Widgets.EndScrollView();

            // Write after layout end
            Settings.Write();
        }

        private static void SectionHeader(Listing_Standard list, string title)
        {
            Text.Font = GameFont.Medium;
            list.Label(title);
            Text.Font = GameFont.Small;
            list.Gap(6f);
        }

        private static void LabelTicksSeconds(Listing_Standard list, string label, int ticks)
        {
            list.Label($"{label}: {ticks} ticks (~{ticks / 60f:0.0}s)");
        }

        private static void TipText(Listing_Standard list, string text)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            list.Label(text);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private readonly struct GuiEnabledScope : System.IDisposable
        {
            private readonly bool old;

            public GuiEnabledScope(bool enabled)
            {
                old = GUI.enabled;
                GUI.enabled = enabled;
            }

            public void Dispose()
            {
                GUI.enabled = old;
            }
        }
    }
}
