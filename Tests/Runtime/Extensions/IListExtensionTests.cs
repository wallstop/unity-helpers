namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class IListExtensionTests
    {
        private const int NumTries = 1_000;

        private readonly struct IntComparer : IComparer<int>
        {
            public int Compare(int x, int y) => x.CompareTo(y);
        }

        private readonly struct IntEqualityComparer : IEqualityComparer<int>
        {
            public bool Equals(int x, int y) => x == y;

            public int GetHashCode(int obj) => obj.GetHashCode();
        }

        [Test]
        public void ShiftLeft()
        {
            int[] input = Enumerable.Range(0, 10).ToArray();
            for (int i = 0; i < input.Length * 2; ++i)
            {
                int[] shifted = input.ToArray();
                shifted.Shift(-1 * i);
                Assert.That(
                    input.Skip(i % input.Length).Concat(input.Take(i % input.Length)),
                    Is.EqualTo(shifted)
                );
            }
        }

        [Test]
        public void ShiftRight()
        {
            int[] input = Enumerable.Range(0, 10).ToArray();
            for (int i = 0; i < input.Length * 2; ++i)
            {
                int[] shifted = input.ToArray();
                shifted.Shift(i);
                Assert.That(
                    input
                        .Skip((input.Length * 3 - i) % input.Length)
                        .Concat(input.Take((input.Length * 3 - i) % input.Length)),
                    Is.EqualTo(shifted),
                    $"Shift failed for amount {i}."
                );
            }
        }

        [Test]
        public void Reverse()
        {
            int[] input = Enumerable.Range(0, 10).ToArray();
            for (int i = 0; i < input.Length; ++i)
            {
                int[] shifted = input.ToArray();
                shifted.Reverse(0, i);
                Assert.That(
                    input.Take(i + 1).Reverse().Concat(input.Skip(i + 1)),
                    Is.EqualTo(shifted),
                    $"Reverse failed for reversal from [0, {i}]."
                );
            }

            // Test various ranges
            for (int start = 0; start < input.Length; ++start)
            {
                for (int end = start; end < input.Length; ++end)
                {
                    int[] reversed = input.ToArray();
                    reversed.Reverse(start, end);

                    // Build expected result
                    int[] expected = input.ToArray();
                    int left = start;
                    int right = end;
                    while (left < right)
                    {
                        (expected[left], expected[right]) = (expected[right], expected[left]);
                        left++;
                        right--;
                    }

                    Assert.That(
                        expected,
                        Is.EqualTo(reversed),
                        $"Reverse failed for range [{start}, {end}]."
                    );
                }
            }
        }

        [Test]
        public void ReverseInvalidArguments()
        {
            int[] input = Enumerable.Range(0, 10).ToArray();
            Assert.Throws<ArgumentException>(() => input.Reverse(-1, 1));
            Assert.Throws<ArgumentException>(() => input.Reverse(input.Length, 1));
            Assert.Throws<ArgumentException>(() => input.Reverse(int.MaxValue, 1));
            Assert.Throws<ArgumentException>(() => input.Reverse(int.MinValue, 1));

            Assert.Throws<ArgumentException>(() => input.Reverse(1, -1));
            Assert.Throws<ArgumentException>(() => input.Reverse(1, input.Length));
            Assert.Throws<ArgumentException>(() => input.Reverse(1, int.MaxValue));
            Assert.Throws<ArgumentException>(() => input.Reverse(1, int.MinValue));
        }

        [Test]
        public void InsertionSort()
        {
            for (int i = 0; i < NumTries; ++i)
            {
                int[] input = Enumerable
                    .Range(0, 100)
                    .Select(_ => PRNG.Instance.Next(int.MinValue, int.MaxValue))
                    .ToArray();
                int[] conventionalSorted = input.ToArray();
                Array.Sort(conventionalSorted);

                int[] insertionSorted = input.ToArray();
                insertionSorted.InsertionSort(new IntComparer());
                Assert.That(conventionalSorted, Is.EqualTo(insertionSorted));
                Assert.That(input.OrderBy(x => x), Is.EqualTo(insertionSorted));
            }
        }

        [Test]
        public void ShellSortEnhanced()
        {
            for (int i = 0; i < NumTries; ++i)
            {
                int[] input = Enumerable
                    .Range(0, 100)
                    .Select(_ => PRNG.Instance.Next(int.MinValue, int.MaxValue))
                    .ToArray();
                int[] conventionalSorted = input.ToArray();
                Array.Sort(conventionalSorted);

                int[] insertionSorted = input.ToArray();
                insertionSorted.GhostSort(new IntComparer());
                Assert.That(conventionalSorted, Is.EqualTo(insertionSorted));
                Assert.That(input.OrderBy(x => x), Is.EqualTo(insertionSorted));
            }
        }

        [Test]
        public void SortDefaultAlgorithm()
        {
            for (int i = 0; i < NumTries; ++i)
            {
                int[] input = Enumerable
                    .Range(0, 100)
                    .Select(_ => PRNG.Instance.Next(int.MinValue, int.MaxValue))
                    .ToArray();
                int[] conventionalSorted = input.ToArray();
                Array.Sort(conventionalSorted);

                int[] insertionSorted = input.ToArray();
                insertionSorted.Sort(new IntComparer());
                Assert.That(conventionalSorted, Is.EqualTo(insertionSorted));
                Assert.That(input.OrderBy(x => x), Is.EqualTo(insertionSorted));
            }
        }

        [Test]
        public void SortAllAlgorithms()
        {
            SortAlgorithm[] sortAlgorithms = Enum.GetValues(typeof(SortAlgorithm))
                .OfType<SortAlgorithm>()
#pragma warning disable CS0618 // Type or member is obsolete
                .Except(Enumerables.Of(SortAlgorithm.None))
#pragma warning restore CS0618 // Type or member is obsolete
                .ToArray();
            Assert.That(sortAlgorithms.Length, Is.GreaterThan(0));

            foreach (SortAlgorithm sortAlgorithm in sortAlgorithms)
            {
                for (int i = 0; i < NumTries; ++i)
                {
                    int[] input = Enumerable
                        .Range(0, 100)
                        .Select(_ => PRNG.Instance.Next(int.MinValue, int.MaxValue))
                        .ToArray();
                    int[] conventionalSorted = input.ToArray();
                    Array.Sort(conventionalSorted);

                    int[] insertionSorted = input.ToArray();
                    insertionSorted.Sort(new IntComparer(), sortAlgorithm);
                    Assert.That(conventionalSorted, Is.EqualTo(insertionSorted));
                    Assert.That(input.OrderBy(x => x), Is.EqualTo(insertionSorted));
                }
            }
        }

        // ===== New Method Tests =====

        [Test]
        public void ShuffleEmptyList()
        {
            int[] empty = Array.Empty<int>();
            empty.Shuffle();
            Assert.That(empty, Is.Empty);
        }

        [Test]
        public void ShuffleSingleElement()
        {
            int[] single = { 42 };
            single.Shuffle();
            Assert.That(single, Is.EqualTo(new[] { 42 }));
        }

        [Test]
        public void ShuffleActuallyShuffles()
        {
            int[] input = Enumerable.Range(0, 100).ToArray();
            int[] shuffled = input.ToArray();
            shuffled.Shuffle(new SystemRandom(42));

            // Should have same elements
            Assert.That(shuffled.OrderBy(x => x), Is.EqualTo(input));

            // Should be different order (very high probability)
            bool isDifferent = false;
            for (int i = 0; i < input.Length; ++i)
            {
                if (input[i] != shuffled[i])
                {
                    isDifferent = true;
                    break;
                }
            }
            Assert.That(isDifferent, Is.True, "Shuffle should change order");
        }

        [Test]
        public void ShuffleDifferentSeeds()
        {
            int[] input = Enumerable.Range(0, 50).ToArray();
            int[] shuffle1 = input.ToArray();
            int[] shuffle2 = input.ToArray();

            shuffle1.Shuffle(new SystemRandom(42));
            shuffle2.Shuffle(new SystemRandom(43));

            Assert.That(shuffle1, Is.Not.EqualTo(shuffle2));
        }

        [Test]
        public void ShiftEmptyList()
        {
            int[] empty = Array.Empty<int>();
            empty.Shift(5);
            Assert.That(empty, Is.Empty);
        }

        [Test]
        public void ShiftSingleElement()
        {
            int[] single = { 42 };
            single.Shift(10);
            Assert.That(single, Is.EqualTo(new[] { 42 }));
        }

        [Test]
        public void ShiftZero()
        {
            int[] input = Enumerable.Range(0, 10).ToArray();
            int[] expected = input.ToArray();
            input.Shift(0);
            Assert.That(input, Is.EqualTo(expected));
        }

        [Test]
        public void RemoveAtSwapBackSingleElement()
        {
            List<int> single = new() { 42 };
            single.RemoveAtSwapBack(0);
            Assert.That(single, Is.Empty);
        }

        [Test]
        public void RemoveAtSwapBackLastElement()
        {
            List<int> list = new() { 1, 2, 3, 4, 5 };
            list.RemoveAtSwapBack(4);
            Assert.That(list, Is.EqualTo(new[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void RemoveAtSwapBackFirstElement()
        {
            List<int> list = new() { 1, 2, 3, 4, 5 };
            list.RemoveAtSwapBack(0);
            Assert.That(list, Is.EqualTo(new[] { 5, 2, 3, 4 }));
        }

        [Test]
        public void RemoveAtSwapBackMiddleElement()
        {
            List<int> list = new() { 1, 2, 3, 4, 5 };
            list.RemoveAtSwapBack(2);
            Assert.That(list, Is.EqualTo(new[] { 1, 2, 5, 4 }));
        }

        [Test]
        public void IsSortedEmptyList()
        {
            int[] empty = Array.Empty<int>();
            Assert.That(empty.IsSorted(), Is.True);
        }

        [Test]
        public void IsSortedSingleElement()
        {
            int[] single = { 42 };
            Assert.That(single.IsSorted(), Is.True);
        }

        [Test]
        public void IsSortedSorted()
        {
            int[] sorted = { 1, 2, 3, 4, 5 };
            Assert.That(sorted.IsSorted(), Is.True);
        }

        [Test]
        public void IsSortedNotSorted()
        {
            int[] notSorted = { 1, 3, 2, 4, 5 };
            Assert.That(notSorted.IsSorted(), Is.False);
        }

        [Test]
        public void IsSortedDuplicates()
        {
            int[] duplicates = { 1, 2, 2, 3, 3, 3, 4 };
            Assert.That(duplicates.IsSorted(), Is.True);
        }

        [Test]
        public void IsSortedCustomComparer()
        {
            int[] descending = { 5, 4, 3, 2, 1 };
            Assert.That(
                descending.IsSorted(Comparer<int>.Create((a, b) => b.CompareTo(a))),
                Is.True
            );
        }

        [Test]
        public void SwapValidIndices()
        {
            int[] arr = { 1, 2, 3, 4, 5 };
            arr.Swap(1, 3);
            Assert.That(arr, Is.EqualTo(new[] { 1, 4, 3, 2, 5 }));
        }

        [Test]
        public void SwapSameIndex()
        {
            int[] arr = { 1, 2, 3 };
            int[] expected = arr.ToArray();
            arr.Swap(1, 1);
            Assert.That(arr, Is.EqualTo(expected));
        }

        [Test]
        public void SwapInvalidIndices()
        {
            int[] arr = { 1, 2, 3 };
            Assert.Throws<ArgumentOutOfRangeException>(() => arr.Swap(-1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => arr.Swap(1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => arr.Swap(3, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => arr.Swap(1, 3));
        }

        [Test]
        public void BinarySearchFound()
        {
            int[] sorted = { 1, 3, 5, 7, 9, 11, 13 };
            Assert.That(sorted.BinarySearch(7), Is.EqualTo(3));
            Assert.That(sorted.BinarySearch(1), Is.EqualTo(0));
            Assert.That(sorted.BinarySearch(13), Is.EqualTo(6));
        }

        [Test]
        public void BinarySearchNotFound()
        {
            int[] sorted = { 1, 3, 5, 7, 9 };
            int result = sorted.BinarySearch(4);
            Assert.That(result, Is.LessThan(0));
            Assert.That(~result, Is.EqualTo(2)); // Should insert at index 2
        }

        [Test]
        public void BinarySearchEmptyList()
        {
            int[] empty = Array.Empty<int>();
            int result = empty.BinarySearch(42);
            Assert.That(result, Is.LessThan(0));
            Assert.That(~result, Is.EqualTo(0));
        }

        [Test]
        public void BinarySearchSingleElement()
        {
            int[] single = { 42 };
            Assert.That(single.BinarySearch(42), Is.EqualTo(0));
            Assert.That(single.BinarySearch(41), Is.LessThan(0));
            Assert.That(single.BinarySearch(43), Is.LessThan(0));
        }

        [Test]
        public void FillValue()
        {
            int[] arr = new int[10];
            arr.Fill(42);
            Assert.That(arr, Is.All.EqualTo(42));
        }

        [Test]
        public void FillFactory()
        {
            int[] arr = new int[10];
            arr.Fill(i => i * 2);
            Assert.That(arr, Is.EqualTo(Enumerable.Range(0, 10).Select(i => i * 2)));
        }

        [Test]
        public void FillFactoryNull()
        {
            int[] arr = new int[10];
            Assert.Throws<ArgumentNullException>(() => arr.Fill(null));
        }

        [Test]
        public void IndexOfFound()
        {
            int[] arr = { 1, 2, 3, 4, 5 };
            Assert.That(arr.IndexOf(x => x > 3), Is.EqualTo(3));
            Assert.That(arr.IndexOf(x => x == 1), Is.EqualTo(0));
        }

        [Test]
        public void IndexOfNotFound()
        {
            int[] arr = { 1, 2, 3 };
            Assert.That(arr.IndexOf(x => x > 10), Is.EqualTo(-1));
        }

        [Test]
        public void IndexOfNullPredicate()
        {
            int[] arr = { 1, 2, 3 };
            Assert.Throws<ArgumentNullException>(() => arr.IndexOf(null));
        }

        [Test]
        public void LastIndexOfFound()
        {
            int[] arr = { 1, 2, 3, 2, 1 };
            Assert.That(arr.LastIndexOf(x => x == 2), Is.EqualTo(3));
            Assert.That(arr.LastIndexOf(x => x == 1), Is.EqualTo(4));
        }

        [Test]
        public void LastIndexOfNotFound()
        {
            int[] arr = { 1, 2, 3 };
            Assert.That(arr.LastIndexOf(x => x > 10), Is.EqualTo(-1));
        }

        [Test]
        public void FindAllFound()
        {
            int[] arr = { 1, 2, 3, 4, 5, 6 };
            List<int> result = arr.FindAll(x => x % 2 == 0);
            Assert.That(result, Is.EqualTo(new[] { 2, 4, 6 }));
        }

        [Test]
        public void FindAllNoneFound()
        {
            int[] arr = { 1, 3, 5 };
            List<int> result = arr.FindAll(x => x % 2 == 0);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void FindAllNullPredicate()
        {
            int[] arr = { 1, 2, 3 };
            Assert.Throws<ArgumentNullException>(() => arr.FindAll(null));
        }

        [Test]
        public void AddRangeToList()
        {
            List<int> list = new() { 1, 2, 3 };
            list.AddRange(new[] { 4, 5, 6 });
            Assert.That(list, Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6 }));
        }

        [Test]
        public void AddRangeNullItems()
        {
            List<int> list = new() { 1, 2, 3 };
            Assert.Throws<ArgumentNullException>(() => list.AddRange(null));
        }

        [Test]
        public void RemoveAllSomeRemoved()
        {
            List<int> list = new() { 1, 2, 3, 4, 5, 6 };
            int removed = list.RemoveAll(x => x % 2 == 0);
            Assert.That(removed, Is.EqualTo(3));
            Assert.That(list, Is.EqualTo(new[] { 1, 3, 5 }));
        }

        [Test]
        public void RemoveAllNoneRemoved()
        {
            List<int> list = new() { 1, 3, 5 };
            int removed = list.RemoveAll(x => x % 2 == 0);
            Assert.That(removed, Is.EqualTo(0));
            Assert.That(list, Is.EqualTo(new[] { 1, 3, 5 }));
        }

        [Test]
        public void RemoveAllAllRemoved()
        {
            List<int> list = new() { 2, 4, 6 };
            int removed = list.RemoveAll(x => x % 2 == 0);
            Assert.That(removed, Is.EqualTo(3));
            Assert.That(list, Is.Empty);
        }

        [Test]
        public void RemoveAllNullPredicate()
        {
            List<int> list = new() { 1, 2, 3 };
            Assert.Throws<ArgumentNullException>(() => list.RemoveAll(null));
        }

        [Test]
        public void RotateLeftBasic()
        {
            int[] arr = { 1, 2, 3, 4, 5 };
            arr.RotateLeft(2);
            Assert.That(arr, Is.EqualTo(new[] { 3, 4, 5, 1, 2 }));
        }

        [Test]
        public void RotateRightBasic()
        {
            int[] arr = { 1, 2, 3, 4, 5 };
            arr.RotateRight(2);
            Assert.That(arr, Is.EqualTo(new[] { 4, 5, 1, 2, 3 }));
        }

        [Test]
        public void PartitionBasic()
        {
            int[] arr = { 1, 2, 3, 4, 5, 6 };
            (List<int> even, List<int> odd) = arr.Partition(x => x % 2 == 0);
            Assert.That(even, Is.EqualTo(new[] { 2, 4, 6 }));
            Assert.That(odd, Is.EqualTo(new[] { 1, 3, 5 }));
        }

        [Test]
        public void PartitionAllMatch()
        {
            int[] arr = { 2, 4, 6 };
            (List<int> matching, List<int> notMatching) = arr.Partition(x => x % 2 == 0);
            Assert.That(matching, Is.EqualTo(new[] { 2, 4, 6 }));
            Assert.That(notMatching, Is.Empty);
        }

        [Test]
        public void PartitionNoneMatch()
        {
            int[] arr = { 1, 3, 5 };
            (List<int> matching, List<int> notMatching) = arr.Partition(x => x % 2 == 0);
            Assert.That(matching, Is.Empty);
            Assert.That(notMatching, Is.EqualTo(new[] { 1, 3, 5 }));
        }

        [Test]
        public void PartitionNullPredicate()
        {
            int[] arr = { 1, 2, 3 };
            Assert.Throws<ArgumentNullException>(() => arr.Partition(null));
        }

        [Test]
        public void PopBackSuccess()
        {
            List<int> list = new() { 1, 2, 3, 4, 5 };
            int popped = list.PopBack();
            Assert.That(popped, Is.EqualTo(5));
            Assert.That(list, Is.EqualTo(new[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void PopBackEmptyList()
        {
            List<int> list = new();
            Assert.Throws<InvalidOperationException>(() => list.PopBack());
        }

        [Test]
        public void PopFrontSuccess()
        {
            List<int> list = new() { 1, 2, 3, 4, 5 };
            int popped = list.PopFront();
            Assert.That(popped, Is.EqualTo(1));
            Assert.That(list, Is.EqualTo(new[] { 2, 3, 4, 5 }));
        }

        [Test]
        public void PopFrontEmptyList()
        {
            List<int> list = new();
            Assert.Throws<InvalidOperationException>(() => list.PopFront());
        }

        [Test]
        public void GetRandomElementSuccess()
        {
            int[] arr = { 1, 2, 3, 4, 5 };
            int element = arr.GetRandomElement(new SystemRandom(42));
            Assert.That(arr, Does.Contain(element));
        }

        [Test]
        public void GetRandomElementEmptyList()
        {
            int[] arr = Array.Empty<int>();
            Assert.Throws<InvalidOperationException>(() => arr.GetRandomElement());
        }

        [Test]
        public void GetRandomElementSingleElement()
        {
            int[] arr = { 42 };
            Assert.That(arr.GetRandomElement(), Is.EqualTo(42));
        }

        // ===== Edge Case Combination Tests =====

        [Test]
        public void SortEmptyList()
        {
            int[] empty = Array.Empty<int>();
            empty.Sort(new IntComparer());
            Assert.That(empty, Is.Empty);
        }

        [Test]
        public void SortSingleElement()
        {
            int[] single = { 42 };
            single.Sort(new IntComparer());
            Assert.That(single, Is.EqualTo(new[] { 42 }));
        }

        [Test]
        public void SortAllDuplicates()
        {
            int[] duplicates = { 5, 5, 5, 5, 5 };
            duplicates.Sort(new IntComparer());
            Assert.That(duplicates, Is.EqualTo(new[] { 5, 5, 5, 5, 5 }));
        }

        [Test]
        public void SortAlreadySorted()
        {
            int[] sorted = { 1, 2, 3, 4, 5 };
            sorted.Sort(new IntComparer());
            Assert.That(sorted, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
        }

        [Test]
        public void SortReverseSorted()
        {
            int[] reversed = { 5, 4, 3, 2, 1 };
            reversed.Sort(new IntComparer());
            Assert.That(reversed, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
        }

        [Test]
        public void InsertionSortEmptyList()
        {
            int[] empty = Array.Empty<int>();
            empty.InsertionSort(new IntComparer());
            Assert.That(empty, Is.Empty);
        }

        [Test]
        public void InsertionSortSingleElement()
        {
            int[] single = { 42 };
            single.InsertionSort(new IntComparer());
            Assert.That(single, Is.EqualTo(new[] { 42 }));
        }

        [Test]
        public void GhostSortEmptyList()
        {
            int[] empty = Array.Empty<int>();
            empty.GhostSort(new IntComparer());
            Assert.That(empty, Is.Empty);
        }

        [Test]
        public void GhostSortSingleElement()
        {
            int[] single = { 42 };
            single.GhostSort(new IntComparer());
            Assert.That(single, Is.EqualTo(new[] { 42 }));
        }

        [Test]
        public void CombinedOperationsShuffleThenSort()
        {
            int[] arr = Enumerable.Range(0, 100).ToArray();
            arr.Shuffle(new SystemRandom(42));
            arr.Sort(new IntComparer());
            Assert.That(arr.IsSorted(), Is.True);
            Assert.That(arr, Is.EqualTo(Enumerable.Range(0, 100)));
        }

        [Test]
        public void CombinedOperationsShiftThenReverse()
        {
            int[] arr = { 1, 2, 3, 4, 5 };
            arr.Shift(2);
            arr.Reverse(0, arr.Length - 1);
            Assert.That(arr, Is.EqualTo(new[] { 3, 2, 1, 5, 4 }));
        }

        [Test]
        public void CombinedOperationsFillThenPartition()
        {
            int[] arr = new int[10];
            arr.Fill(i => i);
            (List<int> even, List<int> odd) = arr.Partition(x => x % 2 == 0);
            Assert.That(even, Is.EqualTo(new[] { 0, 2, 4, 6, 8 }));
            Assert.That(odd, Is.EqualTo(new[] { 1, 3, 5, 7, 9 }));
        }

        [Test]
        public void CombinedOperationsRemoveAllThenIsSorted()
        {
            List<int> list = new() { 5, 2, 8, 1, 9, 3, 7, 4, 6 };
            list.RemoveAll(x => x > 5);
            list.Sort(new IntComparer());
            Assert.That(list.IsSorted(), Is.True);
            Assert.That(list, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
        }

        [Test]
        public void StressTestMultipleOperations()
        {
            for (int i = 0; i < 100; ++i)
            {
                List<int> list = Enumerable.Range(0, 50).ToList();

                list.Shuffle(new SystemRandom(i));

                list.RemoveAll(x => x % 7 == 0);

                list.RotateLeft(3);

                list.Sort(new IntComparer());

                Assert.That(list.IsSorted(), Is.True);

                HashSet<int> seen = new();
                foreach (int val in list)
                {
                    Assert.That(seen.Add(val), Is.True, "No duplicates should exist");
                    Assert.That(val % 7, Is.Not.EqualTo(0), "Multiples of 7 should be removed");
                }
            }
        }

        [Test]
        public void SortByNameEmptyList()
        {
            List<UnityEngine.GameObject> list = new();
            list.SortByName();
            Assert.That(list, Is.Empty);
        }

        [Test]
        public void SortByNameSingleElement()
        {
            UnityEngine.GameObject obj = new("SingleObject");
            try
            {
                List<UnityEngine.GameObject> list = new() { obj };
                list.SortByName();
                Assert.That(list, Has.Count.EqualTo(1));
                Assert.That(list[0].name, Is.EqualTo("SingleObject"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }

        [Test]
        public void SortByNameArray()
        {
            UnityEngine.GameObject obj1 = new("Zebra");
            UnityEngine.GameObject obj2 = new("Alpha");
            UnityEngine.GameObject obj3 = new("Bravo");
            try
            {
                UnityEngine.GameObject[] array = { obj1, obj2, obj3 };
                array.SortByName();
                Assert.That(array[0].name, Is.EqualTo("Alpha"));
                Assert.That(array[1].name, Is.EqualTo("Bravo"));
                Assert.That(array[2].name, Is.EqualTo("Zebra"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(obj1);
                UnityEngine.Object.DestroyImmediate(obj2);
                UnityEngine.Object.DestroyImmediate(obj3);
            }
        }

        [Test]
        public void SortByNameList()
        {
            UnityEngine.GameObject obj1 = new("Zebra");
            UnityEngine.GameObject obj2 = new("Alpha");
            UnityEngine.GameObject obj3 = new("Bravo");
            UnityEngine.GameObject obj4 = new("Charlie");
            try
            {
                List<UnityEngine.GameObject> list = new() { obj1, obj2, obj3, obj4 };
                list.SortByName();
                Assert.That(list[0].name, Is.EqualTo("Alpha"));
                Assert.That(list[1].name, Is.EqualTo("Bravo"));
                Assert.That(list[2].name, Is.EqualTo("Charlie"));
                Assert.That(list[3].name, Is.EqualTo("Zebra"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(obj1);
                UnityEngine.Object.DestroyImmediate(obj2);
                UnityEngine.Object.DestroyImmediate(obj3);
                UnityEngine.Object.DestroyImmediate(obj4);
            }
        }

        [Test]
        public void SortByNameCustomIList()
        {
            UnityEngine.GameObject obj1 = new("Zebra");
            UnityEngine.GameObject obj2 = new("Alpha");
            UnityEngine.GameObject obj3 = new("Bravo");
            try
            {
                IList<UnityEngine.GameObject> list = new CustomList<UnityEngine.GameObject>
                {
                    obj1,
                    obj2,
                    obj3,
                };
                list.SortByName();
                Assert.That(list[0].name, Is.EqualTo("Alpha"));
                Assert.That(list[1].name, Is.EqualTo("Bravo"));
                Assert.That(list[2].name, Is.EqualTo("Zebra"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(obj1);
                UnityEngine.Object.DestroyImmediate(obj2);
                UnityEngine.Object.DestroyImmediate(obj3);
            }
        }

        [Test]
        public void SortByNameDuplicateNames()
        {
            UnityEngine.GameObject obj1 = new("Same");
            UnityEngine.GameObject obj2 = new("Same");
            UnityEngine.GameObject obj3 = new("Alpha");
            try
            {
                List<UnityEngine.GameObject> list = new() { obj1, obj2, obj3 };
                list.SortByName();
                Assert.That(list[0].name, Is.EqualTo("Alpha"));
                Assert.That(list[1].name, Is.EqualTo("Same"));
                Assert.That(list[2].name, Is.EqualTo("Same"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(obj1);
                UnityEngine.Object.DestroyImmediate(obj2);
                UnityEngine.Object.DestroyImmediate(obj3);
            }
        }

        private sealed class CustomList<T> : IList<T>
        {
            private readonly List<T> _inner = new();

            public T this[int index]
            {
                get => _inner[index];
                set => _inner[index] = value;
            }

            public int Count => _inner.Count;
            public bool IsReadOnly => false;

            public void Add(T item) => _inner.Add(item);

            public void Clear() => _inner.Clear();

            public bool Contains(T item) => _inner.Contains(item);

            public void CopyTo(T[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);

            public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();

            public int IndexOf(T item) => _inner.IndexOf(item);

            public void Insert(int index, T item) => _inner.Insert(index, item);

            public bool Remove(T item) => _inner.Remove(item);

            public void RemoveAt(int index) => _inner.RemoveAt(index);

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() =>
                _inner.GetEnumerator();
        }
    }
}
