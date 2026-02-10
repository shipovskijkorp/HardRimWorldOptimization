using LudeonTK;
using RimWorld;
using Verse;

namespace MyRimWorldMod
{
    public static class DebugTools
    {
        private static bool IsQuestThing(Thing t)
        {
            return t?.questTags != null && t.questTags.Count > 0;
        }

        // ------------------------------------------------------------
        // DELETE CHUNKS / SLAG
        // ------------------------------------------------------------

        [DebugAction(
            "Hard RimWorld Optimization",
            "Delete chunks & slag on current map",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DeleteChunksAndSlagOnMap()
        {
            Map map = Find.CurrentMap;
            if (map == null) return;

            int removed = 0;

            // Stone chunks
            var chunks = map.listerThings.ThingsInGroup(ThingRequestGroup.Chunk);
            for (int i = chunks.Count - 1; i >= 0; i--)
            {
                Thing t = chunks[i];
                if (t.Destroyed || IsQuestThing(t)) continue;

                t.Destroy(DestroyMode.Vanish);
                removed++;
            }

            // Steel slag (fallback)
            var all = map.listerThings.AllThings;
            for (int i = all.Count - 1; i >= 0; i--)
            {
                Thing t = all[i];
                if (t.Destroyed || IsQuestThing(t)) continue;

                if (t.def?.defName == "ChunkSlagSteel")
                {
                    t.Destroy(DestroyMode.Vanish);
                    removed++;
                }
            }

            Messages.Message(
                $"Deleted chunks / slag: {removed}",
                MessageTypeDefOf.TaskCompletion,
                false);
        }

        // ------------------------------------------------------------
        // DELETE FOREIGN CORPSES
        // ------------------------------------------------------------

        [DebugAction(
            "Hard RimWorld Optimization",
            "Delete non-player, non-quest corpses on current map",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DeleteForeignCorpsesOnMap()
        {
            Map map = Find.CurrentMap;
            if (map == null) return;

            int removed = 0;

            var corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
            for (int i = corpses.Count - 1; i >= 0; i--)
            {
                if (corpses[i] is not Corpse corpse) continue;
                if (corpse.Destroyed || IsQuestThing(corpse)) continue;

                Pawn pawn = corpse.InnerPawn;
                if (pawn?.Faction == Faction.OfPlayer) continue;

                corpse.Destroy(DestroyMode.Vanish);
                removed++;
            }

            Messages.Message(
                $"Deleted foreign corpses: {removed}",
                MessageTypeDefOf.TaskCompletion,
                false);
        }
    }
}
