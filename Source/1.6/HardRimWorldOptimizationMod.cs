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

            list.CheckboxLabeled(
                "Throttle factionless neutral animals (major TPS saver)",
                ref Settings.throttleWildAnimals);

            list.Gap();

            using (new GuiEnabledScope(Settings.throttleWildAnimals))
            {
                list.Label($"Throttle interval: {Settings.throttleIntervalTicks} ticks ({Settings.throttleIntervalTicks / 60f:0.#} sec)");
                Settings.throttleIntervalTicks = (int)list.Slider(Settings.throttleIntervalTicks, 60, 7200);

                list.GapLine();

                list.CheckboxLabeled("Exclude predators (do not throttle predators)", ref Settings.excludePredators);

                list.Gap();

                list.CheckboxLabeled("Exclude animals near colonists (keep local wildlife active)", ref Settings.excludeNearColonists);

                using (new GuiEnabledScope(Settings.excludeNearColonists))
                {
                    list.Label($"Near-colonist radius: {Settings.excludeNearColonistsRadius} cells");
                    Settings.excludeNearColonistsRadius = (int)list.Slider(Settings.excludeNearColonistsRadius, 0, 80);
                }

                list.GapLine();
                list.Label("Recommended interval: 1800–3600 ticks (30–60 sec).");
            }

            list.End();
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
