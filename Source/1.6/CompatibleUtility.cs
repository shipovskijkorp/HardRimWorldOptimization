using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

#nullable disable
namespace MyRimWorldMod;

public static class CompatibleUtility
{
    private static Type Hierarchy = AccessTools.TypeByName("VFEEmpire.WorldComponent_Hierarchy");
    private static FieldInfo Hierarchy_TitleHolders = AccessTools.Field(CompatibleUtility.Hierarchy, "TitleHolders");
    private static HediffDef Transcendent = DefDatabase<HediffDef>.GetNamedSilentFail("VRE_Transcendent");
    private static FactionDef LTS_Courier = DefDatabase<FactionDef>.GetNamedSilentFail(nameof(LTS_Courier));
    private static FactionDef LTS_Tenant = DefDatabase<FactionDef>.GetNamedSilentFail(nameof(LTS_Tenant));

    public static bool InHierarchy(Pawn pawn)
    {
        object component = (object)Find.World.GetComponent(CompatibleUtility.Hierarchy);
        return (CompatibleUtility.Hierarchy_TitleHolders.GetValue(component) as List<Pawn>).Contains(pawn);
    }

    public static bool IsTranscendent(Pawn pawn)
    {
        HediffSet hediffSet = pawn.health.hediffSet;
        return hediffSet != null && hediffSet.HasHediff(CompatibleUtility.Transcendent, false);
    }

    public static bool TenantReserved(Pawn pawn)
    {
        return ((Thing)pawn).Faction.def == CompatibleUtility.LTS_Courier || ((Thing)pawn).Faction.def == CompatibleUtility.LTS_Tenant;
    }
}
