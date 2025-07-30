namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using Helper;
    using Random;
    using Utils;

    public enum SortAlgorithm
    {
        Ghost = 0,
        Insertion = 1,
    }

    public static class IListExtensions
    {
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
            }

            return true;
        }
    }
}
