namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.DataStructure;

    public sealed class MinHeapTests
    {
        [Test]
        public void ConstructorWithDefaultCapacityCreatesEmptyHeap()
        {
            Heap<int> heap = new();
            Assert.AreEqual(0, heap.Count);
            Assert.IsTrue(heap.IsEmpty);
        }

        [Test]
        public void ConstructorWithSpecificCapacityCreatesEmptyHeap()
        {
            Heap<int> heap = new(32);
            Assert.AreEqual(0, heap.Count);
            Assert.IsTrue(heap.IsEmpty);
            Assert.AreEqual(32, heap.Capacity);
        }

        [Test]
        public void ConstructorWithZeroCapacityThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Heap<int>(0));
        }

        [Test]
        public void ConstructorWithNegativeCapacityThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Heap<int>(-1));
        }

        [Test]
        public void ConstructorWithNullComparerThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new Heap<int>(null, 16));
        }

        [Test]
        public void ConstructorWithNullCollectionThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new Heap<int>((IEnumerable<int>)null));
        }

        [Test]
        public void ConstructorWithCollectionHeapifiesCorrectly()
        {
            int[] items = { 5, 3, 7, 1, 9, 2, 8 };
            Heap<int> heap = new(items);

            Assert.AreEqual(items.Length, heap.Count);
            Assert.AreEqual(1, heap.Peek()); // Min element
        }

        [Test]
        public void ConstructorWithEmptyCollectionCreatesEmptyHeap()
        {
            Heap<int> heap = new(Array.Empty<int>());
            Assert.AreEqual(0, heap.Count);
            Assert.IsTrue(heap.IsEmpty);
        }

        [Test]
        public void CreateMinHeapCreatesMinHeap()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap();
            heap.Add(5);
            heap.Add(3);
            heap.Add(7);

            Assert.AreEqual(3, heap.Peek());
        }

        [Test]
        public void CreateMinHeapFromCollectionCreatesMinHeap()
        {
            int[] items = { 5, 3, 7, 1, 9 };
            Heap<int> heap = Heap<int>.CreateMinHeap(items);

            Assert.AreEqual(1, heap.Peek());
            Assert.AreEqual(5, heap.Count);
        }

        [Test]
        public void AddInsertsElementsCorrectly()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap();
            heap.Add(5);
            heap.Add(3);
            heap.Add(7);
            heap.Add(1);

            Assert.AreEqual(4, heap.Count);
            Assert.AreEqual(1, heap.Peek());
        }

        [Test]
        public void AddMaintainsMinHeapProperty()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap();
            int[] values = { 10, 20, 5, 15, 30, 25, 3 };

            foreach (int value in values)
            {
                heap.Add(value);
            }

            List<int> sorted = new();
            while (!heap.IsEmpty)
            {
                sorted.Add(heap.Pop());
            }

            CollectionAssert.AreEqual(values.OrderBy(x => x).ToList(), sorted);
        }

        [Test]
        public void AddTriggersResize()
        {
            Heap<int> heap = new(2);
            heap.Add(1);
            heap.Add(2);

            Assert.AreEqual(2, heap.Capacity);

            heap.Add(3);

            Assert.AreEqual(4, heap.Capacity);
            Assert.AreEqual(3, heap.Count);
        }

        [Test]
        public void PeekReturnsMinElementWithoutRemoving()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap(new[] { 5, 3, 7, 1 });

            int min = heap.Peek();

            Assert.AreEqual(1, min);
            Assert.AreEqual(4, heap.Count);
        }

        [Test]
        public void PeekThrowsWhenEmpty()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap();
            Assert.Throws<InvalidOperationException>(() => heap.Peek());
        }

        [Test]
        public void TryPeekReturnsTrueWhenNotEmpty()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap(new[] { 5, 3, 7 });

            bool success = heap.TryPeek(out int result);

            Assert.IsTrue(success);
            Assert.AreEqual(3, result);
            Assert.AreEqual(3, heap.Count);
        }

        [Test]
        public void TryPeekReturnsFalseWhenEmpty()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap();

            bool success = heap.TryPeek(out int result);

            Assert.IsFalse(success);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void PopRemovesAndReturnsMinElement()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap(new[] { 5, 3, 7, 1 });

            int min = heap.Pop();

            Assert.AreEqual(1, min);
            Assert.AreEqual(3, heap.Count);
            Assert.AreEqual(3, heap.Peek());
        }

        [Test]
        public void PopMaintainsHeapProperty()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap(new[] { 5, 3, 7, 1, 9, 2, 8 });

            List<int> results = new();
            while (!heap.IsEmpty)
            {
                results.Add(heap.Pop());
            }

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 5, 7, 8, 9 }, results);
        }

        [Test]
        public void PopThrowsWhenEmpty()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap();
            Assert.Throws<InvalidOperationException>(() => heap.Pop());
        }

        [Test]
        public void TryPopReturnsTrueWhenNotEmpty()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap(new[] { 5, 3, 7 });

            bool success = heap.TryPop(out int result);

            Assert.IsTrue(success);
            Assert.AreEqual(3, result);
            Assert.AreEqual(2, heap.Count);
        }

        [Test]
        public void TryPopReturnsFalseWhenEmpty()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap();

            bool success = heap.TryPop(out int result);

            Assert.IsFalse(success);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void ClearRemovesAllElements()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap(new[] { 5, 3, 7, 1 });

            heap.Clear();

            Assert.AreEqual(0, heap.Count);
            Assert.IsTrue(heap.IsEmpty);
            Assert.Throws<InvalidOperationException>(() => heap.Peek());
        }

        [Test]
        public void ContainsReturnsTrueForExistingElement()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap(new[] { 5, 3, 7, 1 });

            Assert.IsTrue(heap.Contains(3));
            Assert.IsTrue(heap.Contains(7));
        }

        [Test]
        public void ContainsReturnsFalseForNonExistingElement()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap(new[] { 5, 3, 7, 1 });

            Assert.IsFalse(heap.Contains(10));
        }

        [Test]
        public void ContainsReturnsFalseWhenEmpty()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap();

            Assert.IsFalse(heap.Contains(5));
        }

        [Test]
        public void CopyToCopiesToArray()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap(new[] { 5, 3, 7, 1 });
            int[] array = new int[6];

            heap.CopyTo(array, 1);

            Assert.AreEqual(0, array[0]);
            Assert.AreEqual(4, array.Skip(1).Take(4).Count(x => x != 0));
        }

        [Test]
        public void CopyToThrowsWhenArrayIsNull()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap(new[] { 5, 3, 7 });

            Assert.Throws<ArgumentNullException>(() => heap.CopyTo(null, 0));
        }

        [Test]
        public void CopyToThrowsWhenArrayIndexIsNegative()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap(new[] { 5, 3, 7 });
            int[] array = new int[10];

            Assert.Throws<ArgumentOutOfRangeException>(() => heap.CopyTo(array, -1));
        }

        [Test]
        public void CopyToThrowsWhenArrayIsTooSmall()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap(new[] { 5, 3, 7, 1 });
            int[] array = new int[3];

            Assert.Throws<ArgumentException>(() => heap.CopyTo(array, 0));
        }

        [Test]
        public void ToArrayReturnsAllElements()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap(new[] { 5, 3, 7, 1 });

            int[] array = heap.ToArray();

            Assert.AreEqual(4, array.Length);
            CollectionAssert.AreEquivalent(new[] { 5, 3, 7, 1 }, array);
        }

        [Test]
        public void ToArrayReturnsEmptyArrayWhenEmpty()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap();

            int[] array = heap.ToArray();

            Assert.AreEqual(0, array.Length);
        }

        [Test]
        public void TrimExcessReducesCapacity()
        {
            Heap<int> heap = new(100);
            heap.Add(1);
            heap.Add(2);
            heap.Add(3);

            int capacityBefore = heap.Capacity;
            heap.TrimExcess();
            int capacityAfter = heap.Capacity;

            Assert.Less(capacityAfter, capacityBefore);
            Assert.AreEqual(3, heap.Count);
        }

        [Test]
        public void TrimExcessDoesNotReduceWhenNearCapacity()
        {
            Heap<int> heap = new(10);
            for (int i = 0; i < 9; i++)
            {
                heap.Add(i);
            }

            int capacityBefore = heap.Capacity;
            heap.TrimExcess();

            Assert.AreEqual(capacityBefore, heap.Capacity);
        }

        [Test]
        public void GetEnumeratorReturnsAllElements()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap(new[] { 5, 3, 7, 1 });

            List<int> results = new();
            foreach (int item in heap)
            {
                results.Add(item);
            }

            Assert.AreEqual(4, results.Count);
            CollectionAssert.AreEquivalent(new[] { 5, 3, 7, 1 }, results);
        }

        [Test]
        public void HeapHandlesDuplicateElements()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap(new[] { 5, 3, 3, 7, 1, 1 });

            List<int> results = new();
            while (!heap.IsEmpty)
            {
                results.Add(heap.Pop());
            }

            CollectionAssert.AreEqual(new[] { 1, 1, 3, 3, 5, 7 }, results);
        }

        [Test]
        public void HeapHandlesSingleElement()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap();
            heap.Add(42);

            Assert.AreEqual(1, heap.Count);
            Assert.AreEqual(42, heap.Peek());
            Assert.AreEqual(42, heap.Pop());
            Assert.IsTrue(heap.IsEmpty);
        }

        [Test]
        public void HeapHandlesLargeNumberOfElements()
        {
            int[] items = Enumerable.Range(0, 10000).Reverse().ToArray();
            Heap<int> heap = Heap<int>.CreateMinHeap(items);

            List<int> results = new();
            while (!heap.IsEmpty)
            {
                results.Add(heap.Pop());
            }

            CollectionAssert.AreEqual(Enumerable.Range(0, 10000).ToList(), results);
        }

        [Test]
        public void HeapHandlesCustomComparerForMinHeap()
        {
            IComparer<int> comparer = Comparer<int>.Default;
            Heap<int> heap = new(comparer);
            heap.Add(5);
            heap.Add(3);
            heap.Add(7);

            Assert.AreEqual(3, heap.Peek());
        }

        [Test]
        public void HeapHandlesReferenceTypes()
        {
            Heap<string> heap = Heap<string>.CreateMinHeap();
            heap.Add("banana");
            heap.Add("apple");
            heap.Add("cherry");

            Assert.AreEqual("apple", heap.Peek());

            List<string> results = new();
            while (!heap.IsEmpty)
            {
                results.Add(heap.Pop());
            }

            CollectionAssert.AreEqual(new[] { "apple", "banana", "cherry" }, results);
        }

        [Test]
        public void HeapMaintainsHeapPropertyAfterMultipleOperations()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap();

            heap.Add(10);
            heap.Add(5);
            Assert.AreEqual(5, heap.Peek());

            heap.Add(3);
            Assert.AreEqual(3, heap.Peek());

            heap.Pop();
            Assert.AreEqual(5, heap.Peek());

            heap.Add(1);
            Assert.AreEqual(1, heap.Peek());

            heap.Pop();
            heap.Pop();
            Assert.AreEqual(10, heap.Peek());
        }

        [Test]
        public void HeapSupportsNullableValueTypes()
        {
            Heap<int?> heap = Heap<int?>.CreateMinHeap();
            heap.Add(5);
            heap.Add(null);
            heap.Add(3);

            // Null should be treated as minimum by default comparer
            Assert.AreEqual(null, heap.Pop());
            Assert.AreEqual(3, heap.Pop());
            Assert.AreEqual(5, heap.Pop());
        }
    }

    public sealed class MaxHeapTests
    {
        [Test]
        public void CreateMaxHeapCreatesMaxHeap()
        {
            Heap<int> heap = Heap<int>.CreateMaxHeap();
            heap.Add(5);
            heap.Add(3);
            heap.Add(7);

            Assert.AreEqual(7, heap.Peek());
        }

        [Test]
        public void CreateMaxHeapFromCollectionCreatesMaxHeap()
        {
            int[] items = { 5, 3, 7, 1, 9 };
            Heap<int> heap = Heap<int>.CreateMaxHeap(items);

            Assert.AreEqual(9, heap.Peek());
            Assert.AreEqual(5, heap.Count);
        }

        [Test]
        public void MaxHeapAddMaintainsMaxHeapProperty()
        {
            Heap<int> heap = Heap<int>.CreateMaxHeap();
            int[] values = { 10, 20, 5, 15, 30, 25, 3 };

            foreach (int value in values)
            {
                heap.Add(value);
            }

            List<int> sorted = new();
            while (!heap.IsEmpty)
            {
                sorted.Add(heap.Pop());
            }

            CollectionAssert.AreEqual(values.OrderByDescending(x => x).ToList(), sorted);
        }

        [Test]
        public void MaxHeapPeekReturnsMaxElement()
        {
            Heap<int> heap = Heap<int>.CreateMaxHeap(new[] { 5, 3, 7, 1 });

            int max = heap.Peek();

            Assert.AreEqual(7, max);
            Assert.AreEqual(4, heap.Count);
        }

        [Test]
        public void MaxHeapPopRemovesAndReturnsMaxElement()
        {
            Heap<int> heap = Heap<int>.CreateMaxHeap(new[] { 5, 3, 7, 1 });

            int max = heap.Pop();

            Assert.AreEqual(7, max);
            Assert.AreEqual(3, heap.Count);
            Assert.AreEqual(5, heap.Peek());
        }

        [Test]
        public void MaxHeapPopMaintainsHeapProperty()
        {
            Heap<int> heap = Heap<int>.CreateMaxHeap(new[] { 5, 3, 7, 1, 9, 2, 8 });

            List<int> results = new();
            while (!heap.IsEmpty)
            {
                results.Add(heap.Pop());
            }

            CollectionAssert.AreEqual(new[] { 9, 8, 7, 5, 3, 2, 1 }, results);
        }

        [Test]
        public void MaxHeapHandlesDuplicateElements()
        {
            Heap<int> heap = Heap<int>.CreateMaxHeap(new[] { 5, 3, 3, 7, 7, 1 });

            List<int> results = new();
            while (!heap.IsEmpty)
            {
                results.Add(heap.Pop());
            }

            CollectionAssert.AreEqual(new[] { 7, 7, 5, 3, 3, 1 }, results);
        }

        [Test]
        public void MaxHeapHandlesSingleElement()
        {
            Heap<int> heap = Heap<int>.CreateMaxHeap();
            heap.Add(42);

            Assert.AreEqual(1, heap.Count);
            Assert.AreEqual(42, heap.Peek());
            Assert.AreEqual(42, heap.Pop());
            Assert.IsTrue(heap.IsEmpty);
        }

        [Test]
        public void MaxHeapHandlesLargeNumberOfElements()
        {
            int[] items = Enumerable.Range(0, 10000).ToArray();
            Heap<int> heap = Heap<int>.CreateMaxHeap(items);

            List<int> results = new();
            while (!heap.IsEmpty)
            {
                results.Add(heap.Pop());
            }

            CollectionAssert.AreEqual(Enumerable.Range(0, 10000).Reverse().ToList(), results);
        }

        [Test]
        public void MaxHeapHandlesReferenceTypes()
        {
            Heap<string> heap = Heap<string>.CreateMaxHeap();
            heap.Add("banana");
            heap.Add("apple");
            heap.Add("cherry");

            Assert.AreEqual("cherry", heap.Peek());

            List<string> results = new();
            while (!heap.IsEmpty)
            {
                results.Add(heap.Pop());
            }

            CollectionAssert.AreEqual(new[] { "cherry", "banana", "apple" }, results);
        }

        [Test]
        public void MaxHeapMaintainsHeapPropertyAfterMultipleOperations()
        {
            Heap<int> heap = Heap<int>.CreateMaxHeap();

            heap.Add(10);
            heap.Add(15);
            Assert.AreEqual(15, heap.Peek());

            heap.Add(20);
            Assert.AreEqual(20, heap.Peek());

            heap.Pop();
            Assert.AreEqual(15, heap.Peek());

            heap.Add(25);
            Assert.AreEqual(25, heap.Peek());

            heap.Pop();
            heap.Pop();
            Assert.AreEqual(10, heap.Peek());
        }

        [Test]
        public void MaxHeapContainsWorksCorrectly()
        {
            Heap<int> heap = Heap<int>.CreateMaxHeap(new[] { 5, 3, 7, 1 });

            Assert.IsTrue(heap.Contains(7));
            Assert.IsTrue(heap.Contains(3));
            Assert.IsFalse(heap.Contains(10));
        }

        [Test]
        public void MaxHeapClearRemovesAllElements()
        {
            Heap<int> heap = Heap<int>.CreateMaxHeap(new[] { 5, 3, 7, 1 });

            heap.Clear();

            Assert.AreEqual(0, heap.Count);
            Assert.IsTrue(heap.IsEmpty);
        }
    }

    public sealed class CustomComparerHeapTests
    {
        private class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }

            public Person(string name, int age)
            {
                Name = name;
                Age = age;
            }
        }

        private class PersonAgeComparer : IComparer<Person>
        {
            public int Compare(Person x, Person y)
            {
                if (x == null && y == null)
                    return 0;
                if (x == null)
                    return -1;
                if (y == null)
                    return 1;
                return x.Age.CompareTo(y.Age);
            }
        }

        [Test]
        public void HeapWorksWithCustomComparer()
        {
            IComparer<Person> comparer = new PersonAgeComparer();
            Heap<Person> heap = new(comparer);

            heap.Add(new Person("Alice", 30));
            heap.Add(new Person("Bob", 25));
            heap.Add(new Person("Charlie", 35));

            Person youngest = heap.Peek();
            Assert.AreEqual("Bob", youngest.Name);
            Assert.AreEqual(25, youngest.Age);
        }

        [Test]
        public void HeapWithCustomComparerMaintainsOrder()
        {
            IComparer<Person> comparer = new PersonAgeComparer();
            Person[] people =
            {
                new Person("Alice", 30),
                new Person("Bob", 25),
                new Person("Charlie", 35),
                new Person("David", 20),
            };
            Heap<Person> heap = new(people, comparer);

            List<string> names = new();
            while (!heap.IsEmpty)
            {
                names.Add(heap.Pop().Name);
            }

            CollectionAssert.AreEqual(new[] { "David", "Bob", "Alice", "Charlie" }, names);
        }

        [Test]
        public void HeapWithReverseCustomComparerCreatesMaxHeap()
        {
            IComparer<Person> reverseComparer = Comparer<Person>.Create(
                (x, y) => new PersonAgeComparer().Compare(y, x)
            );
            Heap<Person> heap = new(reverseComparer);

            heap.Add(new Person("Alice", 30));
            heap.Add(new Person("Bob", 25));
            heap.Add(new Person("Charlie", 35));

            Person oldest = heap.Peek();
            Assert.AreEqual("Charlie", oldest.Name);
            Assert.AreEqual(35, oldest.Age);
        }
    }

    public sealed class HeapEdgeCaseTests
    {
        [Test]
        public void HeapHandlesAlternatingAddAndPop()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap();

            heap.Add(5);
            Assert.AreEqual(5, heap.Pop());

            heap.Add(3);
            heap.Add(7);
            Assert.AreEqual(3, heap.Pop());

            heap.Add(1);
            Assert.AreEqual(1, heap.Peek());
        }

        [Test]
        public void HeapHandlesAllSameElements()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap(new[] { 5, 5, 5, 5, 5 });

            Assert.AreEqual(5, heap.Count);
            while (!heap.IsEmpty)
            {
                Assert.AreEqual(5, heap.Pop());
            }
        }

        [Test]
        public void HeapHandlesAlreadySortedAscending()
        {
            int[] sorted = Enumerable.Range(1, 100).ToArray();
            Heap<int> heap = Heap<int>.CreateMinHeap(sorted);

            List<int> results = new();
            while (!heap.IsEmpty)
            {
                results.Add(heap.Pop());
            }

            CollectionAssert.AreEqual(sorted, results);
        }

        [Test]
        public void HeapHandlesAlreadySortedDescending()
        {
            int[] sorted = Enumerable.Range(1, 100).Reverse().ToArray();
            Heap<int> heap = Heap<int>.CreateMinHeap(sorted);

            List<int> results = new();
            while (!heap.IsEmpty)
            {
                results.Add(heap.Pop());
            }

            CollectionAssert.AreEqual(sorted.OrderBy(x => x).ToList(), results);
        }

        [Test]
        public void MaxHeapHandlesAlreadySortedAscending()
        {
            int[] sorted = Enumerable.Range(1, 100).ToArray();
            Heap<int> heap = Heap<int>.CreateMaxHeap(sorted);

            List<int> results = new();
            while (!heap.IsEmpty)
            {
                results.Add(heap.Pop());
            }

            CollectionAssert.AreEqual(sorted.Reverse().ToList(), results);
        }

        [Test]
        public void MaxHeapHandlesAlreadySortedDescending()
        {
            int[] sorted = Enumerable.Range(1, 100).Reverse().ToArray();
            Heap<int> heap = Heap<int>.CreateMaxHeap(sorted);

            List<int> results = new();
            while (!heap.IsEmpty)
            {
                results.Add(heap.Pop());
            }

            CollectionAssert.AreEqual(sorted, results);
        }

        [Test]
        public void HeapHandlesIntMinAndMax()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap();
            heap.Add(int.MaxValue);
            heap.Add(int.MinValue);
            heap.Add(0);

            Assert.AreEqual(int.MinValue, heap.Pop());
            Assert.AreEqual(0, heap.Pop());
            Assert.AreEqual(int.MaxValue, heap.Pop());
        }

        [Test]
        public void HeapHandlesDoubleNaN()
        {
            Heap<double> heap = Heap<double>.CreateMinHeap();
            heap.Add(5.0);
            heap.Add(double.NaN);
            heap.Add(3.0);

            // NaN comparisons are tricky, but heap should still function
            Assert.AreEqual(3, heap.Count);
        }

        [Test]
        public void HeapHandlesDoubleInfinity()
        {
            Heap<double> heap = Heap<double>.CreateMinHeap();
            heap.Add(5.0);
            heap.Add(double.NegativeInfinity);
            heap.Add(double.PositiveInfinity);
            heap.Add(0.0);

            Assert.AreEqual(double.NegativeInfinity, heap.Pop());
            Assert.AreEqual(0.0, heap.Pop());
            Assert.AreEqual(5.0, heap.Pop());
            Assert.AreEqual(double.PositiveInfinity, heap.Pop());
        }

        [Test]
        public void HeapHandlesRepeatedClearAndAdd()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap();

            for (int i = 0; i < 5; i++)
            {
                heap.Add(i);
                heap.Add(i + 10);
                heap.Clear();
            }

            Assert.IsTrue(heap.IsEmpty);

            heap.Add(42);
            Assert.AreEqual(42, heap.Peek());
        }

        [Test]
        public void HeapToArrayDoesNotAffectHeap()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap(new[] { 5, 3, 7, 1 });

            int[] array1 = heap.ToArray();
            int[] array2 = heap.ToArray();

            Assert.AreEqual(4, heap.Count);
            Assert.AreEqual(4, array1.Length);
            Assert.AreEqual(4, array2.Length);
        }

        [Test]
        public void HeapEnumerationDoesNotModifyHeap()
        {
            Heap<int> heap = Heap<int>.CreateMinHeap(new[] { 5, 3, 7, 1 });

            int count = 0;
            foreach (int _ in heap)
            {
                count++;
            }

            Assert.AreEqual(4, count);
            Assert.AreEqual(4, heap.Count);
            Assert.AreEqual(1, heap.Peek());
        }
    }
}
