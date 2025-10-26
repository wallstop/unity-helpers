namespace WallstopStudios.UnityHelpers.Tests.Performance
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class RandomPerformanceTests
    {
        private const int NumInvocationsPerIteration = 100_000;

        [Test, Timeout(0)]
        public void Benchmark()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(1);

            const string headerLine =
                "| Random | NextBool | Next | NextUInt | NextFloat | NextDouble | NextUint - Range | NextInt - Range |";
            const string dividerLine =
                "| ------ | -------- | ---- | -------- | --------- | ---------- | ---------------- | --------------- |";

            List<string> tableLines = new() { headerLine, dividerLine };

            UnityEngine.Debug.Log(headerLine);
            UnityEngine.Debug.Log(dividerLine);

            LogRow(RunTest(new DotNetRandom(), timeout));
            LogRow(RunTest(new LinearCongruentialGenerator(), timeout));
            LogRow(RunTest(new IllusionFlow(), timeout));
            LogRow(RunTest(new PcgRandom(), timeout));
            LogRow(RunTest(new RomuDuo(), timeout));
            LogRow(RunTest(new SplitMix64(), timeout));
            LogRow(RunTest(new FlurryBurstRandom(), timeout));
            LogRow(RunTest(new SquirrelRandom(), timeout));
            LogRow(RunTest(new SystemRandom(), timeout));
            LogRow(RunTest(new UnityRandom(), timeout));
            LogRow(RunTest(new WyRandom(), timeout));
            LogRow(RunTest(new XorShiftRandom(), timeout));
            LogRow(RunTest(new XoroShiroRandom(), timeout));
            LogRow(RunTest(new PhotonSpinRandom(), timeout));

            BenchmarkReadmeUpdater.UpdateSection(
                "RANDOM_BENCHMARKS",
                tableLines,
                "Docs/RANDOM_PERFORMANCE.md"
            );

            void LogRow(string row)
            {
                tableLines.Add(row);
                UnityEngine.Debug.Log(row);
            }
        }

        private static string RunTest<T>(T random, TimeSpan timeout)
            where T : IRandom
        {
            int nextBool = RunNextBool(timeout, random);
            int nextInt = RunNext(timeout, random);
            int nextUint = RunNextUint(timeout, random);
            int nextFloat = RunNextFloat(timeout, random);
            int nextDouble = RunNextDouble(timeout, random);
            int nextUintRange = RunNextUintRange(timeout, random);
            int nextIntRange = RunNextIntRange(timeout, random);

            return $"| {random.GetType().Name} | "
                + $"{(nextBool / timeout.TotalSeconds):N0} | "
                + $"{(nextInt / timeout.TotalSeconds):N0} | "
                + $"{(nextUint / timeout.TotalSeconds):N0} | "
                + $"{(nextFloat / timeout.TotalSeconds):N0} | "
                + $"{(nextDouble / timeout.TotalSeconds):N0} |"
                + $"{(nextUintRange / timeout.TotalSeconds):N0} |"
                + $"{(nextIntRange / timeout.TotalSeconds):N0} |";
        }

        // Copy-pasta'd for maximum speed
        private static int RunNext<T>(TimeSpan timeout, T random)
            where T : IRandom
        {
            int count = 0;
            Stopwatch timer = Stopwatch.StartNew();
            do
            {
                for (int i = 0; i < NumInvocationsPerIteration; ++i)
                {
                    _ = random.Next();
                    ++count;
                }
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
                for (int i = 0; i < NumInvocationsPerIteration; ++i)
                {
                    _ = random.NextBool();
                    ++count;
                }
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
                for (int i = 0; i < NumInvocationsPerIteration; ++i)
                {
                    _ = random.NextUint();
                    ++count;
                }
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
                for (int i = 0; i < NumInvocationsPerIteration; ++i)
                {
                    _ = random.NextUint(1_000);
                    ++count;
                }
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
                for (int i = 0; i < NumInvocationsPerIteration; ++i)
                {
                    _ = random.Next(1_000);
                    ++count;
                }
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
                for (int i = 0; i < NumInvocationsPerIteration; ++i)
                {
                    _ = random.NextFloat();
                    ++count;
                }
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
                for (int i = 0; i < NumInvocationsPerIteration; ++i)
                {
                    _ = random.NextDouble();
                    ++count;
                }
            } while (timer.Elapsed < timeout);

            return count;
        }
    }
}
