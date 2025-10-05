namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Generic;

    public static class IReadonlyListExtensions
    {
        public static int IndexOf<T>(this IReadOnlyList<T> readonlyList, T element)
        {
            if (readonlyList == null)
            {
                throw new ArgumentNullException(nameof(readonlyList));
            }

            return IndexOf(readonlyList, element, 0, readonlyList.Count, null);
        }

        public static int IndexOf<T>(this IReadOnlyList<T> readonlyList, T element, int startIndex)
        {
            if (readonlyList == null)
            {
                throw new ArgumentNullException(nameof(readonlyList));
            }

            int length = readonlyList.Count;
            return IndexOf(readonlyList, element, startIndex, length - startIndex, null);
        }

        public static int IndexOf<T>(
            this IReadOnlyList<T> readonlyList,
            T element,
            int startIndex,
            int count
        )
        {
            return IndexOf(readonlyList, element, startIndex, count, null);
        }

        public static int IndexOf<T>(
            this IReadOnlyList<T> readonlyList,
            T element,
            int startIndex,
            int count,
            IEqualityComparer<T> comparer
        )
        {
            if (readonlyList == null)
            {
                throw new ArgumentNullException(nameof(readonlyList));
            }

            int length = readonlyList.Count;
            ValidateForwardSegment(length, startIndex, count);

            IEqualityComparer<T> defaultComparer = EqualityComparer<T>.Default;
            comparer ??= defaultComparer;
            bool useDefaultComparer = ReferenceEquals(comparer, defaultComparer);

            switch (readonlyList)
            {
                case T[] array when useDefaultComparer:
                {
                    return Array.IndexOf(array, element, startIndex, count);
                }

                case List<T> list when useDefaultComparer:
                {
                    return list.IndexOf(element, startIndex, count);
                }
            }

            int endExclusive = startIndex + count;
            for (int i = startIndex; i < endExclusive; ++i)
            {
                if (comparer.Equals(readonlyList[i], element))
                {
                    return i;
                }
            }

            return -1;
        }

        public static int LastIndexOf<T>(this IReadOnlyList<T> readonlyList, T element)
        {
            if (readonlyList == null)
            {
                throw new ArgumentNullException(nameof(readonlyList));
            }

            int length = readonlyList.Count;
            if (length == 0)
            {
                return -1;
            }

            return LastIndexOf(readonlyList, element, length - 1, length, null);
        }

        public static int LastIndexOf<T>(
            this IReadOnlyList<T> readonlyList,
            T element,
            int startIndex
        )
        {
            if (readonlyList == null)
            {
                throw new ArgumentNullException(nameof(readonlyList));
            }

            return LastIndexOf(readonlyList, element, startIndex, startIndex + 1, null);
        }

        public static int LastIndexOf<T>(
            this IReadOnlyList<T> readonlyList,
            T element,
            int startIndex,
            int count
        )
        {
            return LastIndexOf(readonlyList, element, startIndex, count, null);
        }

        public static int LastIndexOf<T>(
            this IReadOnlyList<T> readonlyList,
            T element,
            int startIndex,
            int count,
            IEqualityComparer<T> comparer
        )
        {
            if (readonlyList == null)
            {
                throw new ArgumentNullException(nameof(readonlyList));
            }

            int length = readonlyList.Count;
            ValidateBackwardSegment(length, startIndex, count);
            if (count == 0)
            {
                return -1;
            }

            IEqualityComparer<T> defaultComparer = EqualityComparer<T>.Default;
            comparer ??= defaultComparer;
            bool useDefaultComparer = ReferenceEquals(comparer, defaultComparer);

            switch (readonlyList)
            {
                case T[] array when useDefaultComparer:
                {
                    return Array.LastIndexOf(array, element, startIndex, count);
                }

                case List<T> list when useDefaultComparer:
                {
                    return list.LastIndexOf(element, startIndex, count);
                }
            }

            int segmentStart = startIndex - count + 1;
            for (int i = startIndex; i >= segmentStart; --i)
            {
                if (comparer.Equals(readonlyList[i], element))
                {
                    return i;
                }
            }

            return -1;
        }

        public static bool Contains<T>(this IReadOnlyList<T> readonlyList, T element)
        {
            return Contains(readonlyList, element, null);
        }

        public static bool Contains<T>(
            this IReadOnlyList<T> readonlyList,
            T element,
            IEqualityComparer<T> comparer
        )
        {
            if (readonlyList == null)
            {
                throw new ArgumentNullException(nameof(readonlyList));
            }

            return IndexOf(readonlyList, element, 0, readonlyList.Count, comparer) >= 0;
        }

        public static bool TryGetElementAt<T>(
            this IReadOnlyList<T> readonlyList,
            int index,
            out T value
        )
        {
            if (readonlyList == null)
            {
                throw new ArgumentNullException(nameof(readonlyList));
            }

            if ((uint)index >= (uint)readonlyList.Count)
            {
                value = default;
                return false;
            }

            value = readonlyList[index];
            return true;
        }

        public static bool TryGetFirst<T>(this IReadOnlyList<T> readonlyList, out T value)
        {
            if (readonlyList == null)
            {
                throw new ArgumentNullException(nameof(readonlyList));
            }

            if (readonlyList.Count == 0)
            {
                value = default;
                return false;
            }

            value = readonlyList[0];
            return true;
        }

        public static bool TryGetLast<T>(this IReadOnlyList<T> readonlyList, out T value)
        {
            if (readonlyList == null)
            {
                throw new ArgumentNullException(nameof(readonlyList));
            }

            int length = readonlyList.Count;
            if (length == 0)
            {
                value = default;
                return false;
            }

            value = readonlyList[length - 1];
            return true;
        }

        public static bool IsNullOrEmpty<T>(this IReadOnlyList<T> readonlyList)
        {
            return readonlyList == null || readonlyList.Count == 0;
        }

        public static int BinarySearch<T>(this IReadOnlyList<T> readonlyList, T value)
        {
            if (readonlyList == null)
            {
                throw new ArgumentNullException(nameof(readonlyList));
            }

            return BinarySearch(readonlyList, 0, readonlyList.Count, value, null);
        }

        public static int BinarySearch<T>(
            this IReadOnlyList<T> readonlyList,
            T value,
            IComparer<T> comparer
        )
        {
            if (readonlyList == null)
            {
                throw new ArgumentNullException(nameof(readonlyList));
            }

            return BinarySearch(readonlyList, 0, readonlyList.Count, value, comparer);
        }

        public static int BinarySearch<T>(
            this IReadOnlyList<T> readonlyList,
            int index,
            int count,
            T value
        )
        {
            return BinarySearch(readonlyList, index, count, value, null);
        }

        public static int BinarySearch<T>(
            this IReadOnlyList<T> readonlyList,
            int index,
            int count,
            T value,
            IComparer<T> comparer
        )
        {
            if (readonlyList == null)
            {
                throw new ArgumentNullException(nameof(readonlyList));
            }

            int length = readonlyList.Count;
            ValidateForwardSegment(length, index, count);

            comparer ??= Comparer<T>.Default;

            switch (readonlyList)
            {
                case T[] array:
                {
                    return Array.BinarySearch(array, index, count, value, comparer);
                }

                case List<T> list:
                {
                    return list.BinarySearch(index, count, value, comparer);
                }
            }

            int low = index;
            int high = index + count - 1;

            while (low <= high)
            {
                int mid = low + ((high - low) >> 1);
                int comparison = comparer.Compare(readonlyList[mid], value);

                if (comparison == 0)
                {
                    return mid;
                }

                if (comparison < 0)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return ~low;
        }

        private static void ValidateForwardSegment(int length, int startIndex, int count)
        {
            if ((uint)startIndex > (uint)length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }

            if (count < 0 || startIndex > length - count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
        }

        private static void ValidateBackwardSegment(int length, int startIndex, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (length == 0)
            {
                if (startIndex != -1 || count != 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(startIndex));
                }

                return;
            }

            if ((uint)startIndex >= (uint)length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }

            if (count > startIndex + 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
        }
    }
}
