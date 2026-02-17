using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace MyRimWorldMod
{
    internal static class QuestTweaks_PointsUtil
    {
        public static float ComputeThreatPointsSafe(IIncidentTarget target)
        {
            if (target == null) return 0f;
            try
            {
                return StorytellerUtility.DefaultThreatPointsNow(target);
            }
            catch
            {
                return 0f;
            }
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
            try
            {
                slate.Set("points", points, false);
            }
            catch { }
        }
    }
}
