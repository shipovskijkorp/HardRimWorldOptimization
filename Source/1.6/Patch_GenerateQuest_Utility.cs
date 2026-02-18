using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace MyRimWorldMod
{
    [HarmonyPatch]
    internal static class Patch_QuestUtility_GenerateQuestAndMakeAvailable_NormalizePoints
    {
        private const float MinPoints = 0.01f;

        // Patch ALL overloads that start with (QuestScriptDef ...)
        static IEnumerable<MethodBase> TargetMethods()
        {
            MethodInfo[] methods = typeof(QuestUtility).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo m = methods[i];
                if (m == null) continue;
                if (!string.Equals(m.Name, "GenerateQuestAndMakeAvailable", StringComparison.Ordinal)) continue;
                if (m.ReturnType != typeof(Quest)) continue;

                ParameterInfo[] ps = m.GetParameters();
                if (ps == null || ps.Length == 0) continue;
                if (ps[0].ParameterType != typeof(QuestScriptDef)) continue;

                yield return m;
            }
        }

        [HarmonyPrefix]
        static void Prefix(MethodBase __originalMethod, object[] __args)
        {
            var s = HardRimWorldOptimizationMod.Settings;
            if (s == null || !s.tweakQuestGeneration || !s.questNormalizeZeroPointsForGeneration)
                return;

            if (__args == null || __args.Length == 0)
                return;

            float points = 0f;
            bool hasPointsArg = false;
            int pointsIndex = -1;
            IIncidentTarget target = null;
            Slate slate = null;

            // Find first float after the QuestScriptDef argument and also grab target/slate if present.
            for (int i = 1; i < __args.Length; i++)
            {
                object obj = __args[i];
                if (obj == null) continue;

                if (!hasPointsArg && obj is float)
                {
                    hasPointsArg = true;
                    pointsIndex = i;
                    points = (float)obj;
                    continue;
                }

                if (target == null && obj is IIncidentTarget)
                {
                    target = (IIncidentTarget)obj;
                    continue;
                }

                if (slate == null && obj is Slate)
                {
                    slate = (Slate)obj;
                    continue;
                }
            }

            if (target == null && slate != null)
                target = QuestTweaks_PointsUtil.TryGetTargetFromSlate(slate);

            // If points were provided and are already valid, do nothing.
            if (hasPointsArg && points > MinPoints)
                return;

            float normalized;
            if (QuestTweaks_PointsContext.TryGetRecentAutoPoints(out normalized))
            {
                // use cached points from recent selection
            }
            else
            {
                normalized = QuestTweaks_PointsUtil.ComputeThreatPointsSafe(target);
                if (normalized > MinPoints)
                    QuestTweaks_PointsContext.RecordAutoPoints(normalized);
            }

            if (normalized <= MinPoints)
                return;

            if (hasPointsArg && pointsIndex >= 0)
                __args[pointsIndex] = normalized;

            if (slate != null)
                QuestTweaks_PointsUtil.EnsureSlatePoints(slate, normalized);
        }
    }
}
