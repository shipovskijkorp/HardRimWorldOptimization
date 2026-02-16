using System.Collections.Generic;
using Verse;

namespace MyRimWorldMod
{
    public class WildAnimalThrottleComponent : GameComponent
    {
        // pawnID -> next allowed full tick
        private Dictionary<int, int> nextTickByPawn = new();

        public static WildAnimalThrottleComponent Instance;

        public WildAnimalThrottleComponent(Game game)
        {
            Instance = this;
        }

        public bool CanDoFullTick(Pawn pawn, int currentTick)
        {
            int id = pawn.thingIDNumber;
            return !nextTickByPawn.TryGetValue(id, out int next) || currentTick >= next;
        }

        public void MarkDidFullTick(Pawn pawn, int currentTick, int intervalTicks)
        {
            nextTickByPawn[pawn.thingIDNumber] = currentTick + intervalTicks;
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref nextTickByPawn, "nextTickByPawn", LookMode.Value, LookMode.Value);
        }
    }
}
