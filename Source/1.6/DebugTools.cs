using LudeonTK;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
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
    
    public static AcceptanceReport CanSafelyClean(Pawn pawn)
        {
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

        [DebugAction("Hard RimWorld Optimization", "Clean", false, false, false, false, false, 0, false)]
        public static void Clean()
        {
            List<Pawn> pawnList = new();

            foreach (Pawn pawn in Find.WorldPawns.AllPawnsAliveOrDead)
            {
                if (CanSafelyClean(pawn))
                    pawnList.Add(pawn);
            }

            Log.Message($"Cleaner Service: clean up {pawnList.Count} pawns");

            foreach (Pawn pawn in pawnList)
                Find.WorldPawns.RemovePawn(pawn);
        }

        [DebugAction("Hard RimWorld Optimization", "Clean: Include family", false, false, false, false, false, 0, false)]
        public static void CleanIncludeFamily()
        {
            List<Pawn> pawnList = new();

            foreach (Pawn pawn in Find.WorldPawns.AllPawnsAliveOrDead)
            {
                AcceptanceReport report = CanSafelyClean(pawn);
                if (report.Accepted || report.Reason == "colonist family")
                    pawnList.Add(pawn);
            }

            Log.Message($"Cleaner Service: clean up {pawnList.Count} pawns");

            foreach (Pawn pawn in pawnList)
                Find.WorldPawns.RemovePawn(pawn);
        }

        [DebugAction("Hard RimWorld Optimization", "Clean a selected pawn", false, false, false, false, false, 0, false)]
        public static void CleanOne()
        {
            List<DebugMenuOption> options = new();

            options.Add(new DebugMenuOption("[!] README [!]", DebugMenuOptionMode.Action, () =>
            {
                Log.Warning("[Cleaner Service] The suffix after pawn's name indicates its reason of avoid being cleaned up. It's unsafe to clean a pawn with suffix, if you really want, save your game before do it.");
                Log.TryOpenLogWindow();
            }));

            foreach (Pawn pawn in Find.WorldPawns.AllPawnsAliveOrDead)
            {
                Pawn pLocal = pawn;
                string label = pawn.LabelShort;

                AcceptanceReport report = CanSafelyClean(pawn);
                if (!report.Accepted)
                    label = $"{label} [{report.Reason}]";

                options.Add(new DebugMenuOption(label, DebugMenuOptionMode.Action, () =>
                    Find.WorldPawns.RemovePawn(pLocal)));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(options, null));
        }
    }
}

