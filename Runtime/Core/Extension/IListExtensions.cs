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
        /// <param name="sortAlgorithm">The sorting algorithm to use (Ghost or Insertion). Defaults to Ghost.</param>
        /// <remarks>
        /// <para>Null handling: Throws NullReferenceException if array is null. Comparer behavior depends on implementation.</para>
        /// <para>Thread safety: Not thread-safe. Modifies the list in place. No Unity main thread requirement.</para>
        /// <para>Performance: Ghost sort is O(n log n) average case. Insertion sort is O(n^2).</para>
        /// <para>Allocations: No allocations.</para>
        /// <para>Edge cases: Empty or single element lists require no sorting. Ghost sort is currently not stable.</para>
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
            for (int i = 1; i < arrayCount; ++i)
            {
                T key = array[i];
                int j = i - 1;
                while (0 <= j && 0 < comparer.Compare(array[j], key))
                {
                    array[j + 1] = array[j];
                    j--;
                }
                array[j + 1] = key;
            }
        }

        /*
            Implementation copyright Will Stafford Parsons,
            https://github.com/wstaffordp/ghostsort/blob/master/src/ghostsort.c

            Note: Ghost Sort is currently not stable.

            Please contact the original author if you would like an explanation of constants.
         */
        /// <summary>
        /// Sorts the elements in the list using the Ghost Sort algorithm, a hybrid gap-based sorting algorithm.
        /// </summary>
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
