// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;

    /// <summary>
    /// Specifies the eviction algorithm used by a <see cref="Cache{TKey,TValue}"/> to determine
    /// which entries to remove when the cache reaches capacity.
    /// </summary>
    /// <remarks>
    /// Different policies offer trade-offs between hit rate, implementation complexity, and memory overhead:
    /// <list type="bullet">
    ///   <item><description><see cref="Lru"/>: Good general-purpose choice, O(1) operations, low memory overhead.</description></item>
    ///   <item><description><see cref="Slru"/>: Better hit rate for mixed workloads, protects frequently-accessed items.</description></item>
    ///   <item><description><see cref="Lfu"/>: Optimal for stable access patterns, evicts least-frequently-used items.</description></item>
    ///   <item><description><see cref="Fifo"/>: Simplest policy, evicts in insertion order regardless of access.</description></item>
    ///   <item><description><see cref="Random"/>: Unpredictable eviction, useful for adversarial workloads.</description></item>
    /// </list>
    /// </remarks>
    public enum EvictionPolicy
    {
        /// <summary>
        /// Reserved for uninitialized state. Do not use directly.
        /// </summary>
        [Obsolete("Use Lru for standard least-recently-used eviction.")]
        None = 0,

        /// <summary>
        /// Least Recently Used: Evicts the entry that has not been accessed for the longest time.
        /// Provides O(1) get/set operations with low memory overhead.
        /// </summary>
        Lru = 1,

        /// <summary>
        /// Segmented LRU: Divides cache into probation and protected segments.
        /// Newly inserted items start in probation; accessing them promotes to protected.
        /// Eviction first targets probation, then demotes from protected.
        /// Better hit rate than LRU for mixed workloads.
        /// </summary>
        Slru = 2,

        /// <summary>
        /// Least Frequently Used: Evicts the entry with the lowest access count.
        /// Ties are broken by recency. Best for stable, predictable access patterns.
        /// </summary>
        Lfu = 3,

        /// <summary>
        /// First In First Out: Evicts entries in the order they were inserted.
        /// Access does not affect eviction order. Simplest implementation.
        /// </summary>
        Fifo = 4,

        /// <summary>
        /// Random: Evicts a randomly selected entry when capacity is exceeded.
        /// Useful for adversarial workloads or when access patterns are unpredictable.
        /// </summary>
        Random = 5,
    }
}
