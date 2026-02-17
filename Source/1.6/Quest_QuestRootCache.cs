using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MyRimWorldMod
{
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
}
