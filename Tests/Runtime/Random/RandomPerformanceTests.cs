namespace UnityHelpers.Tests.Random
{
    using NUnit.Framework;
    using System.Diagnostics;
    using System;
    using UnityHelpers.Core.Random;

    public sealed class RandomPerformanceTests
    {
        [Test]
        public void Benchmark()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(1);

            UnityEngine.Debug.Log($"| Random | Next | NextFloat | NextDouble |");
            UnityEngine.Debug.Log($"| ------ | ---- | --------- | ---------- |");
            
            RunTest(new PcgRandom(), timeout);
            RunTest(new SystemRandom(), timeout);
            RunTest(new SquirrelRandom(), timeout);
            RunTest(new XorShiftRandom(), timeout);
        }

        private void RunTest<T>(T random, TimeSpan timeout) where T : IRandom
        {
            int nextInt = RunTest(timeout, random, test => test.Next());
            int nextFloat = RunTest(timeout, random, test => test.NextFloat());
            int nextDouble = RunTest(timeout, random, test => test.NextDouble());

            UnityEngine.Debug.Log($"| {random.GetType().Name} | {(nextInt / timeout.TotalSeconds):N0} | {(nextFloat / timeout.TotalSeconds):N0} | {(nextDouble / timeout.TotalSeconds):N0} |");
        }

        private int RunTest<T>(TimeSpan timeout, T random, Action<T> test) where T : IRandom
        {
            int count = 0;
            Stopwatch timer = Stopwatch.StartNew();
            do
            {
                test(random);
                ++count;
            }
            while (timer.Elapsed < timeout);

            return count;
        }
    }
}
