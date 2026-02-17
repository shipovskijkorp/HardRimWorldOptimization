using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using Verse;

namespace MyRimWorldMod
{
    [HarmonyPatch]
    internal static class Patch_NaturalRandomQuestChooser_ChooseNaturalRandomQuest
    {
        private const float MinPoints = 0.01f;

        // Robust target: find any ChooseNaturalRandomQuest that starts with (float, IIncidentTarget)
        static MethodBase TargetMethod()
        {
            var t = typeof(NaturalRandomQuestChooser);
            var methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            for (int i = 0; i < methods.Length; i++)
            {
                var m = methods[i];
                if (m == null) continue;
                if (!string.Equals(m.Name, "ChooseNaturalRandomQuest", StringComparison.Ordinal)) continue;
                if (m.ReturnType != typeof(QuestScriptDef)) continue;

                var ps = m.GetParameters();
                if (ps == null || ps.Length < 2) continue;
                if (ps[0].ParameterType != typeof(float)) continue;
                if (ps[1].ParameterType != typeof(IIncidentTarget)) continue;

                return m;
            }
            return null;
        }

        [HarmonyPrefix]
        public static bool Prefix(ref float points, IIncidentTarget target, ref QuestScriptDef __result)
        {
            var settings = HardRimWorldOptimizationMod.Settings;
            if (settings == null || !settings.tweakQuestGeneration || !settings.questUseFastNaturalRandomChooser)
                return true;

            if (target == null)
            {
                __result = null;
                return false;
            }

            using (QuestTweaks_Profiler.Measure("ChooseNaturalRandomQuest fast", 10))
            {
                // Normalize bad points (and keep it for vanilla fallback too)
                if (points <= MinPoints)
                {
                    float auto = QuestTweaks_PointsUtil.ComputeThreatPointsSafe(target);
                    if (auto > MinPoints)
                    {
                        points = auto;
                        QuestTweaks_PointsContext.RecordAutoPoints(points);
                        if (QuestTweaks_Log.Verbose)
                            QuestTweaks_Log.Message($"Auto-points for selection: {points:F1}");
                    }
                }

                bool incPop = Rand.Chance(NaturalRandomQuestChooser.PopulationIncreasingQuestChance());

                QuestScriptDef chosen;
                if (QuestTweaks_FastNaturalRandomQuestChooser.TryGetQuestFast(incPop, points, target, settings, out chosen))
                {
                    __result = chosen;
                    return false; // skip vanilla
                }

                // If we tried incPop first, also try non-incPop.
                if (incPop && QuestTweaks_FastNaturalRandomQuestChooser.TryGetQuestFast(false, points, target, settings, out chosen))
                {
                    __result = chosen;
                    return false; // skip vanilla
                }

                if (settings.questUseAncientComplexFallback)
                {
                    QuestScriptDef fallback = QuestTweaks_QuestFilters.TryGetAncientComplexFallback(points, target);
                    if (fallback != null)
                    {
                        __result = fallback;
                        return false; // skip vanilla
                    }
                }

                // IMPORTANT: fallback to vanilla if fast path failed
                if (QuestTweaks_Log.Verbose)
                    QuestTweaks_Log.Message("Fast chooser failed -> falling back to vanilla.");

                return true;
            }
        }
    }
}
