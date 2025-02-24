namespace UnityHelpers.Tests.DataStructures
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.DataStructure;
    using Core.Random;
    using NUnit.Framework;

    public sealed class CyclicBufferTests
    {
        private const int NumTries = 100;
        private const int CapacityMultiplier = 3;

        [Test]
        public void InvalidCapacity()
        {
            Assert.Throws<ArgumentException>(() => new CyclicBuffer<int>(-1));
            Assert.Throws<ArgumentException>(() => new CyclicBuffer<int>(int.MinValue));
            for (int i = 0; i < NumTries; i++)
            {
                Assert.Throws<ArgumentException>(
                    () => new CyclicBuffer<int>(PRNG.Instance.Next(int.MinValue, -1))
                );
            }
        }

        [Test]
        public void ZeroCapacityOk()
        {
            CyclicBuffer<int> buffer = new(0);
            CollectionAssert.AreEquivalent(Array.Empty<int>(), buffer);
            for (int i = 0; i < NumTries; ++i)
            {
                buffer.Add(PRNG.Instance.Next());
                CollectionAssert.AreEquivalent(Array.Empty<int>(), buffer);
            }
        }

        [Test]
        public void IntMaxCapacityOk()
        {
            CyclicBuffer<int> buffer = new(int.MaxValue);
            CollectionAssert.AreEquivalent(Array.Empty<int>(), buffer);
            const int tries = 50;
            List<int> expected = new(tries);
            for (int i = 0; i < tries; ++i)
            {
                int value = PRNG.Instance.Next();
                buffer.Add(value);
                expected.Add(value);
                CollectionAssert.AreEquivalent(expected, buffer);
            }
        }

        [Test]
        public void OneCapacityOk()
        {
            CyclicBuffer<int> buffer = new(1);
            CollectionAssert.AreEquivalent(Array.Empty<int>(), buffer);
            int[] expected = new int[1];
            for (int i = 0; i < NumTries; ++i)
            {
                int value = PRNG.Instance.Next();
                buffer.Add(value);
                expected[0] = value;
                CollectionAssert.AreEquivalent(expected, buffer);
            }
        }

        [Test]
        public void NormalAndWrappingBehavior()
        {
            List<int> expected = new();
            for (int i = 0; i < NumTries; ++i)
            {
                int capacity = PRNG.Instance.Next(100, 1_000);
                CyclicBuffer<int> buffer = new(capacity);
                CollectionAssert.AreEquivalent(Array.Empty<int>(), buffer);
                expected.Clear();
                for (int j = 0; j < capacity * CapacityMultiplier; ++j)
                {
                    int newValue = PRNG.Instance.Next();
                    if (capacity <= j)
                    {
                        expected[j % capacity] = newValue;
                    }
                    else
                    {
                        expected.Add(newValue);
                    }
                    buffer.Add(newValue);
                    Assert.IsTrue(
                        expected.SequenceEqual(buffer),
                        $"Failure at iteration {i}, j={j}, capacity={capacity}, capacityMultiplier={CapacityMultiplier}"
                    );
                }
            }
        }
    }
}
