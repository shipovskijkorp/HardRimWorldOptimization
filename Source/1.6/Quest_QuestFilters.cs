using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;

namespace MyRimWorldMod
{
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
}
