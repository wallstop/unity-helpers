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
            Stopwatch timer = Stopwatch.StartNew();
            int nextInt = RunTest(timer, timeout, random, test => test.Next());
            int nextFloat = RunTest(timer, timeout, random, test => test.NextFloat());
            int nextDouble = RunTest(timer, timeout, random, test => test.NextDouble());

            UnityEngine.Debug.Log($"| {random.GetType().Name} | {(nextInt / timeout.TotalSeconds):N0} | {(nextFloat / timeout.TotalSeconds):N0} | {(nextDouble / timeout.TotalSeconds):N0} |");
        }

        private int RunTest<T>(Stopwatch timer, TimeSpan timeout, T random, Action<T> test) where T : IRandom
        {
            int count = 0;
            timer.Restart();
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
