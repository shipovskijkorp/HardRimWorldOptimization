using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MyRimWorldMod
{
    /// <summary>
    /// Quest generation / selection optimizations and helpers.
    /// Kept internal to avoid exposing API surface.
    /// </summary>
    internal static class QuestTweaks_PointsContext
    {
        // Keeps a very short-lived "best guess" points value so that
        // GenerateQuestAndMakeAvailable(...) called shortly after ChooseNaturalRandomQuest(...) can reuse it.
        private const int FreshTicks = 120;
        private static float lastAutoPoints;
        private static int lastAutoPointsTick = -999999999;

        public static void RecordAutoPoints(float points)
        {
            lastAutoPoints = points;
            lastAutoPointsTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
        }

        public static bool TryGetRecentAutoPoints(out float points)
        {
            points = 0f;
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (now - lastAutoPointsTick <= FreshTicks && lastAutoPoints > 0.01f)
            {
                points = lastAutoPoints;
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Tiny list pool to reduce GC churn during quest selection.
    /// RimWorld runs on main thread; this is intentionally simple.
    /// </summary>
    internal static class QuestTweaks_ListPool<T>
    {
        private static readonly Stack<List<T>> Pool = new Stack<List<T>>(32);

        public static List<T> Get()
        {
            lock (Pool)
            {
                if (Pool.Count > 0)
                {
                    var list = Pool.Pop();
                    list.Clear();
                    return list;
                }
            }

            return new List<T>();
        }

        public static void Return(List<T> list)
        {
            if (list == null) return;
            list.Clear();

            lock (Pool)
            {
                // avoid unbounded growth
                if (Pool.Count < 128)
                    Pool.Push(list);
            }
        }
    }

    internal static class QuestTweaks_Profiler
    {
        // Placeholder for future lightweight profiling.
        // (kept since patches already wrap in Measure(...))
        public static MeasureScope Measure(string name, int minMsToLog = 10) => new MeasureScope(name, minMsToLog);

        internal readonly struct MeasureScope : IDisposable
        {
            public MeasureScope(string name, int minMs) { }
            public void Dispose() { }
        }
    }

    internal static class QuestTweaks_PointsUtil
    {
        public static float ComputeThreatPointsSafe(IIncidentTarget target)
        {
            if (target == null) return 0f;
            try { return StorytellerUtility.DefaultThreatPointsNow(target); }
            catch { return 0f; }
        }

        public static IIncidentTarget TryGetTargetFromSlate(object slateObj)
        {
            var slate = slateObj as Slate;
            if (slate == null) return null;

            try
            {
                Map map = slate.Get<Map>("map", null, false);
                if (map != null) return map;
            }
            catch { }

            try
            {
                IIncidentTarget t = slate.Get<IIncidentTarget>("target", null, false);
                if (t != null) return t;
            }
            catch { }

            return null;
        }

        public static void EnsureSlatePoints(Slate slate, float points)
        {
            if (slate == null) return;
            try { slate.Set("points", points, false); }
            catch { }
        }
    }

    internal static class QuestTweaks_QuestRootCache
    {
        private static List<QuestScriptDef> rootsIncPopTrue;
        private static List<QuestScriptDef> rootsIncPopFalse;
        private static int lastAllDefsCount = -1;

        public static void EnsureBuilt()
        {
            List<QuestScriptDef> defs = DefDatabase<QuestScriptDef>.AllDefsListForReading;
            if (defs == null) return;
            if (rootsIncPopTrue != null && rootsIncPopFalse != null && lastAllDefsCount == defs.Count)
                return;

            rootsIncPopTrue = new List<QuestScriptDef>(64);
            rootsIncPopFalse = new List<QuestScriptDef>(64);

            for (int i = 0; i < defs.Count; i++)
            {
                QuestScriptDef q = defs[i];
                if (q == null) continue;
                if (!q.IsRootRandomSelected) continue;

                if (q.rootIncreasesPopulation)
                    rootsIncPopTrue.Add(q);
                else
                    rootsIncPopFalse.Add(q);
            }

            lastAllDefsCount = defs.Count;
        }

        public static List<QuestScriptDef> GetRoots(bool incPop)
        {
            EnsureBuilt();
            return incPop ? rootsIncPopTrue : rootsIncPopFalse;
        }
    }

    internal static class QuestTweaks_QuestFilters
    {
        public static bool PassesExtraPlanetLayerGate(QuestScriptDef quest, IIncidentTarget target)
        {
            if (quest == null || target == null) return true;

            PlanetTile tile = target.Tile;
            if (!tile.Valid) return true;

            PlanetLayerDef layerDef = tile.LayerDef;
            if (layerDef == null) return true;

            // Keep parity with vanilla/Quest Generation Tweaks logic.
            bool inWhitelist = quest.layerWhitelist != null && quest.layerWhitelist.Contains(layerDef);

            if (layerDef.onlyAllowWhitelistedQuests)
            {
                if (quest.layerWhitelist == null || quest.layerWhitelist.Count == 0) return false;
                if (!inWhitelist) return false;
            }

            if (!quest.canOccurOnAllPlanetLayers && layerDef.onlyAllowWhitelistedIncidents)
            {
                if (quest.layerWhitelist == null || quest.layerWhitelist.Count == 0) return false;
                if (!inWhitelist) return false;
            }

            return true;
        }

        public static QuestScriptDef TryGetAncientComplexFallback(float points, IIncidentTarget target)
        {
            // Try the well-known defNames first.
            QuestScriptDef q;

            q = DefDatabase<QuestScriptDef>.GetNamedSilentFail("OpportunitySite_AncientComplex");
            if (q != null && SafeCanRun(q, points, target)) return q;

            q = DefDatabase<QuestScriptDef>.GetNamedSilentFail("OpportunitySite_AncientComplex_Giver");
            if (q != null && SafeCanRun(q, points, target)) return q;

            q = DefDatabase<QuestScriptDef>.GetNamedSilentFail("OpportunitySite_AncientComplex_Mechanitor");
            if (q != null && SafeCanRun(q, points, target)) return q;

            q = DefDatabase<QuestScriptDef>.GetNamedSilentFail("AncientComplex_Standard");
            if (q != null && SafeCanRun(q, points, target)) return q;

            // Fallback: any quest defName containing "AncientComplex".
            List<QuestScriptDef> defs = DefDatabase<QuestScriptDef>.AllDefsListForReading;
            for (int i = 0; i < defs.Count; i++)
            {
                var cand = defs[i];
                if (cand == null) continue;
                string defName = cand.defName;
                if (string.IsNullOrEmpty(defName)) continue;
                if (defName.IndexOf("AncientComplex", StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (SafeCanRun(cand, points, target)) return cand;
            }

            return null;
        }

        private static bool SafeCanRun(QuestScriptDef quest, float points, IIncidentTarget target)
        {
            try { return quest.CanRun(points, target); }
            catch { return false; }
        }
    }

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
