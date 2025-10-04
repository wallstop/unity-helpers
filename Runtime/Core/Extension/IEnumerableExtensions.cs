namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Random;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Utils;

    public static class IEnumerableExtensions
    {
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
            FuncBasedComparer<T> typedComparer = new(comparer);
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
            switch (enumerable)
            {
                case IReadOnlyList<T> { Count: 0 }:
                {
                    yield break;
                }
                case IReadOnlyList<T> readonlyList:
                {
                    while (true)
                    {
                        for (int i = 0; i < readonlyList.Count; ++i)
                        {
                            yield return readonlyList[i];
                        }
                    }
                }
                case IList<T> { Count: 0 }:
                {
                    yield break;
                }
                case IList<T> list:
                {
                    while (true)
                    {
                        for (int i = 0; i < list.Count; ++i)
                        {
                            yield return list[i];
                        }
                    }
                }
                case HashSet<T> { Count: 0 }:
                {
                    yield break;
                }
                case HashSet<T> hashSet:
                {
                    while (true)
                    {
                        foreach (T element in hashSet)
                        {
                            yield return element;
                        }
                    }
                }
                case Queue<T> { Count: 0 }:
                {
                    yield break;
                }
                case Queue<T> queue:
                {
                    while (true)
                    {
                        foreach (T element in queue)
                        {
                            yield return element;
                        }
                    }
                }
                case Stack<T> { Count: 0 }:
                {
                    yield break;
                }
                case Stack<T> stack:
                {
                    while (true)
                    {
                        foreach (T element in stack)
                        {
                            yield return element;
                        }
                    }
                }
                case SortedSet<T> { Count: 0 }:
                {
                    yield break;
                }
                case SortedSet<T> sortedSet:
                {
                    while (true)
                    {
                        foreach (T element in sortedSet)
                        {
                            yield return element;
                        }
                    }
                }
                case LinkedList<T> { Count: 0 }:
                {
                    yield break;
                }
                case LinkedList<T> linkedList:
                {
                    while (true)
                    {
                        foreach (T element in linkedList)
                        {
                            yield return element;
                        }
                    }
                }
            }

            using PooledResource<List<T>> buffer = Buffers<T>.List.Get();
            List<T> bufferList = buffer.resource;
            foreach (T element in enumerable)
            {
                bufferList.Add(element);
                yield return element;
            }

            if (bufferList.Count == 0)
            {
                yield break;
            }

            while (true)
            {
                foreach (T element in bufferList)
                {
                    yield return element;
                }
            }
        }

        public static IEnumerable<IEnumerable<T>> Partition<T>(this IEnumerable<T> items, int size)
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
