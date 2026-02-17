using Verse;

namespace MyRimWorldMod
{
    internal static class QuestTweaks_Log
    {
        public static bool Verbose
        {
            get
            {
                var s = HardRimWorldOptimizationMod.Settings;
                return s != null && s.tweakQuestGeneration && s.questVerboseLogging;
            }
        }

        public static void Message(string msg)
        {
            if (!Verbose) return;
            Log.Message("[HardRimWorldOptimization:Quest] " + msg);
        }

        public static void Warning(string msg)
        {
            if (!Verbose) return;
            Log.Warning("[HardRimWorldOptimization:Quest] " + msg);
        }
    }
}
