namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Random;

    public static class IEnumerableExtensions
    {
        private static readonly ConcurrentDictionary<object, object> ComparerCache = new();

        public static LinkedList<T> ToLinkedList<T>(this IEnumerable<T> source)
        {
            return new LinkedList<T>(source);
        }

        public static IList<T> AsList<T>(this IEnumerable<T> enumeration)
        {
            if (enumeration is IList<T> list)
            {
                return list;
            }

            return enumeration.ToList();
        }

        public static IEnumerable<T> OrderBy<T>(
            this IEnumerable<T> enumeration,
            Func<T, T, int> comparer
        )
        {
            FuncBasedComparer<T> comparerObject =
                (FuncBasedComparer<T>)
                    ComparerCache.GetOrAdd(comparer, () => new FuncBasedComparer<T>(comparer));
            return enumeration.OrderBy(x => x, comparerObject);
        }

        public static IEnumerable<T> Ordered<T>(this IEnumerable<T> enumerable)
            where T : IComparable
        {
            return enumerable.OrderBy(x => x);
        }

        public static IEnumerable<T> Shuffled<T>(
            this IEnumerable<T> enumerable,
            IRandom random = null
        )
        {
            random ??= ThreadLocalRandom<PcgRandom>.Instance;
            return enumerable.OrderBy(_ => random.Next());
        }

        public static IEnumerable<T> Infinite<T>(this IEnumerable<T> enumerable)
        {
            ICollection<T> collection = enumerable as ICollection<T> ?? enumerable.ToList();
            if (collection.Count == 0)
            {
                yield break;
            }

            while (true)
            {
                foreach (T element in collection)
                {
                    yield return element;
                }
            }
        }

        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (T item in enumerable)
            {
                action(item);
            }
        }

        public static IEnumerable<IEnumerable<T>> Partition<T>(this IEnumerable<T> items, int size)
        {
            using IEnumerator<T> enumerator = items.GetEnumerator();
            bool hasNext = enumerator.MoveNext();

            while (hasNext)
            {
                yield return NextPartitionOf().ToList();
            }

            yield break;

            IEnumerable<T> NextPartitionOf()
            {
                int remainingCountForPartition = size;
                while (remainingCountForPartition-- > 0 && hasNext)
                {
                    yield return enumerator.Current;
                    hasNext = enumerator.MoveNext();
                }
            }
        }

        public static List<T> ToList<T>(this IEnumerable<T> enumerable, int count)
        {
            List<T> list = new(count);
            list.AddRange(enumerable);
            return list;
        }

        private class FuncBasedComparer<T> : IComparer<T>
        {
            private readonly Func<T, T, int> _comparer;

            public FuncBasedComparer(Func<T, T, int> comparer)
            {
                _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
            }

            public int Compare(T x, T y)
            {
                return _comparer(x, y);
            }
        }
    }
}
