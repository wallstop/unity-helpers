// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

// ReSharper disable ConvertClosureToMethodGroup
namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;
    using UnityEngine;
    using Debug = UnityEngine.Debug;
#if SINGLE_THREADED
    using WallstopStudios.UnityHelpers.Core.Extension;
#else
    using System.Collections.Concurrent;
#endif
    /// <summary>
    /// Provides thread-safe pooled access to commonly used Unity coroutine yield instructions and StringBuilder instances.
    /// This class helps reduce allocations by reusing frequently created objects.
    /// </summary>
    public static class Buffers
    {
        /// <summary>
        /// Gets or sets the quantization step (in seconds) applied to pooled WaitForSeconds/WaitForSecondsRealtime durations.
        /// Values less than or equal to zero disable quantization.
        /// </summary>
        public static float WaitInstructionQuantizationStepSeconds
        {
            get => Volatile.Read(ref _waitInstructionQuantizationStepSeconds);
            set
            {
                float sanitized = value;
                if (float.IsNaN(sanitized) || sanitized <= 0f)
                {
                    sanitized = 0f;
                }
                Volatile.Write(ref _waitInstructionQuantizationStepSeconds, sanitized);
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of distinct WaitForSeconds/WaitForSecondsRealtime entries cached.
        /// A value of 0 disables the cap (unbounded cache).
        /// </summary>
        public static int WaitInstructionMaxDistinctEntries
        {
            get => Volatile.Read(ref _waitInstructionMaxDistinctEntries);
            set
            {
                int sanitized = value < 0 ? 0 : value;
                Volatile.Write(ref _waitInstructionMaxDistinctEntries, sanitized);
            }
        }

        /// <summary>
        /// Snapshot of the WaitForSeconds cache (distinct entries, limit hits, quantization info).
        /// </summary>
        public static WaitInstructionCacheDiagnostics WaitForSecondsCacheDiagnostics =>
            BuildDiagnostics(
                WaitForSecondsCacheName,
                GetWaitForSecondsEntryCount(),
                Volatile.Read(ref _waitForSecondsLimitHits),
                Volatile.Read(ref _waitForSecondsEvictions)
            );

        /// <summary>
        /// Snapshot of the WaitForSecondsRealtime cache (distinct entries, limit hits, quantization info).
        /// </summary>
        public static WaitInstructionCacheDiagnostics WaitForSecondsRealtimeCacheDiagnostics =>
            BuildDiagnostics(
                WaitForSecondsRealtimeCacheName,
                GetWaitForSecondsRealtimeEntryCount(),
                Volatile.Read(ref _waitForSecondsRealtimeLimitHits),
                Volatile.Read(ref _waitForSecondsRealtimeEvictions)
            );

        /// <summary>
        /// Enables or disables LRU eviction when the cache reaches the max distinct entry count. When enabled, the oldest entries are removed and reused instead of refusing new durations.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>Performance Characteristics:</strong>
        /// All LRU operations (lookup, insertion, eviction, access-order update) run in O(1) time.
        /// This is achieved by storing a <see cref="LinkedListNode{T}"/> reference directly in each
        /// cache entry, enabling O(1) removal and re-insertion for access-order updates.
        /// </para>
        /// <para>
        /// <strong>Thread Safety:</strong>
        /// When <c>SINGLE_THREADED</c> is not defined (default), all cache operations are protected
        /// by a lock. When <c>SINGLE_THREADED</c> is defined, lock overhead is eliminated for
        /// WebGL and other single-threaded runtimes.
        /// </para>
        /// </remarks>
        public static bool WaitInstructionUseLruEviction
        {
            get => Volatile.Read(ref _waitInstructionUseLruEvictionFlag) != 0;
            set => Volatile.Write(ref _waitInstructionUseLruEvictionFlag, value ? 1 : 0);
        }

        /// <summary>
        /// Stores a cached wait instruction alongside its position in the LRU ordering.
        /// </summary>
        /// <typeparam name="TInstruction">The type of wait instruction (WaitForSeconds or WaitForSecondsRealtime).</typeparam>
        /// <remarks>
        /// By storing the <see cref="LinkedListNode{T}"/> directly, we achieve O(1) complexity for:
        /// <list type="bullet">
        ///   <item><description>Removing the entry from its current position: <see cref="LinkedList{T}.Remove(LinkedListNode{T})"/> is O(1)</description></item>
        ///   <item><description>Moving the entry to the end (most recently used): <see cref="LinkedList{T}.AddLast(LinkedListNode{T})"/> is O(1)</description></item>
        ///   <item><description>Evicting the oldest entry: <see cref="LinkedList{T}.First"/> and <see cref="LinkedList{T}.RemoveFirst"/> are O(1)</description></item>
        /// </list>
        /// This avoids the O(n) traversal that would be required if we only stored keys and had to search for them.
        /// </remarks>
        private readonly struct WaitInstructionCacheEntry<TInstruction>
        {
            internal readonly TInstruction _value;
            internal readonly LinkedListNode<float> _node;

            internal WaitInstructionCacheEntry(TInstruction value, LinkedListNode<float> node)
            {
                this._value = value;
                this._node = node;
            }
        }

        private static readonly Dictionary<
            float,
            WaitInstructionCacheEntry<WaitForSeconds>
        > WaitForSeconds = new();
        private static readonly Dictionary<
            float,
            WaitInstructionCacheEntry<WaitForSecondsRealtime>
        > WaitForSecondsRealtime = new();
        private static readonly LinkedList<float> WaitForSecondsOrder = new();
        private static readonly LinkedList<float> WaitForSecondsRealtimeOrder = new();

        public const int WaitInstructionDefaultMaxDistinctEntries = 512;
        private const int WaitInstructionLimitWarningInterval = 25;
        private const string WaitForSecondsCacheName = "WaitForSeconds";
        private const string WaitForSecondsRealtimeCacheName = "WaitForSecondsRealtime";

        private static float _waitInstructionQuantizationStepSeconds;
        private static int _waitInstructionMaxDistinctEntries =
            WaitInstructionDefaultMaxDistinctEntries;
        private static int _waitInstructionUseLruEvictionFlag;
        private static int _waitForSecondsLimitHits;
        private static int _waitForSecondsRealtimeLimitHits;
        private static int _waitForSecondsEvictions;
        private static int _waitForSecondsRealtimeEvictions;
        private static readonly object WaitInstructionCacheLock = new();

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
            WaitForSeconds pooled = RentWaitInstruction(
                WaitForSeconds,
                WaitForSecondsOrder,
                wait => CreateWaitForSeconds(wait),
                seconds,
                ref _waitForSecondsLimitHits,
                ref _waitForSecondsEvictions,
                WaitForSecondsCacheName,
                allowEviction: true
            );
            return pooled ?? new WaitForSeconds(seconds);
        }

        /// <summary>
        /// Attempts to retrieve a cached WaitForSeconds instance without allocating a fallback when the cache limit is reached.
        /// Returns null if the duration would exceed the configured cache size.
        /// </summary>
        public static WaitForSeconds TryGetWaitForSecondsPooled(float seconds)
        {
            return RentWaitInstruction(
                WaitForSeconds,
                WaitForSecondsOrder,
                wait => CreateWaitForSeconds(wait),
                seconds,
                ref _waitForSecondsLimitHits,
                ref _waitForSecondsEvictions,
                WaitForSecondsCacheName,
                allowEviction: false
            );
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
            WaitForSecondsRealtime pooled = RentWaitInstruction(
                WaitForSecondsRealtime,
                WaitForSecondsRealtimeOrder,
                wait => CreateWaitForSecondsRealtime(wait),
                seconds,
                ref _waitForSecondsRealtimeLimitHits,
                ref _waitForSecondsRealtimeEvictions,
                WaitForSecondsRealtimeCacheName,
                allowEviction: true
            );
            return pooled ?? new WaitForSecondsRealtime(seconds);
        }

        /// <summary>
        /// Attempts to retrieve a cached WaitForSecondsRealtime instance without allocating a fallback when the cache limit is reached.
        /// Returns null if the duration would exceed the configured cache size.
        /// </summary>
        public static WaitForSecondsRealtime TryGetWaitForSecondsRealtimePooled(float seconds)
        {
            return RentWaitInstruction(
                WaitForSecondsRealtime,
                WaitForSecondsRealtimeOrder,
                wait => CreateWaitForSecondsRealtime(wait),
                seconds,
                ref _waitForSecondsRealtimeLimitHits,
                ref _waitForSecondsRealtimeEvictions,
                WaitForSecondsRealtimeCacheName,
                allowEviction: false
            );
        }

        private static WaitForSeconds CreateWaitForSeconds(float seconds)
        {
            return new WaitForSeconds(seconds);
        }

        private static WaitForSecondsRealtime CreateWaitForSecondsRealtime(float seconds)
        {
            return new WaitForSecondsRealtime(seconds);
        }

        /// <summary>
        /// Core LRU cache implementation for wait instructions. Provides O(1) operations for all cache operations.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>Algorithm:</strong>
        /// Uses a Dictionary for O(1) key lookup combined with a LinkedList for O(1) LRU ordering.
        /// Each cache entry stores a reference to its LinkedListNode, enabling O(1) removal and reinsertion.
        /// </para>
        /// <para>
        /// <strong>Operations:</strong>
        /// <list type="bullet">
        ///   <item><description>Cache hit: O(1) lookup + O(1) move to end of LRU list</description></item>
        ///   <item><description>Cache miss with capacity: O(1) add to dictionary + O(1) add to end of LRU list</description></item>
        ///   <item><description>Cache miss with eviction: O(1) remove oldest + O(1) add new entry</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Thread Safety:</strong>
        /// Protected by <see cref="WaitInstructionCacheLock"/> unless <c>SINGLE_THREADED</c> is defined.
        /// </para>
        /// </remarks>
        private static TInstruction RentWaitInstruction<TInstruction>(
            Dictionary<float, WaitInstructionCacheEntry<TInstruction>> cache,
            LinkedList<float> order,
            Func<float, TInstruction> factory,
            float requestedSeconds,
            ref int limitHits,
            ref int evictionCount,
            string cacheName,
            bool allowEviction
        )
            where TInstruction : class
        {
            float quantized = QuantizeSeconds(requestedSeconds);
#if !SINGLE_THREADED
            lock (WaitInstructionCacheLock)
            {
#endif
                if (cache.TryGetValue(quantized, out WaitInstructionCacheEntry<TInstruction> entry))
                {
                    if (entry._node.List != null)
                    {
                        order.Remove(entry._node);
                        order.AddLast(entry._node);
                    }
                    return entry._value;
                }

                bool useLru =
                    allowEviction
                    && WaitInstructionUseLruEviction
                    && WaitInstructionMaxDistinctEntries > 0;
                if (useLru && cache.Count >= WaitInstructionMaxDistinctEntries)
                {
                    if (order.First != null)
                    {
                        float evictKey = order.First.Value;
                        order.RemoveFirst();
                        if (cache.Remove(evictKey))
                        {
                            Interlocked.Increment(ref evictionCount);
                        }
                    }
                }
                else if (!CanCacheNewEntry(cache.Count))
                {
                    ReportCacheLimit(cacheName, ref limitHits);
                    return null;
                }

                LinkedListNode<float> node = order.AddLast(quantized);
                TInstruction created = factory(quantized);
                cache[quantized] = new WaitInstructionCacheEntry<TInstruction>(created, node);
                return created;
#if !SINGLE_THREADED
            }
#endif
        }

        private static float QuantizeSeconds(float seconds)
        {
            float step = WaitInstructionQuantizationStepSeconds;
            if (step <= 0f || float.IsNaN(step) || float.IsInfinity(step))
            {
                return seconds;
            }

            if (float.IsNaN(seconds) || float.IsInfinity(seconds))
            {
                return seconds;
            }

            float normalized = seconds / step;
            float rounded = Mathf.Round(normalized);
            return rounded * step;
        }

        private static bool CanCacheNewEntry(int currentCount)
        {
            int maxEntries = WaitInstructionMaxDistinctEntries;
            return maxEntries <= 0 || currentCount < maxEntries;
        }

        private static void ReportCacheLimit(string cacheName, ref int limitHits)
        {
            int hits = Interlocked.Increment(ref limitHits);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            int maxEntries = WaitInstructionMaxDistinctEntries;
            if (maxEntries > 0 && (hits == 1 || hits % WaitInstructionLimitWarningInterval == 0))
            {
                Debug.LogWarning(
                    $"[Buffers] {cacheName} cache reached the configured limit of {maxEntries} unique wait instructions. Consider using Buffers.TryGet... or increasing Buffers.WaitInstructionMaxDistinctEntries."
                );
            }
#endif
        }

        private static int GetWaitForSecondsEntryCount()
        {
#if !SINGLE_THREADED
            lock (WaitInstructionCacheLock)
            {
#endif
                return WaitForSeconds.Count;
#if !SINGLE_THREADED
            }
#endif
        }

        private static int GetWaitForSecondsRealtimeEntryCount()
        {
#if !SINGLE_THREADED
            lock (WaitInstructionCacheLock)
            {
#endif
                return WaitForSecondsRealtime.Count;
#if !SINGLE_THREADED
            }
#endif
        }

        private static WaitInstructionCacheDiagnostics BuildDiagnostics(
            string cacheName,
            int distinctEntries,
            int limitHits,
            int evictions
        )
        {
            return new WaitInstructionCacheDiagnostics(
                cacheName,
                distinctEntries,
                WaitInstructionMaxDistinctEntries,
                limitHits,
                evictions,
                WaitInstructionQuantizationStepSeconds,
                WaitInstructionUseLruEviction
            );
        }

        internal static void ResetWaitInstructionCachesForTesting()
        {
#if !SINGLE_THREADED
            lock (WaitInstructionCacheLock)
            {
#endif
                WaitForSeconds.Clear();
                WaitForSecondsRealtime.Clear();
                WaitForSecondsOrder.Clear();
                WaitForSecondsRealtimeOrder.Clear();
#if !SINGLE_THREADED
            }
#endif
            WaitInstructionQuantizationStepSeconds = 0f;
            WaitInstructionMaxDistinctEntries = WaitInstructionDefaultMaxDistinctEntries;
            WaitInstructionUseLruEviction = false;
            Volatile.Write(ref _waitForSecondsLimitHits, 0);
            Volatile.Write(ref _waitForSecondsRealtimeLimitHits, 0);
            Volatile.Write(ref _waitForSecondsEvictions, 0);
            Volatile.Write(ref _waitForSecondsRealtimeEvictions, 0);
        }

        internal static IDisposable BeginWaitInstructionTestScope()
        {
            return new WaitInstructionTestScope();
        }

        private sealed class WaitInstructionTestScope : IDisposable
        {
            private readonly WaitInstructionCacheSnapshot<WaitForSeconds> _waitForSecondsSnapshot;
            private readonly WaitInstructionCacheSnapshot<WaitForSecondsRealtime> _waitForSecondsRealtimeSnapshot;
            private readonly float _quantizationStepSnapshot;
            private readonly int _maxDistinctEntriesSnapshot;
            private readonly bool _useLruSnapshot;
            private readonly int _waitForSecondsLimitHitsSnapshot;
            private readonly int _waitForSecondsRealtimeLimitHitsSnapshot;
            private readonly int _waitForSecondsEvictionsSnapshot;
            private readonly int _waitForSecondsRealtimeEvictionsSnapshot;
            private bool _disposed;

            internal WaitInstructionTestScope()
            {
                _waitForSecondsSnapshot = SnapshotCache(WaitForSeconds, WaitForSecondsOrder);
                _waitForSecondsRealtimeSnapshot = SnapshotCache(
                    WaitForSecondsRealtime,
                    WaitForSecondsRealtimeOrder
                );
                _quantizationStepSnapshot = WaitInstructionQuantizationStepSeconds;
                _maxDistinctEntriesSnapshot = WaitInstructionMaxDistinctEntries;
                _useLruSnapshot = WaitInstructionUseLruEviction;
                _waitForSecondsLimitHitsSnapshot = Volatile.Read(ref _waitForSecondsLimitHits);
                _waitForSecondsRealtimeLimitHitsSnapshot = Volatile.Read(
                    ref _waitForSecondsRealtimeLimitHits
                );
                _waitForSecondsEvictionsSnapshot = Volatile.Read(ref _waitForSecondsEvictions);
                _waitForSecondsRealtimeEvictionsSnapshot = Volatile.Read(
                    ref _waitForSecondsRealtimeEvictions
                );

                ResetWaitInstructionCachesForTesting();
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;

                WaitInstructionQuantizationStepSeconds = _quantizationStepSnapshot;
                WaitInstructionMaxDistinctEntries = _maxDistinctEntriesSnapshot;
                WaitInstructionUseLruEviction = _useLruSnapshot;
                Volatile.Write(ref _waitForSecondsLimitHits, _waitForSecondsLimitHitsSnapshot);
                Volatile.Write(
                    ref _waitForSecondsRealtimeLimitHits,
                    _waitForSecondsRealtimeLimitHitsSnapshot
                );
                Volatile.Write(ref _waitForSecondsEvictions, _waitForSecondsEvictionsSnapshot);
                Volatile.Write(
                    ref _waitForSecondsRealtimeEvictions,
                    _waitForSecondsRealtimeEvictionsSnapshot
                );

                RestoreCache(WaitForSeconds, WaitForSecondsOrder, _waitForSecondsSnapshot);
                RestoreCache(
                    WaitForSecondsRealtime,
                    WaitForSecondsRealtimeOrder,
                    _waitForSecondsRealtimeSnapshot
                );
            }

            private static WaitInstructionCacheSnapshot<TInstruction> SnapshotCache<TInstruction>(
                Dictionary<float, WaitInstructionCacheEntry<TInstruction>> cache,
                LinkedList<float> order
            )
                where TInstruction : class
            {
#if !SINGLE_THREADED
                lock (WaitInstructionCacheLock)
                {
#endif
                    Dictionary<float, TInstruction> entries = new(cache.Count);
                    foreach (
                        KeyValuePair<float, WaitInstructionCacheEntry<TInstruction>> pair in cache
                    )
                    {
                        entries[pair.Key] = pair.Value._value;
                    }

                    List<float> ordering = new(order);
                    return new WaitInstructionCacheSnapshot<TInstruction>(entries, ordering);
#if !SINGLE_THREADED
                }
#endif
            }

            private static void RestoreCache<TInstruction>(
                Dictionary<float, WaitInstructionCacheEntry<TInstruction>> cache,
                LinkedList<float> order,
                WaitInstructionCacheSnapshot<TInstruction> snapshot
            )
                where TInstruction : class
            {
#if !SINGLE_THREADED
                lock (WaitInstructionCacheLock)
                {
#endif
                    cache.Clear();
                    order.Clear();

                    if (snapshot.Order == null || snapshot.Entries == null)
                    {
                        return;
                    }

                    Dictionary<float, LinkedListNode<float>> nodes = new(snapshot.Order.Count);
                    foreach (float key in snapshot.Order)
                    {
                        LinkedListNode<float> node = order.AddLast(key);
                        nodes[key] = node;
                    }

                    foreach (KeyValuePair<float, TInstruction> pair in snapshot.Entries)
                    {
                        if (!nodes.TryGetValue(pair.Key, out LinkedListNode<float> node))
                        {
                            node = order.AddLast(pair.Key);
                            nodes[pair.Key] = node;
                        }

                        cache[pair.Key] = new WaitInstructionCacheEntry<TInstruction>(
                            pair.Value,
                            node
                        );
                    }
#if !SINGLE_THREADED
                }
#endif
            }

            private readonly struct WaitInstructionCacheSnapshot<TInstruction>
                where TInstruction : class
            {
                internal WaitInstructionCacheSnapshot(
                    Dictionary<float, TInstruction> entries,
                    List<float> order
                )
                {
                    Entries = entries;
                    Order = order;
                }

                internal Dictionary<float, TInstruction> Entries { get; }

                internal List<float> Order { get; }
            }
        }
    }

    public readonly struct WaitInstructionCacheDiagnostics
    {
        public WaitInstructionCacheDiagnostics(
            string cacheName,
            int distinctEntries,
            int maxDistinctEntries,
            int limitRefusals,
            int evictions,
            float quantizationStepSeconds,
            bool lruEnabled
        )
        {
            CacheName = cacheName;
            DistinctEntries = distinctEntries;
            MaxDistinctEntries = maxDistinctEntries;
            LimitRefusals = limitRefusals;
            Evictions = evictions;
            QuantizationStepSeconds = quantizationStepSeconds;
            IsLruEnabled = lruEnabled;
        }

        public string CacheName { get; }

        public int DistinctEntries { get; }

        public int MaxDistinctEntries { get; }

        public int LimitRefusals { get; }

        public int Evictions { get; }

        public float QuantizationStepSeconds { get; }

        public bool IsQuantized => QuantizationStepSeconds > 0f;

        public bool IsLruEnabled { get; }

        public override string ToString()
        {
            return $"{CacheName}: entries={DistinctEntries}, max={MaxDistinctEntries}, refusals={LimitRefusals}, evictions={Evictions}, quantizationStep={QuantizationStepSeconds}, lru={IsLruEnabled}";
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

    /// <summary>
    /// A wrapper around <see cref="System.Buffers.ArrayPool{T}"/> that provides a Wallstop-style
    /// auto-disposal pattern using <see cref="PooledArray{T}"/>.
    /// </summary>
    /// <typeparam name="T">The element type for the arrays.</typeparam>
    /// <remarks>
    /// <para>
    /// <strong>Key Difference from <see cref="WallstopArrayPool{T}"/>:</strong>
    /// This pool uses .NET's <see cref="System.Buffers.ArrayPool{T}.Shared"/> which returns arrays
    /// that may be <em>larger</em> than the requested size (typically rounded up to a power of 2).
    /// Callers MUST use the <see cref="PooledArray{T}.length"/> property (which returns the
    /// originally requested length) instead of accessing the underlying array's Length directly.
    /// </para>
    /// <para>
    /// <strong>When to use this pool:</strong>
    /// Use <see cref="SystemArrayPool{T}"/> when array sizes vary widely or unpredictably (e.g., user input,
    /// collection sizes, dynamically computed sizes). The shared pool handles size bucketing efficiently,
    /// reducing memory fragmentation for variable-size workloads.
    /// </para>
    /// <para>
    /// <strong>When to use <see cref="WallstopArrayPool{T}"/> instead:</strong>
    /// Use <see cref="WallstopArrayPool{T}"/> when array sizes are fixed or highly predictable (e.g., internal
    /// PRNG state buffers, algorithm-constant sizes). This avoids the overhead of size bucketing when you
    /// always request the same size.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Correct usage - use lease.Length, not lease.Array.Length
    /// using PooledArray&lt;int&gt; lease = SystemArrayPool&lt;int&gt;.Get(count, out int[] buffer);
    /// for (int i = 0; i &lt; lease.Length; i++)  // ✓ Use lease.Length
    /// {
    ///     buffer[i] = ProcessItem(i);
    /// }
    ///
    /// // WRONG - buffer.Length may be larger than requested
    /// for (int i = 0; i &lt; buffer.Length; i++)  // ✗ May iterate past valid data
    /// {
    ///     ...
    /// }
    /// </code>
    /// </example>
    public static class SystemArrayPool<T>
    {
        /// <summary>
        /// Gets a pooled array of at least the specified size. When disposed, the array is returned to the pool.
        /// </summary>
        /// <param name="minimumLength">The minimum size of the array to retrieve. Must be non-negative.</param>
        /// <returns>A <see cref="PooledArray{T}"/> wrapping an array of at least the specified size.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when minimumLength is negative.</exception>
        /// <remarks>
        /// The returned array may be larger than <paramref name="minimumLength"/>. Always use
        /// <see cref="PooledArray{T}.length"/> to determine the valid portion of the array.
        /// </remarks>
        public static PooledArray<T> Get(int minimumLength)
        {
            return Get(minimumLength, out _);
        }

        /// <summary>
        /// Gets a pooled array of at least the specified size and outputs the array. When disposed, the array is returned to the pool.
        /// </summary>
        /// <param name="minimumLength">The minimum size of the array to retrieve. Must be non-negative.</param>
        /// <param name="array">The retrieved array. May be larger than <paramref name="minimumLength"/>.</param>
        /// <returns>A <see cref="PooledArray{T}"/> wrapping the array with proper length tracking.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when minimumLength is negative.</exception>
        /// <remarks>
        /// The returned array may be larger than <paramref name="minimumLength"/>. Always use
        /// <see cref="PooledArray{T}.length"/> (or the <paramref name="minimumLength"/> you passed in)
        /// to determine the valid portion of the array.
        /// </remarks>
        public static PooledArray<T> Get(int minimumLength, out T[] array)
        {
            if (minimumLength < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumLength),
                    minimumLength,
                    "Must be non-negative."
                );
            }

            if (minimumLength == 0)
            {
                array = Array.Empty<T>();
                return new PooledArray<T>(array, 0);
            }

            array = System.Buffers.ArrayPool<T>.Shared.Rent(minimumLength);
            return new PooledArray<T>(array, minimumLength);
        }

        /// <summary>
        /// Gets a pooled array of at least the specified size with optional clearing. When disposed, the array is returned to the pool.
        /// </summary>
        /// <param name="minimumLength">The minimum size of the array to retrieve. Must be non-negative.</param>
        /// <param name="clearArray">If true, the array is cleared to default values before being returned.</param>
        /// <param name="array">The retrieved array. May be larger than <paramref name="minimumLength"/>.</param>
        /// <returns>A <see cref="PooledArray{T}"/> wrapping the array with proper length tracking.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when minimumLength is negative.</exception>
        /// <remarks>
        /// <para>
        /// When <paramref name="clearArray"/> is true, only the portion of the array up to
        /// <paramref name="minimumLength"/> is guaranteed to be cleared. The remainder of the
        /// array (if any) may contain stale data.
        /// </para>
        /// <para>
        /// For security-sensitive scenarios or when using reference types, consider always setting
        /// <paramref name="clearArray"/> to true to prevent data leakage between uses.
        /// </para>
        /// </remarks>
        public static PooledArray<T> Get(int minimumLength, bool clearArray, out T[] array)
        {
            if (minimumLength < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumLength),
                    minimumLength,
                    "Must be non-negative."
                );
            }

            if (minimumLength == 0)
            {
                array = Array.Empty<T>();
                return new PooledArray<T>(array, 0);
            }

            array = System.Buffers.ArrayPool<T>.Shared.Rent(minimumLength);
            if (clearArray)
            {
                Array.Clear(array, 0, minimumLength);
            }
            return new PooledArray<T>(array, minimumLength);
        }
    }

    /// <summary>
    /// A struct that wraps a pooled array and automatically returns it to the pool when disposed.
    /// This struct provides a unified return type for all array pools (<see cref="SystemArrayPool{T}"/>,
    /// <see cref="WallstopArrayPool{T}"/>, and <see cref="WallstopFastArrayPool{T}"/>).
    /// </summary>
    /// <typeparam name="T">The element type for the array.</typeparam>
    /// <remarks>
    /// <para>
    /// <strong>Important:</strong> The underlying <see cref="array"/> may be larger than the
    /// requested size (especially when using <see cref="SystemArrayPool{T}"/>). Always use
    /// <see cref="length"/> to determine the valid portion of the array.
    /// </para>
    /// <para>
    /// This struct implements <see cref="IDisposable"/> to enable automatic resource return via
    /// 'using' statements. The array is returned to the pool when <see cref="Dispose"/> is called.
    /// </para>
    /// <para>
    /// <strong>Warning:</strong> Do NOT use <c>foreach</c> on pooled arrays since
    /// <see cref="array"/>.Length may exceed <see cref="length"/>. Use indexed iteration instead.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Using with SystemArrayPool (array may be larger than requested)
    /// using PooledArray&lt;int&gt; pooled = SystemArrayPool&lt;int&gt;.Get(100, out int[] array);
    /// for (int i = 0; i &lt; pooled.Length; i++) // Use pooled.Length, not array.Length
    /// {
    ///     array[i] = i * 2;
    /// }
    ///
    /// // Using with WallstopArrayPool (array is exact size)
    /// using PooledArray&lt;int&gt; pooled2 = WallstopArrayPool&lt;int&gt;.Get(100, out int[] buffer);
    /// for (int i = 0; i &lt; pooled2.Length; i++)
    /// {
    ///     buffer[i] = i * 3;
    /// }
    /// </code>
    /// </example>
    public struct PooledArray<T> : IDisposable
    {
        /// <summary>
        /// The underlying pooled array. May be larger than <see cref="length"/> when using
        /// <see cref="SystemArrayPool{T}"/> (due to power-of-2 bucketing), or exactly equal
        /// to <see cref="length"/> when using <see cref="WallstopArrayPool{T}"/> or
        /// <see cref="WallstopFastArrayPool{T}"/>.
        /// </summary>
        public readonly T[] array;

        /// <summary>
        /// The originally requested length. Use this instead of <see cref="array"/>.Length
        /// to determine the valid portion of the array.
        /// </summary>
        public readonly int length;

        private readonly Action<T[]> _onDispose;
        private bool _disposed;

        /// <summary>
        /// Creates a new <see cref="PooledArray{T}"/> wrapping the specified array with the given logical length.
        /// Uses the default disposal action that returns the array to <see cref="System.Buffers.ArrayPool{T}.Shared"/>.
        /// </summary>
        /// <param name="array">The pooled array.</param>
        /// <param name="length">The logical length (originally requested size).</param>
        internal PooledArray(T[] array, int length)
            : this(array, length, null) { }

        /// <summary>
        /// Creates a new <see cref="PooledArray{T}"/> wrapping the specified array with the given logical length
        /// and custom disposal action.
        /// </summary>
        /// <param name="array">The pooled array.</param>
        /// <param name="length">The logical length (originally requested size).</param>
        /// <param name="onDispose">The action to invoke when disposing. If null, uses the default
        /// <see cref="System.Buffers.ArrayPool{T}.Shared"/> return logic.</param>
        internal PooledArray(T[] array, int length, Action<T[]> onDispose)
        {
            this.array = array;
            this.length = length;
            _onDispose = onDispose;
            _disposed = false;
        }

        /// <summary>
        /// Returns the array to the pool. The clearing behavior depends on which pool the array came from:
        /// <list type="bullet">
        /// <item><see cref="SystemArrayPool{T}"/>: Array is NOT cleared by default</item>
        /// <item><see cref="WallstopArrayPool{T}"/>: Array IS cleared on return</item>
        /// <item><see cref="WallstopFastArrayPool{T}"/>: Array is NOT cleared (for performance)</item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// After disposal, the array should not be used as it may be reused by another caller.
        /// For reference types when using <see cref="SystemArrayPool{T}"/>, consider using
        /// <see cref="SystemArrayPool{T}.Get(int, bool, out T[])"/> with clearArray=true
        /// to prevent data leakage.
        /// </remarks>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            if (array == null || array.Length == 0)
            {
                return;
            }

            if (_onDispose != null)
            {
                _onDispose(array);
            }
            else
            {
                System.Buffers.ArrayPool<T>.Shared.Return(array, clearArray: true);
            }
        }
    }

#if SINGLE_THREADED
    /// <summary>
    /// A static array pool that provides pooled arrays of specific sizes.
    /// Arrays are cleared (set to default values) when returned to the pool.
    /// This single-threaded implementation uses Dictionary and List for storage.
    /// </summary>
    /// <typeparam name="T">The element type for the arrays.</typeparam>
    /// <remarks>
    /// <para>
    /// Unlike <see cref="SystemArrayPool{T}"/>, this pool returns arrays of the <em>exact</em> requested size,
    /// making it ideal for fixed-size or predictable-size scenarios where memory efficiency is important.
    /// </para>
    /// <para>
    /// Arrays are automatically cleared when returned to the pool to prevent data leakage.
    /// </para>
    /// <para>
    /// <strong>⚠️ MEMORY LEAK WARNING:</strong> This pool creates a separate pool bucket for EVERY unique
    /// size requested. If you pass variable sizes (user input, collection.Count, dynamic values), each unique
    /// size creates a new bucket that persists forever, causing unbounded memory growth.
    /// </para>
    /// <para>
    /// <strong>SAFE uses:</strong>
    /// <list type="bullet">
    ///   <item><description>Compile-time constants: <c>Get(16)</c>, <c>Get(64)</c>, <c>Get(256)</c></description></item>
    ///   <item><description>Algorithm-bounded sizes with small fixed upper limits</description></item>
    ///   <item><description>PRNG internal state buffers (fixed sizes like 16, 32, 64 bytes)</description></item>
    ///   <item><description>Sizes from a small, known set of values</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>UNSAFE uses (will leak memory):</strong>
    /// <list type="bullet">
    ///   <item><description><c>Get(userInput)</c> — Every unique user value creates a permanent bucket</description></item>
    ///   <item><description><c>Get(collection.Count)</c> — Every unique collection size leaks memory</description></item>
    ///   <item><description><c>Get(random.Next(1, 1000))</c> — Creates up to 1000 permanent buckets</description></item>
    ///   <item><description><c>Get(dynamicCalculation)</c> — Unbounded sizes = unbounded memory</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Rule of thumb:</strong> If you cannot enumerate ALL possible sizes at compile time,
    /// use <see cref="SystemArrayPool{T}"/> instead.
    /// </para>
    /// </remarks>
    public static class WallstopArrayPool<T>
    {
        private static readonly Dictionary<int, List<T[]>> Pool = new();
        private static readonly Action<T[]> OnDispose = Release;

        /// <summary>
        /// Gets a pooled array of the specified size. When disposed, the array is cleared and returned to the pool.
        /// </summary>
        /// <param name="size">The size of the array to retrieve. Must be non-negative.</param>
        /// <returns>A <see cref="PooledArray{T}"/> wrapping an array of the exact specified size.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when size is negative.</exception>
        public static PooledArray<T> Get(int size)
        {
            return Get(size, out _);
        }

        /// <summary>
        /// Gets a pooled array of the specified size and outputs the value. When disposed, the array is cleared and returned to the pool.
        /// </summary>
        /// <param name="size">The size of the array to retrieve. Must be non-negative.</param>
        /// <param name="array">The retrieved array. Will be exactly <paramref name="size"/> elements.</param>
        /// <returns>A <see cref="PooledArray{T}"/> wrapping an array of the exact specified size.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when size is negative.</exception>
        public static PooledArray<T> Get(int size, out T[] array)
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
                    array = Array.Empty<T>();
                    return new PooledArray<T>(array, 0, null);
                }
            }

            List<T[]> pool = Pool.GetOrAdd(size);

            if (pool.Count == 0)
            {
                array = new T[size];
                return new PooledArray<T>(array, size, OnDispose);
            }

            int lastIndex = pool.Count - 1;
            array = pool[lastIndex];
            pool.RemoveAt(lastIndex);
            return new PooledArray<T>(array, size, OnDispose);
        }

        private static void Release(T[] resource)
        {
            int length = resource.Length;
            Array.Clear(resource, 0, length);
            List<T[]> pool = Pool.GetOrAdd(length);
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
    /// <remarks>
    /// <para>
    /// Unlike <see cref="SystemArrayPool{T}"/>, this pool returns arrays of the <em>exact</em> requested size,
    /// making it ideal for fixed-size or predictable-size scenarios where memory efficiency is important.
    /// </para>
    /// <para>
    /// Arrays are automatically cleared when returned to the pool to prevent data leakage.
    /// </para>
    /// <para>
    /// <strong>⚠️ MEMORY LEAK WARNING:</strong> This pool creates a separate pool bucket for EVERY unique
    /// size requested. If you pass variable sizes (user input, collection.Count, dynamic values), each unique
    /// size creates a new bucket that persists forever, causing unbounded memory growth.
    /// </para>
    /// <para>
    /// <strong>SAFE uses:</strong>
    /// <list type="bullet">
    ///   <item><description>Compile-time constants: <c>Get(16)</c>, <c>Get(64)</c>, <c>Get(256)</c></description></item>
    ///   <item><description>Algorithm-bounded sizes with small fixed upper limits</description></item>
    ///   <item><description>PRNG internal state buffers (fixed sizes like 16, 32, 64 bytes)</description></item>
    ///   <item><description>Sizes from a small, known set of values</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>UNSAFE uses (will leak memory):</strong>
    /// <list type="bullet">
    ///   <item><description><c>Get(userInput)</c> — Every unique user value creates a permanent bucket</description></item>
    ///   <item><description><c>Get(collection.Count)</c> — Every unique collection size leaks memory</description></item>
    ///   <item><description><c>Get(random.Next(1, 1000))</c> — Creates up to 1000 permanent buckets</description></item>
    ///   <item><description><c>Get(dynamicCalculation)</c> — Unbounded sizes = unbounded memory</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Rule of thumb:</strong> If you cannot enumerate ALL possible sizes at compile time,
    /// use <see cref="SystemArrayPool{T}"/> instead.
    /// </para>
    /// </remarks>
    public static class WallstopArrayPool<T>
    {
        private static readonly ConcurrentDictionary<int, ConcurrentStack<T[]>> _pool = new();
        private static readonly Action<T[]> _onRelease = Release;

        /// <summary>
        /// Gets a pooled array of the specified size. When disposed, the array is cleared and returned to the pool.
        /// This method is thread-safe.
        /// </summary>
        /// <param name="size">The size of the array to retrieve. Must be non-negative.</param>
        /// <returns>A <see cref="PooledArray{T}"/> wrapping an array of the exact specified size.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when size is negative.</exception>
        public static PooledArray<T> Get(int size)
        {
            return Get(size, out _);
        }

        /// <summary>
        /// Gets a pooled array of the specified size and outputs the value. When disposed, the array is cleared and returned to the pool.
        /// This method is thread-safe.
        /// </summary>
        /// <param name="size">The size of the array to retrieve. Must be non-negative.</param>
        /// <param name="array">The retrieved array. Will be exactly <paramref name="size"/> elements.</param>
        /// <returns>A <see cref="PooledArray{T}"/> wrapping an array of the exact specified size.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when size is negative.</exception>
        public static PooledArray<T> Get(int size, out T[] array)
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
                    array = Array.Empty<T>();
                    return new PooledArray<T>(array, 0, null);
                }
            }

            ConcurrentStack<T[]> result = _pool.GetOrAdd(size, _ => new ConcurrentStack<T[]>());
            if (!result.TryPop(out array))
            {
                array = new T[size];
            }

            return new PooledArray<T>(array, size, _onRelease);
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
    /// <typeparam name="T">The element type for the arrays. Must be an unmanaged type.</typeparam>
    /// <remarks>
    /// <para>
    /// <strong>Warning:</strong> This pool does NOT clear arrays on release. Arrays may contain
    /// data from previous uses. Only use this pool when you will overwrite all array contents
    /// before reading, or when stale data is acceptable.
    /// </para>
    /// <para>
    /// Unlike <see cref="SystemArrayPool{T}"/>, this pool returns arrays of the <em>exact</em> requested size.
    /// </para>
    /// <para>
    /// <strong>⚠️ MEMORY LEAK WARNING:</strong> This pool creates a separate pool bucket for EVERY unique
    /// size requested. If you pass variable sizes (user input, collection.Count, dynamic values), each unique
    /// size creates a new bucket that persists forever, causing unbounded memory growth.
    /// </para>
    /// <para>
    /// <strong>SAFE uses:</strong>
    /// <list type="bullet">
    ///   <item><description>Compile-time constants: <c>Get(16)</c>, <c>Get(64)</c>, <c>Get(256)</c></description></item>
    ///   <item><description>Algorithm-bounded sizes with small fixed upper limits</description></item>
    ///   <item><description>PRNG internal state buffers (fixed sizes like 16, 32, 64 bytes)</description></item>
    ///   <item><description>Sizes from a small, known set of values</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>UNSAFE uses (will leak memory):</strong>
    /// <list type="bullet">
    ///   <item><description><c>Get(userInput)</c> — Every unique user value creates a permanent bucket</description></item>
    ///   <item><description><c>Get(collection.Count)</c> — Every unique collection size leaks memory</description></item>
    ///   <item><description><c>Get(random.Next(1, 1000))</c> — Creates up to 1000 permanent buckets</description></item>
    ///   <item><description><c>Get(dynamicCalculation)</c> — Unbounded sizes = unbounded memory</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Rule of thumb:</strong> If you cannot enumerate ALL possible sizes at compile time,
    /// use <see cref="SystemArrayPool{T}"/> instead.
    /// </para>
    /// </remarks>
    public static class WallstopFastArrayPool<T>
        where T : unmanaged
    {
        private static readonly List<Stack<T[]>> Pool = new();
        private static readonly Action<T[]> OnRelease = Release;

        /// <summary>
        /// Gets a pooled array of the specified size. When disposed, the array is returned to the pool WITHOUT being cleared.
        /// </summary>
        /// <param name="size">The size of the array to retrieve. Must be non-negative.</param>
        /// <returns>A <see cref="PooledArray{T}"/> wrapping an array of the exact specified size.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when size is negative.</exception>
        /// <remarks>Arrays are NOT cleared on return. The caller is responsible for clearing if needed.</remarks>
        public static PooledArray<T> Get(int size)
        {
            return Get(size, out _);
        }

        /// <summary>
        /// Gets a pooled array of the specified size and outputs the value. When disposed, the array is returned to the pool WITHOUT being cleared.
        /// </summary>
        /// <param name="size">The size of the array to retrieve. Must be non-negative.</param>
        /// <param name="array">The retrieved array. Will be exactly <paramref name="size"/> elements.</param>
        /// <returns>A <see cref="PooledArray{T}"/> wrapping an array of the exact specified size.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when size is negative.</exception>
        /// <remarks>Arrays are NOT cleared on return. The caller is responsible for clearing if needed.</remarks>
        public static PooledArray<T> Get(int size, out T[] array)
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
                    array = Array.Empty<T>();
                    return new PooledArray<T>(array, 0, null);
                }
            }

            while (Pool.Count <= size)
            {
                Pool.Add(null);
            }

            Stack<T[]> pool = Pool[size];
            if (pool == null)
            {
                pool = new Stack<T[]>();
                Pool[size] = pool;
            }

            if (!pool.TryPop(out array))
            {
                array = new T[size];
            }

            return new PooledArray<T>(array, size, OnRelease);
        }

        private static void Release(T[] resource)
        {
            Pool[resource.Length].Push(resource);
        }

        /// <summary>
        /// Clears all pooled arrays for testing purposes. Internal visibility for test assemblies.
        /// </summary>
        internal static void ClearForTesting()
        {
            for (int i = 0; i < Pool.Count; i++)
            {
                Pool[i]?.Clear();
            }
        }
    }
#else
    /// <summary>
    /// A thread-safe fast static array pool optimized for index-based lookup with minimal overhead.
    /// Unlike WallstopArrayPool, arrays are NOT cleared when returned to the pool, providing better performance.
    /// This multi-threaded implementation uses a List of ConcurrentStacks with ReaderWriterLockSlim for thread-safe index access.
    /// </summary>
    /// <typeparam name="T">The element type for the arrays. Must be an unmanaged type.</typeparam>
    /// <remarks>
    /// <para>
    /// <strong>Warning:</strong> This pool does NOT clear arrays on release. Arrays may contain
    /// data from previous uses. Only use this pool when you will overwrite all array contents
    /// before reading, or when stale data is acceptable.
    /// </para>
    /// <para>
    /// Unlike <see cref="SystemArrayPool{T}"/>, this pool returns arrays of the <em>exact</em> requested size.
    /// </para>
    /// <para>
    /// <strong>⚠️ MEMORY LEAK WARNING:</strong> This pool creates a separate pool bucket for EVERY unique
    /// size requested. If you pass variable sizes (user input, collection.Count, dynamic values), each unique
    /// size creates a new bucket that persists forever, causing unbounded memory growth.
    /// </para>
    /// <para>
    /// <strong>SAFE uses:</strong>
    /// <list type="bullet">
    ///   <item><description>Compile-time constants: <c>Get(16)</c>, <c>Get(64)</c>, <c>Get(256)</c></description></item>
    ///   <item><description>Algorithm-bounded sizes with small fixed upper limits</description></item>
    ///   <item><description>PRNG internal state buffers (fixed sizes like 16, 32, 64 bytes)</description></item>
    ///   <item><description>Sizes from a small, known set of values</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>UNSAFE uses (will leak memory):</strong>
    /// <list type="bullet">
    ///   <item><description><c>Get(userInput)</c> — Every unique user value creates a permanent bucket</description></item>
    ///   <item><description><c>Get(collection.Count)</c> — Every unique collection size leaks memory</description></item>
    ///   <item><description><c>Get(random.Next(1, 1000))</c> — Creates up to 1000 permanent buckets</description></item>
    ///   <item><description><c>Get(dynamicCalculation)</c> — Unbounded sizes = unbounded memory</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Rule of thumb:</strong> If you cannot enumerate ALL possible sizes at compile time,
    /// use <see cref="SystemArrayPool{T}"/> instead.
    /// </para>
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
        /// <returns>A <see cref="PooledArray{T}"/> wrapping an array of the exact specified size.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when size is negative.</exception>
        /// <remarks>Arrays are NOT cleared on return. The caller is responsible for clearing if needed.</remarks>
        public static PooledArray<T> Get(int size)
        {
            return Get(size, out _);
        }

        /// <summary>
        /// Gets a pooled array of the specified size and outputs the value. When disposed, the array is returned to the pool WITHOUT being cleared.
        /// This method is thread-safe.
        /// </summary>
        /// <param name="size">The size of the array to retrieve. Must be non-negative.</param>
        /// <param name="array">The retrieved array. Will be exactly <paramref name="size"/> elements.</param>
        /// <returns>A <see cref="PooledArray{T}"/> wrapping an array of the exact specified size.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when size is negative.</exception>
        /// <remarks>Arrays are NOT cleared on return. The caller is responsible for clearing if needed.</remarks>
        public static PooledArray<T> Get(int size, out T[] array)
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
                    array = Array.Empty<T>();
                    return new PooledArray<T>(array, 0, null);
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

            if (!pool.TryPop(out array))
            {
                array = new T[size];
            }

            return new PooledArray<T>(array, size, _onRelease);
        }

        private static void Release(T[] resource)
        {
            _pool[resource.Length].Push(resource);
        }

        /// <summary>
        /// Clears all pooled arrays for testing purposes. Internal visibility for test assemblies.
        /// Thread-safe implementation.
        /// </summary>
        internal static void ClearForTesting()
        {
            _lock.EnterWriteLock();
            try
            {
                for (int i = 0; i < _pool.Count; i++)
                {
                    _pool[i]?.Clear();
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
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
