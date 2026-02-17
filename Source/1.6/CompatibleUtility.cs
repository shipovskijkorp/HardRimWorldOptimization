using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MyRimWorldMod
{
    internal static class CompatibleUtility
    {
        private static Type hierarchyType;
        private static FieldInfo hierarchyTitleHoldersField;
        private static HediffDef transcendentDef;
        private static FactionDef ltsCourierDef;
        private static FactionDef ltsTenantDef;

        private static void EnsureInitialized()
        {
            if (hierarchyType == null)
            {
                hierarchyType = AccessTools.TypeByName("VFEEmpire.WorldComponent_Hierarchy");

                if (hierarchyType != null)
                {
                    hierarchyTitleHoldersField = AccessTools.Field(hierarchyType, "TitleHolders");
                }
            }

            if (transcendentDef == null)
                transcendentDef = DefDatabase<HediffDef>.GetNamedSilentFail("VRE_Transcendent");

            if (ltsCourierDef == null)
                ltsCourierDef = DefDatabase<FactionDef>.GetNamedSilentFail("LTS_Courier");

            if (ltsTenantDef == null)
                ltsTenantDef = DefDatabase<FactionDef>.GetNamedSilentFail("LTS_Tenant");
        }

        public static bool InHierarchy(Pawn pawn)
        {
            if (pawn == null)
                return false;

            EnsureInitialized();

            if (hierarchyType == null || hierarchyTitleHoldersField == null)
                return false;

            var comp = Find.World.GetComponent(hierarchyType);
            if (comp == null)
                return false;

            var list = hierarchyTitleHoldersField.GetValue(comp) as IEnumerable;
            if (list == null)
                return false;

            foreach (var entry in list)
            {
                if (entry is Pawn p && p == pawn)
                    return true;
            }

            return false;
        }

        public static bool IsTranscendent(Pawn pawn)
        {
            if (pawn == null)
                return false;

            EnsureInitialized();

            if (transcendentDef == null)
                return false;

            return pawn.health?.hediffSet?.HasHediff(transcendentDef) == true;
        }

        public static bool TenantReserved(Pawn pawn)
        {
            if (pawn == null)
                return false;

            EnsureInitialized();

            var def = pawn.Faction?.def;
            if (def == null)
                return false;

            return def == ltsCourierDef || def == ltsTenantDef;
        }
    }
}
