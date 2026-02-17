using System.Collections.Generic;
using Verse;

namespace MyRimWorldMod
{
    public class PrisonerThrottleComponent : GameComponent
    {
        private Dictionary<int, int> nextTickByPawn = new Dictionary<int, int>();
        private int nextCleanupTick;

        public static PrisonerThrottleComponent Instance;

        public PrisonerThrottleComponent(Game game)
        {
            Instance = this;
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            Instance = this;
        }

        public bool CanDoFullTick(Pawn pawn, int now)
        {
            if (pawn == null) return true;

            if (now >= nextCleanupTick)
            {
                Cleanup(now);
                nextCleanupTick = now + 60000; // ~1 day
            }

            int id = pawn.thingIDNumber;
            int next;
            return !nextTickByPawn.TryGetValue(id, out next) || now >= next;
        }

        public void MarkDidFullTick(Pawn pawn, int now, int interval)
        {
            if (pawn == null) return;
            nextTickByPawn[pawn.thingIDNumber] = now + interval;
        }

        private void Cleanup(int now)
        {
            if (nextTickByPawn == null || nextTickByPawn.Count == 0) return;

            int cutoff = now - 60000;
            List<int> remove = null;

            foreach (var kv in nextTickByPawn)
            {
                if (kv.Value < cutoff)
                {
                    if (remove == null) remove = new List<int>();
                    remove.Add(kv.Key);
                }
            }

            if (remove == null) return;
            for (int i = 0; i < remove.Count; i++)
                nextTickByPawn.Remove(remove[i]);
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref nextTickByPawn, "prisoner_nextTickByPawn", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && nextTickByPawn == null)
                nextTickByPawn = new Dictionary<int, int>();
        }
    }
}
