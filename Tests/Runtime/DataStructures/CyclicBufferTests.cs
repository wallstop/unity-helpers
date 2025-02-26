﻿namespace UnityHelpers.Tests.DataStructures
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
        public void CapacityInitializedOk()
        {
            for (int i = 0; i < NumTries; i++)
            {
                int capacity = PRNG.Instance.Next(1, int.MaxValue);
                CyclicBuffer<int> buffer = new(capacity);
                Assert.AreEqual(capacity, buffer.capacity);
            }
        }

        [Test]
        public void CountInitializedOk()
        {
            for (int i = 0; i < NumTries; i++)
            {
                int capacity = PRNG.Instance.Next(1, int.MaxValue);
                CyclicBuffer<int> buffer = new(capacity);
                Assert.AreEqual(0, buffer.Count);
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
                if (!expected.SequenceEqual(buffer))
                {
                    Assert.Fail(
                        $"Failure at iteration {i}, capacity={buffer.capacity}, "
                            + $"capacityMultiplier={CapacityMultiplier}\n"
                            + $"Expected: [{string.Join(",", expected)}], Actual: [{string.Join(",", buffer)}]"
                    );
                }
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
        public void InitialElementsVariableSize()
        {
            for (int i = 0; i < NumTries; ++i)
            {
                int capacity = PRNG.Instance.Next(100, 1_000);
                int[] elements = Enumerable
                    .Range(0, (int)(capacity * PRNG.Instance.NextFloat(0.5f, 1.5f)))
                    .Select(_ => PRNG.Instance.Next())
                    .ToArray();
                CyclicBuffer<int> buffer = new(capacity, elements);
                if (capacity < elements.Length)
                {
                    Assert.IsTrue(elements.Skip(elements.Length - capacity).SequenceEqual(buffer));
                }
                else
                {
                    Assert.IsTrue(elements.SequenceEqual(buffer));
                }
            }
        }

        [Test]
        public void InitialElementsSizeSameAsCapacity()
        {
            for (int i = 0; i < NumTries; ++i)
            {
                int capacity = PRNG.Instance.Next(100, 1_000);
                int[] elements = Enumerable
                    .Range(0, capacity)
                    .Select(_ => PRNG.Instance.Next())
                    .ToArray();
                CyclicBuffer<int> buffer = new(capacity, elements);
                Assert.IsTrue(elements.SequenceEqual(buffer));
            }
        }

        [Test]
        public void InitialElementsEmptyInput()
        {
            for (int i = 0; i < NumTries; ++i)
            {
                int capacity = PRNG.Instance.Next(100, 1_000);
                CyclicBuffer<int> buffer = new(capacity, Array.Empty<int>());
                Assert.IsTrue(Array.Empty<int>().SequenceEqual(buffer));
            }
        }

        [Test]
        public void NormalAndWrappingBehavior()
        {
            LinkedList<int> expected = new();
            for (int i = 0; i < NumTries; ++i)
            {
                int capacity = PRNG.Instance.Next(100, 1_000);
                CyclicBuffer<int> buffer = new(capacity);
                CollectionAssert.AreEquivalent(Array.Empty<int>(), buffer);
                expected.Clear();
                for (int j = 0; j < capacity * CapacityMultiplier; ++j)
                {
                    int newValue = PRNG.Instance.Next();
                    expected.AddLast(newValue);
                    while (capacity < expected.Count)
                    {
                        expected.RemoveFirst();
                    }
                    buffer.Add(newValue);
                    Assert.AreEqual(expected.Count, buffer.Count);
                    if (!expected.SequenceEqual(buffer))
                    {
                        Assert.Fail(
                            $"Failure at iteration {i}, j={j}, capacity={buffer.capacity}, "
                                + $"capacityMultiplier={CapacityMultiplier}\n"
                                + $"Expected: [{string.Join(",", expected)}], Actual: [{string.Join(",", buffer)}]"
                        );
                    }
                }

                foreach (int item in expected)
                {
                    Assert.IsTrue(buffer.Contains(item));
                }

                for (int j = 0; j < NumTries; ++j)
                {
                    Assert.IsFalse(buffer.Contains(PRNG.Instance.Next(int.MinValue, -1)));
                }
            }
        }

        [Test]
        public void ClearOk()
        {
            HashSet<int> seen = new();
            for (int i = 0; i < NumTries; ++i)
            {
                int capacity = PRNG.Instance.Next(100, 1_000);
                CyclicBuffer<int> buffer = new(capacity);
                float fillPercent = PRNG.Instance.NextFloat(0.5f, 1.5f);
                seen.Clear();
                for (int j = 0; j < capacity * fillPercent; ++j)
                {
                    int value = PRNG.Instance.Next();
                    seen.Add(value);
                    buffer.Add(value);
                }

                Assert.AreNotEqual(0, buffer.Count);
                Assert.IsFalse(Array.Empty<int>().SequenceEqual(buffer));
                buffer.Clear();

                Assert.AreEqual(0, buffer.Count);
                Assert.AreEqual(capacity, buffer.capacity);
                Assert.IsTrue(Array.Empty<int>().SequenceEqual(buffer));

                // Make sure our data is actually cleaned up, none of our input data should be "Contained"
                foreach (int value in seen)
                {
                    Assert.IsFalse(buffer.Contains(value));
                }
            }
        }
    }
}
