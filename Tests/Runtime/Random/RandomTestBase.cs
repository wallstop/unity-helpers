namespace UnityHelpers.Tests.Random
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using NUnit.Framework;
    using UnityHelpers.Core.Random;

    public abstract class RandomTestBase
    {
        protected const int SampleCount = 10_000_000;

        protected int[] _samples = new int[1_000];

        protected abstract IRandom NewRandom();

        [SetUp]
        public virtual void Setup()
        {
            Array.Clear(_samples, 0, _samples.Length);
        }

        [TearDown]
        public virtual void Teardown()
        {
            // No-op in base
        }

        [Test]
        public void Int()
        {
            TestAndVerify(random => random.Next(0, _samples.Length));
        }

        [Test]
        public void Uint()
        {
            TestAndVerify(random => (int)random.NextUint(0, (uint)_samples.Length));
        }

        [Test]
        public void Short()
        {
            TestAndVerify(random => random.NextShort(0, (short)_samples.Length));
        }

        [Test]
        public void Byte()
        {
            TestAndVerify(random => random.NextByte(0, (byte)(_samples.Length < byte.MaxValue ? _samples.Length : byte.MaxValue)), byte.MaxValue);
        }

        [Test]
        public void Float()
        {
            TestAndVerify(random => (int)Math.Floor(random.NextFloat(0, _samples.Length)));
        }

        [Test]
        public void Double()
        {
            TestAndVerify(random => (int)Math.Floor(random.NextDouble(0, _samples.Length)));
        }

        [Test]
        public void Long()
        {
            TestAndVerify(random => (int)random.NextLong(0, _samples.Length));
        }

        [Test]
        public void Ulong()
        {
            TestAndVerify(random => (int)random.NextUlong(0, (ulong)_samples.Length));
        }

        protected void TestAndVerify(Func<IRandom, int> sample, int? maxLength = null)
        {
            IRandom random = NewRandom();
            for (int i = 0; i < SampleCount; ++i)
            {
                int index = sample(random);
                if (index < 0 || _samples.Length <= index)
                {
                    Assert.Fail("Index {0} out of range", index);
                }
                else
                {
                    _samples[index]++;
                }
            }

            int sampleLength = Math.Min(_samples.Length, maxLength ?? _samples.Length);
            double average = SampleCount * 1.0 / sampleLength;
            double deviationAllowed = average * 0.05;
            List<int> zeroCountIndexes = new();
            List<int> outsideRange = new();
            for (int i = 0; i < sampleLength; i++)
            {
                int count = _samples[i];
                if (count == 0)
                {
                    zeroCountIndexes.Add(i);
                }

                if (deviationAllowed < Math.Abs(count - average))
                {
                    outsideRange.Add(i);
                }
            }

            Assert.AreEqual(0, zeroCountIndexes.Count, "No samples at {0} indices: [{1}]", zeroCountIndexes.Count, string.Join(",", zeroCountIndexes));
            Assert.AreEqual(0, outsideRange.Count, "{0} indexes outside of dev {1:0.00}. Expected: {2:0.00}. Found: [{3}]", outsideRange.Count, deviationAllowed, average, string.Join(",", outsideRange.Select(index => _samples[index])));
        }
    }
}
