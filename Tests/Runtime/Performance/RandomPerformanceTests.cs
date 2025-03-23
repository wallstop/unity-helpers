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
            TimeSpan timeout = TimeSpan.FromSeconds(1);

            UnityEngine.Debug.Log(
                "| Random | NextBool | Next | NextUInt | NextFloat | NextDouble | NextUint - Range | NextInt - Range |"
            );
            UnityEngine.Debug.Log(
                "| ------ | -------- | ---- | -------- | --------- | ---------- | ---------------- | --------------- |"
            );

            RunTest(new PcgRandom(), timeout);
            RunTest(new SystemRandom(), timeout);
            RunTest(new SquirrelRandom(), timeout);
            RunTest(new XorShiftRandom(), timeout);
            RunTest(new DotNetRandom(), timeout);
            RunTest(new WyRandom(), timeout);
            RunTest(new SplitMix64(), timeout);
            RunTest(new RomuDuo(), timeout);
            RunTest(new XorShiroRandom(), timeout);
            RunTest(new UnityRandom(), timeout);
            RunTest(new LinearCongruentialGenerator(), timeout);
        }

        private static void RunTest<T>(T random, TimeSpan timeout)
            where T : IRandom
        {
            int nextBool = RunNextBool(timeout, random);
            int nextInt = RunNext(timeout, random);
            int nextUint = RunNextUint(timeout, random);
            int nextFloat = RunNextFloat(timeout, random);
            int nextDouble = RunNextDouble(timeout, random);
            int nextUintRange = RunNextUintRange(timeout, random);
            int nextIntRange = RunNextIntRange(timeout, random);

            UnityEngine.Debug.Log(
                $"| {random.GetType().Name} | "
                    + $"{(nextBool / timeout.TotalSeconds):N0} | "
                    + $"{(nextInt / timeout.TotalSeconds):N0} | "
                    + $"{(nextUint / timeout.TotalSeconds):N0} | "
                    + $"{(nextFloat / timeout.TotalSeconds):N0} | "
                    + $"{(nextDouble / timeout.TotalSeconds):N0} |"
                    + $"{(nextUintRange / timeout.TotalSeconds):N0} |"
                    + $"{(nextIntRange / timeout.TotalSeconds):N0} |"
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

        private static int RunNextBool<T>(TimeSpan timeout, T random)
            where T : IRandom
        {
            int count = 0;
            Stopwatch timer = Stopwatch.StartNew();
            do
            {
                _ = random.NextBool();
                ++count;
            } while (timer.Elapsed < timeout);

            return count;
        }

        private static int RunNextUint<T>(TimeSpan timeout, T random)
            where T : IRandom
        {
            int count = 0;
            Stopwatch timer = Stopwatch.StartNew();
            do
            {
                _ = random.NextUint();
                ++count;
            } while (timer.Elapsed < timeout);

            return count;
        }

        private static int RunNextUintRange<T>(TimeSpan timeout, T random)
            where T : IRandom
        {
            int count = 0;
            Stopwatch timer = Stopwatch.StartNew();
            do
            {
                _ = random.NextUint(1_000);
                ++count;
            } while (timer.Elapsed < timeout);

            return count;
        }

        private static int RunNextIntRange<T>(TimeSpan timeout, T random)
            where T : IRandom
        {
            int count = 0;
            Stopwatch timer = Stopwatch.StartNew();
            do
            {
                _ = random.Next(1_000);
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
