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

            List<RandomBenchmarkResult> results = new()
            {
                RunBenchmark(new DotNetRandom(), timeout),
                RunBenchmark(new LinearCongruentialGenerator(), timeout),
                RunBenchmark(new IllusionFlow(), timeout),
                RunBenchmark(new PcgRandom(), timeout),
                RunBenchmark(new RomuDuo(), timeout),
                RunBenchmark(new SplitMix64(), timeout),
                RunBenchmark(new FlurryBurstRandom(), timeout),
                RunBenchmark(new SquirrelRandom(), timeout),
                RunBenchmark(new SystemRandom(), timeout),
                RunBenchmark(new UnityRandom(), timeout),
                RunBenchmark(new WyRandom(), timeout),
                RunBenchmark(new XorShiftRandom(), timeout),
                RunBenchmark(new XoroShiroRandom(), timeout),
                RunBenchmark(new PhotonSpinRandom(), timeout),
                RunBenchmark(new StormDropRandom(), timeout),
                RunBenchmark(new BlastCircuitRandom(), timeout),
                RunBenchmark(new WaveSplatRandom(), timeout),
            };

            ApplySpeedBuckets(results);

            List<string> markdown = RandomBenchmarkMarkdownBuilder.BuildTables(results);

            BenchmarkReadmeUpdater.UpdateSection(
                "RANDOM_BENCHMARKS",
                markdown,
                "Docs/RANDOM_PERFORMANCE.md"
            );

            UnityEngine.Debug.Log("Random benchmark summary generated.");
        }

        private static RandomBenchmarkResult RunBenchmark<T>(T random, TimeSpan timeout)
            where T : IRandom
        {
            int nextBool = RunNextBool(timeout, random);
            int nextInt = RunNext(timeout, random);
            int nextUint = RunNextUint(timeout, random);
            int nextFloat = RunNextFloat(timeout, random);
            int nextDouble = RunNextDouble(timeout, random);
            int nextUintRange = RunNextUintRange(timeout, random);
            int nextIntRange = RunNextIntRange(timeout, random);

            double durationSeconds = timeout.TotalSeconds;

            RandomGeneratorMetadata metadata = RandomGeneratorMetadataRegistry.Snapshot(random);

            return new RandomBenchmarkResult(
                random.GetType(),
                nextBool / durationSeconds,
                nextInt / durationSeconds,
                nextUint / durationSeconds,
                nextFloat / durationSeconds,
                nextDouble / durationSeconds,
                nextUintRange / durationSeconds,
                nextIntRange / durationSeconds,
                metadata
            );
        }

        private static void ApplySpeedBuckets(List<RandomBenchmarkResult> results)
        {
            if (results == null || results.Count == 0)
            {
                return;
            }

            double maxNextUint = 0;
            foreach (RandomBenchmarkResult result in results)
            {
                if (result.NextUintPerSecond > maxNextUint)
                {
                    maxNextUint = result.NextUintPerSecond;
                }
            }

            if (maxNextUint <= 0)
            {
                return;
            }

            foreach (RandomBenchmarkResult result in results)
            {
                double ratio = result.NextUintPerSecond / maxNextUint;
                result.SpeedRatio = ratio;
                result.SpeedBucket = RandomSpeedBucketExtensions.FromRatio(ratio);
            }
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
