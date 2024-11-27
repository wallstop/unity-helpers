namespace UnityHelpers.Tests.Random
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Core.Extension;
    using Core.Serialization;
    using NUnit.Framework;
    using UnityHelpers.Core.Random;

    public abstract class RandomTestBase
    {
        private const int NumGeneratorChecks = 1_000;
        private const int SampleCount = 12_750_000;

        private readonly int[] _samples = new int[1_000];

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
        [Parallelizable]
        public void Bool()
        {
            TestAndVerify(random => Convert.ToInt32(random.NextBool()), maxLength: 2);
        }

        [Test]
        [Parallelizable]
        public void Int()
        {
            TestAndVerify(random => random.Next(0, _samples.Length));
        }

        [Test]
        [Parallelizable]
        public void IntRange()
        {
            TestAndVerify(random => random.Next(_samples.Length));
        }

        [Test]
        [Parallelizable]
        public void IntDistribution()
        {
            TestAndVerify(random =>
                (int)(random.Next() / ((1.0 * int.MaxValue) / _samples.Length))
            );
        }

        [Test]
        [Parallelizable]
        public void IntMaxRange()
        {
            TestAndVerify(random =>
                (int)(
                    (random.Next(int.MinValue, int.MaxValue) + (-1.0 * int.MinValue))
                    / (1.0 * int.MaxValue - int.MinValue)
                    * _samples.Length
                )
            );
        }

        [Test]
        [Parallelizable]
        public void Uint()
        {
            TestAndVerify(random => (int)random.NextUint(0, (uint)_samples.Length));
        }

        [Test]
        [Parallelizable]
        public void UintRange()
        {
            TestAndVerify(random => (int)random.NextUint((uint)_samples.Length));
        }

        [Test]
        [Parallelizable]
        public void UintDistribution()
        {
            TestAndVerify(random =>
                (int)(random.NextUint() / ((1.0 * uint.MaxValue) / _samples.Length))
            );
        }

        [Test]
        [Parallelizable]
        public void UintMaxRange()
        {
            TestAndVerify(random =>
                (int)(
                    (random.NextUint(uint.MinValue, uint.MaxValue) + (1.0 * uint.MinValue))
                    / (1.0 * uint.MaxValue)
                    * _samples.Length
                )
            );
        }

        [Test]
        [Parallelizable]
        public void Short()
        {
            TestAndVerify(
                random => random.NextShort(0, (short)_samples.Length),
                maxLength: (short.MaxValue - short.MinValue)
            );
        }

        [Test]
        [Parallelizable]
        public void ShortRange()
        {
            TestAndVerify(
                random => random.NextShort((short)_samples.Length),
                maxLength: (short.MaxValue - short.MinValue)
            );
        }

        [Test]
        [Parallelizable]
        public void ShortMaxRange()
        {
            TestAndVerify(random =>
                (int)(
                    (random.NextShort(short.MinValue, short.MaxValue) + (-1.0 * short.MinValue))
                    / (1.0 * short.MaxValue - short.MinValue)
                    * _samples.Length
                )
            );
        }

        [Test]
        [Parallelizable]
        public void Byte()
        {
            TestAndVerify(
                random =>
                    random.NextByte(
                        0,
                        (byte)(_samples.Length < byte.MaxValue ? _samples.Length : byte.MaxValue)
                    ),
                byte.MaxValue
            );
        }

        [Test]
        [Parallelizable]
        public void ByteRange()
        {
            TestAndVerify(
                random =>
                    random.NextByte(
                        (byte)(_samples.Length < byte.MaxValue ? _samples.Length : byte.MaxValue)
                    ),
                byte.MaxValue
            );
        }

        [Test]
        [Parallelizable]
        public void ByteMaxRange()
        {
            int sampleCount = Math.Min((byte.MaxValue - byte.MinValue), _samples.Length);
            TestAndVerify(
                random =>
                    Math.Clamp(
                        (int)(
                            (random.NextByte(byte.MinValue, byte.MaxValue) + (-1.0 * byte.MinValue))
                            / (1.0 * byte.MaxValue - byte.MinValue)
                            * sampleCount
                        ),
                        0,
                        sampleCount - 1
                    ),
                maxLength: sampleCount
            );
        }

        [Test]
        [Parallelizable]
        public void Float()
        {
            TestAndVerify(random =>
                Math.Clamp(
                    (int)Math.Floor(random.NextFloat(0, _samples.Length)),
                    0,
                    _samples.Length - 1
                )
            );
        }

        [Test]
        [Parallelizable]
        public void FloatRange()
        {
            TestAndVerify(random =>
                Math.Clamp(
                    (int)Math.Floor(random.NextFloat(_samples.Length)),
                    0,
                    _samples.Length - 1
                )
            );
        }

        [Test]
        [Parallelizable]
        public void FloatDistribution()
        {
            TestAndVerify(random => (int)(random.NextFloat() * _samples.Length));
        }

        [Test]
        [Parallelizable]
        public void FloatMaxRange()
        {
            TestAndVerify(random =>
                Math.Clamp(
                    (int)(
                        (random.NextFloat(float.MinValue, float.MaxValue) + (-1.0 * float.MinValue))
                        / (1.0 * float.MaxValue - float.MinValue)
                        * _samples.Length
                    ),
                    0,
                    _samples.Length - 1
                )
            );
        }

        [Test]
        [Parallelizable]
        public void Double()
        {
            TestAndVerify(random =>
                Math.Clamp(
                    (int)Math.Floor(random.NextDouble(0, _samples.Length)),
                    0,
                    _samples.Length - 1
                )
            );
        }

        [Test]
        [Parallelizable]
        public void DoubleRange()
        {
            TestAndVerify(random =>
                Math.Clamp(
                    (int)Math.Floor(random.NextDouble(_samples.Length)),
                    0,
                    _samples.Length - 1
                )
            );
        }

        [Test]
        [Parallelizable]
        public void DoubleDistribution()
        {
            TestAndVerify(random => (int)(random.NextDouble() * _samples.Length));
        }

        [Test]
        [Parallelizable]
        public void DoubleMaxRange()
        {
            IRandom random = NewRandom();
            for (int i = 0; i < SampleCount; ++i)
            {
                double value = random.NextDouble(double.MinValue, double.MaxValue);
                Assert.IsFalse(double.IsNaN(value));
                Assert.IsFalse(double.IsInfinity(value));
            }
        }

        [Test]
        [Parallelizable]
        public void Long()
        {
            TestAndVerify(random => (int)random.NextLong(0, _samples.Length));
        }

        [Test]
        [Parallelizable]
        public void LongRange()
        {
            TestAndVerify(random => (int)random.NextLong(_samples.Length));
        }

        [Test]
        [Parallelizable]
        public void LongMaxRange()
        {
            TestAndVerify(random =>
                (int)(
                    (random.NextLong(long.MinValue, long.MaxValue) + (-1.0 * long.MinValue))
                    / (1.0 * long.MaxValue - long.MinValue)
                    * _samples.Length
                )
            );
        }

        [Test]
        [Parallelizable]
        public void Ulong()
        {
            TestAndVerify(random => (int)random.NextUlong(0, (ulong)_samples.Length));
        }

        [Test]
        [Parallelizable]
        public void UlongRange()
        {
            TestAndVerify(random => (int)random.NextUlong((ulong)_samples.Length));
        }

        [Test]
        [Parallelizable]
        public void UlongMaxRange()
        {
            TestAndVerify(random =>
                (int)(
                    (random.NextUlong(ulong.MinValue, ulong.MaxValue) + (-1.0 * ulong.MinValue))
                    / (1.0 * ulong.MaxValue - ulong.MinValue)
                    * _samples.Length
                )
            );
        }

        [Test]
        [Parallelizable]
        public void Copy()
        {
            IRandom random1 = NewRandom();
            IRandom random2 = random1.Copy();
            Assert.AreEqual(random1.InternalState, random2.InternalState);
            // UnityRandom has shared state, the below test is not possible for it. We did all we could.
            if (NewRandom() is not UnityRandom)
            {
                for (int i = 0; i < NumGeneratorChecks; ++i)
                {
                    Assert.AreEqual(random1.Next(), random2.Next());
                    Assert.AreEqual(random1.InternalState, random2.InternalState);
                }
            }

            Assert.AreEqual(random1.InternalState, random2.InternalState);
            IRandom random3 = random1.Copy();
            Assert.AreEqual(random1.InternalState, random3.InternalState);
            if (NewRandom() is not UnityRandom)
            {
                for (int i = 0; i < NumGeneratorChecks; ++i)
                {
                    Assert.AreEqual(random1.Next(), random3.Next());
                    Assert.AreEqual(random1.InternalState, random3.InternalState);
                }
            }
        }

        [Test]
        [Parallelizable]
        public void Json()
        {
            IRandom random = NewRandom();
            string json = random.ToJson();
            IRandom deserialized = Serializer.JsonDeserialize<IRandom>(json, random.GetType());
            Assert.AreEqual(random.InternalState, deserialized.InternalState);

            if (NewRandom() is not UnityRandom)
            {
                for (int i = 0; i < NumGeneratorChecks; ++i)
                {
                    Assert.AreEqual(random.Next(), deserialized.Next());
                    Assert.AreEqual(random.InternalState, deserialized.InternalState);
                }
            }
        }

        protected virtual double DeviationFor(string caller)
        {
            return 0.0625;
        }

        private void TestAndVerify(
            Func<IRandom, int> sample,
            int? maxLength = null,
            [CallerMemberName] string caller = ""
        )
        {
            IRandom random = NewRandom();
            int sampleLength = _samples.Length;
            for (int i = 0; i < SampleCount; ++i)
            {
                int index = sample(random);
                if (index < 0 || sampleLength <= index)
                {
                    Assert.Fail("Index {0} out of range", index);
                }
                else
                {
                    _samples[index]++;
                }
            }

            sampleLength = Math.Min(sampleLength, maxLength ?? sampleLength);
            double average = SampleCount * 1.0 / sampleLength;
            double deviationAllowed = average * DeviationFor(caller);
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

            Assert.AreEqual(
                0,
                zeroCountIndexes.Count,
                "No samples at {0} indices: [{1}]",
                zeroCountIndexes.Count,
                string.Join(",", zeroCountIndexes)
            );
            Assert.AreEqual(
                0,
                outsideRange.Count,
                "{0} indexes outside of dev {1:0.00}. Expected: {2:0.00}. Found: [{3}]",
                outsideRange.Count,
                deviationAllowed,
                average,
                string.Join(",", outsideRange.Select(index => _samples[index]))
            );
        }
    }
}
