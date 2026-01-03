// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Configuration options for constructing a <see cref="Cache{TKey,TValue}"/>.
    /// This is a value type to avoid allocations. Reference-type properties (delegates)
    /// are stored as references within the struct.
    /// Use <see cref="CacheBuilder{TKey,TValue}"/> for a fluent API.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
    /// <typeparam name="TValue">The type of values in the cache.</typeparam>
    public struct CacheOptions<TKey, TValue>
    {
        /// <summary>
        /// Default maximum number of entries in a cache.
        /// </summary>
        public const int DefaultMaximumSize = 1000;

        /// <summary>
        /// Default value indicating no time-based expiration.
        /// </summary>
        public const float DefaultExpireAfterWriteSeconds = 0f;

        /// <summary>
        /// Default value indicating no sliding expiration.
        /// </summary>
        public const float DefaultExpireAfterAccessSeconds = 0f;

        /// <summary>
        /// Default eviction policy.
        /// </summary>
        public const EvictionPolicy DefaultEvictionPolicy = EvictionPolicy.Lru;

        /// <summary>
        /// Default ratio of protected segment for SLRU (80%).
        /// </summary>
        public const float DefaultProtectedRatio = 0.8f;

        /// <summary>
        /// Default growth factor when dynamic sizing is enabled.
        /// </summary>
        public const float DefaultGrowthFactor = 1.5f;

        /// <summary>
        /// Default evictions per second threshold for thrash detection.
        /// </summary>
        public const float DefaultThrashThreshold = 100f;

        /// <summary>
        /// The maximum number of entries the cache can hold before eviction occurs.
        /// If zero, <see cref="DefaultMaximumSize"/> is used.
        /// </summary>
        public int MaximumSize;

        /// <summary>
        /// The maximum total weight of all cache entries.
        /// Only used when <see cref="Weigher"/> is specified.
        /// </summary>
        public long MaximumWeight;

        /// <summary>
        /// Function to compute the weight of a cache entry.
        /// When specified, <see cref="MaximumWeight"/> is used instead of <see cref="MaximumSize"/>.
        /// </summary>
        public Func<TKey, TValue, long> Weigher;

        /// <summary>
        /// Time in seconds after which an entry expires following its creation or last update.
        /// A value of 0 or less disables write-based expiration.
        /// </summary>
        public float ExpireAfterWriteSeconds;

        /// <summary>
        /// Time in seconds after which an entry expires following its last access (sliding window).
        /// A value of 0 or less disables access-based expiration.
        /// </summary>
        public float ExpireAfterAccessSeconds;

        /// <summary>
        /// Function to compute a custom expiration time (in seconds) for each entry.
        /// Takes precedence over <see cref="ExpireAfterWriteSeconds"/> when specified.
        /// </summary>
        public Func<TKey, TValue, float> ExpireAfter;

        /// <summary>
        /// When true, adds a random jitter to expiration times to prevent thundering herd.
        /// </summary>
        public bool UseJitter;

        /// <summary>
        /// Maximum jitter in seconds to add to expiration times.
        /// If zero, defaults to 10% of the TTL.
        /// </summary>
        public float JitterMaxSeconds;

        /// <summary>
        /// The eviction algorithm to use when the cache reaches capacity.
        /// </summary>
        public EvictionPolicy Policy;

        /// <summary>
        /// For <see cref="EvictionPolicy.Slru"/>, the ratio of the cache dedicated to the protected segment.
        /// Value should be between 0 and 1. Default is 0.8 (80% protected, 20% probation).
        /// </summary>
        public float ProtectedRatio;

        /// <summary>
        /// When true, allows the cache to grow beyond <see cref="MaximumSize"/> when thrashing is detected.
        /// </summary>
        public bool AllowGrowth;

        /// <summary>
        /// The factor by which to grow the cache when thrashing is detected.
        /// </summary>
        public float GrowthFactor;

        /// <summary>
        /// The maximum size the cache can grow to when dynamic sizing is enabled.
        /// If zero, growth is unbounded.
        /// </summary>
        public int MaxGrowthSize;

        /// <summary>
        /// The evictions per second threshold above which the cache is considered thrashing.
        /// </summary>
        public float ThrashThresholdEvictionsPerSecond;

        /// <summary>
        /// Callback invoked when an entry is evicted from the cache.
        /// </summary>
        public Action<TKey, TValue, EvictionReason> OnEviction;

        /// <summary>
        /// Callback invoked when an entry is retrieved from the cache (hit).
        /// </summary>
        public Action<TKey, TValue> OnGet;

        /// <summary>
        /// Callback invoked when an entry is added or updated in the cache.
        /// </summary>
        public Action<TKey, TValue> OnSet;

        /// <summary>
        /// When true, the cache records statistics accessible via <see cref="Cache{TKey,TValue}.GetStatistics"/>.
        /// </summary>
        public bool RecordStatistics;

        /// <summary>
        /// Function that returns the current time in seconds.
        /// Defaults to <see cref="Time.realtimeSinceStartup"/> when null.
        /// </summary>
        public Func<float> TimeProvider;

        /// <summary>
        /// Factory function used to load values when they are not present in the cache.
        /// When specified, <see cref="Cache{TKey,TValue}.GetOrAdd(TKey, Func{TKey, TValue})"/>
        /// uses this loader if no explicit factory is provided.
        /// </summary>
        public Func<TKey, TValue> Loader;

        /// <summary>
        /// Creates a new options instance with default values.
        /// </summary>
        /// <returns>A new <see cref="CacheOptions{TKey,TValue}"/> with defaults.</returns>
        public static CacheOptions<TKey, TValue> Default()
        {
            return new CacheOptions<TKey, TValue>
            {
                MaximumSize = DefaultMaximumSize,
                ExpireAfterWriteSeconds = DefaultExpireAfterWriteSeconds,
                ExpireAfterAccessSeconds = DefaultExpireAfterAccessSeconds,
                Policy = DefaultEvictionPolicy,
                ProtectedRatio = DefaultProtectedRatio,
                GrowthFactor = DefaultGrowthFactor,
                ThrashThresholdEvictionsPerSecond = DefaultThrashThreshold,
            };
        }
    }
}
