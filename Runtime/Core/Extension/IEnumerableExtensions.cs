namespace Core.Extension
{
    using Random;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    public static class IEnumerableExtensions
    {
        private static readonly ConcurrentDictionary<object, object> ComparerCache = new();

        public static IList<T> AsList<T>(this IEnumerable<T> enumeration)
        {
            if (enumeration is IList<T> list)
            {
                return list;
            }

            return enumeration.ToList();
        }

        public static IEnumerable<T> OrderBy<T>(this IEnumerable<T> enumeration, Func<T, T, int> comparer)
        {
            FuncBasedComparer<T> comparerObject = (FuncBasedComparer<T>) ComparerCache.GetOrAdd(comparer, () => new FuncBasedComparer<T>(comparer));
            return enumeration.OrderBy(_ => _, comparerObject);
        }

        public static IEnumerable<T> Ordered<T>(this IEnumerable<T> enumerable) where T : IComparable
        {
            return enumerable.OrderBy(_ => _);
        }

        public static IEnumerable<T> Shuffled<T>(this IEnumerable<T> enumerable, IRandom random = null)
        {
            random = random ?? ThreadLocalRandom<PcgRandom>.Instance;
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

        public static IEnumerable<IEnumerable<T>> Partition<T>(this IEnumerable<T> enumerable, int partitionSize)
        {
            return enumerable.Select((item, index) => new {item, index})
                .GroupBy(item => item.index / partitionSize)
                .Select(group => group.Select(item => item.item));
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

            public int Compare(T lhs, T rhs)
            {
                return _comparer(lhs, rhs);
            }
        }
    }
}
