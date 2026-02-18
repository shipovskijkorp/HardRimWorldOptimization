using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MyRimWorldMod
{
    public static class DebugTools
    {
        private static bool IsQuestThing(Thing t)
        {
            if (t == null) return false;

            // Quest-tagged things (common for spawned quest items)
            if (t.questTags != null && t.questTags.Count > 0)
                return true;

            // Quest reservation check exists for Pawn (not generic Thing).
            if (t is Pawn pawn)
                return QuestUtility.IsReservedByQuestOrQuestBeingGenerated(pawn);

            // Corpses can be quest-related via inner pawn.
            if (t is Corpse corpse && corpse.InnerPawn != null)
                return QuestUtility.IsReservedByQuestOrQuestBeingGenerated(corpse.InnerPawn);

            return false;
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

        // =========================
        // NEW: Biocoded weapon cleanup
        // =========================
        [DebugAction("Hard RimWorld Optimization", "Delete biocoded weapons (map, simple)",
            actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DeleteBiocodedWeaponsOnMap_Simple()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                Messages.Message("No current map.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            int removed = 0;
            int skipped = 0;

            var weapons = map.listerThings.ThingsInGroup(ThingRequestGroup.Weapon);
            for (int i = weapons.Count - 1; i >= 0; i--)
            {
                Thing t = weapons[i];
                if (t == null || t.Destroyed) continue;
                if (!t.Spawned) continue;

                if (t.def == null || !t.def.IsWeapon) continue;
                if (IsQuestThing(t)) { skipped++; continue; }

                CompBiocodable bio = t.TryGetComp<CompBiocodable>();
                if (bio == null || !bio.Biocoded) continue;

                Pawn owner = bio.CodedPawn;

                if (owner != null)
                {
                    // Rule 3: owner is player's colonist (regardless of state)
                    if (owner.Faction == Faction.OfPlayer)
                    {
                        skipped++;
                        continue;
                    }

                    if (!owner.Dead)
                    {
                        // Rule 1: owner alive and on the map
                        if (owner.Spawned)
                        {
                            skipped++;
                            continue;
                        }

                        // Rule 2: owner alive and in a container (sarcophagus, transport, inventory, etc.)
                        if (owner.ParentHolder != null)
                        {
                            skipped++;
                            continue;
                        }
                    }
                }

                // Otherwise: safe to delete
                t.Destroy(DestroyMode.Vanish);
                removed++;
            }

            Messages.Message($"Deleted biocoded weapons: {removed}, skipped: {skipped}", MessageTypeDefOf.TaskCompletion, false);
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
            if (t == null) return false;
            if (t.def == null) return false;

            // Only ever act on actual weapon items.
            if (!t.def.IsWeapon) return false;
            if (t.def.category != ThingCategory.Item) return false;

            // Extra safety: many mods tag weird stuff as "weapon". Require it to live in Weapons category.
            if (t.def.thingCategories == null || !t.def.thingCategories.Contains(ThingCategoryDefOf.Weapons))
                return false;

            // Never delete quest-related items.
            if (IsQuestThing(t))
                return false;

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

        public static AcceptanceReport CanSafelyClean(Pawn pawn)
        {
            if (pawn == null)
                return "null";

            if (pawn.Faction != null && pawn.Faction == Faction.OfPlayer)
                return "of player";

            if (pawn.IsSlaveOfColony || pawn.IsPrisonerOfColony)
                return "colony property";

            if (QuestUtility.IsReservedByQuestOrQuestBeingGenerated(pawn))
                return "quest";

            if (PawnUtility.IsFactionLeader(pawn))
                return "faction leader";

            if (PawnUtility.ForSaleBySettlement(pawn))
                return "for sale";

            if (pawn.relations != null && pawn.relations.FamilyByBlood.Any(p => p.IsColonist))
                return "colonist family";

            if (pawn.Corpse != null && (pawn.Corpse.Spawned || pawn.Corpse.everBuriedInSarcophagus))
                return "corpse exists";

            if (pawn.ParentHolder != null)
                return "inside something";

            if (Flag.VFEEmpire && CompatibleUtility.InHierarchy(pawn))
                return "VFEEmpire";

            if (Flag.VREArchon && CompatibleUtility.IsTranscendent(pawn))
                return "VREArchon";

            if (Flag.LTSTenant && CompatibleUtility.TenantReserved(pawn))
                return "LTSTenant";

            return true;
        }

        [DebugAction("Hard RimWorld Optimization", "Clean World Pawns", false, false, false, false, false, 0, false)]
        public static void Clean()
        {
            List<Pawn> pawnList = new();

            foreach (Pawn pawn in Find.WorldPawns.AllPawnsAliveOrDead)
            {
                if (pawn == null) continue;
                if (CanSafelyClean(pawn))
                    pawnList.Add(pawn);
            }

            Log.Message($"Cleaner Service: clean up {pawnList.Count} pawns");

            foreach (Pawn pawn in pawnList)
            {
                if (pawn == null) continue;
                try
                {
                    Find.WorldPawns.RemovePawn(pawn);
                }
                catch (Exception e)
                {
                    Log.Error($"[Cleaner Service] Failed to remove pawn '{pawn?.LabelShort ?? "<null>"}': {e}");
                }
            }
        }

        [DebugAction("Hard RimWorld Optimization", "Clean World Pawns: Include family", false, false, false, false, false, 0, false)]
        public static void CleanIncludeFamily()
        {
            List<Pawn> pawnList = new();

            foreach (Pawn pawn in Find.WorldPawns.AllPawnsAliveOrDead)
            {
                if (pawn == null) continue;
                AcceptanceReport report = CanSafelyClean(pawn);
                if (report.Accepted || report.Reason == "colonist family")
                    pawnList.Add(pawn);
            }

            Log.Message($"Cleaner Service: clean up {pawnList.Count} pawns");

            foreach (Pawn pawn in pawnList)
            {
                if (pawn == null) continue;
                try
                {
                    Find.WorldPawns.RemovePawn(pawn);
                }
                catch (Exception e)
                {
                    Log.Error($"[Cleaner Service] Failed to remove pawn '{pawn?.LabelShort ?? "<null>"}': {e}");
                }
            }
        }
    }
}
