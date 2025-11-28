namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using Helper;
    using Random;
    using Utils;

    /// <summary>
    /// Defines sorting algorithms available for list operations.
    /// </summary>
    public enum SortAlgorithm
    {
        /// <summary>Invalid sorting algorithm placeholder.</summary>
        [Obsolete("Please use a valid SortAlgorithm")]
        None = 0,

        /// <summary>Ghost sort algorithm - adaptive sorting with caching optimizations.</summary>
        Ghost = 1,

        /// <summary>Insertion sort algorithm - efficient for small or nearly-sorted lists.</summary>
        Insertion = 2,

        /// <summary>Meteor sort algorithm - adaptive gap-based sorting variant.</summary>
        Meteor = 3,

        /// <summary>Pattern-defeating quicksort - adaptive quicksort with pattern detection.</summary>
        PatternDefeatingQuickSort = 4,

        /// <summary>Grail sort algorithm - stable mergesort leveraging pooled buffers.</summary>
        Grail = 5,

        /// <summary>Power sort algorithm - adaptive mergesort that exploits natural runs.</summary>
        Power = 6,

        /// <summary>Shear sort algorithm - mesh-based alternating row/column sorter adapted for IList.</summary>
        Shear = 7,

        /// <summary>Tim sort algorithm - hybrid stable run-detecting mergesort popularized by Python/Java.</summary>
        Tim = 8,

        /// <summary>Jesse sort algorithm - dual-patience sort hybrid inspired by Jesse Michel’s research.</summary>
        Jesse = 9,

        /// <summary>Green sort algorithm - symmetric merge strategy inspired by greeNsort sustainability work.</summary>
        Green = 10,

        /// <summary>Ska sort algorithm - multi-pivot quicksort adapted from Malte Skarupke’s research.</summary>
        Ska = 11,

        /// <summary>Drift sort algorithm - stable drift-aware merge strategy from Voultapher’s Rust research.</summary>
        Drift = 12,

        /// <summary>Ipn sort algorithm - in-place, adaptive quicksort variant from Voultapher’s research.</summary>
        Ipn = 13,

        /// <summary>Smooth sort algorithm - weak-heap/smoothsort hybrid optimized for presorted data.</summary>
        Smooth = 14,

        /// <summary>Block merge sort (WikiSort-style) - stable low-buffer mergesort.</summary>
        Block = 15,

        /// <summary>IPS4o samplesort - cache-efficient multi-way samplesort.</summary>
        Ips4o = 16,

        /// <summary>Power sort plus - enhanced run-priority mergesort inspired by Wild & Nebel.</summary>
        PowerPlus = 17,

        /// <summary>Glide sort - stable galloping merges inspired by Rust glidesort.</summary>
        Glide = 18,

        /// <summary>Flux sort - pattern-defeating dual-pivot quicksort from sort-research.</summary>
        Flux = 19,

        /// <summary>Indy sort - stable queue-based merge strategy (Rust indiesort).</summary>
        Indy = 20,

        /// <summary>Sled sort - buffered stable merge leveraging sledsort heuristics.</summary>
        Sled = 21,
    }

    /// <summary>
    /// Extension methods for IList providing shuffling, shifting, sorting, searching, and element manipulation.
    /// </summary>
    /// <remarks>
    /// Thread Safety: Methods are not thread-safe and modify lists in-place unless noted otherwise.
    /// Performance: Methods are optimized for performance with minimal allocations.
    /// Most operations work directly on the list without creating copies.
    /// </remarks>
    public static class IListExtensions
    {
        private static readonly int[] SmoothSortLeonardoNumbers =
        {
            1,
            1,
            3,
            5,
            9,
            15,
            25,
            41,
            67,
            109,
            177,
            287,
            465,
            753,
            1219,
            1973,
            3193,
            5167,
            8361,
            13529,
            21891,
            35421,
            57313,
            92735,
            150049,
            242785,
            392835,
            635621,
            1028457,
            1664079,
            2692537,
            4356617,
            7049155,
            11405773,
            18454929,
            29860703,
            48315633,
            78176337,
            126491971,
            204668309,
            331160281,
            535828591,
            866988873,
            1402817465,
            int.MaxValue,
        };

        private const int Ips4oInsertionThreshold = 32;
        private const int Ips4oFallbackThreshold = 256;
        private const int Ips4oTargetBucketSize = 64;
        private const int Ips4oMaxBucketCount = 32;
        private const int GlideSortMinRun = 32;
        private const int GlideGallopTrigger = 7;

        /// <summary>
        /// Randomly shuffles the elements of a list in-place using the Fisher-Yates algorithm.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to shuffle.</param>
        /// <param name="random">The random number generator to use. If null, uses PRNG.Instance.</param>
        /// <remarks>
        /// <para>Null handling: If list is null, returns immediately. If random is null, uses PRNG.Instance.</para>
        /// <para>Thread safety: Not thread-safe. Modifies the list in place. If random is shared, may not be thread-safe. No Unity main thread requirement.</para>
        /// <para>Performance: O(n) where n is the number of elements.</para>
        /// <para>Allocations: No allocations.</para>
        /// <para>Edge cases: Lists with 0 or 1 elements are not modified.</para>
        /// </remarks>
        public static void Shuffle<T>(this IList<T> list, IRandom random = null)
        {
            if (list is not { Count: > 1 })
            {
                return;
            }

            random ??= PRNG.Instance;

            int length = list.Count;
            for (int i = 0; i < length - 1; ++i)
            {
                int nextIndex = random.Next(i, length);
                if (nextIndex == i)
                {
                    continue;
                }
                (list[i], list[nextIndex]) = (list[nextIndex], list[i]);
            }
        }

        /// <summary>
        /// Shifts (rotates) the elements of a list by the specified amount.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to shift.</param>
        /// <param name="amount">The number of positions to shift. Positive shifts right, negative shifts left.</param>
        /// <remarks>
        /// <para>Null handling: If list is null, returns immediately.</para>
        /// <para>Thread safety: Not thread-safe. Modifies the list in place. No Unity main thread requirement.</para>
        /// <para>Performance: O(n) where n is the number of elements. Uses three reversals algorithm.</para>
        /// <para>Allocations: No allocations.</para>
        /// <para>Edge cases: Lists with 0 or 1 elements are not modified. Amount is normalized using modulo. Amount of 0 or multiples of count result in no change.</para>
        /// </remarks>
        public static void Shift<T>(this IList<T> list, int amount)
        {
            if (list is not { Count: > 1 })
            {
                return;
            }

            int count = list.Count;
            amount = amount.PositiveMod(count);
            if (amount == 0)
            {
                return;
            }

            Reverse(list, 0, count - 1);
            Reverse(list, 0, amount - 1);
            Reverse(list, amount, count - 1);
        }

        /// <summary>
        /// Reverses the elements in a list within the specified range in-place.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to reverse a portion of.</param>
        /// <param name="start">The starting index (inclusive).</param>
        /// <param name="end">The ending index (inclusive).</param>
        /// <remarks>
        /// <para>Null handling: Throws NullReferenceException if list is null.</para>
        /// <para>Thread safety: Not thread-safe. Modifies the list in place. No Unity main thread requirement.</para>
        /// <para>Performance: O(n) where n is the number of elements in the range (end - start + 1).</para>
        /// <para>Allocations: No allocations.</para>
        /// <para>Edge cases: If start equals end, no change occurs. If start greater than end, no change occurs.</para>
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when start or end are out of range [0, Count).</exception>
        public static void Reverse<T>(this IList<T> list, int start, int end)
        {
            if (start < 0 || list.Count <= start)
            {
                throw new ArgumentException(nameof(start));
            }
            if (end < 0 || list.Count <= end)
            {
                throw new ArgumentException(nameof(end));
            }

            while (start < end)
            {
                (list[start], list[end]) = (list[end], list[start]);
                start++;
                end--;
            }
        }

        /// <summary>
        /// Removes an element at the specified index by swapping it with the last element, then removing the last element.
        /// This is faster than regular RemoveAt but does not preserve order.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to remove an element from.</param>
        /// <param name="index">The index of the element to remove.</param>
        /// <remarks>
        /// <para>Null handling: Throws NullReferenceException if list is null.</para>
        /// <para>Thread safety: Not thread-safe. Modifies the list in place. No Unity main thread requirement.</para>
        /// <para>Performance: O(1) regardless of list size or index position.</para>
        /// <para>Allocations: No allocations beyond what RemoveAt might allocate.</para>
        /// <para>Edge cases: Lists with 1 element are cleared. If index is the last element, behaves like normal RemoveAt. Does not preserve element order.</para>
        /// </remarks>
        public static void RemoveAtSwapBack<T>(this IList<T> list, int index)
        {
            if (list.Count <= 1)
            {
                list.Clear();
                return;
            }

            int lastIndex = list.Count - 1;
            if (index == lastIndex)
            {
                list.RemoveAt(index);
                return;
            }

            T last = list[lastIndex];
            list[index] = last;
            list.RemoveAt(lastIndex);
        }

        /// <summary>
        /// Sorts the elements in the list using the specified comparer and sort algorithm.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <typeparam name="TComparer">The type of comparer.</typeparam>
        /// <param name="array">The list to sort.</param>
        /// <param name="comparer">The comparer to use for element comparisons.</param>
        /// <param name="sortAlgorithm">
        /// The sorting algorithm to use (Ghost, Meteor, PatternDefeatingQuickSort, Grail, Power, or Insertion).
        /// Defaults to Ghost.
        /// </param>
        /// <remarks>
        /// <para>Null handling: Throws NullReferenceException if array is null. Comparer behavior depends on implementation.</para>
        /// <para>Thread safety: Not thread-safe. Modifies the list in place. No Unity main thread requirement.</para>
        /// <para>
        /// Performance: Ghost, Meteor, PatternDefeatingQuickSort, Grail, and Power sorts are O(n log n) on average.
        /// Insertion sort is O(n^2) worst/average case.
        /// </para>
        /// <para>Allocations: No allocations.</para>
        /// <para>
        /// Edge cases: Empty or single element lists require no sorting. Ghost, Meteor, and PatternDefeatingQuickSort
        /// are currently not stable. Grail and Power sorts are stable.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidEnumArgumentException">Thrown when sortAlgorithm is not a valid SortAlgorithm value.</exception>
        public static void Sort<T, TComparer>(
            this IList<T> array,
            TComparer comparer,
            SortAlgorithm sortAlgorithm = SortAlgorithm.Ghost
        )
            where TComparer : IComparer<T>
        {
            switch (sortAlgorithm)
            {
                case SortAlgorithm.Ghost:
                {
                    GhostSort(array, comparer);
                    return;
                }
                case SortAlgorithm.Insertion:
                {
                    InsertionSort(array, comparer);
                    return;
                }
                case SortAlgorithm.Meteor:
                {
                    MeteorSort(array, comparer);
                    return;
                }
                case SortAlgorithm.PatternDefeatingQuickSort:
                {
                    PatternDefeatingQuickSort(array, comparer);
                    return;
                }
                case SortAlgorithm.Grail:
                {
                    GrailSort(array, comparer);
                    return;
                }
                case SortAlgorithm.Power:
                {
                    PowerSort(array, comparer);
                    return;
                }
                case SortAlgorithm.Shear:
                {
                    ShearSort(array, comparer);
                    return;
                }
                case SortAlgorithm.Tim:
                {
                    TimSort(array, comparer);
                    return;
                }
                case SortAlgorithm.Jesse:
                {
                    JesseSort(array, comparer);
                    return;
                }
                case SortAlgorithm.Green:
                {
                    GreenSort(array, comparer);
                    return;
                }
                case SortAlgorithm.Ska:
                {
                    SkaSort(array, comparer);
                    return;
                }
                case SortAlgorithm.Drift:
                {
                    DriftSort(array, comparer);
                    return;
                }
                case SortAlgorithm.Ipn:
                {
                    IpnSort(array, comparer);
                    return;
                }
                case SortAlgorithm.Smooth:
                {
                    SmoothSort(array, comparer);
                    return;
                }
                case SortAlgorithm.Block:
                {
                    BlockMergeSort(array, comparer);
                    return;
                }
                case SortAlgorithm.Ips4o:
                {
                    Ips4oSort(array, comparer);
                    return;
                }
                case SortAlgorithm.PowerPlus:
                {
                    PowerSortPlus(array, comparer);
                    return;
                }
                case SortAlgorithm.Glide:
                {
                    GlideSort(array, comparer);
                    return;
                }
                case SortAlgorithm.Flux:
                {
                    FluxSort(array, comparer);
                    return;
                }
                case SortAlgorithm.Indy:
                {
                    IndySort(array, comparer);
                    return;
                }
                case SortAlgorithm.Sled:
                {
                    SledSort(array, comparer);
                    return;
                }
                default:
                {
                    throw new InvalidEnumArgumentException(
                        nameof(sortAlgorithm),
                        (int)sortAlgorithm,
                        typeof(SortAlgorithm)
                    );
                }
            }
        }

        /// <summary>
        /// Sorts the elements in the list using insertion sort algorithm.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <typeparam name="TComparer">The type of comparer.</typeparam>
        /// <param name="array">The list to sort.</param>
        /// <param name="comparer">The comparer to use for element comparisons.</param>
        /// <remarks>
        /// <para>Null handling: Throws NullReferenceException if array is null. Comparer behavior depends on implementation.</para>
        /// <para>Thread safety: Not thread-safe. Modifies the list in place. No Unity main thread requirement.</para>
        /// <para>Performance: O(n^2) worst/average case, O(n) best case when nearly sorted. Stable sort.</para>
        /// <para>Allocations: No allocations.</para>
        /// <para>Edge cases: Efficient for small or nearly sorted lists. Empty or single element lists require no sorting.</para>
        /// </remarks>
        public static void InsertionSort<T, TComparer>(this IList<T> array, TComparer comparer)
            where TComparer : IComparer<T>
        {
            int arrayCount = array.Count;
            if (arrayCount < 2)
            {
                return;
            }

            InsertionSortRange(array, 0, arrayCount - 1, comparer);
        }

        /// <summary>
        /// Sorts the elements in the list using the Meteor Sort algorithm, a gap-sequence-based hybrid sort.
        /// </summary>
        /// <remarks>
        /// Implementation reference: Meteor Sort by Wiley Looper, https://github.com/wileylooper/meteorsort.
        /// Note: Meteor Sort is currently not stable.
        /// </remarks>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <typeparam name="TComparer">The type of comparer.</typeparam>
        /// <param name="array">The list to sort.</param>
        /// <param name="comparer">The comparer to use for element comparisons.</param>
        /// <remarks>
        /// <para>Null handling: Throws NullReferenceException if array is null. Comparer behavior depends on implementation.</para>
        /// <para>Thread safety: Not thread-safe. Modifies the list in place. No Unity main thread requirement.</para>
        /// <para>Performance: O(n log n) average case using adaptive gap reductions.</para>
        /// <para>Allocations: No allocations.</para>
        /// <para>Edge cases: Not a stable sort - equal elements may be reordered.</para>
        /// </remarks>
        public static void MeteorSort<T, TComparer>(this IList<T> array, TComparer comparer)
            where TComparer : IComparer<T>
        {
            int length = array.Count;
            int gap = length;

            int i;
            int j;
            while (gap > 15)
            {
                gap = ((gap >> 2) - (gap >> 4)) + (gap >> 3);
                i = gap;

                while (i < length)
                {
                    T element = array[i];
                    j = i;

                    while (j >= gap && 0 < comparer.Compare(array[j - gap], element))
                    {
                        array[j] = array[j - gap];
                        j -= gap;
                    }

                    array[j] = element;
                    i++;
                }
            }

            i = 1;
            gap = 0;

            while (i < length)
            {
                T element = array[i];
                j = i;

                while (j > 0 && 0 < comparer.Compare(array[gap], element))
                {
                    array[j] = array[gap];
                    j = gap;
                    gap--;
                }

                array[j] = element;
                gap = i;
                i++;
            }
        }

        /// <summary>
        /// Sorts the elements in the list using pattern-defeating quicksort, an adaptive quicksort variant with
        /// introspective fallbacks and pattern detection.
        /// </summary>
        /// <remarks>
        /// Implementation reference: Pattern-Defeating Quicksort by Orson Peters, https://github.com/orlp/pdqsort (zlib License).
        /// This is a C# adaptation that retains the pattern-detection heuristics while operating on <c>IList&lt;T&gt;</c>.
        /// PatternDefeatingQuickSort is not stable.
        /// </remarks>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <typeparam name="TComparer">The type of comparer.</typeparam>
        /// <param name="array">The list to sort.</param>
        /// <param name="comparer">The comparer to use for element comparisons.</param>
        /// <remarks>
        /// <para>Null handling: Throws NullReferenceException if array is null. Comparer behavior depends on implementation.</para>
        /// <para>Thread safety: Not thread-safe. Modifies the list in place. No Unity main thread requirement.</para>
        /// <para>Performance: O(n log n) on average with protection against quadratic worst cases via heapsort fallback.</para>
        /// <para>Allocations: No allocations.</para>
        /// <para>Edge cases: Not a stable sort - equal elements may be reordered.</para>
        /// </remarks>
        public static void PatternDefeatingQuickSort<T, TComparer>(
            this IList<T> array,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            int count = array.Count;
            if (count < 2)
            {
                return;
            }

            int depthLimit = 2 * FloorLog2(count);
            PatternDefeatingQuickSortRange(array, 0, count - 1, comparer, depthLimit);
        }

        /// <summary>
        /// Sorts the elements in the list using the Grail Sort algorithm, a stable mergesort that adapts buffer usage.
        /// </summary>
        /// <remarks>
        /// Implementation reference: Grail Sort by Mrrl (MIT License), https://github.com/Mrrl/GrailSort.
        /// This adaptation uses pooled buffers instead of manual block buffers while keeping stability.
        /// </remarks>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <typeparam name="TComparer">The type of comparer.</typeparam>
        /// <param name="array">The list to sort.</param>
        /// <param name="comparer">The comparer to use for element comparisons.</param>
        /// <remarks>
        /// <para>Null handling: Throws NullReferenceException if array is null. Comparer behavior depends on implementation.</para>
        /// <para>Thread safety: Not thread-safe. Modifies the list in place. No Unity main thread requirement.</para>
        /// <para>Performance: O(n log n) worst/average case. Stable sort.</para>
        /// <para>Allocations: Uses pooled temporary buffers sized to half of the list.</para>
        /// <para>Edge cases: Empty or single element lists require no sorting.</para>
        /// </remarks>
        public static void GrailSort<T, TComparer>(this IList<T> array, TComparer comparer)
            where TComparer : IComparer<T>
        {
            int count = array.Count;
            if (count < 2)
            {
                return;
            }

            int bufferLength = count / 2 + 1;
            using PooledResource<T[]> bufferLease = WallstopArrayPool<T>.Get(
                bufferLength,
                out T[] buffer
            );
            GrailSortRange(array, buffer, 0, count - 1, comparer);
        }

        /// <summary>
        /// Sorts the elements in the list using the Power Sort algorithm, which exploits existing runs and merges them
        /// in near-optimal order.
        /// </summary>
        /// <remarks>
        /// Implementation reference: PowerSort (Munro & Wild) — adaptive mergesort leveraging natural runs.
        /// https://arxiv.org/abs/1805.04154 (CC BY 4.0). This adaptation detects runs and merges them with pooled buffers.
        /// </remarks>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <typeparam name="TComparer">The type of comparer.</typeparam>
        /// <param name="array">The list to sort.</param>
        /// <param name="comparer">The comparer to use for element comparisons.</param>
        /// <remarks>
        /// <para>Null handling: Throws NullReferenceException if array is null. Comparer behavior depends on implementation.</para>
        /// <para>Thread safety: Not thread-safe. Modifies the list in place. No Unity main thread requirement.</para>
        /// <para>Performance: O(n log n) worst/average case, approaching O(n) on partially sorted data. Stable sort.</para>
        /// <para>Allocations: Uses pooled lists and buffers for run management and merging.</para>
        /// <para>Edge cases: Empty or single element lists require no sorting.</para>
        /// </remarks>
        public static void PowerSort<T, TComparer>(this IList<T> array, TComparer comparer)
            where TComparer : IComparer<T>
        {
            int count = array.Count;
            if (count < 2)
            {
                return;
            }

            using PooledResource<List<(int start, int length)>> runBuffer = Buffers<(
                int start,
                int length
            )>.List.Get(out List<(int start, int length)> runs);
            using PooledResource<List<(int start, int length)>> mergeBuffer = Buffers<(
                int start,
                int length
            )>.List.Get(out List<(int start, int length)> mergedRuns);

            CollectNaturalRuns(array, comparer, runs);
            if (runs.Count <= 1)
            {
                return;
            }

            int bufferLength = count / 2 + 1;
            using PooledResource<T[]> tempLease = WallstopArrayPool<T>.Get(
                bufferLength,
                out T[] buffer
            );

            while (runs.Count > 1)
            {
                mergedRuns.Clear();
                int runCount = runs.Count;
                for (int i = 0; i < runCount; i += 2)
                {
                    if (i + 1 >= runCount)
                    {
                        mergedRuns.Add(runs[i]);
                        continue;
                    }

                    (int start, int length) leftRun = runs[i];
                    (int start, int length) rightRun = runs[i + 1];
                    MergeRuns(
                        array,
                        buffer,
                        leftRun.start,
                        leftRun.length,
                        rightRun.start,
                        rightRun.length,
                        comparer
                    );
                    mergedRuns.Add((leftRun.start, leftRun.length + rightRun.length));
                }

                (runs, mergedRuns) = (mergedRuns, runs);
            }
        }

        /// <summary>
        /// Sorts the list using Shear Sort, alternating between row-wise and column-wise passes over a virtual mesh.
        /// </summary>
        /// <remarks>
        /// Implementation reference: Shear Sort (Batcher, 1968), https://en.wikipedia.org/wiki/Shear_sort.
        /// This adaptation projects the 1D <c>IList&lt;T&gt;</c> into a virtual square mesh and alternates row/column passes.
        /// </remarks>
        public static void ShearSort<T, TComparer>(this IList<T> array, TComparer comparer)
            where TComparer : IComparer<T>
        {
            int count = array.Count;
            if (count < 2)
            {
                return;
            }

            int dimension = 1;
            while ((long)dimension * dimension < count)
            {
                dimension++;
            }

            int meshCells = dimension * dimension;
            if (meshCells != count)
            {
                // Shear Sort operates on perfect meshes; fall back to a robust quicksort variant otherwise.
                PatternDefeatingQuickSort(array, comparer);
                return;
            }

            int phases = Math.Max(1, (int)Math.Ceiling(Math.Log(meshCells, 2.0))) + 1;

            for (int phase = 0; phase < phases; ++phase)
            {
                bool rowPhase = (phase & 1) == 0;
                if (rowPhase)
                {
                    for (int row = 0; row < dimension; ++row)
                    {
                        bool ascending = (row & 1) == 0;
                        ShearSortRow(array, comparer, row, dimension, count, ascending);
                    }
                }
                else
                {
                    for (int column = 0; column < dimension; ++column)
                    {
                        ShearSortColumn(array, comparer, column, dimension, count);
                    }
                }
            }

            for (int row = 0; row < dimension; ++row)
            {
                ShearSortRow(array, comparer, row, dimension, count, true);
            }
        }

        /// <summary>
        /// Sorts the list using TimSort, a hybrid stable sort that detects natural runs and merges them adaptively.
        /// </summary>
        /// <remarks>
        /// Implementation reference: TimSort by Tim Peters (Python) and the OpenJDK adaptation.
        /// Sources: https://bugs.python.org/file4451/timsort.txt and https://openjdk.java.net/projects/amber/.
        /// </remarks>
        public static void TimSort<T, TComparer>(this IList<T> array, TComparer comparer)
            where TComparer : IComparer<T>
        {
            int count = array.Count;
            if (count < 2)
            {
                return;
            }

            int minRun = ComputeTimSortMinRun(count);
            using PooledResource<T[]> bufferLease = WallstopArrayPool<T>.Get(
                Math.Max(count / 2 + 1, 32),
                out T[] buffer
            );
            using PooledResource<List<(int start, int length)>> runStackLease = Buffers<(
                int start,
                int length
            )>.List.Get(out List<(int start, int length)> runStack);
            runStack.Clear();

            int index = 0;
            while (index < count)
            {
                int runLength = MakeAscendingRun(array, index, count, comparer);
                int forcedLength = Math.Max(runLength, minRun);
                int targetEnd = Math.Min(count, index + forcedLength);
                if (runLength < forcedLength)
                {
                    InsertionSortRange(array, index, targetEnd - 1, comparer);
                    runLength = targetEnd - index;
                }

                runStack.Add((index, runLength));
                TimSortMergeCollapse(array, comparer, buffer, runStack);
                index += runLength;
            }

            TimSortMergeForce(array, comparer, buffer, runStack);
        }

        /// <summary>
        /// Sorts the list using JesseSort, a hybrid that routes natural runs through two patience games
        /// before merging all piles with a k-way heap.
        /// </summary>
        /// <remarks>
        /// Implementation reference: JesseSort by Jesse Michel, https://github.com/lewj85/jessesort.
        /// This adaptation materializes patience piles as <c>IList</c>-backed stacks and performs a k-way merge.
        /// </remarks>
        public static void JesseSort<T, TComparer>(this IList<T> array, TComparer comparer)
            where TComparer : IComparer<T>
        {
            int count = array.Count;
            if (count < 2)
            {
                return;
            }

            using PooledResource<List<List<T>>> ascendingLease = Buffers<List<T>>.List.Get(
                out List<List<T>> ascendingPiles
            );
            using PooledResource<List<List<T>>> descendingLease = Buffers<List<T>>.List.Get(
                out List<List<T>> descendingPiles
            );
            ascendingPiles.Clear();
            descendingPiles.Clear();

            int index = 0;
            while (index < count)
            {
                int runStart = index;
                index++;
                if (index == count)
                {
                    JessePatienceInsert(ascendingPiles, array[runStart], comparer);
                    break;
                }

                int compare = comparer.Compare(array[index - 1], array[index]);
                bool ascendingRun = compare <= 0;
                while (index < count)
                {
                    int nextCompare = comparer.Compare(array[index - 1], array[index]);
                    if (ascendingRun)
                    {
                        if (nextCompare <= 0)
                        {
                            index++;
                            continue;
                        }
                    }
                    else
                    {
                        if (nextCompare >= 0)
                        {
                            index++;
                            continue;
                        }
                    }
                    break;
                }

                int runEnd = index - 1;
                if (ascendingRun)
                {
                    for (int i = runStart; i <= runEnd; ++i)
                    {
                        JessePatienceInsert(ascendingPiles, array[i], comparer);
                    }
                }
                else
                {
                    for (int i = runEnd; i >= runStart; --i)
                    {
                        JessePatienceInsert(descendingPiles, array[i], comparer);
                    }
                }
            }

            using PooledResource<List<JesseCursor<T>>> heapLease = Buffers<JesseCursor<T>>.List.Get(
                out List<JesseCursor<T>> heap
            );
            heap.Clear();
            InitializeJesseHeap(ascendingPiles, heap, comparer);
            InitializeJesseHeap(descendingPiles, heap, comparer);

            using PooledResource<T[]> outputLease = WallstopArrayPool<T>.Get(count, out T[] output);
            int outputIndex = 0;
            while (heap.Count > 0)
            {
                JesseCursor<T> cursor = heap[0];
                output[outputIndex] = cursor.Peek();
                outputIndex++;
                if (cursor.MoveNext())
                {
                    heap[0] = cursor;
                }
                else
                {
                    heap[0] = heap[heap.Count - 1];
                    heap.RemoveAt(heap.Count - 1);
                }

                if (heap.Count > 0)
                {
                    JesseHeapSiftDown(heap, 0, comparer);
                }
            }

            for (int i = 0; i < count; ++i)
            {
                array[i] = output[i];
            }

            for (int i = 0; i < ascendingPiles.Count; ++i)
            {
                ascendingPiles[i].Clear();
            }
            for (int i = 0; i < descendingPiles.Count; ++i)
            {
                descendingPiles[i].Clear();
            }
            ascendingPiles.Clear();
            descendingPiles.Clear();
        }

        /// <summary>
        /// Sorts the list using GreenSort, a stable symmetric mergesort that trims already ordered prefixes and suffixes.
        /// </summary>
        /// <remarks>
        /// Implementation reference: greeNsort (Jens Oehlschlägel) — https://www.greensort.org.
        /// This adaptation uses symmetric trimming before merging halves to reduce data movement.
        /// </remarks>
        public static void GreenSort<T, TComparer>(this IList<T> array, TComparer comparer)
            where TComparer : IComparer<T>
        {
            int count = array.Count;
            if (count < 2)
            {
                return;
            }

            using PooledResource<T[]> bufferLease = WallstopArrayPool<T>.Get(
                Math.Max(count / 2 + 1, 64),
                out T[] buffer
            );
            GreenSortRange(array, buffer, 0, count - 1, comparer);
        }

        /// <summary>
        /// Sorts the list using Ska Sort, a branch-friendly dual-pivot quicksort inspired by Skarupke’s research.
        /// </summary>
        /// <remarks>
        /// Implementation reference: Ska Sort by Malte Skarupke, https://probablydance.com/2016/12/27/i-wrote-a-faster-sorting-algorithm/.
        /// This adaptation applies multi-way partitioning with ninther sampling and tail recursion elimination.
        /// </remarks>
        public static void SkaSort<T, TComparer>(this IList<T> array, TComparer comparer)
            where TComparer : IComparer<T>
        {
            int count = array.Count;
            if (count < 2)
            {
                return;
            }

            SkaSortRange(array, 0, count - 1, comparer, 2 * FloorLog2(count));
        }

        /// <summary>
        /// Sorts the list using DriftSort, a stable block merge that eagerly merges regions whose boundaries drift.
        /// </summary>
        /// <remarks>
        /// Implementation reference: DriftSort (Voultapher), https://github.com/Voultapher/sort-research-rs/tree/main/writeup/driftsort_introduction.
        /// </remarks>
        public static void DriftSort<T, TComparer>(this IList<T> array, TComparer comparer)
            where TComparer : IComparer<T>
        {
            int count = array.Count;
            if (count < 2)
            {
                return;
            }

            int blockSize = Math.Max(32, (int)Math.Ceiling(Math.Sqrt(count)));
            using PooledResource<T[]> bufferLease = WallstopArrayPool<T>.Get(
                Math.Max(count / 2 + 1, blockSize),
                out T[] buffer
            );
            using PooledResource<List<(int start, int length)>> runsLease = Buffers<(
                int start,
                int length
            )>.List.Get(out List<(int start, int length)> runs);
            runs.Clear();

            for (int i = 0; i < count; i += blockSize)
            {
                int end = Math.Min(count - 1, i + blockSize - 1);
                InsertionSortRange(array, i, end, comparer);
                runs.Add((i, end - i + 1));
            }

            while (runs.Count > 1)
            {
                bool merged = false;
                for (int i = 0; i < runs.Count - 1; ++i)
                {
                    (int startA, int lengthA) = runs[i];
                    (int startB, int lengthB) = runs[i + 1];
                    int endA = startA + lengthA - 1;
                    if (comparer.Compare(array[endA], array[startB]) > 0)
                    {
                        MergeRuns(array, buffer, startA, lengthA, startB, lengthB, comparer);
                        runs[i] = (startA, lengthA + lengthB);
                        runs.RemoveAt(i + 1);
                        merged = true;
                        break;
                    }
                }

                if (!merged)
                {
                    (int start, int length) first = runs[0];
                    (int start, int length) second = runs[1];
                    MergeRuns(
                        array,
                        buffer,
                        first.start,
                        first.length,
                        second.start,
                        second.length,
                        comparer
                    );
                    runs[0] = (first.start, first.length + second.length);
                    runs.RemoveAt(1);
                }
            }
        }

        /// <summary>
        /// Sorts the list using IpnSort, an unstable introspective quicksort with median-of-medians sampling.
        /// </summary>
        /// <remarks>
        /// Implementation reference: ipnsort (Voultapher) — https://github.com/Voultapher/sort-research-rs/tree/main/writeup/ipnsort_introduction.
        /// </remarks>
        public static void IpnSort<T, TComparer>(this IList<T> array, TComparer comparer)
            where TComparer : IComparer<T>
        {
            int count = array.Count;
            if (count < 2)
            {
                return;
            }

            IpnSortRange(array, 0, count - 1, comparer, 2 * FloorLog2(count));
        }

        /// <summary>
        /// Sorts the list using SmoothSort, providing O(n) behavior on nearly sorted data while remaining in-place.
        /// </summary>
        /// <remarks>
        /// Implementation reference: SmoothSort (Dijkstra, Edelkamp/Wegener) via Nico Lomuto’s refresher and Keith Schwarz’s lecture notes.
        /// </remarks>
        public static void SmoothSort<T, TComparer>(this IList<T> array, TComparer comparer)
            where TComparer : IComparer<T>
        {
            int count = array.Count;
            if (count < 2)
            {
                return;
            }

            int head = 0;
            int last = count - 1;
            int p = 1;
            int pshift = 1;

            while (head < last)
            {
                if ((p & 3) == 3)
                {
                    SmoothSortSift(array, head, pshift, comparer);
                    p >>= 2;
                    pshift += 2;
                }
                else
                {
                    if (SmoothSortLeonardoNumbers[pshift - 1] >= last - head)
                    {
                        SmoothSortTrinkle(array, head, p, pshift, false, comparer);
                    }
                    else
                    {
                        SmoothSortSift(array, head, pshift, comparer);
                    }

                    if (pshift == 1)
                    {
                        p <<= 1;
                        pshift--;
                    }
                    else
                    {
                        p <<= pshift - 1;
                        pshift = 1;
                    }
                }

                p |= 1;
                head++;
            }

            SmoothSortTrinkle(array, head, p, pshift, false, comparer);

            while (pshift != 1 || p != 1)
            {
                if (pshift <= 1)
                {
                    int trailing = SmoothSortTrailingZeroCount(p & ~1);
                    p >>= trailing;
                    pshift += trailing;
                }
                else
                {
                    p <<= 2;
                    p ^= 7;
                    pshift -= 2;

                    SmoothSortTrinkle(
                        array,
                        head - SmoothSortLeonardoNumbers[pshift] - 1,
                        p >> 1,
                        pshift + 1,
                        true,
                        comparer
                    );
                    SmoothSortTrinkle(array, head - 1, p, pshift, true, comparer);
                }

                head--;
            }
        }

        private static void SmoothSortSift<T, TComparer>(
            IList<T> array,
            int head,
            int pshift,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            T value = array[head];

            while (pshift > 1)
            {
                int right = head - 1;
                int left = head - 1 - SmoothSortLeonardoNumbers[pshift - 2];

                if (
                    comparer.Compare(value, array[left]) >= 0
                    && comparer.Compare(value, array[right]) >= 0
                )
                {
                    break;
                }

                if (comparer.Compare(array[left], array[right]) >= 0)
                {
                    array[head] = array[left];
                    head = left;
                    pshift--;
                }
                else
                {
                    array[head] = array[right];
                    head = right;
                    pshift -= 2;
                }
            }

            array[head] = value;
        }

        private static void SmoothSortTrinkle<T, TComparer>(
            IList<T> array,
            int head,
            int p,
            int pshift,
            bool isTrusty,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            T value = array[head];

            while (p != 1)
            {
                int stepson = head - SmoothSortLeonardoNumbers[pshift];
                if (comparer.Compare(array[stepson], value) <= 0)
                {
                    break;
                }

                if (!isTrusty && pshift > 1)
                {
                    int right = head - 1;
                    int left = head - 1 - SmoothSortLeonardoNumbers[pshift - 2];
                    if (
                        comparer.Compare(array[right], array[stepson]) >= 0
                        || comparer.Compare(array[left], array[stepson]) >= 0
                    )
                    {
                        break;
                    }
                }

                array[head] = array[stepson];
                head = stepson;
                int trailing = SmoothSortTrailingZeroCount(p & ~1);
                p >>= trailing;
                pshift += trailing;
                isTrusty = false;
            }

            if (!isTrusty)
            {
                array[head] = value;
                SmoothSortSift(array, head, pshift, comparer);
                return;
            }

            array[head] = value;
        }

        private static int SmoothSortTrailingZeroCount(int value)
        {
            if (value == 0)
            {
                return 32;
            }

            int count = 0;
            while ((value & 1) == 0)
            {
                count++;
                value >>= 1;
            }

            return count;
        }

        /// <summary>
        /// Sorts the list using block-based stable merges backed by a pooled buffer.
        /// </summary>
        /// <remarks>
        /// Implementation reference: WikiSort / block merge sort by Mike Ash (public domain),
        /// https://github.com/BonzaiThePenguin/WikiSort. This adaptation uses a pooled full-size buffer.
        /// </remarks>
        public static void BlockMergeSort<T, TComparer>(this IList<T> array, TComparer comparer)
            where TComparer : IComparer<T>
        {
            int count = array.Count;
            if (count < 2)
            {
                return;
            }

            using PooledResource<T[]> bufferLease = WallstopArrayPool<T>.Get(count, out T[] buffer);
            int runSize = 1;
            while (runSize < count)
            {
                for (int left = 0; left < count - runSize; left += runSize << 1)
                {
                    int mid = left + runSize - 1;
                    int right = Math.Min(count - 1, left + (runSize << 1) - 1);
                    BlockMerge(array, buffer, left, mid, right, comparer);
                }

                runSize <<= 1;
            }
        }

        /// <summary>
        /// Sorts the list using IPS⁴o, a cache-aware samplesort that partitions into multiple buckets per pass.
        /// </summary>
        /// <remarks>
        /// Implementation reference: IPS⁴o samplesort (Axtmann, Sanders, Schulz, Wenger), https://arxiv.org/abs/1705.02257.
        /// </remarks>
        public static void Ips4oSort<T, TComparer>(this IList<T> array, TComparer comparer)
            where TComparer : IComparer<T>
        {
            int count = array.Count;
            if (count < 2)
            {
                return;
            }

            Ips4oSortRange(array, 0, count - 1, comparer);
        }

        private static void Ips4oSortRange<T, TComparer>(
            IList<T> array,
            int left,
            int right,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            int length = right - left + 1;
            if (length <= Ips4oInsertionThreshold)
            {
                InsertionSortRange(array, left, right, comparer);
                return;
            }

            if (length <= Ips4oFallbackThreshold)
            {
                PatternDefeatingQuickSortRange(array, left, right, comparer, 2 * FloorLog2(length));
                return;
            }

            int bucketCount = DetermineIps4oBucketCount(length);
            int pivotCount = bucketCount - 1;
            int sampleSize = Math.Min(length, (bucketCount * 2) - 1);

            using PooledResource<T[]> sampleLease = WallstopArrayPool<T>.Get(
                sampleSize,
                out T[] sample
            );
            Ips4oBuildSample(array, left, right, sample, sampleSize);
            Array.Sort(sample, 0, sampleSize, comparer);

            using PooledResource<T[]> pivotLease = WallstopArrayPool<T>.Get(
                pivotCount,
                out T[] pivots
            );
            Ips4oSelectPivots(sample, sampleSize, pivots, bucketCount);

            using PooledResource<int[]> countLease = WallstopArrayPool<int>.Get(
                bucketCount,
                out int[] bucketCounts
            );
            using PooledResource<int[]> offsetLease = WallstopArrayPool<int>.Get(
                bucketCount,
                out int[] bucketOffsets
            );
            using PooledResource<int[]> positionLease = WallstopArrayPool<int>.Get(
                bucketCount,
                out int[] bucketPositions
            );
            Array.Clear(bucketCounts, 0, bucketCount);

            for (int i = left; i <= right; ++i)
            {
                int bucket = Ips4oLocateBucket(array[i], pivots, pivotCount, comparer);
                bucketCounts[bucket]++;
            }

            int running = 0;
            for (int i = 0; i < bucketCount; ++i)
            {
                bucketOffsets[i] = running;
                running += bucketCounts[i];
            }

            Array.Copy(bucketOffsets, bucketPositions, bucketCount);

            using PooledResource<T[]> bufferLease = WallstopArrayPool<T>.Get(
                length,
                out T[] buffer
            );
            for (int i = left; i <= right; ++i)
            {
                T value = array[i];
                int bucket = Ips4oLocateBucket(value, pivots, pivotCount, comparer);
                int destination = bucketPositions[bucket]++;
                buffer[destination] = value;
            }

            for (int i = 0; i < length; ++i)
            {
                array[left + i] = buffer[i];
            }

            bool degeneratePartition = false;
            for (int i = 0; i < bucketCount; ++i)
            {
                if (bucketCounts[i] == length)
                {
                    degeneratePartition = true;
                    break;
                }
            }

            if (degeneratePartition)
            {
                PatternDefeatingQuickSortRange(array, left, right, comparer, 2 * FloorLog2(length));
                return;
            }

            for (int i = 0; i < bucketCount; ++i)
            {
                int bucketSize = bucketCounts[i];
                if (bucketSize <= 1)
                {
                    continue;
                }

                int bucketLeft = left + bucketOffsets[i];
                int bucketRight = bucketLeft + bucketSize - 1;
                Ips4oSortRange(array, bucketLeft, bucketRight, comparer);
            }
        }

        private static int DetermineIps4oBucketCount(int length)
        {
            int estimate = length / Ips4oTargetBucketSize;
            if (estimate < 4)
            {
                estimate = 4;
            }
            else if (estimate > Ips4oMaxBucketCount)
            {
                estimate = Ips4oMaxBucketCount;
            }

            return estimate;
        }

        private static void Ips4oBuildSample<T>(
            IList<T> array,
            int left,
            int right,
            T[] sample,
            int sampleSize
        )
        {
            if (sampleSize == 0)
            {
                return;
            }

            int length = right - left + 1;
            double stride = (double)length / sampleSize;
            double position = stride * 0.5d;

            for (int i = 0; i < sampleSize; ++i)
            {
                int offset = (int)Math.Max(0, Math.Min(length - 1, Math.Floor(position)));
                sample[i] = array[left + offset];
                position += stride;
            }
        }

        private static void Ips4oSelectPivots<T>(
            T[] sample,
            int sampleSize,
            T[] pivots,
            int bucketCount
        )
        {
            if (pivots.Length == 0)
            {
                return;
            }

            for (int i = 1; i < bucketCount; ++i)
            {
                int index = (i * sampleSize) / bucketCount;
                if (index >= sampleSize)
                {
                    index = sampleSize - 1;
                }
                pivots[i - 1] = sample[index];
            }
        }

        private static int Ips4oLocateBucket<T, TComparer>(
            T value,
            T[] pivots,
            int pivotCount,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            if (pivotCount == 0)
            {
                return 0;
            }

            int low = 0;
            int high = pivotCount;
            while (low < high)
            {
                int mid = (low + high) >> 1;
                if (comparer.Compare(value, pivots[mid]) > 0)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid;
                }
            }

            return low;
        }

        /// <summary>
        /// Sorts the list using PowerSort+, an enhanced run-aware mergesort with priority-based merging.
        /// </summary>
        /// <remarks>
        /// Implementation reference: PowerSort+ (Wild & Nebel), which prioritizes runs via their power metric.
        /// </remarks>
        public static void PowerSortPlus<T, TComparer>(this IList<T> array, TComparer comparer)
            where TComparer : IComparer<T>
        {
            int count = array.Count;
            if (count < 2)
            {
                return;
            }

            using PooledResource<List<(int start, int length)>> runBuffer = Buffers<(
                int start,
                int length
            )>.List.Get(out List<(int start, int length)> runs);
            runs.Clear();
            CollectNaturalRuns(array, comparer, runs);
            if (runs.Count <= 1)
            {
                return;
            }

            int bufferLength = Math.Max(32, count / 2 + 1);
            using PooledResource<T[]> tempLease = WallstopArrayPool<T>.Get(
                bufferLength,
                out T[] buffer
            );

            using PooledResource<List<PowerSortPlusRun>> nodeLease =
                Buffers<PowerSortPlusRun>.List.Get(out List<PowerSortPlusRun> nodes);
            nodes.Clear();
            PowerSortPlusRun head = BuildPowerSortPlusRuns(runs, nodes);

            List<PowerSortPlusCandidate> heap = new();
            for (PowerSortPlusRun node = head; node != null && node.Next != null; node = node.Next)
            {
                PowerSortPlusPushCandidate(heap, node, node.Next);
            }

            int activeRuns = runs.Count;
            while (activeRuns > 1)
            {
                if (!PowerSortPlusTryPopValidCandidate(heap, out PowerSortPlusCandidate candidate))
                {
                    PowerSortPlusRun fallbackLeft = head;
                    PowerSortPlusRun fallbackRight = fallbackLeft?.Next;
                    if (fallbackLeft == null || fallbackRight == null)
                    {
                        break;
                    }

                    head = PowerSortPlusMergeNodes(
                        array,
                        buffer,
                        comparer,
                        head,
                        heap,
                        fallbackLeft,
                        fallbackRight
                    );
                    activeRuns--;
                    continue;
                }

                head = PowerSortPlusMergeNodes(
                    array,
                    buffer,
                    comparer,
                    head,
                    heap,
                    candidate.Left,
                    candidate.Right
                );
                activeRuns--;
            }

            foreach (PowerSortPlusRun node in nodes)
            {
                node.Prev = null;
                node.Next = null;
            }
        }

        /// <summary>
        /// Sorts the list using GlideSort, a stable mergesort that glides runs together with galloping.
        /// </summary>
        /// <remarks>
        /// Implementation reference: Glidesort (Rust std::sort write-up by Orson Peters & Sebastian W.),
        /// https://github.com/Voultapher/sort-research-rs/tree/main/writeup/glidesort.
        /// </remarks>
        public static void GlideSort<T, TComparer>(this IList<T> array, TComparer comparer)
            where TComparer : IComparer<T>
        {
            int count = array.Count;
            if (count < 2)
            {
                return;
            }

            using PooledResource<T[]> bufferLease = WallstopArrayPool<T>.Get(count, out T[] buffer);
            using PooledResource<List<(int start, int length)>> runsLease = Buffers<(
                int start,
                int length
            )>.List.Get(out List<(int start, int length)> runs);
            using PooledResource<List<(int start, int length)>> nextRunsLease = Buffers<(
                int start,
                int length
            )>.List.Get(out List<(int start, int length)> nextRuns);

            runs.Clear();
            nextRuns.Clear();

            int minRun = Math.Max(GlideSortMinRun, ComputeTimSortMinRun(count));
            int index = 0;
            while (index < count)
            {
                int runLength = MakeAscendingRun(array, index, count, comparer);
                int forcedLength = Math.Max(runLength, minRun);
                int targetEnd = Math.Min(count, index + forcedLength);
                if (runLength < forcedLength)
                {
                    InsertionSortRange(array, index, targetEnd - 1, comparer);
                    runLength = targetEnd - index;
                }

                runs.Add((index, runLength));
                index += runLength;
            }

            if (runs.Count <= 1)
            {
                return;
            }

            while (runs.Count > 1)
            {
                nextRuns.Clear();
                int i = 0;
                while (i < runs.Count)
                {
                    if (i + 1 >= runs.Count)
                    {
                        nextRuns.Add(runs[i]);
                        break;
                    }

                    (int leftStart, int leftLength) = runs[i];
                    (int rightStart, int rightLength) = runs[i + 1];
                    GlideMergeRuns(
                        array,
                        buffer,
                        leftStart,
                        leftLength,
                        rightStart,
                        rightLength,
                        comparer
                    );
                    nextRuns.Add((leftStart, leftLength + rightLength));
                    i += 2;
                }

                (runs, nextRuns) = (nextRuns, runs);
            }
        }

        /// <summary>
        /// Sorts the list using FluxSort, an unstable dual-pivot quicksort with adaptive pair partitioning.
        /// </summary>
        /// <remarks>
        /// Implementation reference: Fluxsort / Fluxsort2 (Voultapher), https://github.com/Voultapher/sort-research-rs/tree/main/writeup/fluxsort.
        /// </remarks>
        public static void FluxSort<T, TComparer>(this IList<T> array, TComparer comparer)
            where TComparer : IComparer<T>
        {
            int count = array.Count;
            if (count < 2)
            {
                return;
            }

            DualPivotQuickSort(array, 0, count - 1, comparer);
        }

        /// <summary>
        /// Sorts the list using IndySort, a stable queue-based merge sort.
        /// </summary>
        /// <remarks>
        /// Implementation reference: indiesort (glidesort companion algorithm),
        /// https://github.com/Voultapher/sort-research-rs/tree/main/writeup/ipnsort_introduction.
        /// </remarks>
        public static void IndySort<T, TComparer>(this IList<T> array, TComparer comparer)
            where TComparer : IComparer<T>
        {
            int count = array.Count;
            if (count < 2)
            {
                return;
            }

            using PooledResource<List<(int start, int length)>> runLease = Buffers<(
                int start,
                int length
            )>.List.Get(out List<(int start, int length)> runs);
            runs.Clear();
            CollectNaturalRuns(array, comparer, runs);
            if (runs.Count <= 1)
            {
                return;
            }

            int bufferLength = Math.Max(32, (count / 2) + 1);
            using PooledResource<T[]> bufferLease = WallstopArrayPool<T>.Get(
                bufferLength,
                out T[] buffer
            );

            int head = 0;
            while (runs.Count - head > 1)
            {
                (int startA, int lengthA) = runs[head];
                (int startB, int lengthB) = runs[head + 1];
                MergeRuns(array, buffer, startA, lengthA, startB, lengthB, comparer);
                runs[head + 1] = (startA, lengthA + lengthB);
                head++;
            }
        }

        /// <summary>
        /// Sorts the list using SledSort, a stable buffered merge sort tuned for large datasets.
        /// </summary>
        /// <remarks>
        /// Implementation reference: Sledsort (greeNsort portfolio), https://www.greensort.org/portfolio.html.
        /// </remarks>
        public static void SledSort<T, TComparer>(this IList<T> array, TComparer comparer)
            where TComparer : IComparer<T>
        {
            int count = array.Count;
            if (count < 2)
            {
                return;
            }

            using PooledResource<List<(int start, int length)>> runLease = Buffers<(
                int start,
                int length
            )>.List.Get(out List<(int start, int length)> runs);
            using PooledResource<List<(int start, int length)>> workLease = Buffers<(
                int start,
                int length
            )>.List.Get(out List<(int start, int length)> workRuns);
            runs.Clear();
            workRuns.Clear();
            CollectNaturalRuns(array, comparer, runs);
            if (runs.Count <= 1)
            {
                return;
            }

            workRuns.AddRange(runs);

            using PooledResource<T[]> bufferLease = WallstopArrayPool<T>.Get(
                Math.Max(32, (count / 2) + 1),
                out T[] buffer
            );

            while (workRuns.Count > 1)
            {
                (int startA, int lengthA) = workRuns[0];
                (int startB, int lengthB) = workRuns[1];
                if (startB < startA)
                {
                    (startA, startB) = (startB, startA);
                    (lengthA, lengthB) = (lengthB, lengthA);
                }

                MergeRuns(array, buffer, startA, lengthA, startB, lengthB, comparer);
                workRuns.RemoveAt(0);
                workRuns.RemoveAt(0);

                int mergedStart = startA;
                int mergedLength = lengthA + lengthB;
                int insertIndex = 0;
                while (insertIndex < workRuns.Count && workRuns[insertIndex].start < mergedStart)
                {
                    insertIndex++;
                }

                workRuns.Insert(insertIndex, (mergedStart, mergedLength));
            }
        }

        /// <summary>
        /// Sorts the elements in the list using the Ghost Sort algorithm, a hybrid gap-based sorting algorithm.
        /// </summary>
        /// <remarks>
        /// Implementation copyright Will Stafford Parsons (ghostsort). Repository currently offline; preserved locally.
        /// Ghost Sort is not stable.
        /// </remarks>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <typeparam name="TComparer">The type of comparer.</typeparam>
        /// <param name="array">The list to sort.</param>
        /// <param name="comparer">The comparer to use for element comparisons.</param>
        /// <remarks>
        /// <para>Null handling: Throws NullReferenceException if array is null. Comparer behavior depends on implementation.</para>
        /// <para>Thread safety: Not thread-safe. Modifies the list in place. No Unity main thread requirement.</para>
        /// <para>Performance: O(n log n) average case. Hybrid algorithm combining gap sort with insertion sort.</para>
        /// <para>Allocations: No allocations.</para>
        /// <para>Edge cases: Not a stable sort - equal elements may be reordered. Implementation by Will Stafford Parsons.</para>
        /// </remarks>
        public static void GhostSort<T, TComparer>(this IList<T> array, TComparer comparer)
            where TComparer : IComparer<T>
        {
            int length = array.Count;
            int gap = array.Count;

            int i;
            int j;
            while (gap > 15)
            {
                gap = (gap >> 5) + (gap >> 3);
                i = gap;

                while (i < length)
                {
                    T element = array[i];
                    j = i;
                    while (j >= gap && 0 < comparer.Compare(array[j - gap], element))
                    {
                        array[j] = array[j - gap];
                        j -= gap;
                    }

                    array[j] = element;
                    i++;
                }
            }

            i = 1;
            gap = 0;

            while (i < length)
            {
                T element = array[i];
                j = i;

                while (j > 0 && 0 < comparer.Compare(array[gap], element))
                {
                    array[j] = array[gap];
                    j = gap;
                    gap--;
                }

                array[j] = element;
                gap = i;
                i++;
            }
        }

        private static void PatternDefeatingQuickSortRange<T, TComparer>(
            IList<T> array,
            int left,
            int right,
            TComparer comparer,
            int depthLimit
        )
            where TComparer : IComparer<T>
        {
            const int insertionThreshold = 16;
            while (right - left > insertionThreshold)
            {
                if (depthLimit == 0)
                {
                    HeapSortRange(array, left, right, comparer);
                    return;
                }

                int pivotIndex = SelectPivotIndex(array, left, right, comparer);
                (int pivotStart, int pivotEnd, bool swapped) = PartitionRange(
                    array,
                    left,
                    right,
                    pivotIndex,
                    comparer
                );

                if (!swapped && IsRangeSorted(array, left, right, comparer))
                {
                    return;
                }

                depthLimit--;

                int leftSize = pivotStart - left;
                int rightSize = right - pivotEnd;

                if (leftSize < rightSize)
                {
                    if (leftSize > 0)
                    {
                        PatternDefeatingQuickSortRange(
                            array,
                            left,
                            pivotStart - 1,
                            comparer,
                            depthLimit
                        );
                    }
                    left = pivotEnd + 1;
                }
                else
                {
                    if (rightSize > 0)
                    {
                        PatternDefeatingQuickSortRange(
                            array,
                            pivotEnd + 1,
                            right,
                            comparer,
                            depthLimit
                        );
                    }
                    right = pivotStart - 1;
                }
            }

            InsertionSortRange(array, left, right, comparer);
        }

        private static int SelectPivotIndex<T, TComparer>(
            IList<T> array,
            int left,
            int right,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            int mid = left + ((right - left) >> 1);
            if (0 < comparer.Compare(array[left], array[mid]))
            {
                array.Swap(left, mid);
            }
            if (0 < comparer.Compare(array[left], array[right]))
            {
                array.Swap(left, right);
            }
            if (0 < comparer.Compare(array[mid], array[right]))
            {
                array.Swap(mid, right);
            }
            return mid;
        }

        private static (int pivotStart, int pivotEnd, bool swapped) PartitionRange<T, TComparer>(
            IList<T> array,
            int left,
            int right,
            int pivotIndex,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            array.Swap(left, pivotIndex);
            T pivot = array[left];
            int i = left + 1;
            int j = right;
            bool swapped = false;

            while (i <= j)
            {
                while (i <= j && comparer.Compare(array[i], pivot) < 0)
                {
                    i++;
                }

                while (i <= j && comparer.Compare(array[j], pivot) > 0)
                {
                    j--;
                }

                if (i > j)
                {
                    break;
                }

                if (i < j)
                {
                    array.Swap(i, j);
                    swapped = true;
                }

                i++;
                j--;
            }

            int pivotPosition = j;
            array.Swap(left, pivotPosition);

            int pivotStart = pivotPosition;
            int pivotEnd = pivotPosition;

            while (pivotStart > left && comparer.Compare(array[pivotStart - 1], pivot) == 0)
            {
                pivotStart--;
            }

            while (pivotEnd < right && comparer.Compare(array[pivotEnd + 1], pivot) == 0)
            {
                pivotEnd++;
            }

            return (pivotStart, pivotEnd, swapped || pivotIndex != pivotPosition);
        }

        private static void GrailSortRange<T, TComparer>(
            IList<T> array,
            T[] buffer,
            int left,
            int right,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            if (left >= right)
            {
                return;
            }

            int mid = left + ((right - left) >> 1);
            GrailSortRange(array, buffer, left, mid, comparer);
            GrailSortRange(array, buffer, mid + 1, right, comparer);

            if (comparer.Compare(array[mid], array[mid + 1]) <= 0)
            {
                return;
            }

            MergeRuns(array, buffer, left, mid - left + 1, mid + 1, right - mid, comparer);
        }

        private static void GlideMergeRuns<T, TComparer>(
            IList<T> array,
            T[] buffer,
            int leftStart,
            int leftLength,
            int rightStart,
            int rightLength,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            if (leftLength == 0 || rightLength == 0)
            {
                return;
            }

            int leftEnd = leftStart + leftLength - 1;
            if (comparer.Compare(array[leftEnd], array[rightStart]) <= 0)
            {
                return;
            }

            for (int i = 0; i < leftLength; ++i)
            {
                buffer[i] = array[leftStart + i];
            }

            int leftIndex = 0;
            int rightIndex = rightStart;
            int dest = leftStart;
            int leftRemaining = leftLength;
            int rightRemaining = rightLength;
            int leftWins = 0;
            int rightWins = 0;
            IList<T> leftBuffer = buffer;

            while (leftRemaining > 0 && rightRemaining > 0)
            {
                if (comparer.Compare(leftBuffer[leftIndex], array[rightIndex]) <= 0)
                {
                    array[dest++] = leftBuffer[leftIndex++];
                    leftRemaining--;
                    leftWins++;
                    rightWins = 0;
                }
                else
                {
                    array[dest++] = array[rightIndex++];
                    rightRemaining--;
                    rightWins++;
                    leftWins = 0;
                }

                if (leftWins >= GlideGallopTrigger && rightRemaining > 0)
                {
                    leftWins = 0;
                    T key = array[rightIndex];
                    int advance = GlideGallopRight(
                        leftBuffer,
                        leftIndex,
                        leftRemaining,
                        key,
                        comparer
                    );
                    if (advance > 0)
                    {
                        for (int k = 0; k < advance; ++k)
                        {
                            array[dest++] = leftBuffer[leftIndex++];
                        }
                        leftRemaining -= advance;
                        if (leftRemaining == 0)
                        {
                            break;
                        }
                    }
                }
                else if (rightWins >= GlideGallopTrigger && leftRemaining > 0)
                {
                    rightWins = 0;
                    T key = leftBuffer[leftIndex];
                    int advance = GlideGallopLeft(array, rightIndex, rightRemaining, key, comparer);
                    if (advance > 0)
                    {
                        for (int k = 0; k < advance; ++k)
                        {
                            array[dest++] = array[rightIndex++];
                        }
                        rightRemaining -= advance;
                        if (rightRemaining == 0)
                        {
                            break;
                        }
                    }
                }
            }

            while (leftRemaining > 0)
            {
                array[dest++] = leftBuffer[leftIndex++];
                leftRemaining--;
            }
        }

        private static void MergeRuns<T, TComparer>(
            IList<T> array,
            T[] buffer,
            int leftStart,
            int leftLength,
            int rightStart,
            int rightLength,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            if (leftLength == 0 || rightLength == 0)
            {
                return;
            }

            int leftEnd = leftStart + leftLength - 1;
            int rightEnd = rightStart + rightLength - 1;
            if (comparer.Compare(array[leftEnd], array[rightStart]) <= 0)
            {
                return;
            }

            if (leftLength <= rightLength)
            {
                for (int i = 0; i < leftLength; ++i)
                {
                    buffer[i] = array[leftStart + i];
                }

                int leftIndex = 0;
                int rightIndex = rightStart;
                int dest = leftStart;
                int leftLimit = leftLength;

                while (leftIndex < leftLimit && rightIndex <= rightEnd)
                {
                    if (0 < comparer.Compare(buffer[leftIndex], array[rightIndex]))
                    {
                        array[dest] = array[rightIndex];
                        rightIndex++;
                    }
                    else
                    {
                        array[dest] = buffer[leftIndex];
                        leftIndex++;
                    }
                    dest++;
                }

                while (leftIndex < leftLimit)
                {
                    array[dest] = buffer[leftIndex];
                    leftIndex++;
                    dest++;
                }
            }
            else
            {
                for (int i = 0; i < rightLength; ++i)
                {
                    buffer[i] = array[rightStart + i];
                }

                int leftIndex = leftEnd;
                int rightIndex = rightLength - 1;
                int dest = rightEnd;

                while (leftIndex >= leftStart && rightIndex >= 0)
                {
                    if (0 < comparer.Compare(array[leftIndex], buffer[rightIndex]))
                    {
                        array[dest] = array[leftIndex];
                        leftIndex--;
                    }
                    else
                    {
                        array[dest] = buffer[rightIndex];
                        rightIndex--;
                    }
                    dest--;
                }

                while (rightIndex >= 0)
                {
                    array[dest] = buffer[rightIndex];
                    rightIndex--;
                    dest--;
                }
            }
        }

        private static int GlideGallopLeft<T, TComparer>(
            IList<T> list,
            int start,
            int length,
            T key,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            int low = 0;
            int high = length;
            while (low < high)
            {
                int mid = low + ((high - low) >> 1);
                if (comparer.Compare(list[start + mid], key) < 0)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid;
                }
            }

            return low;
        }

        private static int GlideGallopRight<T, TComparer>(
            IList<T> list,
            int start,
            int length,
            T key,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            int low = 0;
            int high = length;
            while (low < high)
            {
                int mid = low + ((high - low) >> 1);
                if (comparer.Compare(key, list[start + mid]) < 0)
                {
                    high = mid;
                }
                else
                {
                    low = mid + 1;
                }
            }

            return low;
        }

        private static PowerSortPlusRun BuildPowerSortPlusRuns(
            List<(int start, int length)> runs,
            List<PowerSortPlusRun> storage
        )
        {
            PowerSortPlusRun head = null;
            PowerSortPlusRun previous = null;
            for (int i = 0; i < runs.Count; ++i)
            {
                (int start, int length) run = runs[i];
                PowerSortPlusRun node = new()
                {
                    Start = run.start,
                    Length = run.length,
                    Prev = previous,
                    Next = null,
                    Version = 0,
                };
                storage.Add(node);
                if (previous != null)
                {
                    previous.Next = node;
                }
                else
                {
                    head = node;
                }
                previous = node;
            }

            return head;
        }

        private static PowerSortPlusRun PowerSortPlusMergeNodes<T, TComparer>(
            IList<T> array,
            T[] buffer,
            TComparer comparer,
            PowerSortPlusRun head,
            List<PowerSortPlusCandidate> heap,
            PowerSortPlusRun left,
            PowerSortPlusRun right
        )
            where TComparer : IComparer<T>
        {
            if (left == null || right == null)
            {
                return head ?? left ?? right;
            }

            MergeRuns(array, buffer, left.Start, left.Length, right.Start, right.Length, comparer);

            left.Length += right.Length;
            left.Version++;

            PowerSortPlusRun next = right.Next;
            left.Next = next;
            if (next != null)
            {
                next.Prev = left;
            }

            right.Prev = null;
            right.Next = null;
            right.Version++;

            if (left.Prev != null)
            {
                PowerSortPlusPushCandidate(heap, left.Prev, left);
            }
            if (left.Next != null)
            {
                PowerSortPlusPushCandidate(heap, left, left.Next);
            }

            if (left.Prev == null)
            {
                head = left;
            }

            return head;
        }

        private static void PowerSortPlusPushCandidate(
            List<PowerSortPlusCandidate> heap,
            PowerSortPlusRun left,
            PowerSortPlusRun right
        )
        {
            if (left == null || right == null)
            {
                return;
            }

            PowerSortPlusCandidate candidate = new()
            {
                Left = left,
                Right = right,
                Priority = left.Length + right.Length,
                LeftVersion = left.Version,
                RightVersion = right.Version,
                TieBreaker = left.Start,
            };

            heap.Add(candidate);
            PowerSortPlusSiftUp(heap, heap.Count - 1);
        }

        private static bool PowerSortPlusTryPopValidCandidate(
            List<PowerSortPlusCandidate> heap,
            out PowerSortPlusCandidate candidate
        )
        {
            while (heap.Count > 0)
            {
                PowerSortPlusCandidate top = PowerSortPlusPop(heap);
                if (
                    top.Left != null
                    && top.Right != null
                    && top.Left.Version == top.LeftVersion
                    && top.Right.Version == top.RightVersion
                    && top.Left.Next == top.Right
                    && top.Right.Prev == top.Left
                )
                {
                    candidate = top;
                    return true;
                }
            }

            candidate = default;
            return false;
        }

        private static PowerSortPlusCandidate PowerSortPlusPop(List<PowerSortPlusCandidate> heap)
        {
            int lastIndex = heap.Count - 1;
            PowerSortPlusCandidate result = heap[0];
            heap[0] = heap[lastIndex];
            heap.RemoveAt(lastIndex);
            if (heap.Count > 0)
            {
                PowerSortPlusSiftDown(heap, 0);
            }
            return result;
        }

        private static void PowerSortPlusSiftUp(List<PowerSortPlusCandidate> heap, int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) >> 1;
                if (PowerSortPlusCompare(heap[index], heap[parent]) >= 0)
                {
                    break;
                }

                (heap[index], heap[parent]) = (heap[parent], heap[index]);
                index = parent;
            }
        }

        private static void PowerSortPlusSiftDown(List<PowerSortPlusCandidate> heap, int index)
        {
            int count = heap.Count;
            while (true)
            {
                int left = (index << 1) + 1;
                if (left >= count)
                {
                    break;
                }

                int right = left + 1;
                int smallest = left;
                if (right < count && PowerSortPlusCompare(heap[right], heap[left]) < 0)
                {
                    smallest = right;
                }

                if (PowerSortPlusCompare(heap[index], heap[smallest]) <= 0)
                {
                    break;
                }

                (heap[index], heap[smallest]) = (heap[smallest], heap[index]);
                index = smallest;
            }
        }

        private static int PowerSortPlusCompare(PowerSortPlusCandidate a, PowerSortPlusCandidate b)
        {
            int priorityCompare = a.Priority.CompareTo(b.Priority);
            if (priorityCompare != 0)
            {
                return priorityCompare;
            }

            return a.TieBreaker.CompareTo(b.TieBreaker);
        }

        private sealed class PowerSortPlusRun
        {
            public int Start;
            public int Length;
            public PowerSortPlusRun Prev;
            public PowerSortPlusRun Next;
            public int Version;
        }

        private struct PowerSortPlusCandidate
        {
            public PowerSortPlusRun Left;
            public PowerSortPlusRun Right;
            public int Priority;
            public int LeftVersion;
            public int RightVersion;
            public int TieBreaker;
        }

        private static void CollectNaturalRuns<T, TComparer>(
            IList<T> array,
            TComparer comparer,
            List<(int start, int length)> runs
        )
            where TComparer : IComparer<T>
        {
            runs.Clear();
            int count = array.Count;
            int index = 0;
            while (index < count)
            {
                int start = index;
                index++;
                if (index == count)
                {
                    runs.Add((start, 1));
                    break;
                }

                int compare = comparer.Compare(array[index - 1], array[index]);
                bool ascending = compare <= 0;

                while (index < count)
                {
                    int nextCompare = comparer.Compare(array[index - 1], array[index]);
                    if (ascending)
                    {
                        if (nextCompare <= 0)
                        {
                            index++;
                            continue;
                        }
                    }
                    else
                    {
                        if (nextCompare >= 0)
                        {
                            index++;
                            continue;
                        }
                    }
                    break;
                }

                int end = index - 1;
                if (!ascending && start < end)
                {
                    Reverse(array, start, end);
                }

                runs.Add((start, end - start + 1));
            }
        }

        /// <summary>
        /// Dual-pivot quicksort helper modeled after Vladimir Yaroslavskiy’s Java 7 implementation.
        /// </summary>
        /// <remarks>
        /// Adapted for <c>IList&lt;T&gt;</c> with an insertion sort threshold to avoid excess recursion.
        /// </remarks>
        private static void DualPivotQuickSort<T, TComparer>(
            IList<T> array,
            int left,
            int right,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            const int insertionThreshold = 27;
            if (right - left < insertionThreshold)
            {
                InsertionSortRange(array, left, right, comparer);
                return;
            }

            int third = (right - left) / 3;
            int m1 = left + third;
            int m2 = right - third;
            if (m1 <= left)
            {
                m1 = left + 1;
            }
            if (m2 >= right)
            {
                m2 = right - 1;
            }

            if (comparer.Compare(array[m1], array[m2]) > 0)
            {
                array.Swap(m1, m2);
            }

            array.Swap(left, m1);
            array.Swap(right, m2);

            T pivot1 = array[left];
            T pivot2 = array[right];
            if (comparer.Compare(pivot1, pivot2) > 0)
            {
                (pivot1, pivot2) = (pivot2, pivot1);
                array[left] = pivot1;
                array[right] = pivot2;
            }

            int lt = left + 1;
            int gt = right - 1;
            int i = lt;

            while (i <= gt)
            {
                if (comparer.Compare(array[i], pivot1) < 0)
                {
                    array.Swap(i, lt);
                    lt++;
                }
                else if (comparer.Compare(array[i], pivot2) > 0)
                {
                    while (i < gt && comparer.Compare(array[gt], pivot2) > 0)
                    {
                        gt--;
                    }
                    array.Swap(i, gt);
                    gt--;
                    if (comparer.Compare(array[i], pivot1) < 0)
                    {
                        array.Swap(i, lt);
                        lt++;
                    }
                }
                i++;
            }

            lt--;
            gt++;
            array.Swap(left, lt);
            array.Swap(right, gt);

            DualPivotQuickSort(array, left, lt - 1, comparer);
            DualPivotQuickSort(array, lt + 1, gt - 1, comparer);
            DualPivotQuickSort(array, gt + 1, right, comparer);
        }

        private static void BlockMerge<T, TComparer>(
            IList<T> array,
            T[] buffer,
            int left,
            int mid,
            int right,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            if (comparer.Compare(array[mid], array[mid + 1]) <= 0)
            {
                return;
            }

            for (int index = left; index <= right; ++index)
            {
                buffer[index] = array[index];
            }

            int leftIndex = left;
            int rightIndex = mid + 1;
            int dest = left;

            while (leftIndex <= mid && rightIndex <= right)
            {
                if (comparer.Compare(buffer[leftIndex], buffer[rightIndex]) <= 0)
                {
                    array[dest++] = buffer[leftIndex++];
                }
                else
                {
                    array[dest++] = buffer[rightIndex++];
                }
            }

            while (leftIndex <= mid)
            {
                array[dest++] = buffer[leftIndex++];
            }

            while (rightIndex <= right)
            {
                array[dest++] = buffer[rightIndex++];
            }
        }

        private static void ShearSortRow<T, TComparer>(
            IList<T> array,
            TComparer comparer,
            int row,
            int dimension,
            int count,
            bool ascending
        )
            where TComparer : IComparer<T>
        {
            int start = row * dimension;
            if (start >= count)
            {
                return;
            }

            int end = Math.Min(count - 1, start + dimension - 1);
            if (start == end)
            {
                return;
            }

            for (int i = start + 1; i <= end; ++i)
            {
                T key = array[i];
                int j = i - 1;
                while (j >= start)
                {
                    int compare = comparer.Compare(array[j], key);
                    bool shouldShift = ascending ? compare > 0 : compare < 0;
                    if (!shouldShift)
                    {
                        break;
                    }
                    array[j + 1] = array[j];
                    j--;
                }
                array[j + 1] = key;
            }
        }

        private static void ShearSortColumn<T, TComparer>(
            IList<T> array,
            TComparer comparer,
            int column,
            int dimension,
            int count
        )
            where TComparer : IComparer<T>
        {
            if (column >= dimension || column >= count)
            {
                return;
            }

            for (int index = column + dimension; index < count; index += dimension)
            {
                T key = array[index];
                int jIndex = index - dimension;
                while (jIndex >= column && comparer.Compare(array[jIndex], key) > 0)
                {
                    array[jIndex + dimension] = array[jIndex];
                    jIndex -= dimension;
                }
                array[jIndex + dimension] = key;
            }
        }

        private static int ComputeTimSortMinRun(int length)
        {
            int remainder = 0;
            while (length >= 64)
            {
                remainder |= length & 1;
                length >>= 1;
            }

            return length + remainder;
        }

        private static int MakeAscendingRun<T, TComparer>(
            IList<T> array,
            int start,
            int count,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            if (start >= count - 1)
            {
                return count - start;
            }

            int runEnd = start + 1;
            int compare = comparer.Compare(array[runEnd], array[runEnd - 1]);
            bool ascending = compare >= 0;

            if (ascending)
            {
                while (runEnd < count && comparer.Compare(array[runEnd], array[runEnd - 1]) >= 0)
                {
                    runEnd++;
                }
            }
            else
            {
                while (runEnd < count && comparer.Compare(array[runEnd], array[runEnd - 1]) < 0)
                {
                    runEnd++;
                }
                Reverse(array, start, runEnd - 1);
            }

            return runEnd - start;
        }

        private static void TimSortMergeCollapse<T, TComparer>(
            IList<T> array,
            TComparer comparer,
            T[] buffer,
            List<(int start, int length)> runStack
        )
            where TComparer : IComparer<T>
        {
            while (runStack.Count > 1)
            {
                int size = runStack.Count;
                if (size >= 3)
                {
                    (int startA, int lengthA) = runStack[size - 3];
                    (int startB, int lengthB) = runStack[size - 2];
                    (int startC, int lengthC) = runStack[size - 1];
                    if (lengthA <= lengthB + lengthC || lengthB <= lengthC)
                    {
                        if (lengthA < lengthC)
                        {
                            TimSortMergeAt(array, comparer, buffer, runStack, size - 3);
                        }
                        else
                        {
                            TimSortMergeAt(array, comparer, buffer, runStack, size - 2);
                        }
                        continue;
                    }
                }

                (int prevStart, int prevLength) = runStack[size - 2];
                (int lastStart, int lastLength) = runStack[size - 1];
                if (prevLength <= lastLength)
                {
                    TimSortMergeAt(array, comparer, buffer, runStack, size - 2);
                    continue;
                }

                break;
            }
        }

        private static void TimSortMergeForce<T, TComparer>(
            IList<T> array,
            TComparer comparer,
            T[] buffer,
            List<(int start, int length)> runStack
        )
            where TComparer : IComparer<T>
        {
            while (runStack.Count > 1)
            {
                int size = runStack.Count;
                int index = size - 2;
                if (size >= 3 && runStack[size - 3].length < runStack[size - 1].length)
                {
                    index = size - 3;
                }

                TimSortMergeAt(array, comparer, buffer, runStack, index);
            }
        }

        private static void TimSortMergeAt<T, TComparer>(
            IList<T> array,
            TComparer comparer,
            T[] buffer,
            List<(int start, int length)> runStack,
            int index
        )
            where TComparer : IComparer<T>
        {
            (int leftStart, int leftLength) = runStack[index];
            (int rightStart, int rightLength) = runStack[index + 1];
            MergeRuns(array, buffer, leftStart, leftLength, rightStart, rightLength, comparer);
            runStack[index] = (leftStart, leftLength + rightLength);
            runStack.RemoveAt(index + 1);
        }

        private static void JessePatienceInsert<T, TComparer>(
            List<List<T>> piles,
            T value,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            int left = 0;
            int right = piles.Count;
            while (left < right)
            {
                int mid = (left + right) >> 1;
                List<T> pile = piles[mid];
                T top = pile[pile.Count - 1];
                if (comparer.Compare(value, top) <= 0)
                {
                    right = mid;
                }
                else
                {
                    left = mid + 1;
                }
            }

            if (left == piles.Count)
            {
                List<T> newPile = new();
                newPile.Add(value);
                piles.Add(newPile);
            }
            else
            {
                piles[left].Add(value);
            }
        }

        private static void InitializeJesseHeap<T, TComparer>(
            List<List<T>> piles,
            List<JesseCursor<T>> heap,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            for (int i = 0; i < piles.Count; ++i)
            {
                List<T> pile = piles[i];
                if (pile.Count == 0)
                {
                    continue;
                }

                JesseCursor<T> cursor = new JesseCursor<T>(pile);
                heap.Add(cursor);
                JesseHeapSiftUp(heap, heap.Count - 1, comparer);
            }
        }

        private static void JesseHeapSiftDown<T, TComparer>(
            List<JesseCursor<T>> heap,
            int index,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            int count = heap.Count;
            while (true)
            {
                int leftChild = (index << 1) + 1;
                int rightChild = leftChild + 1;
                int smallest = index;

                if (
                    leftChild < count
                    && comparer.Compare(heap[leftChild].Peek(), heap[smallest].Peek()) < 0
                )
                {
                    smallest = leftChild;
                }

                if (
                    rightChild < count
                    && comparer.Compare(heap[rightChild].Peek(), heap[smallest].Peek()) < 0
                )
                {
                    smallest = rightChild;
                }

                if (smallest == index)
                {
                    return;
                }

                (heap[index], heap[smallest]) = (heap[smallest], heap[index]);
                index = smallest;
            }
        }

        private static void JesseHeapSiftUp<T, TComparer>(
            List<JesseCursor<T>> heap,
            int index,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            while (index > 0)
            {
                int parent = (index - 1) >> 1;
                if (comparer.Compare(heap[index].Peek(), heap[parent].Peek()) >= 0)
                {
                    return;
                }

                (heap[index], heap[parent]) = (heap[parent], heap[index]);
                index = parent;
            }
        }

        private struct JesseCursor<T>
        {
            public JesseCursor(List<T> pile)
            {
                Pile = pile;
                Index = pile.Count - 1;
            }

            public List<T> Pile { get; }

            public int Index { get; private set; }

            public T Peek()
            {
                return Pile[Index];
            }

            public bool MoveNext()
            {
                Index--;
                return Index >= 0;
            }
        }

        private static void GreenSortRange<T, TComparer>(
            IList<T> array,
            T[] buffer,
            int left,
            int right,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            if (left >= right)
            {
                return;
            }

            int length = right - left + 1;
            if (length <= 32)
            {
                InsertionSortRange(array, left, right, comparer);
                return;
            }

            int mid = left + ((right - left) >> 1);
            GreenSortRange(array, buffer, left, mid, comparer);
            GreenSortRange(array, buffer, mid + 1, right, comparer);
            GreenSymmetricMerge(array, buffer, left, mid, right, comparer);
        }

        private static void GreenSymmetricMerge<T, TComparer>(
            IList<T> array,
            T[] buffer,
            int left,
            int mid,
            int right,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            if (comparer.Compare(array[mid], array[mid + 1]) <= 0)
            {
                return;
            }

            int leftTrim = left;
            while (leftTrim <= mid && comparer.Compare(array[leftTrim], array[mid + 1]) <= 0)
            {
                leftTrim++;
            }

            int rightTrim = right;
            while (rightTrim >= mid + 1 && comparer.Compare(array[mid], array[rightTrim]) <= 0)
            {
                rightTrim--;
            }

            if (leftTrim > mid || rightTrim < mid + 1)
            {
                return;
            }

            MergeRuns(
                array,
                buffer,
                leftTrim,
                mid - leftTrim + 1,
                mid + 1,
                rightTrim - mid,
                comparer
            );
        }

        private static void SkaSortRange<T, TComparer>(
            IList<T> array,
            int left,
            int right,
            TComparer comparer,
            int depthLimit
        )
            where TComparer : IComparer<T>
        {
            while (left < right)
            {
                int length = right - left + 1;
                if (length <= 32)
                {
                    InsertionSortRange(array, left, right, comparer);
                    return;
                }

                if (depthLimit == 0)
                {
                    HeapSortRange(array, left, right, comparer);
                    return;
                }

                depthLimit--;
                int pivotIndex = SkaSelectPivot(array, left, right, comparer);
                T pivot = array[pivotIndex];
                (int leftEnd, int rightStart) = SkaPartition(array, left, right, pivot, comparer);

                if ((leftEnd - left) < (right - rightStart))
                {
                    if (left < leftEnd)
                    {
                        SkaSortRange(array, left, leftEnd, comparer, depthLimit);
                    }
                    left = rightStart;
                }
                else
                {
                    if (rightStart < right)
                    {
                        SkaSortRange(array, rightStart, right, comparer, depthLimit);
                    }
                    right = leftEnd;
                }
            }
        }

        private static int SkaSelectPivot<T, TComparer>(
            IList<T> array,
            int left,
            int right,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            int length = right - left + 1;
            if (length < 64)
            {
                return SelectPivotIndex(array, left, right, comparer);
            }

            int step = length / 4;
            int a = left;
            int b = left + step;
            int c = left + (step << 1);
            int d = left + (step * 3);
            int e = right;
            return MedianOfFiveIndices(array, a, b, c, d, e, comparer);
        }

        private static (int leftEnd, int rightStart) SkaPartition<T, TComparer>(
            IList<T> array,
            int left,
            int right,
            T pivot,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            int lt = left;
            int i = left;
            int gt = right;

            while (i <= gt)
            {
                int compare = comparer.Compare(array[i], pivot);
                if (compare < 0)
                {
                    array.Swap(i, lt);
                    lt++;
                    i++;
                }
                else if (compare > 0)
                {
                    array.Swap(i, gt);
                    gt--;
                }
                else
                {
                    i++;
                }
            }

            return (lt - 1, gt + 1);
        }

        private static int MedianOfFiveIndices<T, TComparer>(
            IList<T> array,
            int first,
            int second,
            int third,
            int fourth,
            int fifth,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            int[] indices = { first, second, third, fourth, fifth };
            for (int i = 1; i < indices.Length; ++i)
            {
                int candidate = indices[i];
                T candidateValue = array[candidate];
                int j = i - 1;
                while (j >= 0 && comparer.Compare(array[indices[j]], candidateValue) > 0)
                {
                    indices[j + 1] = indices[j];
                    j--;
                }
                indices[j + 1] = candidate;
            }

            return indices[2];
        }

        private static void IpnSortRange<T, TComparer>(
            IList<T> array,
            int left,
            int right,
            TComparer comparer,
            int depthLimit
        )
            where TComparer : IComparer<T>
        {
            const int insertionThreshold = 32;
            while (right - left > insertionThreshold)
            {
                if (depthLimit == 0)
                {
                    HeapSortRange(array, left, right, comparer);
                    return;
                }

                int pivotIndex = IpnSelectPivotIndex(array, left, right, comparer);
                (int pivotStart, int pivotEnd, bool swapped) = PartitionRange(
                    array,
                    left,
                    right,
                    pivotIndex,
                    comparer
                );

                depthLimit--;
                if (!swapped && IsRangeSorted(array, left, right, comparer))
                {
                    return;
                }

                if (pivotStart - left < right - pivotEnd)
                {
                    if (left < pivotStart)
                    {
                        IpnSortRange(array, left, pivotStart - 1, comparer, depthLimit);
                    }
                    left = pivotEnd + 1;
                }
                else
                {
                    if (pivotEnd < right)
                    {
                        IpnSortRange(array, pivotEnd + 1, right, comparer, depthLimit);
                    }
                    right = pivotStart - 1;
                }
            }

            InsertionSortRange(array, left, right, comparer);
        }

        private static int IpnSelectPivotIndex<T, TComparer>(
            IList<T> array,
            int left,
            int right,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            int length = right - left + 1;
            if (length < 128)
            {
                return SelectPivotIndex(array, left, right, comparer);
            }

            int step = length / 4;
            int a = left;
            int b = left + step;
            int c = left + (step << 1);
            int d = left + (step * 3);
            int e = right;
            return MedianOfFiveIndices(array, a, b, c, d, e, comparer);
        }

        private static void InsertionSortRange<T, TComparer>(
            IList<T> array,
            int left,
            int right,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            if (left >= right)
            {
                return;
            }

            for (int i = left + 1; i <= right; ++i)
            {
                T key = array[i];
                int j = i - 1;
                while (j >= left && 0 < comparer.Compare(array[j], key))
                {
                    array[j + 1] = array[j];
                    j--;
                }
                array[j + 1] = key;
            }
        }

        private static void HeapSortRange<T, TComparer>(
            IList<T> array,
            int start,
            int end,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            int length = end - start + 1;
            if (length <= 1)
            {
                return;
            }

            for (int i = (length >> 1) - 1; i >= 0; --i)
            {
                SiftDown(array, start, length, i, comparer);
            }

            for (int i = length - 1; i > 0; --i)
            {
                array.Swap(start, start + i);
                SiftDown(array, start, i, 0, comparer);
            }
        }

        private static void SiftDown<T, TComparer>(
            IList<T> array,
            int start,
            int length,
            int root,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            while (true)
            {
                int child = (root << 1) + 1;
                if (child >= length)
                {
                    return;
                }

                int rightChild = child + 1;
                if (
                    rightChild < length
                    && comparer.Compare(array[start + child], array[start + rightChild]) < 0
                )
                {
                    child = rightChild;
                }

                if (comparer.Compare(array[start + root], array[start + child]) >= 0)
                {
                    return;
                }

                array.Swap(start + root, start + child);
                root = child;
            }
        }

        private static bool IsRangeSorted<T, TComparer>(
            IList<T> array,
            int left,
            int right,
            TComparer comparer
        )
            where TComparer : IComparer<T>
        {
            for (int i = left + 1; i <= right; ++i)
            {
                if (0 < comparer.Compare(array[i - 1], array[i]))
                {
                    return false;
                }
            }
            return true;
        }

        private static int FloorLog2(int value)
        {
            int result = 0;
            while (value > 1)
            {
                value >>= 1;
                result++;
            }
            return result;
        }

        /// <summary>
        /// Sorts a list of Unity Objects by their name property in ascending alphabetical order.
        /// </summary>
        /// <typeparam name="T">The type of Unity Object.</typeparam>
        /// <param name="inputList">The list to sort.</param>
        /// <remarks>
        /// <para>Null handling: Throws NullReferenceException if inputList is null.</para>
        /// <para>Thread safety: Not thread-safe. Modifies the list in place. Requires Unity main thread for Object.name access.</para>
        /// <para>Performance: O(n log n) - delegates to Array.Sort or List.Sort when possible for optimized performance.</para>
        /// <para>Allocations: Minimal - uses cached UnityObjectNameComparer.Instance.</para>
        /// <para>Edge cases: Empty or single element lists require no sorting. Null objects may cause exceptions depending on comparer.</para>
        /// </remarks>
        public static void SortByName<T>(this IList<T> inputList)
            where T : UnityEngine.Object
        {
            switch (inputList)
            {
                case T[] array:
                {
                    Array.Sort(array, UnityObjectNameComparer<T>.Instance);
                    return;
                }
                case List<T> list:
                {
                    list.Sort(UnityObjectNameComparer<T>.Instance);
                    return;
                }
                default:
                {
                    inputList.Sort(UnityObjectNameComparer<T>.Instance);
                    break;
                }
            }
        }

        /// <summary>
        /// Determines whether the list is sorted in ascending order according to the specified comparer.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to check.</param>
        /// <param name="comparer">The comparer to use. If null, uses Comparer&lt;T&gt;.Default.</param>
        /// <returns>True if the list is sorted in ascending order, false otherwise.</returns>
        /// <remarks>
        /// <para>Null handling: Throws NullReferenceException if list is null. Comparer defaults to Comparer&lt;T&gt;.Default if null.</para>
        /// <para>Thread safety: Thread-safe for read-only access. Not thread-safe if list is modified during execution. No Unity main thread requirement.</para>
        /// <para>Performance: O(n) where n is the number of elements. Short-circuits on first unsorted pair.</para>
        /// <para>Allocations: No allocations if comparer is provided, otherwise allocates default comparer.</para>
        /// <para>Edge cases: Empty lists and single element lists are considered sorted.</para>
        /// </remarks>
        [Pure]
        public static bool IsSorted<T>(this IList<T> list, IComparer<T> comparer = null)
        {
            if (list.Count <= 1)
            {
                return true;
            }

            comparer ??= Comparer<T>.Default;

            T previous = list[0];
            for (int i = 1; i < list.Count; ++i)
            {
                T current = list[i];
                if (comparer.Compare(previous, current) > 0)
                {
                    return false;
                }

                previous = current;
            }

            return true;
        }

        /// <summary>
        /// Swaps two elements in the list at the specified indices.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list containing the elements to swap.</param>
        /// <param name="indexA">The index of the first element.</param>
        /// <param name="indexB">The index of the second element.</param>
        /// <remarks>
        /// <para>Null handling: Throws NullReferenceException if list is null.</para>
        /// <para>Thread safety: Not thread-safe. Modifies the list in place. No Unity main thread requirement.</para>
        /// <para>Performance: O(1).</para>
        /// <para>Allocations: No allocations.</para>
        /// <para>Edge cases: If indexA equals indexB, no swap occurs.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when indexA or indexB are outside the valid range [0, Count).</exception>
        public static void Swap<T>(this IList<T> list, int indexA, int indexB)
        {
            if (indexA < 0 || indexA >= list.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(indexA));
            }
            if (indexB < 0 || indexB >= list.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(indexB));
            }

            if (indexA == indexB)
            {
                return;
            }

            (list[indexA], list[indexB]) = (list[indexB], list[indexA]);
        }

        /// <summary>
        /// Fills all elements in the list with the specified value.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to fill.</param>
        /// <param name="value">The value to assign to all elements.</param>
        /// <remarks>
        /// <para>Null handling: Throws NullReferenceException if list is null. Value can be null if T is nullable.</para>
        /// <para>Thread safety: Not thread-safe. Modifies the list in place. No Unity main thread requirement.</para>
        /// <para>Performance: O(n) where n is the number of elements.</para>
        /// <para>Allocations: No allocations.</para>
        /// <para>Edge cases: Empty lists are not modified.</para>
        /// </remarks>
        public static void Fill<T>(this IList<T> list, T value)
        {
            for (int i = 0; i < list.Count; ++i)
            {
                list[i] = value;
            }
        }

        /// <summary>
        /// Fills all elements in the list using a factory function that receives the element index.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to fill.</param>
        /// <param name="factory">A function that takes an index and returns the value for that position.</param>
        /// <remarks>
        /// <para>Null handling: Throws ArgumentNullException if factory is null. Throws NullReferenceException if list is null.</para>
        /// <para>Thread safety: Not thread-safe. Modifies the list in place. No Unity main thread requirement.</para>
        /// <para>Performance: O(n) where n is the number of elements, plus the cost of factory invocations.</para>
        /// <para>Allocations: Allocations depend on factory function behavior.</para>
        /// <para>Edge cases: Empty lists result in no factory invocations.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when factory is null.</exception>
        public static void Fill<T>(this IList<T> list, Func<int, T> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            for (int i = 0; i < list.Count; ++i)
            {
                list[i] = factory(i);
            }
        }

        /// <summary>
        /// Finds the index of the first element that matches the specified predicate.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to search.</param>
        /// <param name="predicate">A function to test each element.</param>
        /// <returns>The index of the first matching element, or -1 if no match is found.</returns>
        /// <remarks>
        /// <para>Null handling: Throws ArgumentNullException if predicate is null. Throws NullReferenceException if list is null.</para>
        /// <para>Thread safety: Thread-safe for read-only access. Not thread-safe if list is modified during execution. No Unity main thread requirement.</para>
        /// <para>Performance: O(n) where n is the number of elements. Short-circuits on first match.</para>
        /// <para>Allocations: No allocations.</para>
        /// <para>Edge cases: Returns -1 if no matching element is found. Empty lists always return -1.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when predicate is null.</exception>
        public static int IndexOf<T>(this IList<T> list, Func<T, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            for (int i = 0; i < list.Count; ++i)
            {
                if (predicate(list[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Finds the index of the last element that matches the specified predicate.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to search.</param>
        /// <param name="predicate">A function to test each element.</param>
        /// <returns>The index of the last matching element, or -1 if no match is found.</returns>
        /// <remarks>
        /// <para>Null handling: Throws ArgumentNullException if predicate is null. Throws NullReferenceException if list is null.</para>
        /// <para>Thread safety: Thread-safe for read-only access. Not thread-safe if list is modified during execution. No Unity main thread requirement.</para>
        /// <para>Performance: O(n) where n is the number of elements. Searches from end to beginning, short-circuits on first match.</para>
        /// <para>Allocations: No allocations.</para>
        /// <para>Edge cases: Returns -1 if no matching element is found. Empty lists always return -1.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when predicate is null.</exception>
        public static int LastIndexOf<T>(this IList<T> list, Func<T, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            for (int i = list.Count - 1; i >= 0; --i)
            {
                if (predicate(list[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns all elements in the list that match the specified predicate.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to search.</param>
        /// <param name="predicate">A function to test each element.</param>
        /// <returns>A new List containing all matching elements.</returns>
        /// <remarks>
        /// <para>Null handling: Throws ArgumentNullException if predicate is null. Throws NullReferenceException if list is null.</para>
        /// <para>Thread safety: Thread-safe for read-only access. Not thread-safe if list is modified during execution. No Unity main thread requirement.</para>
        /// <para>Performance: O(n) where n is the number of elements.</para>
        /// <para>Allocations: Allocates a new List. Size depends on number of matching elements.</para>
        /// <para>Edge cases: Returns empty list if no matches found.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when predicate is null.</exception>
        public static List<T> FindAll<T>(this IList<T> list, Func<T, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            List<T> result = new();
            for (int i = 0; i < list.Count; ++i)
            {
                if (predicate(list[i]))
                {
                    result.Add(list[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// Adds the elements of the specified collection to the end of the list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to add items to.</param>
        /// <param name="items">The collection whose elements should be added.</param>
        /// <remarks>
        /// <para>Null handling: Throws ArgumentNullException if items is null. Throws NullReferenceException if list is null.</para>
        /// <para>Thread safety: Not thread-safe. Modifies the list in place. No Unity main thread requirement.</para>
        /// <para>Performance: O(m) where m is the number of items to add. Optimized for List&lt;T&gt; using AddRange.</para>
        /// <para>Allocations: May allocate if list needs to grow capacity.</para>
        /// <para>Edge cases: Empty items collection adds nothing to the list.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when items is null.</exception>
        public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (list is List<T> concreteList)
            {
                concreteList.AddRange(items);
                return;
            }

            foreach (T item in items)
            {
                list.Add(item);
            }
        }

        /// <summary>
        /// Rotates the list elements to the left by the specified number of positions.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to rotate.</param>
        /// <param name="positions">The number of positions to rotate left. Defaults to 1.</param>
        /// <remarks>
        /// <para>Null handling: If list is null, returns immediately (delegated to Shift).</para>
        /// <para>Thread safety: Not thread-safe. Modifies the list in place. No Unity main thread requirement.</para>
        /// <para>Performance: O(n) where n is the number of elements. Delegates to Shift.</para>
        /// <para>Allocations: No allocations.</para>
        /// <para>Edge cases: See Shift method for edge cases.</para>
        /// </remarks>
        public static void RotateLeft<T>(this IList<T> list, int positions = 1)
        {
            Shift(list, -positions);
        }

        /// <summary>
        /// Rotates the list elements to the right by the specified number of positions.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to rotate.</param>
        /// <param name="positions">The number of positions to rotate right. Defaults to 1.</param>
        /// <remarks>
        /// <para>Null handling: If list is null, returns immediately (delegated to Shift).</para>
        /// <para>Thread safety: Not thread-safe. Modifies the list in place. No Unity main thread requirement.</para>
        /// <para>Performance: O(n) where n is the number of elements. Delegates to Shift.</para>
        /// <para>Allocations: No allocations.</para>
        /// <para>Edge cases: See Shift method for edge cases.</para>
        /// </remarks>
        public static void RotateRight<T>(this IList<T> list, int positions = 1)
        {
            Shift(list, positions);
        }

        /// <summary>
        /// Partitions the list into two lists based on a predicate: elements that match and elements that don't.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to partition.</param>
        /// <param name="predicate">The function to test each element.</param>
        /// <returns>A tuple containing two lists: matching elements and non-matching elements.</returns>
        /// <remarks>
        /// <para>Null handling: Throws ArgumentNullException if predicate is null. Throws NullReferenceException if list is null.</para>
        /// <para>Thread safety: Thread-safe for read-only access. Not thread-safe if list is modified during execution. No Unity main thread requirement.</para>
        /// <para>Performance: O(n) where n is the number of elements.</para>
        /// <para>Allocations: Allocates two new Lists. Total size equals original list size.</para>
        /// <para>Edge cases: One of the returned lists may be empty if all elements match or none match.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when predicate is null.</exception>
        public static (List<T> matching, List<T> notMatching) Partition<T>(
            this IList<T> list,
            Func<T, bool> predicate
        )
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            List<T> matching = new();
            List<T> notMatching = new();

            for (int i = 0; i < list.Count; ++i)
            {
                if (predicate(list[i]))
                {
                    matching.Add(list[i]);
                }
                else
                {
                    notMatching.Add(list[i]);
                }
            }

            return (matching, notMatching);
        }

        /// <summary>
        /// Removes and returns the last element of the list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to pop from.</param>
        /// <returns>The last element of the list.</returns>
        /// <remarks>
        /// <para>Null handling: Throws NullReferenceException if list is null.</para>
        /// <para>Thread safety: Not thread-safe. Modifies the list in place. No Unity main thread requirement.</para>
        /// <para>Performance: O(1) for most list implementations.</para>
        /// <para>Allocations: No allocations beyond what RemoveAt might allocate.</para>
        /// <para>Edge cases: Throws InvalidOperationException if list is empty.</para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown when attempting to pop from an empty list.</exception>
        public static T PopBack<T>(this IList<T> list)
        {
            if (list.Count == 0)
            {
                throw new InvalidOperationException("Cannot pop from empty list");
            }

            int lastIndex = list.Count - 1;
            T item = list[lastIndex];
            list.RemoveAt(lastIndex);
            return item;
        }

        /// <summary>
        /// Removes and returns the first element of the list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to pop from.</param>
        /// <returns>The first element of the list.</returns>
        /// <remarks>
        /// <para>Null handling: Throws NullReferenceException if list is null.</para>
        /// <para>Thread safety: Not thread-safe. Modifies the list in place. No Unity main thread requirement.</para>
        /// <para>Performance: O(n) for most list implementations due to element shifting.</para>
        /// <para>Allocations: No allocations beyond what RemoveAt might allocate.</para>
        /// <para>Edge cases: Throws InvalidOperationException if list is empty. Expensive for large lists due to element shifting.</para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown when attempting to pop from an empty list.</exception>
        public static T PopFront<T>(this IList<T> list)
        {
            if (list.Count == 0)
            {
                throw new InvalidOperationException("Cannot pop from empty list");
            }

            T item = list[0];
            list.RemoveAt(0);
            return item;
        }

        /// <summary>
        /// Returns a random element from the list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to get a random element from.</param>
        /// <param name="random">The random number generator to use. If null, uses PRNG.Instance.</param>
        /// <returns>A randomly selected element from the list.</returns>
        /// <remarks>
        /// <para>Null handling: Throws NullReferenceException if list is null. If random is null, uses PRNG.Instance.</para>
        /// <para>Thread safety: Thread-safe for read-only access. If random is shared, may not be thread-safe. No Unity main thread requirement.</para>
        /// <para>Performance: O(1).</para>
        /// <para>Allocations: No allocations.</para>
        /// <para>Edge cases: Throws InvalidOperationException if list is empty.</para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown when attempting to get random element from an empty list.</exception>
        [Pure]
        public static T GetRandomElement<T>(this IList<T> list, IRandom random = null)
        {
            if (list.Count == 0)
            {
                throw new InvalidOperationException("Cannot get random element from empty list");
            }

            random ??= PRNG.Instance;
            return list[random.Next(0, list.Count)];
        }
    }
}
