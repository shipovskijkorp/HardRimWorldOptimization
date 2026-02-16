using Verse;

namespace MyRimWorldMod
{
    public class OptimizationSettings : ModSettings
    {
        public bool throttleWildAnimals = true;
        public int throttleIntervalTicks = 1800;
        public bool excludePredators = true;
        public bool excludeNearColonists = true;
        public int excludeNearColonistsRadius = 30;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref throttleWildAnimals, "throttleWildAnimals", true);
            Scribe_Values.Look(ref throttleIntervalTicks, "throttleIntervalTicks", 1800);

            Scribe_Values.Look(ref excludePredators, "excludePredators", true);
            Scribe_Values.Look(ref excludeNearColonists, "excludeNearColonists", true);
            Scribe_Values.Look(ref excludeNearColonistsRadius, "excludeNearColonistsRadius", 30);
        }
    }
}
