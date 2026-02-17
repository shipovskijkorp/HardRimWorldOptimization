using System.Collections.Generic;

namespace MyRimWorldMod
{
    /// <summary>
    /// Tiny list pool to reduce GC churn during quest selection.
    /// Keep it simple (no thread-safety needed for RimWorld main thread).
    /// </summary>
    internal static class QuestTweaks_ListPool<T>
    {
        private static readonly Stack<List<T>> Pool = new Stack<List<T>>(32);

        public static List<T> Get()
        {
            lock (Pool)
            {
                if (Pool.Count > 0)
                {
                    var list = Pool.Pop();
                    list.Clear();
                    return list;
                }
            }

            return new List<T>();
        }

        public static void Return(List<T> list)
        {
            if (list == null) return;
            list.Clear();

            lock (Pool)
            {
                // avoid unbounded growth
                if (Pool.Count < 128)
                    Pool.Push(list);
            }
        }
    }
}
