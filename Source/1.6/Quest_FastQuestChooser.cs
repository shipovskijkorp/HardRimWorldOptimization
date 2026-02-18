using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MyRimWorldMod
{
    internal static class QuestTweaks_FastNaturalRandomQuestChooser
    {
        public static bool TryGetQuestFast(bool incPop, float points, IIncidentTarget target, OptimizationSettings settings, out QuestScriptDef chosen)
        {
            chosen = null;
            if (target == null || settings == null) return false;

            StoryState storyState = target.StoryState;
            List<QuestScriptDef> roots = QuestTweaks_QuestRootCache.GetRoots(incPop);
            if (roots == null || roots.Count == 0) return false;

            List<QuestScriptDef> cands = QuestTweaks_ListPool<QuestScriptDef>.Get();
            List<float> weights = QuestTweaks_ListPool<float>.Get();
            int canRunChecks = 0;

            try
            {
                for (int i = 0; i < roots.Count; i++)
                {
                    QuestScriptDef quest = roots[i];
                    if (quest == null) continue;
                    if (!QuestTweaks_QuestFilters.PassesExtraPlanetLayerGate(quest, target)) continue;

                    float w = NaturalRandomQuestChooser.GetNaturalRandomSelectionWeight(quest, points, storyState);
                    if (w <= 0f) continue;

                    cands.Add(quest);
                    weights.Add(w);
                }

                if (cands.Count == 0) return false;

                int maxChecks = Mathf.Clamp(settings.questMaxCanRunChecksPerSelection, 1, 200);
                maxChecks = Mathf.Min(maxChecks, cands.Count);

                for (int i = 0; i < maxChecks; i++)
                {
                    int idx = PickWeightedIndex(weights);
                    if (idx < 0) break;

                    QuestScriptDef q = cands[idx];
                    canRunChecks++;

                    bool ok;
                    try { ok = q != null && q.CanRun(points, target); }
                    catch { ok = false; }

                    if (ok)
                    {
                        chosen = q;

                        return true;
                    }

                    RemoveAtSwapBack(cands, weights, idx);
                }

                return false;
            }
            finally
            {
                QuestTweaks_ListPool<QuestScriptDef>.Return(cands);
                QuestTweaks_ListPool<float>.Return(weights);
            }
        }

        private static int PickWeightedIndex(List<float> weights)
        {
            if (weights == null || weights.Count == 0) return -1;

            float total = 0f;
            for (int i = 0; i < weights.Count; i++)
            {
                float w = weights[i];
                if (w > 0f) total += w;
            }

            if (total <= 0f) return -1;

            float pick = Rand.Value * total;
            float acc = 0f;
            for (int i = 0; i < weights.Count; i++)
            {
                float w = weights[i];
                if (w <= 0f) continue;
                acc += w;
                if (pick <= acc) return i;
            }

            return weights.Count - 1;
        }

        private static void RemoveAtSwapBack(List<QuestScriptDef> cands, List<float> weights, int idx)
        {
            int last = cands.Count - 1;
            if (idx < 0 || idx > last) return;

            cands[idx] = cands[last];
            cands.RemoveAt(last);

            weights[idx] = weights[last];
            weights.RemoveAt(last);
        }
    }
}
