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

        [DebugAction("Hard RimWorld Optimization", "Delete chunks & slag on current map",
            actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DeleteChunksAndSlagOnMap()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                Messages.Message("No current map.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            int removed = 0;

            var chunks = map.listerThings.ThingsInGroup(ThingRequestGroup.Chunk);
            for (int i = chunks.Count - 1; i >= 0; i--)
            {
                Thing t = chunks[i];
                if (t == null || t.Destroyed) continue;
                if (IsQuestThing(t)) continue;

                t.Destroy(DestroyMode.Vanish);
                removed++;
            }

            var all = map.listerThings.AllThings;
            for (int i = all.Count - 1; i >= 0; i--)
            {
                Thing t = all[i];
                if (t == null || t.Destroyed) continue;
                if (IsQuestThing(t)) continue;

                if (t.def != null && t.def.defName == "ChunkSlagSteel")
                {
                    t.Destroy(DestroyMode.Vanish);
                    removed++;
                }
            }

            Messages.Message($"Deleted chunks/slag: {removed}", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Hard RimWorld Optimization", "Delete non-player, non-quest corpses on current map",
            actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DeleteForeignCorpsesOnMap()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                Messages.Message("No current map.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            int removed = 0;

            var corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
            for (int i = corpses.Count - 1; i >= 0; i--)
            {
                if (corpses[i] is not Corpse corpse) continue;
                if (corpse == null || corpse.Destroyed) continue;
                if (IsQuestThing(corpse)) continue;

                Pawn inner = corpse.InnerPawn;
                Faction f = inner?.Faction;
                if (f == Faction.OfPlayer) continue;

                corpse.Destroy(DestroyMode.Vanish);
                removed++;
            }

            Messages.Message($"Deleted foreign corpses: {removed}", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Hard RimWorld Optimization", "Delete ALL filth (map)",
            actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DeleteAllFilthOnMap()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                Messages.Message("No current map.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            int removed = 0;

            var filths = map.listerThings.ThingsInGroup(ThingRequestGroup.Filth);
            for (int i = filths.Count - 1; i >= 0; i--)
            {
                Thing t = filths[i];
                if (t == null || t.Destroyed) continue;
                if (IsQuestThing(t)) continue;

                t.Destroy(DestroyMode.Vanish);
                removed++;
            }

            Messages.Message($"Deleted filth: {removed}", MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Hard RimWorld Optimization", "Cleanup raid trash: Awful/Poor + Neolithic weapons (map)",
            actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void CleanupRaidTrashOnMap()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                Messages.Message("No current map.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            int removedQuality = 0;
            int removedNeolithic = 0;

            var all = map.listerThings.AllThings;
            for (int i = all.Count - 1; i >= 0; i--)
            {
                Thing t = all[i];
                if (t == null || t.Destroyed) continue;
                if (!t.Spawned) continue;

                if (IsQuestThing(t)) continue;
                if (t.Faction == Faction.OfPlayer) continue;

                bool isWeapon = t.def != null && t.def.IsWeapon;
                bool isApparel = t.def != null && t.def.IsApparel;
                if (!isWeapon && !isApparel) continue;

                if (HasLowQuality(t))
                {
                    t.Destroy(DestroyMode.Vanish);
                    removedQuality++;
                    continue;
                }

                if (isWeapon && IsNeolithicTrashWeapon(t))
                {
                    t.Destroy(DestroyMode.Vanish);
                    removedNeolithic++;
                    continue;
                }
            }

            Messages.Message(
                $"Cleanup done. Deleted low-quality: {removedQuality}, deleted neolithic weapons: {removedNeolithic}",
                MessageTypeDefOf.TaskCompletion, false);
        }

        private static bool HasLowQuality(Thing t)
        {
            CompQuality cq = t.TryGetComp<CompQuality>();
            if (cq == null) return false;

            QualityCategory q = cq.Quality;
            return q == QualityCategory.Awful || q == QualityCategory.Poor;
        }

        private static bool IsNeolithicTrashWeapon(Thing t)
        {
            if (t?.def == null || !t.def.IsWeapon) return false;

            TechLevel tl = t.def.techLevel;
            if (tl <= TechLevel.Neolithic)
                return true;

            var tags = t.def.weaponTags;
            if (tags != null)
            {
                for (int i = 0; i < tags.Count; i++)
                {
                    string tag = tags[i];
                    if (tag == null) continue;
                    if (tag.Contains("Neolithic") || tag.Contains("Tribal"))
                        return true;
                }
            }

            string dn = t.def.defName ?? "";
            if (dn.Contains("Bow") || dn.Contains("ShortBow") || dn.Contains("GreatBow") ||
                dn.Contains("Club") || dn.Contains("Spear") || dn.Contains("Pila") || dn.Contains("Knife") ||
                dn.Contains("Mace") || dn.Contains("Hammer"))
                return true;


            return false;
        }
    }
}
