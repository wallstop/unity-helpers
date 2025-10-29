namespace WallstopStudios.UnityHelpers.Core.Diagnostics
{
    using System.Threading;

#if UNITY_EDITOR
    public static class RelationalComponentInstrumentation
    {
        private static int parentFastPathHits;
        private static int parentFastPathFallbacks;
        private static int childFastPathHits;
        private static int childFastPathFallbacks;

        internal static void RecordParentFastPath(bool usedFastPath)
        {
            if (usedFastPath)
            {
                Interlocked.Increment(ref parentFastPathHits);
            }
            else
            {
                Interlocked.Increment(ref parentFastPathFallbacks);
            }
        }

        internal static void RecordChildFastPath(bool usedFastPath)
        {
            if (usedFastPath)
            {
                Interlocked.Increment(ref childFastPathHits);
            }
            else
            {
                Interlocked.Increment(ref childFastPathFallbacks);
            }
        }

        public static RelationalFastPathStats GetAggregates()
        {
            return new RelationalFastPathStats(
                parentFastPathHits,
                parentFastPathFallbacks,
                childFastPathHits,
                childFastPathFallbacks
            );
        }

        public static string GetSummary()
        {
            RelationalFastPathStats stats = GetAggregates();
            return $"Parent Fast Path: {stats.ParentHits} hits / {stats.ParentFallbacks} fallbacks\n"
                + $"Child Fast Path: {stats.ChildHits} hits / {stats.ChildFallbacks} fallbacks";
        }
    }

    public readonly struct RelationalFastPathStats
    {
        internal RelationalFastPathStats(
            int parentHits,
            int parentFallbacks,
            int childHits,
            int childFallbacks
        )
        {
            ParentHits = parentHits;
            ParentFallbacks = parentFallbacks;
            ChildHits = childHits;
            ChildFallbacks = childFallbacks;
        }

        internal int ParentHits { get; }

        internal int ParentFallbacks { get; }

        internal int ChildHits { get; }

        internal int ChildFallbacks { get; }
    }
#endif
}
