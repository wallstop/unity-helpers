namespace UnityHelpers.Tests.Performance
{
    using System;
    using System.Diagnostics;
    using NUnit.Framework;
    using UnityHelpers.Core.Random;

    public sealed class RandomPerformanceTests
    {
        [Test]
        public void Benchmark()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(1.125);

            UnityEngine.Debug.Log($"| Random | Next | NextFloat | NextDouble |");
            UnityEngine.Debug.Log($"| ------ | ---- | --------- | ---------- |");

            RunTest(new PcgRandom(), timeout);
            RunTest(new SystemRandom(), timeout);
            RunTest(new SquirrelRandom(), timeout);
            RunTest(new XorShiftRandom(), timeout);
            RunTest(new DotNetRandom(), timeout);
            RunTest(new WyRandom(), timeout);
        }

        private static void RunTest<T>(T random, TimeSpan timeout)
            where T : IRandom
        {
            int nextInt = RunNext(timeout, random);
            int nextFloat = RunNextFloat(timeout, random);
            int nextDouble = RunNextDouble(timeout, random);

            UnityEngine.Debug.Log(
                $"| {random.GetType().Name} | {(nextInt / timeout.TotalSeconds):N0} | {(nextFloat / timeout.TotalSeconds):N0} | {(nextDouble / timeout.TotalSeconds):N0} |"
            );
        }

        // Copy-pasta'd for maximum speed
        private static int RunNext<T>(TimeSpan timeout, T random)
            where T : IRandom
        {
            int count = 0;
            Stopwatch timer = Stopwatch.StartNew();
            do
            {
                _ = random.Next();
                ++count;
            } while (timer.Elapsed < timeout);

            return count;
        }

        private static int RunNextFloat<T>(TimeSpan timeout, T random)
            where T : IRandom
        {
            int count = 0;
            Stopwatch timer = Stopwatch.StartNew();
            do
            {
                _ = random.NextFloat();
                ++count;
            } while (timer.Elapsed < timeout);

            return count;
        }

        private static int RunNextDouble<T>(TimeSpan timeout, T random)
            where T : IRandom
        {
            int count = 0;
            Stopwatch timer = Stopwatch.StartNew();
            do
            {
                _ = random.NextDouble();
                ++count;
            } while (timer.Elapsed < timeout);

            return count;
        }
    }
}
