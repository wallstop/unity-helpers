namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using UnityEngine;
#if !SINGLE_THREADED
    using System.Threading;
    using System.Collections.Concurrent;
#else
    using WallstopStudios.UnityHelpers.Core.Extension;
#endif
    /// <summary>
    /// Provides thread-safe pooled access to commonly used Unity coroutine yield instructions and StringBuilder instances.
    /// This class helps reduce allocations by reusing frequently created objects.
    /// </summary>
    public static class Buffers
    {
#if SINGLE_THREADED
        private static readonly Dictionary<float, WaitForSeconds> WaitForSeconds = new();
        private static readonly Dictionary<float, WaitForSecondsRealtime> WaitForSecondsRealtime =
            new();
#else
        private static readonly ConcurrentDictionary<float, WaitForSeconds> WaitForSeconds = new();
        private static readonly ConcurrentDictionary<
            float,
            WaitForSecondsRealtime
        > WaitForSecondsRealtime = new();
#endif

        /// <summary>
        /// Reusable WaitForFixedUpdate instance to avoid repeated allocations in coroutines.
        /// Use this when waiting for the next fixed update frame.
        /// </summary>
        public static readonly WaitForFixedUpdate WaitForFixedUpdate = new();

        /// <summary>
        /// Reusable WaitForEndOfFrame instance to avoid repeated allocations in coroutines.
        /// Use this when waiting for the end of the current frame.
        /// </summary>
        public static readonly WaitForEndOfFrame WaitForEndOfFrame = new();

        /// <summary>
        /// Generic pool for StringBuilder instances. Automatically clears the StringBuilder when returned to the pool.
        /// Use this to reduce allocations when building strings.
        /// </summary>
        public static readonly WallstopGenericPool<StringBuilder> StringBuilder = new(
            () => new StringBuilder(),
            onRelease: builder => builder.Clear()
        );

        /// <summary>
        /// Gets a pooled StringBuilder with at least the requested capacity.
        /// </summary>
        public static PooledResource<StringBuilder> GetStringBuilder(
            int capacity,
            out StringBuilder builder
        )
        {
            PooledResource<StringBuilder> pooled = StringBuilder.Get(out builder);
            if (builder.Capacity < capacity)
            {
                builder.Capacity = capacity;
            }
            return pooled;
        }

        /// <summary>
        /// Gets a cached WaitForSeconds instance for the specified duration.
        /// This method caches instances to avoid repeated allocations in coroutines.
        /// </summary>
        /// <param name="seconds">The duration to wait in seconds.</param>
        /// <returns>A WaitForSeconds instance that waits for the specified duration.</returns>
        /// <remarks>
        /// IMPORTANT: Only use with CONSTANT time values, otherwise this is a memory leak.
        /// DO NOT USE with random or variable values as each unique value creates a cached entry that persists forever.
        /// </remarks>
        public static WaitForSeconds GetWaitForSeconds(float seconds)
        {
            return WaitForSeconds.GetOrAdd(seconds, value => new WaitForSeconds(value));
        }

        /// <summary>
        /// Gets a cached WaitForSecondsRealtime instance for the specified duration.
        /// This method caches instances to avoid repeated allocations in coroutines.
        /// Unlike WaitForSeconds, this uses unscaled (real) time.
        /// </summary>
        /// <param name="seconds">The duration to wait in real seconds (unaffected by Time.timeScale).</param>
        /// <returns>A WaitForSecondsRealtime instance that waits for the specified duration.</returns>
        /// <remarks>
        /// IMPORTANT: Only use with CONSTANT time values, otherwise this is a memory leak.
        /// DO NOT USE with random or variable values as each unique value creates a cached entry that persists forever.
        /// </remarks>
        public static WaitForSecondsRealtime GetWaitForSecondsRealTime(float seconds)
        {
            return WaitForSecondsRealtime.GetOrAdd(
                seconds,
                value => new WaitForSecondsRealtime(value)
            );
        }
    }

    /// <summary>
    /// Provides thread-safe generic pools for commonly used collection types.
    /// All collections are automatically cleared when returned to their respective pools.
    /// </summary>
    /// <typeparam name="T">The element type for the collections.</typeparam>
    public static class Buffers<T>
    {
        /// <summary>
        /// Generic pool for List&lt;T&gt; instances. Lists are automatically cleared when returned to the pool.
        /// </summary>
        public static readonly WallstopGenericPool<List<T>> List = new(
            () => new List<T>(),
            onRelease: list => list.Clear()
        );

        /// <summary>
        /// Gets a pooled List with at least the requested capacity.
        /// </summary>
        public static PooledResource<List<T>> GetList(int capacity, out List<T> list)
        {
            PooledResource<List<T>> pooled = List.Get(out list);
            if (list.Capacity < capacity)
            {
                list.Capacity = capacity;
            }
            return pooled;
        }

        /// <summary>
        /// Generic pool for HashSet&lt;T&gt; instances. Sets are automatically cleared when returned to the pool.
        /// </summary>
        public static readonly WallstopGenericPool<HashSet<T>> HashSet = new(
            () => new HashSet<T>(),
            onRelease: set => set.Clear()
        );

        /// <summary>
        /// Generic pool for Queue&lt;T&gt; instances. Queues are automatically cleared when returned to the pool.
        /// </summary>
        public static readonly WallstopGenericPool<Queue<T>> Queue = new(
            () => new Queue<T>(),
            onRelease: queue => queue.Clear()
        );

        /// <summary>
        /// Generic pool for Stack&lt;T&gt; instances. Stacks are automatically cleared when returned to the pool.
        /// </summary>
        public static readonly WallstopGenericPool<Stack<T>> Stack = new(
            () => new Stack<T>(),
            onRelease: stack => stack.Clear()
        );
    }

    public static class StopwatchBuffers
    {
        public static readonly WallstopGenericPool<Stopwatch> Stopwatch = new(
            () => System.Diagnostics.Stopwatch.StartNew(),
            onGet: stopwatch => stopwatch.Restart(),
            onRelease: stopwatch => stopwatch.Stop()
        );
    }

    /// <summary>
    /// Provides thread-safe generic pools for set collections with custom comparers.
    /// Includes factory methods to create pools with custom equality and comparison logic.
    /// </summary>
    /// <typeparam name="T">The element type for the sets.</typeparam>
    public static class SetBuffers<T>
    {
        /// <summary>
        /// Generic pool for SortedSet&lt;T&gt; instances using the default comparer.
        /// Sets are automatically cleared when returned to the pool.
        /// </summary>
        public static readonly WallstopGenericPool<SortedSet<T>> SortedSet = new(
            () => new SortedSet<T>(),
            onRelease: set => set.Clear()
        );

#if SINGLE_THREADED
        private static readonly Dictionary<
            IComparer<T>,
            WallstopGenericPool<SortedSet<T>>
        > SortedSetCache = new();
        private static readonly Dictionary<
            IEqualityComparer<T>,
            WallstopGenericPool<HashSet<T>>
        > HashSetCache = new();
#else
        private static readonly ConcurrentDictionary<
            IComparer<T>,
            WallstopGenericPool<SortedSet<T>>
        > SortedSetCache = new();
        private static readonly ConcurrentDictionary<
            IEqualityComparer<T>,
            WallstopGenericPool<HashSet<T>>
        > HashSetCache = new();
#endif

        /// <summary>
        /// Gets or creates a pool for SortedSet&lt;T&gt; instances that use the specified comparer.
        /// The pool is cached and reused for subsequent calls with the same comparer instance.
        /// </summary>
        /// <param name="comparer">The comparer to use for sorting elements in the set.</param>
        /// <returns>A pool that creates SortedSet instances with the specified comparer.</returns>
        /// <exception cref="ArgumentNullException">Thrown when comparer is null.</exception>
        public static WallstopGenericPool<SortedSet<T>> GetSortedSetPool(IComparer<T> comparer)
        {
            return comparer == null
                ? throw new ArgumentNullException(nameof(comparer))
                : SortedSetCache.GetOrAdd(
                    comparer,
                    inComparer => new WallstopGenericPool<SortedSet<T>>(
                        () => new SortedSet<T>(inComparer),
                        onRelease: set => set.Clear()
                    )
                );
        }

        /// <summary>
        /// Gets or creates a pool for HashSet&lt;T&gt; instances that use the specified equality comparer.
        /// The pool is cached and reused for subsequent calls with the same comparer instance.
        /// </summary>
        /// <param name="comparer">The equality comparer to use for determining element equality.</param>
        /// <returns>A pool that creates HashSet instances with the specified equality comparer.</returns>
        /// <exception cref="ArgumentNullException">Thrown when comparer is null.</exception>
        public static WallstopGenericPool<HashSet<T>> GetHashSetPool(IEqualityComparer<T> comparer)
        {
            return comparer == null
                ? throw new ArgumentNullException(nameof(comparer))
                : HashSetCache.GetOrAdd(
                    comparer,
                    inComparer => new WallstopGenericPool<HashSet<T>>(
                        () => new HashSet<T>(inComparer),
                        onRelease: set => set.Clear()
                    )
                );
        }

        /// <summary>
        /// Checks if a HashSet pool has been created for the specified equality comparer.
        /// </summary>
        /// <param name="comparer">The equality comparer to check for.</param>
        /// <returns>True if a pool exists for this comparer; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when comparer is null.</exception>
        public static bool HasHashSetPool(IEqualityComparer<T> comparer)
        {
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            return HashSetCache.ContainsKey(comparer);
        }

        /// <summary>
        /// Checks if a SortedSet pool has been created for the specified comparer.
        /// </summary>
        /// <param name="comparer">The comparer to check for.</param>
        /// <returns>True if a pool exists for this comparer; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when comparer is null.</exception>
        public static bool HasSortedSetPool(IComparer<T> comparer)
        {
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            return SortedSetCache.ContainsKey(comparer);
        }

        /// <summary>
        /// Destroys the HashSet pool associated with the specified equality comparer and disposes of all pooled instances.
        /// </summary>
        /// <param name="comparer">The equality comparer whose pool should be destroyed.</param>
        /// <returns>True if the pool was found and destroyed; false if no pool existed for this comparer.</returns>
        /// <exception cref="ArgumentNullException">Thrown when comparer is null.</exception>
        public static bool DestroyHashSetPool(IEqualityComparer<T> comparer)
        {
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            if (!HashSetCache.TryRemove(comparer, out WallstopGenericPool<HashSet<T>> pool))
            {
                return false;
            }
            pool.Dispose();
            return true;
        }

        /// <summary>
        /// Destroys the SortedSet pool associated with the specified comparer and disposes of all pooled instances.
        /// </summary>
        /// <param name="comparer">The comparer whose pool should be destroyed.</param>
        /// <returns>True if the pool was found and destroyed; false if no pool existed for this comparer.</returns>
        /// <exception cref="ArgumentNullException">Thrown when comparer is null.</exception>
        public static bool DestroySortedSetPool(IComparer<T> comparer)
        {
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            if (!SortedSetCache.TryRemove(comparer, out WallstopGenericPool<SortedSet<T>> pool))
            {
                return false;
            }
            pool.Dispose();
            return true;
        }
    }

    /// <summary>
    /// Provides a thread-safe generic pool for LinkedList instances.
    /// </summary>
    /// <typeparam name="T">The element type for the linked list.</typeparam>
    public static class LinkedListBuffer<T>
    {
        /// <summary>
        /// Generic pool for LinkedList&lt;T&gt; instances. Lists are automatically cleared when returned to the pool.
        /// </summary>
        public static readonly WallstopGenericPool<LinkedList<T>> LinkedList = new(
            () => new LinkedList<T>(),
            onRelease: linkedList => linkedList.Clear()
        );
    }

    /// <summary>
    /// Provides thread-safe generic pools for dictionary types with custom comparers.
    /// Includes factory methods to create pools with custom equality and comparison logic.
    /// </summary>
    /// <typeparam name="TKey">The key type for the dictionaries.</typeparam>
    /// <typeparam name="TValue">The value type for the dictionaries.</typeparam>
    public static class DictionaryBuffer<TKey, TValue>
    {
        /// <summary>
        /// Generic pool for Dictionary&lt;TKey, TValue&gt; instances using the default equality comparer.
        /// Dictionaries are automatically cleared when returned to the pool.
        /// </summary>
        public static readonly WallstopGenericPool<Dictionary<TKey, TValue>> Dictionary = new(
            () => new Dictionary<TKey, TValue>(),
            onRelease: dictionary => dictionary.Clear()
        );

        /// <summary>
        /// Generic pool for SortedDictionary&lt;TKey, TValue&gt; instances using the default comparer.
        /// Dictionaries are automatically cleared when returned to the pool.
        /// </summary>
        public static readonly WallstopGenericPool<
            SortedDictionary<TKey, TValue>
        > SortedDictionary = new(
            () => new SortedDictionary<TKey, TValue>(),
            onRelease: sortedDictionary => sortedDictionary.Clear()
        );

#if SINGLE_THREADED
        private static readonly Dictionary<
            IEqualityComparer<TKey>,
            WallstopGenericPool<Dictionary<TKey, TValue>>
        > DictionaryCache = new();
        private static readonly Dictionary<
            IComparer<TKey>,
            WallstopGenericPool<SortedDictionary<TKey, TValue>>
        > SortedDictionaryCache = new();
#else
        private static readonly ConcurrentDictionary<
            IEqualityComparer<TKey>,
            WallstopGenericPool<Dictionary<TKey, TValue>>
        > DictionaryCache = new();
        private static readonly ConcurrentDictionary<
            IComparer<TKey>,
            WallstopGenericPool<SortedDictionary<TKey, TValue>>
        > SortedDictionaryCache = new();
#endif

        /// <summary>
        /// Gets or creates a pool for Dictionary&lt;TKey, TValue&gt; instances that use the specified equality comparer.
        /// The pool is cached and reused for subsequent calls with the same comparer instance.
        /// </summary>
        /// <param name="comparer">The equality comparer to use for key equality.</param>
        /// <returns>A pool that creates Dictionary instances with the specified equality comparer.</returns>
        /// <exception cref="ArgumentNullException">Thrown when comparer is null.</exception>
        public static WallstopGenericPool<Dictionary<TKey, TValue>> GetDictionaryPool(
            IEqualityComparer<TKey> comparer
        )
        {
            return comparer == null
                ? throw new ArgumentNullException(nameof(comparer))
                : DictionaryCache.GetOrAdd(
                    comparer,
                    inComparer => new WallstopGenericPool<Dictionary<TKey, TValue>>(
                        () => new Dictionary<TKey, TValue>(inComparer),
                        onRelease: dictionary => dictionary.Clear()
                    )
                );
        }

        /// <summary>
        /// Gets or creates a pool for SortedDictionary&lt;TKey, TValue&gt; instances that use the specified comparer.
        /// The pool is cached and reused for subsequent calls with the same comparer instance.
        /// </summary>
        /// <param name="comparer">The comparer to use for sorting keys.</param>
        /// <returns>A pool that creates SortedDictionary instances with the specified comparer.</returns>
        /// <exception cref="ArgumentNullException">Thrown when comparer is null.</exception>
        public static WallstopGenericPool<SortedDictionary<TKey, TValue>> GetSortedDictionaryPool(
            IComparer<TKey> comparer
        )
        {
            return comparer == null
                ? throw new ArgumentNullException(nameof(comparer))
                : SortedDictionaryCache.GetOrAdd(
                    comparer,
                    inComparer => new WallstopGenericPool<SortedDictionary<TKey, TValue>>(
                        () => new SortedDictionary<TKey, TValue>(inComparer),
                        onRelease: dictionary => dictionary.Clear()
                    )
                );
        }

        /// <summary>
        /// Checks if a Dictionary pool has been created for the specified equality comparer.
        /// </summary>
        /// <param name="comparer">The equality comparer to check for.</param>
        /// <returns>True if a pool exists for this comparer; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when comparer is null.</exception>
        public static bool HasDictionaryPool(IEqualityComparer<TKey> comparer)
        {
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            return DictionaryCache.ContainsKey(comparer);
        }

        /// <summary>
        /// Checks if a SortedDictionary pool has been created for the specified comparer.
        /// </summary>
        /// <param name="comparer">The comparer to check for.</param>
        /// <returns>True if a pool exists for this comparer; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when comparer is null.</exception>
        public static bool HasSortedDictionaryPool(IComparer<TKey> comparer)
        {
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            return SortedDictionaryCache.ContainsKey(comparer);
        }

        /// <summary>
        /// Destroys the Dictionary pool associated with the specified equality comparer and disposes of all pooled instances.
        /// </summary>
        /// <param name="comparer">The equality comparer whose pool should be destroyed.</param>
        /// <returns>True if the pool was found and destroyed; false if no pool existed for this comparer.</returns>
        /// <exception cref="ArgumentNullException">Thrown when comparer is null.</exception>
        public static bool DestroyDictionaryPool(IEqualityComparer<TKey> comparer)
        {
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }
            if (
                !DictionaryCache.TryRemove(
                    comparer,
                    out WallstopGenericPool<Dictionary<TKey, TValue>> pool
                )
            )
            {
                return false;
            }
            pool.Dispose();
            return true;
        }

        /// <summary>
        /// Destroys the SortedDictionary pool associated with the specified comparer and disposes of all pooled instances.
        /// </summary>
        /// <param name="comparer">The comparer whose pool should be destroyed.</param>
        /// <returns>True if the pool was found and destroyed; false if no pool existed for this comparer.</returns>
        /// <exception cref="ArgumentNullException">Thrown when comparer is null.</exception>
        public static bool DestroySortedDictionaryPool(IComparer<TKey> comparer)
        {
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            if (
                !SortedDictionaryCache.TryRemove(
                    comparer,
                    out WallstopGenericPool<SortedDictionary<TKey, TValue>> pool
                )
            )
            {
                return false;
            }

            pool.Dispose();
            return true;
        }
    }

#if SINGLE_THREADED
    /// <summary>
    /// A generic object pool that manages reusable instances of type T.
    /// This single-threaded implementation uses a Stack for storage.
    /// </summary>
    /// <typeparam name="T">The type of objects to pool.</typeparam>
    public sealed class WallstopGenericPool<T> : IDisposable
    {
        /// <summary>
        /// Gets the current number of instances in the pool.
        /// </summary>
        internal int Count => _pool.Count;

        private readonly Func<T> _producer;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly Action<T> _onDispose;

        private readonly Stack<T> _pool = new();

        /// <summary>
        /// Creates a new generic pool with the specified producer function and optional callbacks.
        /// </summary>
        /// <param name="producer">Function that creates new instances when the pool is empty.</param>
        /// <param name="preWarmCount">Number of instances to create and add to the pool during initialization. Default is 0.</param>
        /// <param name="onGet">Optional callback invoked when an instance is retrieved from the pool.</param>
        /// <param name="onRelease">Optional callback invoked when an instance is returned to the pool.</param>
        /// <param name="onDisposal">Optional callback invoked when the pool is disposed for each pooled instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when producer is null.</exception>
        public WallstopGenericPool(
            Func<T> producer,
            int preWarmCount = 0,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onDisposal = null
        )
        {
            _producer = producer ?? throw new ArgumentNullException(nameof(producer));
            _onGet = onGet;
            _onRelease = onRelease ?? (_ => { });
            _onRelease += _pool.Push;
            _onDispose = onDisposal;
            for (int i = 0; i < preWarmCount; ++i)
            {
                T value = _producer();
                _onGet?.Invoke(value);
                _onRelease(value);
            }
        }

        /// <summary>
        /// Gets a pooled resource. When disposed, the resource is automatically returned to the pool.
        /// If the pool is empty, a new instance is created using the producer function.
        /// </summary>
        /// <returns>A PooledResource wrapping the retrieved instance.</returns>
        public PooledResource<T> Get()
        {
            return Get(out _);
        }

        /// <summary>
        /// Gets a pooled resource and outputs the value. When disposed, the resource is automatically returned to the pool.
        /// If the pool is empty, a new instance is created using the producer function.
        /// </summary>
        /// <param name="value">The retrieved instance.</param>
        /// <returns>A PooledResource wrapping the retrieved instance.</returns>
        public PooledResource<T> Get(out T value)
        {
            if (!_pool.TryPop(out value))
            {
                value = _producer();
            }

            _onGet?.Invoke(value);
            return new PooledResource<T>(value, _onRelease);
        }

        /// <summary>
        /// Disposes the pool. If an onDisposal callback was provided, it is invoked for each pooled instance.
        /// Otherwise, the pool is simply cleared.
        /// </summary>
        public void Dispose()
        {
            if (_onDispose == null)
            {
                _pool.Clear();
                return;
            }

            while (_pool.TryPop(out T value))
            {
                _onDispose(value);
            }
        }
    }
#else
    /// <summary>
    /// A thread-safe generic object pool that manages reusable instances of type T.
    /// This multi-threaded implementation uses a ConcurrentStack for thread-safe storage.
    /// </summary>
    /// <typeparam name="T">The type of objects to pool.</typeparam>
    public sealed class WallstopGenericPool<T> : IDisposable
    {
        /// <summary>
        /// Gets the current number of instances in the pool.
        /// </summary>
        internal int Count => _pool.Count;

        private readonly Func<T> _producer;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly Action<T> _onDispose;

        private readonly ConcurrentStack<T> _pool = new();

        /// <summary>
        /// Creates a new thread-safe generic pool with the specified producer function and optional callbacks.
        /// </summary>
        /// <param name="producer">Function that creates new instances when the pool is empty.</param>
        /// <param name="preWarmCount">Number of instances to create and add to the pool during initialization. Default is 0.</param>
        /// <param name="onGet">Optional callback invoked when an instance is retrieved from the pool.</param>
        /// <param name="onRelease">Optional callback invoked when an instance is returned to the pool.</param>
        /// <param name="onDisposal">Optional callback invoked when the pool is disposed for each pooled instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when producer is null.</exception>
        public WallstopGenericPool(
            Func<T> producer,
            int preWarmCount = 0,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onDisposal = null
        )
        {
            _producer = producer ?? throw new ArgumentNullException(nameof(producer));
            _onGet = onGet;
            _onRelease = onRelease ?? (_ => { });
            _onRelease += _pool.Push;
            _onDispose = onDisposal;
            for (int i = 0; i < preWarmCount; ++i)
            {
                T value = _producer();
                _onGet?.Invoke(value);
                _onRelease(value);
            }
        }

        /// <summary>
        /// Gets a pooled resource. When disposed, the resource is automatically returned to the pool.
        /// If the pool is empty, a new instance is created using the producer function.
        /// This method is thread-safe.
        /// </summary>
        /// <returns>A PooledResource wrapping the retrieved instance.</returns>
        public PooledResource<T> Get()
        {
            return Get(out _);
        }

        /// <summary>
        /// Gets a pooled resource and outputs the value. When disposed, the resource is automatically returned to the pool.
        /// If the pool is empty, a new instance is created using the producer function.
        /// This method is thread-safe.
        /// </summary>
        /// <param name="value">The retrieved instance.</param>
        /// <returns>A PooledResource wrapping the retrieved instance.</returns>
        public PooledResource<T> Get(out T value)
        {
            if (!_pool.TryPop(out value))
            {
                value = _producer();
            }

            _onGet?.Invoke(value);
            return new PooledResource<T>(value, _onRelease);
        }

        /// <summary>
        /// Disposes the pool. If an onDisposal callback was provided, it is invoked for each pooled instance.
        /// Otherwise, the pool is simply cleared.
        /// </summary>
        public void Dispose()
        {
            if (_onDispose == null)
            {
                _pool.Clear();
                return;
            }

            while (_pool.TryPop(out T value))
            {
                _onDispose(value);
            }
        }
    }
#endif

#if SINGLE_THREADED
    /// <summary>
    /// A static array pool that provides pooled arrays of specific sizes.
    /// Arrays are cleared (set to default values) when returned to the pool.
    /// This single-threaded implementation uses Dictionary and List for storage.
    /// </summary>
    /// <typeparam name="T">The element type for the arrays.</typeparam>
    public static class WallstopArrayPool<T>
    {
        private static readonly Dictionary<int, List<T[]>> _pool = new();
        private static readonly Action<T[]> _onDispose = Release;

        /// <summary>
        /// Gets a pooled array of the specified size. When disposed, the array is cleared and returned to the pool.
        /// </summary>
        /// <param name="size">The size of the array to retrieve. Must be non-negative.</param>
        /// <returns>A PooledResource wrapping an array of the specified size.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when size is negative.</exception>
        public static PooledResource<T[]> Get(int size)
        {
            return Get(size, out _);
        }

        /// <summary>
        /// Gets a pooled array of the specified size and outputs the value. When disposed, the array is cleared and returned to the pool.
        /// </summary>
        /// <param name="size">The size of the array to retrieve. Must be non-negative.</param>
        /// <param name="value">The retrieved array.</param>
        /// <returns>A PooledResource wrapping an array of the specified size.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when size is negative.</exception>
        public static PooledResource<T[]> Get(int size, out T[] value)
        {
            switch (size)
            {
                case < 0:
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(size),
                        size,
                        "Must be non-negative."
                    );
                }
                case 0:
                {
                    value = Array.Empty<T>();
                    return new PooledResource<T[]>(value, _ => { });
                }
            }

            if (!_pool.TryGetValue(size, out List<T[]> pool))
            {
                pool = new List<T[]>();
                _pool[size] = pool;
            }

            if (pool.Count == 0)
            {
                value = new T[size];
                return new PooledResource<T[]>(value, _onDispose);
            }

            int lastIndex = pool.Count - 1;
            value = pool[lastIndex];
            pool.RemoveAt(lastIndex);
            return new PooledResource<T[]>(value, _onDispose);
        }

        private static void Release(T[] resource)
        {
            int length = resource.Length;
            Array.Clear(resource, 0, length);
            if (!_pool.TryGetValue(length, out List<T[]> pool))
            {
                pool = new List<T[]>();
                _pool[resource.Length] = pool;
            }
            pool.Add(resource);
        }
    }
#else
    /// <summary>
    /// A thread-safe static array pool that provides pooled arrays of specific sizes.
    /// Arrays are cleared (set to default values) when returned to the pool.
    /// This multi-threaded implementation uses ConcurrentDictionary and ConcurrentStack for thread-safe storage.
    /// </summary>
    /// <typeparam name="T">The element type for the arrays.</typeparam>
    public static class WallstopArrayPool<T>
    {
        private static readonly ConcurrentDictionary<int, ConcurrentStack<T[]>> _pool = new();
        private static readonly Action<T[]> _onRelease = Release;

        /// <summary>
        /// Gets a pooled array of the specified size. When disposed, the array is cleared and returned to the pool.
        /// This method is thread-safe.
        /// </summary>
        /// <param name="size">The size of the array to retrieve. Must be non-negative.</param>
        /// <returns>A PooledResource wrapping an array of the specified size.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when size is negative.</exception>
        public static PooledResource<T[]> Get(int size)
        {
            return Get(size, out _);
        }

        /// <summary>
        /// Gets a pooled array of the specified size and outputs the value. When disposed, the array is cleared and returned to the pool.
        /// This method is thread-safe.
        /// </summary>
        /// <param name="size">The size of the array to retrieve. Must be non-negative.</param>
        /// <param name="value">The retrieved array.</param>
        /// <returns>A PooledResource wrapping an array of the specified size.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when size is negative.</exception>
        public static PooledResource<T[]> Get(int size, out T[] value)
        {
            switch (size)
            {
                case < 0:
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(size),
                        size,
                        "Must be non-negative."
                    );
                }
                case 0:
                {
                    value = Array.Empty<T>();
                    return new PooledResource<T[]>(value, _ => { });
                }
            }

            ConcurrentStack<T[]> result = _pool.GetOrAdd(size, _ => new ConcurrentStack<T[]>());
            if (!result.TryPop(out value))
            {
                value = new T[size];
            }

            return new PooledResource<T[]>(value, _onRelease);
        }

        private static void Release(T[] resource)
        {
            int length = resource.Length;
            Array.Clear(resource, 0, length);
            ConcurrentStack<T[]> result = _pool.GetOrAdd(length, _ => new ConcurrentStack<T[]>());
            result.Push(resource);
        }
    }
#endif

#if SINGLE_THREADED
    /// <summary>
    /// A fast static array pool optimized for index-based lookup with minimal overhead.
    /// Unlike WallstopArrayPool, arrays are NOT cleared when returned to the pool, providing better performance.
    /// This single-threaded implementation uses a List of Stacks indexed by array size for O(1) lookups.
    /// </summary>
    /// <typeparam name="T">The element type for the arrays.</typeparam>
    /// <remarks>
    /// IMPORTANT: This pool does NOT clear arrays on release. Callers must manually clear arrays if needed.
    /// This design trades safety for performance in scenarios where array contents don't need to be reset.
    /// </remarks>
    public static class WallstopFastArrayPool<T>
        where T : unmanaged
    {
        private static readonly List<Stack<T[]>> _pool = new();
        private static readonly Action<T[]> _onRelease = Release;

        /// <summary>
        /// Gets a pooled array of the specified size. When disposed, the array is returned to the pool WITHOUT being cleared.
        /// </summary>
        /// <param name="size">The size of the array to retrieve. Must be non-negative.</param>
        /// <returns>A PooledResource wrapping an array of the specified size.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when size is negative.</exception>
        /// <remarks>Arrays are NOT cleared on return. The caller is responsible for clearing if needed.</remarks>
        public static PooledResource<T[]> Get(int size)
        {
            return Get(size, out _);
        }

        /// <summary>
        /// Gets a pooled array of the specified size and outputs the value. When disposed, the array is returned to the pool WITHOUT being cleared.
        /// </summary>
        /// <param name="size">The size of the array to retrieve. Must be non-negative.</param>
        /// <param name="value">The retrieved array.</param>
        /// <returns>A PooledResource wrapping an array of the specified size.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when size is negative.</exception>
        /// <remarks>Arrays are NOT cleared on return. The caller is responsible for clearing if needed.</remarks>
        public static PooledResource<T[]> Get(int size, out T[] value)
        {
            switch (size)
            {
                case < 0:
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(size),
                        size,
                        "Must be non-negative."
                    );
                }
                case 0:
                {
                    value = Array.Empty<T>();
                    return new PooledResource<T[]>(value, _ => { });
                }
            }

            while (_pool.Count <= size)
            {
                _pool.Add(null);
            }

            Stack<T[]> pool = _pool[size];
            if (pool == null)
            {
                pool = new Stack<T[]>();
                _pool[size] = pool;
            }

            if (!pool.TryPop(out value))
            {
                value = new T[size];
            }

            return new PooledResource<T[]>(value, _onRelease);
        }

        private static void Release(T[] resource)
        {
            _pool[resource.Length].Push(resource);
        }
    }
#else
    /// <summary>
    /// A thread-safe fast static array pool optimized for index-based lookup with minimal overhead.
    /// Unlike WallstopArrayPool, arrays are NOT cleared when returned to the pool, providing better performance.
    /// This multi-threaded implementation uses a List of ConcurrentStacks with ReaderWriterLockSlim for thread-safe index access.
    /// </summary>
    /// <typeparam name="T">The element type for the arrays.</typeparam>
    /// <remarks>
    /// IMPORTANT: This pool does NOT clear arrays on release. Callers must manually clear arrays if needed.
    /// This design trades safety for performance in scenarios where array contents don't need to be reset.
    /// </remarks>
    public static class WallstopFastArrayPool<T>
        where T : unmanaged
    {
        private static readonly ReaderWriterLockSlim _lock = new();
        private static readonly List<ConcurrentStack<T[]>> _pool = new();
        private static readonly Action<T[]> _onRelease = Release;

        /// <summary>
        /// Gets a pooled array of the specified size. When disposed, the array is returned to the pool WITHOUT being cleared.
        /// This method is thread-safe.
        /// </summary>
        /// <param name="size">The size of the array to retrieve. Must be non-negative.</param>
        /// <returns>A PooledResource wrapping an array of the specified size.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when size is negative.</exception>
        /// <remarks>Arrays are NOT cleared on return. The caller is responsible for clearing if needed.</remarks>
        public static PooledResource<T[]> Get(int size)
        {
            return Get(size, out _);
        }

        /// <summary>
        /// Gets a pooled array of the specified size and outputs the value. When disposed, the array is returned to the pool WITHOUT being cleared.
        /// This method is thread-safe.
        /// </summary>
        /// <param name="size">The size of the array to retrieve. Must be non-negative.</param>
        /// <param name="value">The retrieved array.</param>
        /// <returns>A PooledResource wrapping an array of the specified size.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when size is negative.</exception>
        /// <remarks>Arrays are NOT cleared on return. The caller is responsible for clearing if needed.</remarks>
        public static PooledResource<T[]> Get(int size, out T[] value)
        {
            switch (size)
            {
                case < 0:
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(size),
                        size,
                        "Must be non-negative."
                    );
                }
                case 0:
                {
                    value = Array.Empty<T>();
                    return new PooledResource<T[]>(value, _ => { });
                }
            }

            bool withinRange;
            ConcurrentStack<T[]> pool = null;
            _lock.EnterReadLock();
            try
            {
                withinRange = size < _pool.Count;
                if (withinRange)
                {
                    pool = _pool[size];
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }

            if (withinRange)
            {
                if (pool == null)
                {
                    _lock.EnterUpgradeableReadLock();
                    try
                    {
                        pool = _pool[size];
                        if (pool == null)
                        {
                            _lock.EnterWriteLock();
                            try
                            {
                                pool = _pool[size];
                                if (pool == null)
                                {
                                    pool = new ConcurrentStack<T[]>();
                                    _pool[size] = pool;
                                }
                            }
                            finally
                            {
                                _lock.ExitWriteLock();
                            }
                        }
                    }
                    finally
                    {
                        _lock.ExitUpgradeableReadLock();
                    }
                }
            }
            else
            {
                _lock.EnterUpgradeableReadLock();
                try
                {
                    if (size < _pool.Count)
                    {
                        pool = _pool[size];
                        if (pool == null)
                        {
                            _lock.EnterWriteLock();
                            try
                            {
                                pool = _pool[size];
                                if (pool == null)
                                {
                                    pool = new ConcurrentStack<T[]>();
                                    _pool[size] = pool;
                                }
                            }
                            finally
                            {
                                _lock.ExitWriteLock();
                            }
                        }
                    }
                    else
                    {
                        _lock.EnterWriteLock();
                        try
                        {
                            while (_pool.Count <= size)
                            {
                                _pool.Add(null);
                            }
                            pool = _pool[size];
                            if (pool == null)
                            {
                                pool = new ConcurrentStack<T[]>();
                                _pool[size] = pool;
                            }
                        }
                        finally
                        {
                            _lock.ExitWriteLock();
                        }
                    }
                }
                finally
                {
                    _lock.ExitUpgradeableReadLock();
                }
            }

            if (!pool.TryPop(out value))
            {
                value = new T[size];
            }

            return new PooledResource<T[]>(value, _onRelease);
        }

        private static void Release(T[] resource)
        {
            _pool[resource.Length].Push(resource);
        }
    }
#endif

    /// <summary>
    /// A readonly struct that wraps a pooled resource and automatically returns it to the pool when disposed.
    /// This type is designed to be used with 'using' statements to ensure resources are properly returned.
    /// </summary>
    /// <typeparam name="T">The type of the pooled resource.</typeparam>
    /// <remarks>
    /// This struct implements IDisposable to enable automatic resource return via 'using' statements.
    /// The resource is returned to its pool when Dispose is called, typically at the end of a 'using' block.
    /// </remarks>
    public struct PooledResource<T> : IDisposable
    {
        /// <summary>
        /// The pooled resource instance. Access this to use the resource.
        /// </summary>
        public readonly T resource;
        private readonly Action<T> _onDispose;
        private bool _initialized;

        /// <summary>
        /// Creates a new PooledResource wrapping the specified resource with a disposal action.
        /// </summary>
        /// <param name="resource">The resource to wrap.</param>
        /// <param name="onDispose">The action to invoke when disposing, typically returning the resource to a pool.</param>
        public PooledResource(T resource, Action<T> onDispose)
        {
            _initialized = true;
            this.resource = resource;
            _onDispose = onDispose;
        }

        /// <summary>
        /// Disposes the resource by invoking the disposal action, typically returning it to the pool.
        /// This method is automatically called at the end of a 'using' block.
        /// </summary>
        public void Dispose()
        {
            if (!_initialized)
            {
                return;
            }
            _initialized = false;
            _onDispose(resource);
        }
    }
}
