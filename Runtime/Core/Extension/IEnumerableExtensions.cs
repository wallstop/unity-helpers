namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Random;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Utils;
#if !SINGLE_THREADED
    using System.Collections.Concurrent;
#endif

    public static class IEnumerableExtensions
    {
#if SINGLE_THREADED
        private static readonly Dictionary<object, object> ComparerCache = new();
#else
        private static readonly ConcurrentDictionary<object, object> ComparerCache = new();
#endif

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
            if (ComparerCache.TryGetValue(comparer, out object cachedComparer))
            {
                return enumeration.OrderBy(x => x, (FuncBasedComparer<T>)cachedComparer);
            }

            FuncBasedComparer<T> typedComparer = new(comparer);
            _ = ComparerCache.TryAdd(comparer, typedComparer);
            return enumeration.OrderBy(x => x, typedComparer);
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
            random ??= PRNG.Instance;
            return enumerable.OrderBy(x => x, new RandomComparer<T>(random));
        }

        public static IEnumerable<T> Infinite<T>(this IEnumerable<T> enumerable)
        {
            ICollection<T> collection = enumerable as ICollection<T> ?? enumerable.ToArray();
            if (collection.Count == 0)
            {
                yield break;
            }

            // Use index-based iteration for arrays and lists to avoid enumerator allocation
            if (collection is IReadOnlyList<T> readonlyList)
            {
                while (true)
                {
                    for (int i = 0; i < readonlyList.Count; ++i)
                    {
                        yield return readonlyList[i];
                    }
                }
            }

            if (collection is IList<T> list)
            {
                while (true)
                {
                    for (int i = 0; i < list.Count; ++i)
                    {
                        yield return list[i];
                    }
                }
            }

            if (collection is HashSet<T> hashSet)
            {
                while (true)
                {
                    foreach (T element in hashSet)
                    {
                        yield return element;
                    }
                }
            }

            if (collection is SortedSet<T> sortedSet)
            {
                while (true)
                {
                    foreach (T element in sortedSet)
                    {
                        yield return element;
                    }
                }
            }

            if (collection is LinkedList<T> linkedList)
            {
                while (true)
                {
                    foreach (T element in linkedList)
                    {
                        yield return element;
                    }
                }
            }

            // Fallback for other collection types
            while (true)
            {
                foreach (T element in collection)
                {
                    yield return element;
                }
            }
        }

        public static IEnumerable<List<T>> Partition<T>(this IEnumerable<T> items, int size)
        {
            using IEnumerator<T> enumerator = items.GetEnumerator();
            using PooledResource<List<T>> listBuffer = Buffers<T>.List.Get();
            List<T> partition = listBuffer.resource;

            while (enumerator.MoveNext())
            {
                int count = 0;
                do
                {
                    partition.Add(enumerator.Current);
                } while (++count < size && enumerator.MoveNext());

                yield return partition;
                partition.Clear();
            }
        }
    }
}
