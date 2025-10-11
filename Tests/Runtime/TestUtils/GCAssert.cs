namespace WallstopStudios.UnityHelpers.Tests.TestUtils
{
    using System;
    using NUnit.Framework;

    // Utility assertions for reliable GC allocation checks in tests.
    public static class GCAssert
    {
        // Measures allocated bytes for the current thread while executing the action.
        // Runs a short warmup to avoid JIT/cold path noise, then measures over multiple iterations.
        public static long MeasureAllocatedBytes(
            Action action,
            int warmupIterations = 5,
            int measuredIterations = 10
        )
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            for (int i = 0; i < warmupIterations; i++)
            {
                action();
            }

            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < measuredIterations; i++)
            {
                action();
            }

            long after = GC.GetAllocatedBytesForCurrentThread();
            long delta = after - before;
            return delta < 0 ? 0 : delta;
        }

        // Asserts that executing the action does not allocate beyond an optional tolerance.
        public static void DoesNotAllocate(
            Action action,
            int warmupIterations = 5,
            int measuredIterations = 10,
            long toleranceBytes = 0
        )
        {
            long allocated = MeasureAllocatedBytes(action, warmupIterations, measuredIterations);
            Assert.LessOrEqual(
                allocated,
                toleranceBytes,
                $"Expected no GC allocations (<= {toleranceBytes} bytes), but measured {allocated} bytes."
            );
        }
    }
}
