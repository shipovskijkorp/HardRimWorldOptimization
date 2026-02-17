using System.Collections.Generic;
using Verse;

namespace MyRimWorldMod
{
    public class WildAnimalThrottleComponent : GameComponent
    {
        // pawnID -> next allowed full tick
        private Dictionary<int, int> nextTickByPawn = new Dictionary<int, int>();

        public static WildAnimalThrottleComponent Instance;

        // cheap cleanup so the dictionary doesn't grow forever on long saves
        private int _nextCleanupTick;

        public WildAnimalThrottleComponent(Game game)
        {
            Instance = this;
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            Instance = this;
        }

        public bool CanDoFullTick(Pawn pawn, int currentTick)
        {
            if (pawn == null) return true;

            // periodic cleanup (every 60k ticks ~= 1 in-game day)
            if (currentTick >= _nextCleanupTick)
            {
                Cleanup(currentTick);
                _nextCleanupTick = currentTick + 60000;
            }

            int id = pawn.thingIDNumber;
            int next;
            return !nextTickByPawn.TryGetValue(id, out next) || currentTick >= next;
        }

        public void MarkDidFullTick(Pawn pawn, int currentTick, int intervalTicks)
        {
            if (pawn == null) return;
            nextTickByPawn[pawn.thingIDNumber] = currentTick + intervalTicks;
        }

        private void Cleanup(int currentTick)
        {
            if (nextTickByPawn == null || nextTickByPawn.Count == 0) return;

            // Remove entries far in the past to keep memory bounded.
            // If the pawn is gone, its thingIDNumber won't be used again anyway.
            int cutoff = currentTick - 60000;

            List<int> keysToRemove = null;

            foreach (var kv in nextTickByPawn)
            {
                if (kv.Value < cutoff)
                {
                    if (keysToRemove == null) keysToRemove = new List<int>();
                    keysToRemove.Add(kv.Key);
                }
            }

            if (keysToRemove == null) return;

            for (int i = 0; i < keysToRemove.Count; i++)
                nextTickByPawn.Remove(keysToRemove[i]);
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref nextTickByPawn, "nextTickByPawn", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && nextTickByPawn == null)
                nextTickByPawn = new Dictionary<int, int>();
        }
    }
}
