using System;
using System.Diagnostics;

namespace MyRimWorldMod
{
    internal static class QuestTweaks_Profiler
    {
        public static MeasureScope Measure(string name, int minMsToLog = 10)
        {
            return new MeasureScope(name, minMsToLog);
        }

        internal readonly struct MeasureScope : IDisposable
        {
            private readonly string name;
            private readonly int minMs;
            private readonly Stopwatch sw;

            public MeasureScope(string name, int minMs)
            {
                this.name = name;
                this.minMs = minMs;
                sw = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                sw.Stop();
                if (QuestTweaks_Log.Verbose && sw.ElapsedMilliseconds >= minMs)
                    QuestTweaks_Log.Message($"{name} took {sw.ElapsedMilliseconds} ms");
            }
        }
    }
}
