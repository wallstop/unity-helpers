namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.DataStructure;
    using WallstopStudios.UnityHelpers.Core.Random;

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
                Assert.Throws<ArgumentException>(() =>
                    new CyclicBuffer<int>(PRNG.Instance.Next(int.MinValue, -1))
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
                Assert.AreEqual(capacity, buffer.Capacity);
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
                        $"Failure at iteration {i}, capacity={buffer.Capacity}, "
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
                            $"Failure at iteration {i}, j={j}, capacity={buffer.Capacity}, "
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
                Assert.AreEqual(capacity, buffer.Capacity);
                Assert.IsTrue(Array.Empty<int>().SequenceEqual(buffer));

                // Make sure our data is actually cleaned up, none of our input data should be "Contained"
                foreach (int value in seen)
                {
                    Assert.IsFalse(buffer.Contains(value));
                }
            }
        }

        [Test]
        public void ResizeFullOk()
        {
            for (int i = 0; i < NumTries; ++i)
            {
                int capacity = PRNG.Instance.Next(100, 1_000);
                CyclicBuffer<int> buffer = new(capacity);
                float fillPercent = PRNG.Instance.NextFloat(1f, 2f);
                float capacityPercent = PRNG.Instance.NextFloat(0.3f, 0.9f);
                for (int j = 0; j < capacity * fillPercent; ++j)
                {
                    int value = PRNG.Instance.Next();
                    buffer.Add(value);
                }

                int[] values = buffer.ToArray();

                int newCapacity = Math.Max(0, (int)(capacity * capacityPercent));
                buffer.Resize(newCapacity);
                int[] newValues = buffer.ToArray();
                Assert.AreEqual(newCapacity, buffer.Capacity);
                Assert.AreEqual(newCapacity, newValues.Length);
                Assert.That(values.Take(newCapacity), Is.EqualTo(newValues));

                buffer.Add(1);
                buffer.Add(2);
                int[] afterAddition = buffer.ToArray();
                Assert.That(afterAddition, Is.EqualTo(newValues.Skip(2).Concat(new[] { 1, 2 })));

                newCapacity = 0;
                buffer.Resize(newCapacity);
                newValues = buffer.ToArray();
                Assert.AreEqual(newCapacity, buffer.Capacity);
                Assert.AreEqual(newCapacity, newValues.Length);
            }
        }

        [Test]
        public void ResizePartialOk()
        {
            for (int i = 0; i < NumTries; ++i)
            {
                int capacity = PRNG.Instance.Next(100, 1_000);
                CyclicBuffer<int> buffer = new(capacity);
                float fillPercent = PRNG.Instance.NextFloat(0.3f, 0.9f);
                float capacityPercent = PRNG.Instance.NextFloat(0.3f, 0.9f);
                int filled = (int)(capacity * fillPercent);
                for (int j = 0; j < filled; ++j)
                {
                    int value = PRNG.Instance.Next();
                    buffer.Add(value);
                }

                int[] values = buffer.ToArray();

                int newCapacity = Math.Max(0, (int)(capacity * capacityPercent));
                buffer.Resize(newCapacity);
                int[] newValues = buffer.ToArray();
                Assert.AreEqual(newCapacity, buffer.Capacity);
                Assert.AreEqual(Math.Min(filled, newCapacity), newValues.Length);
                Assert.That(values.Take(newCapacity), Is.EqualTo(newValues));

                buffer.Add(1);
                buffer.Add(2);
                int[] afterAddition = buffer.ToArray();
                if (newCapacity <= filled)
                {
                    Assert.That(
                        afterAddition,
                        Is.EqualTo(newValues.Skip(2).Concat(new[] { 1, 2 })),
                        $"Resize failed for iteration {i}, fillPercent {fillPercent:0.00}, capacityPercent: {capacityPercent:0.00}. "
                            + $"Capacity: {capacity}, newCapacity: {newCapacity}, filled: {filled}."
                    );
                }
                else if (newCapacity == filled + 1)
                {
                    Assert.That(
                        afterAddition,
                        Is.EqualTo(newValues.Skip(1).Concat(new[] { 1, 2 })),
                        $"Resize failed for iteration {i}, fillPercent {fillPercent:0.00}, capacityPercent: {capacityPercent:0.00}. "
                            + $"Capacity: {capacity}, newCapacity: {newCapacity}, filled: {filled}."
                    );
                }
                else
                {
                    Assert.That(
                        afterAddition,
                        Is.EqualTo(newValues.Concat(new[] { 1, 2 })),
                        $"Resize failed for iteration {i}, fillPercent {fillPercent:0.00}, capacityPercent: {capacityPercent:0.00}. "
                            + $"Capacity: {capacity}, newCapacity: {newCapacity}, filled: {filled}."
                    );
                }

                newCapacity = 0;
                buffer.Resize(newCapacity);
                newValues = buffer.ToArray();
                Assert.AreEqual(newCapacity, buffer.Capacity);
                Assert.AreEqual(newCapacity, newValues.Length);
            }
        }

        [Test]
        public void ResizeInvalidCapacity()
        {
            CyclicBuffer<int> buffer = new(10);
            buffer.Add(1);
            buffer.Add(2);

            Assert.Throws<ArgumentException>(() => buffer.Resize(-1));
            Assert.Throws<ArgumentException>(() => buffer.Resize(int.MinValue));

            // Verify buffer state is unchanged after exception
            Assert.AreEqual(10, buffer.Capacity);
            Assert.AreEqual(2, buffer.Count);
        }

        [Test]
        public void ResizeToSameCapacity()
        {
            CyclicBuffer<int> buffer = new(10);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            buffer.Resize(10);

            Assert.AreEqual(10, buffer.Capacity);
            Assert.AreEqual(3, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void ResizeUpwardsPreservesData()
        {
            CyclicBuffer<int> buffer = new(5);
            for (int i = 1; i <= 10; i++)
            {
                buffer.Add(i);
            }

            int[] beforeResize = buffer.ToArray();
            Assert.That(beforeResize, Is.EqualTo(new[] { 6, 7, 8, 9, 10 }));

            buffer.Resize(10);

            Assert.AreEqual(10, buffer.Capacity);
            Assert.AreEqual(5, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 6, 7, 8, 9, 10 }));

            buffer.Add(11);
            buffer.Add(12);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 6, 7, 8, 9, 10, 11, 12 }));
        }

        [Test]
        public void IndexerGet()
        {
            CyclicBuffer<int> buffer = new(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            Assert.AreEqual(1, buffer[0]);
            Assert.AreEqual(2, buffer[1]);
            Assert.AreEqual(3, buffer[2]);

            // After wraparound
            buffer.Add(4);
            buffer.Add(5);
            buffer.Add(6); // Should overwrite 1

            Assert.AreEqual(2, buffer[0]);
            Assert.AreEqual(3, buffer[1]);
            Assert.AreEqual(4, buffer[2]);
            Assert.AreEqual(5, buffer[3]);
            Assert.AreEqual(6, buffer[4]);
        }

        [Test]
        public void IndexerSet()
        {
            CyclicBuffer<int> buffer = new(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            buffer[0] = 10;
            buffer[1] = 20;
            buffer[2] = 30;

            Assert.AreEqual(10, buffer[0]);
            Assert.AreEqual(20, buffer[1]);
            Assert.AreEqual(30, buffer[2]);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 10, 20, 30 }));

            // After wraparound
            buffer.Add(4);
            buffer.Add(5);
            buffer.Add(6);

            buffer[0] = 100;
            buffer[4] = 200;

            Assert.AreEqual(100, buffer[0]);
            Assert.AreEqual(200, buffer[4]);
        }

        [Test]
        public void IndexerGetOutOfBounds()
        {
            CyclicBuffer<int> buffer = new(5);
            buffer.Add(1);
            buffer.Add(2);

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                int x = buffer[-1];
            });
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                int x = buffer[2];
            });
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                int x = buffer[5];
            });
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                int x = buffer[int.MaxValue];
            });
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                int x = buffer[int.MinValue];
            });
        }

        [Test]
        public void IndexerSetOutOfBounds()
        {
            CyclicBuffer<int> buffer = new(5);
            buffer.Add(1);
            buffer.Add(2);

            Assert.Throws<IndexOutOfRangeException>(() => buffer[-1] = 10);
            Assert.Throws<IndexOutOfRangeException>(() => buffer[2] = 10);
            Assert.Throws<IndexOutOfRangeException>(() => buffer[5] = 10);
            Assert.Throws<IndexOutOfRangeException>(() => buffer[int.MaxValue] = 10);
            Assert.Throws<IndexOutOfRangeException>(() => buffer[int.MinValue] = 10);
        }

        [Test]
        public void IndexerOnEmptyBuffer()
        {
            CyclicBuffer<int> buffer = new(5);

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                int x = buffer[0];
            });
            Assert.Throws<IndexOutOfRangeException>(() => buffer[0] = 10);
        }

        [Test]
        public void RemoveElement()
        {
            CyclicBuffer<int> buffer = new(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            bool removed = buffer.Remove(2);

            Assert.IsTrue(removed);
            Assert.AreEqual(2, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 1, 3 }));
            Assert.IsFalse(buffer.Contains(2));
        }

        [Test]
        public void RemoveFirstElement()
        {
            CyclicBuffer<int> buffer = new(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            bool removed = buffer.Remove(1);

            Assert.IsTrue(removed);
            Assert.AreEqual(2, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public void RemoveLastElement()
        {
            CyclicBuffer<int> buffer = new(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            bool removed = buffer.Remove(3);

            Assert.IsTrue(removed);
            Assert.AreEqual(2, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void RemoveNonExistentElement()
        {
            CyclicBuffer<int> buffer = new(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            bool removed = buffer.Remove(10);

            Assert.IsFalse(removed);
            Assert.AreEqual(3, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void RemoveFromEmptyBuffer()
        {
            CyclicBuffer<int> buffer = new(5);

            bool removed = buffer.Remove(1);

            Assert.IsFalse(removed);
            Assert.AreEqual(0, buffer.Count);
        }

        [Test]
        public void RemoveOnlyElement()
        {
            CyclicBuffer<int> buffer = new(5);
            buffer.Add(1);

            bool removed = buffer.Remove(1);

            Assert.IsTrue(removed);
            Assert.AreEqual(0, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(Array.Empty<int>()));
        }

        [Test]
        public void RemoveWithCustomComparer()
        {
            CyclicBuffer<string> buffer = new(5);
            buffer.Add("Hello");
            buffer.Add("World");
            buffer.Add("HELLO");

            bool removed = buffer.Remove("hello", StringComparer.OrdinalIgnoreCase);

            Assert.IsTrue(removed);
            Assert.AreEqual(2, buffer.Count);
            // Should remove first match ("Hello")
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { "World", "HELLO" }));
        }

        [Test]
        public void RemoveWithNullComparer()
        {
            CyclicBuffer<int> buffer = new(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            bool removed = buffer.Remove(2, null);

            Assert.IsTrue(removed);
            Assert.AreEqual(2, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 1, 3 }));
        }

        [Test]
        public void RemoveNullValue()
        {
            CyclicBuffer<string> buffer = new(5);
            buffer.Add("A");
            buffer.Add(null);
            buffer.Add("B");

            bool removed = buffer.Remove(null);

            Assert.IsTrue(removed);
            Assert.AreEqual(2, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { "A", "B" }));
        }

        [Test]
        public void RemoveAllMatchingElements()
        {
            CyclicBuffer<int> buffer = new(10);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(2);
            buffer.Add(4);
            buffer.Add(2);

            int removed = buffer.RemoveAll(x => x == 2);

            Assert.AreEqual(3, removed);
            Assert.AreEqual(3, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 1, 3, 4 }));
        }

        [Test]
        public void RemoveAllNoMatches()
        {
            CyclicBuffer<int> buffer = new(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            int removed = buffer.RemoveAll(x => x == 10);

            Assert.AreEqual(0, removed);
            Assert.AreEqual(3, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void RemoveAllFromEmptyBuffer()
        {
            CyclicBuffer<int> buffer = new(5);

            int removed = buffer.RemoveAll(x => x > 0);

            Assert.AreEqual(0, removed);
            Assert.AreEqual(0, buffer.Count);
        }

        [Test]
        public void RemoveAllElements()
        {
            CyclicBuffer<int> buffer = new(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            int removed = buffer.RemoveAll(x => true);

            Assert.AreEqual(3, removed);
            Assert.AreEqual(0, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(Array.Empty<int>()));
        }

        [Test]
        public void RemoveAllWithComplexPredicate()
        {
            CyclicBuffer<int> buffer = new(10);
            for (int i = 1; i <= 10; i++)
            {
                buffer.Add(i);
            }

            int removed = buffer.RemoveAll(x => x % 2 == 0 || x > 8);

            Assert.AreEqual(6, removed); // 2, 4, 6, 8, 9, 10
            Assert.AreEqual(4, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 1, 3, 5, 7 }));
        }

        [Test]
        public void EnumerationEmpty()
        {
            CyclicBuffer<int> buffer = new(5);

            int count = 0;
            foreach (int item in buffer)
            {
                count++;
            }

            Assert.AreEqual(0, count);
        }

        [Test]
        public void EnumerationNormal()
        {
            CyclicBuffer<int> buffer = new(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            List<int> items = new();
            foreach (int item in buffer)
            {
                items.Add(item);
            }

            Assert.That(items, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void EnumerationAfterWraparound()
        {
            CyclicBuffer<int> buffer = new(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);
            buffer.Add(5);

            List<int> items = new();
            foreach (int item in buffer)
            {
                items.Add(item);
            }

            Assert.That(items, Is.EqualTo(new[] { 3, 4, 5 }));
        }

        [Test]
        public void EnumerationMultipleTimes()
        {
            CyclicBuffer<int> buffer = new(3);
            buffer.Add(1);
            buffer.Add(2);

            for (int i = 0; i < 3; i++)
            {
                List<int> items = new();
                foreach (int item in buffer)
                {
                    items.Add(item);
                }
                Assert.That(items, Is.EqualTo(new[] { 1, 2 }));
            }
        }

        [Test]
        public void EnumeratorReset()
        {
            CyclicBuffer<int> buffer = new(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            using CyclicBuffer<int>.CyclicBufferEnumerator enumerator = buffer.GetEnumerator();

            // First enumeration
            List<int> firstPass = new();
            while (enumerator.MoveNext())
            {
                firstPass.Add(enumerator.Current);
            }

            // Reset and enumerate again
            enumerator.Reset();
            List<int> secondPass = new();
            while (enumerator.MoveNext())
            {
                secondPass.Add(enumerator.Current);
            }

            Assert.That(firstPass, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(secondPass, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void ContainsNullValue()
        {
            CyclicBuffer<string> buffer = new(5);
            buffer.Add("A");
            buffer.Add(null);
            buffer.Add("B");

            Assert.IsTrue(buffer.Contains(null));
            Assert.IsTrue(buffer.Contains("A"));
            Assert.IsTrue(buffer.Contains("B"));
            Assert.IsFalse(buffer.Contains("C"));
        }

        [Test]
        public void ContainsEmptyBuffer()
        {
            CyclicBuffer<int> buffer = new(5);

            Assert.IsFalse(buffer.Contains(1));
            Assert.IsFalse(buffer.Contains(0));
        }

        [Test]
        public void ContainsAfterClear()
        {
            CyclicBuffer<int> buffer = new(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            Assert.IsTrue(buffer.Contains(2));

            buffer.Clear();

            Assert.IsFalse(buffer.Contains(1));
            Assert.IsFalse(buffer.Contains(2));
            Assert.IsFalse(buffer.Contains(3));
        }

        [Test]
        public void AddAfterRemove()
        {
            CyclicBuffer<int> buffer = new(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            buffer.Remove(2);
            Assert.AreEqual(2, buffer.Count);

            buffer.Add(4);
            Assert.AreEqual(3, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 1, 3, 4 }));

            buffer.Add(5);
            buffer.Add(6);
            Assert.AreEqual(5, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 1, 3, 4, 5, 6 }));
        }

        [Test]
        public void AddAfterRemoveAll()
        {
            CyclicBuffer<int> buffer = new(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);

            buffer.RemoveAll(x => x % 2 == 0);
            Assert.AreEqual(2, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 1, 3 }));

            buffer.Add(5);
            buffer.Add(6);
            buffer.Add(7);
            Assert.AreEqual(5, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 1, 3, 5, 6, 7 }));
        }

        [Test]
        public void InitialContentsNull()
        {
            CyclicBuffer<int> buffer = new(5, null);

            Assert.AreEqual(5, buffer.Capacity);
            Assert.AreEqual(0, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(Array.Empty<int>()));
        }

        [Test]
        public void InitialContentsWithNullElements()
        {
            List<string> initial = new() { "A", null, "B", null, "C" };
            CyclicBuffer<string> buffer = new(10, initial);

            Assert.AreEqual(5, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { "A", null, "B", null, "C" }));
            Assert.IsTrue(buffer.Contains(null));
        }

        [Test]
        public void ResizePreservesOrder()
        {
            CyclicBuffer<int> buffer = new(5);
            for (int i = 1; i <= 8; i++)
            {
                buffer.Add(i);
            }

            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 4, 5, 6, 7, 8 }));

            buffer.Resize(3);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 4, 5, 6 }));

            buffer.Resize(7);
            Assert.AreEqual(3, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 4, 5, 6 }));

            buffer.Add(9);
            buffer.Add(10);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 4, 5, 6, 9, 10 }));
        }

        [Test]
        public void StressTestMixedOperations()
        {
            CyclicBuffer<int> buffer = new(20);
            List<int> expected = new();

            for (int i = 0; i < 100; i++)
            {
                int operation = PRNG.Instance.Next(0, 5);

                switch (operation)
                {
                    case 0: // Add
                        int value = PRNG.Instance.Next(0, 100);
                        buffer.Add(value);
                        expected.Add(value);
                        while (expected.Count > buffer.Capacity)
                        {
                            expected.RemoveAt(0);
                        }
                        break;

                    case 1: // Remove
                        if (expected.Count > 0)
                        {
                            int toRemove = expected[PRNG.Instance.Next(0, expected.Count)];
                            bool removedExpected = expected.Remove(toRemove);
                            bool removedActual = buffer.Remove(toRemove);
                            Assert.AreEqual(removedExpected, removedActual);
                        }
                        break;

                    case 2: // Clear
                        buffer.Clear();
                        expected.Clear();
                        break;

                    case 3: // RemoveAll even
                        int expectedRemoved = expected.RemoveAll(x => x % 2 == 0);
                        int actualRemoved = buffer.RemoveAll(x => x % 2 == 0);
                        Assert.AreEqual(expectedRemoved, actualRemoved);
                        break;

                    case 4: // Indexer set
                        if (expected.Count > 0)
                        {
                            int idx = PRNG.Instance.Next(0, expected.Count);
                            int newValue = PRNG.Instance.Next(0, 100);
                            expected[idx] = newValue;
                            buffer[idx] = newValue;
                        }
                        break;
                }

                Assert.AreEqual(expected.Count, buffer.Count);
                Assert.That(
                    buffer.ToArray(),
                    Is.EqualTo(expected.ToArray()),
                    $"Failed at iteration {i}, operation {operation}"
                );
            }
        }

        [Test]
        public void ZeroCapacityOperations()
        {
            CyclicBuffer<int> buffer = new(0);

            Assert.AreEqual(0, buffer.Count);
            Assert.AreEqual(0, buffer.Capacity);

            buffer.Add(1);
            Assert.AreEqual(0, buffer.Count);

            buffer.Clear();
            Assert.AreEqual(0, buffer.Count);

            Assert.IsFalse(buffer.Remove(1));
            Assert.AreEqual(0, buffer.RemoveAll(x => true));
            Assert.IsFalse(buffer.Contains(1));

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                int x = buffer[0];
            });
            Assert.Throws<IndexOutOfRangeException>(() => buffer[0] = 1);

            int count = 0;
            foreach (int item in buffer)
            {
                count++;
            }
            Assert.AreEqual(0, count);
        }

        [Test]
        public void FullBufferOperations()
        {
            CyclicBuffer<int> buffer = new(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            Assert.AreEqual(3, buffer.Count);
            Assert.AreEqual(3, buffer.Capacity);

            // Add more should wrap
            buffer.Add(4);
            Assert.AreEqual(3, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 2, 3, 4 }));

            // All indexer positions should be accessible
            for (int i = 0; i < buffer.Count; i++)
            {
                Assert.DoesNotThrow(() =>
                {
                    int x = buffer[i];
                });
                Assert.DoesNotThrow(() => buffer[i] = buffer[i] + 10);
            }

            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 12, 13, 14 }));
        }

        [Test]
        public void RemoveAfterWraparound()
        {
            CyclicBuffer<int> buffer = new(5);
            // Fill buffer
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);
            buffer.Add(5);

            // Cause wraparound
            buffer.Add(6);
            buffer.Add(7);

            // Buffer should now contain: [3, 4, 5, 6, 7]
            Console.WriteLine($"[DEBUG_LOG] After wraparound: [{string.Join(", ", buffer)}]");
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 3, 4, 5, 6, 7 }));

            // Remove element from middle
            bool removed = buffer.Remove(5);
            Console.WriteLine(
                $"[DEBUG_LOG] After remove(5): removed={removed}, count={buffer.Count}, buffer=[{string.Join(", ", buffer)}]"
            );

            Assert.IsTrue(removed);
            Assert.AreEqual(4, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 3, 4, 6, 7 }));

            // Add new element after remove
            buffer.Add(8);
            Console.WriteLine(
                $"[DEBUG_LOG] After add(8): count={buffer.Count}, buffer=[{string.Join(", ", buffer)}]"
            );
            Assert.AreEqual(5, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 3, 4, 6, 7, 8 }));
        }

        [Test]
        public void RemoveAllAfterWraparound()
        {
            CyclicBuffer<int> buffer = new(5);
            // Fill buffer
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);
            buffer.Add(5);

            // Cause wraparound
            buffer.Add(6);
            buffer.Add(7);
            buffer.Add(8);

            // Buffer should now contain: [4, 5, 6, 7, 8]
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 4, 5, 6, 7, 8 }));

            // Remove all even numbers
            int removed = buffer.RemoveAll(x => x % 2 == 0);

            Assert.AreEqual(3, removed);
            Assert.AreEqual(2, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 5, 7 }));

            // Add new elements after removeAll
            buffer.Add(9);
            buffer.Add(10);
            Assert.AreEqual(4, buffer.Count);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 5, 7, 9, 10 }));
        }

        [Test]
        public void MultipleOperationsSequence()
        {
            CyclicBuffer<int> buffer = new(5);

            // Initial adds
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));

            // Wrap around
            buffer.Add(4);
            buffer.Add(5);
            buffer.Add(6);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 2, 3, 4, 5, 6 }));

            // Remove from wrapped buffer
            buffer.Remove(4);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 2, 3, 5, 6 }));

            // Add after remove
            buffer.Add(7);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 2, 3, 5, 6, 7 }));

            // Another wrap
            buffer.Add(8);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 3, 5, 6, 7, 8 }));

            // RemoveAll after multiple wraps
            int removed = buffer.RemoveAll(x => x > 6);
            Assert.AreEqual(2, removed);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 3, 5, 6 }));

            // Final add
            buffer.Add(9);
            buffer.Add(10);
            Assert.That(buffer.ToArray(), Is.EqualTo(new[] { 3, 5, 6, 9, 10 }));
        }
    }
}
