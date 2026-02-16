using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

#nullable disable
namespace MyRimWorldMod;

public class Cleaner
{
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
