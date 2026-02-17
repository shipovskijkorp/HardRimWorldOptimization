using Verse;

namespace MyRimWorldMod
{
    public class OptimizationSettings : ModSettings
    {
        // --- Animals optimization ---
        public bool throttleWildAnimals = true;
        public int throttleIntervalTicks = 1800;
        public bool excludePredators = true;
        public bool excludeNearColonists = true;
        public int excludeNearColonistsRadius = 30;

        // --- Faction tab UI optimization ---
        public bool compactEnemyIconsInFactionRow = true;
        public int compactEnemyIconsMaxRowsWithoutScaling = 4;
        public bool compactEnemyIconsVerboseLogging = false;


        // --- Turret optimization ---
        public bool optimizePlayerTurrets = true;

        // When there is NO hostile active threat, allow a full turret target scan only once per N ticks.
        // 500 ticks ≈ 8.3 seconds.
        public int turretIdleScanIntervalTicks = 500;

        // How often to refresh "danger present" cache per map.
        public int turretDangerRefreshIntervalTicks = 500;

        public bool turretVerboseLogging = false;


        // --- Prisoner optimization ---
        public bool throttlePrisoners = true;

        // 60 = 1 сек, 120 = 2 сек, 250 = ~4.1 сек
        public int prisonerThrottleIntervalTicks = 120;

        public bool prisonersExcludeNearColonists = true;
        public int prisonersNearColonistRadius = 25;

        public bool prisonerVerboseLogging = false;

        // --- Quest generation tweaks ---
        public bool tweakQuestGeneration = true;
        public bool questUseFastNaturalRandomChooser = true;
        public int questMaxCanRunChecksPerSelection = 12;
        public bool questNormalizeZeroPointsForGeneration = true;
        public bool questUseAncientComplexFallback = true;
        public bool questVerboseLogging = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref throttleWildAnimals, "throttleWildAnimals", true);
            Scribe_Values.Look(ref throttleIntervalTicks, "throttleIntervalTicks", 1800);

            Scribe_Values.Look(ref excludePredators, "excludePredators", true);
            Scribe_Values.Look(ref excludeNearColonists, "excludeNearColonists", true);
            Scribe_Values.Look(ref excludeNearColonistsRadius, "excludeNearColonistsRadius", 30);

            Scribe_Values.Look(ref optimizePlayerTurrets, "optimizePlayerTurrets", true);
            Scribe_Values.Look(ref turretIdleScanIntervalTicks, "turretIdleScanIntervalTicks", 500);
            Scribe_Values.Look(ref turretDangerRefreshIntervalTicks, "turretDangerRefreshIntervalTicks", 500);
            Scribe_Values.Look(ref turretVerboseLogging, "turretVerboseLogging", false);

            Scribe_Values.Look(ref compactEnemyIconsInFactionRow, "compactEnemyIconsInFactionRow", true);
            Scribe_Values.Look(ref compactEnemyIconsMaxRowsWithoutScaling, "compactEnemyIconsMaxRowsWithoutScaling", 4);
            Scribe_Values.Look(ref compactEnemyIconsVerboseLogging, "compactEnemyIconsVerboseLogging", false);

            Scribe_Values.Look(ref throttlePrisoners, "throttlePrisoners", true);
            Scribe_Values.Look(ref prisonerThrottleIntervalTicks, "prisonerThrottleIntervalTicks", 120);
            Scribe_Values.Look(ref prisonersExcludeNearColonists, "prisonersExcludeNearColonists", true);
            Scribe_Values.Look(ref prisonersNearColonistRadius, "prisonersNearColonistRadius", 25);
            Scribe_Values.Look(ref prisonerVerboseLogging, "prisonerVerboseLogging", false);

            Scribe_Values.Look(ref tweakQuestGeneration, "tweakQuestGeneration", true);
            Scribe_Values.Look(ref questUseFastNaturalRandomChooser, "questUseFastNaturalRandomChooser", true);
            Scribe_Values.Look(ref questMaxCanRunChecksPerSelection, "questMaxCanRunChecksPerSelection", 12);
            Scribe_Values.Look(ref questNormalizeZeroPointsForGeneration, "questNormalizeZeroPointsForGeneration", true);
            Scribe_Values.Look(ref questUseAncientComplexFallback, "questUseAncientComplexFallback", true);
            Scribe_Values.Look(ref questVerboseLogging, "questVerboseLogging", false);
        }
    }
}
