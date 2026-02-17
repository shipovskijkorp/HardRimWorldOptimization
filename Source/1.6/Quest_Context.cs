using Verse;

namespace MyRimWorldMod
{
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
}
