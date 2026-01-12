// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    /// <summary>
    /// Provides factory methods for creating pre-configured <see cref="CacheBuilder{TKey,TValue}"/> instances
    /// optimized for common game development scenarios.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each method returns a <see cref="CacheBuilder{TKey,TValue}"/> that can be further customized
    /// before calling <see cref="CacheBuilder{TKey,TValue}.Build()"/> or
    /// <see cref="CacheBuilder{TKey,TValue}.Build(System.Func{TKey,TValue})"/>.
    /// </para>
    /// <para>
    /// These presets are designed to provide sensible defaults for typical use cases in Unity games,
    /// balancing memory usage, performance, and data freshness requirements.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code><![CDATA[
    /// // Create a short-lived cache for temporary computations
    /// Cache<int, Vector3> positionCache = CachePresets.ShortLived<int, Vector3>().Build();
    ///
    /// // Create a high-throughput cache with custom eviction callback
    /// Cache<string, PathResult> pathCache = CachePresets.HighThroughput<string, PathResult>()
    ///     .OnEviction((key, value, reason) => Debug.Log($"Path {key} evicted: {reason}"))
    ///     .Build();
    /// ]]></code>
    /// </example>
    public static class CachePresets
    {
        /// <summary>
        /// Creates a cache builder configured for short-lived, frequently-changing data.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This preset is ideal for frame-local computations, temporary lookups, and transient data
        /// that changes frequently and should not persist for long periods.
        /// </para>
        /// <para>
        /// Configuration:
        /// <list type="bullet">
        /// <item><description>Maximum Size: 100 entries</description></item>
        /// <item><description>Expiration: 60 seconds after write</description></item>
        /// <item><description>Eviction Policy: LRU (Least Recently Used)</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
        /// <typeparam name="TValue">The type of values in the cache.</typeparam>
        /// <returns>
        /// A <see cref="CacheBuilder{TKey,TValue}"/> configured for short-lived data that can be
        /// further customized before building.
        /// </returns>
        /// <example>
        /// <code><![CDATA[
        /// // Cache for temporary distance calculations
        /// Cache<(int, int), float> distanceCache = CachePresets.ShortLived<(int, int), float>().Build();
        ///
        /// // Cache with custom loader for frame-local data
        /// Cache<string, GameObject> tempObjects = CachePresets.ShortLived<string, GameObject>()
        ///     .Build(key => GameObject.Find(key));
        /// ]]></code>
        /// </example>
        public static CacheBuilder<TKey, TValue> ShortLived<TKey, TValue>()
        {
            CacheBuilder<TKey, TValue> builder = CacheBuilder<TKey, TValue>
                .NewBuilder()
                .MaximumSize(100)
                .ExpireAfterWrite(60f)
                .EvictionPolicy(EvictionPolicy.Lru);
            return builder;
        }

        /// <summary>
        /// Creates a cache builder configured for persistent data that rarely changes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This preset is ideal for asset references, prefab lookups, configuration data, and other
        /// long-lived content that should remain cached for the duration of the session.
        /// </para>
        /// <para>
        /// Configuration:
        /// <list type="bullet">
        /// <item><description>Maximum Size: 1000 entries</description></item>
        /// <item><description>Expiration: None (entries persist until evicted by size)</description></item>
        /// <item><description>Eviction Policy: LRU (Least Recently Used)</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
        /// <typeparam name="TValue">The type of values in the cache.</typeparam>
        /// <returns>
        /// A <see cref="CacheBuilder{TKey,TValue}"/> configured for long-lived data that can be
        /// further customized before building.
        /// </returns>
        /// <example>
        /// <code><![CDATA[
        /// // Cache for prefab references
        /// Cache<string, GameObject> prefabCache = CachePresets.LongLived<string, GameObject>()
        ///     .Build(key => Resources.Load<GameObject>($"Prefabs/{key}"));
        ///
        /// // Cache for configuration lookups
        /// Cache<int, EnemyConfig> enemyConfigs = CachePresets.LongLived<int, EnemyConfig>().Build();
        /// ]]></code>
        /// </example>
        public static CacheBuilder<TKey, TValue> LongLived<TKey, TValue>()
        {
            CacheBuilder<TKey, TValue> builder = CacheBuilder<TKey, TValue>
                .NewBuilder()
                .MaximumSize(1000)
                .ExpireAfterWrite(-1f)
                .EvictionPolicy(EvictionPolicy.Lru);
            return builder;
        }

        /// <summary>
        /// Creates a cache builder configured for player session data with sliding expiration.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This preset is ideal for player-specific data, gameplay state, inventory lookups, and
        /// other session-bound information that should expire based on access patterns rather than
        /// absolute time.
        /// </para>
        /// <para>
        /// Configuration:
        /// <list type="bullet">
        /// <item><description>Maximum Size: 500 entries</description></item>
        /// <item><description>Expiration: 30 minutes (1800 seconds) after last access (sliding window)</description></item>
        /// <item><description>Eviction Policy: LRU (Least Recently Used)</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
        /// <typeparam name="TValue">The type of values in the cache.</typeparam>
        /// <returns>
        /// A <see cref="CacheBuilder{TKey,TValue}"/> configured for session data that can be
        /// further customized before building.
        /// </returns>
        /// <example>
        /// <code><![CDATA[
        /// // Cache for player inventory data
        /// Cache<string, InventoryData> inventoryCache = CachePresets.SessionCache<string, InventoryData>().Build();
        ///
        /// // Cache for quest progress with custom maximum size
        /// Cache<int, QuestProgress> questCache = CachePresets.SessionCache<int, QuestProgress>()
        ///     .MaximumSize(100)
        ///     .Build();
        /// ]]></code>
        /// </example>
        public static CacheBuilder<TKey, TValue> SessionCache<TKey, TValue>()
        {
            CacheBuilder<TKey, TValue> builder = CacheBuilder<TKey, TValue>
                .NewBuilder()
                .MaximumSize(500)
                .ExpireAfterAccess(1800f)
                .EvictionPolicy(EvictionPolicy.Lru);
            return builder;
        }

        /// <summary>
        /// Creates a cache builder configured for high-frequency access scenarios.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This preset is ideal for AI pathfinding results, physics query caching, spatial lookups,
        /// and other performance-critical systems that require high throughput and can benefit from
        /// dynamic growth under load.
        /// </para>
        /// <para>
        /// Configuration:
        /// <list type="bullet">
        /// <item><description>Maximum Size: 2000 entries</description></item>
        /// <item><description>Expiration: 5 minutes (300 seconds) after write</description></item>
        /// <item><description>Eviction Policy: SLRU (Segmented Least Recently Used) for better hit rates</description></item>
        /// <item><description>Statistics: Enabled for monitoring performance</description></item>
        /// <item><description>Growth: Allows 1.5x growth up to 4000 entries when thrashing is detected</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
        /// <typeparam name="TValue">The type of values in the cache.</typeparam>
        /// <returns>
        /// A <see cref="CacheBuilder{TKey,TValue}"/> configured for high-throughput scenarios that can be
        /// further customized before building.
        /// </returns>
        /// <example>
        /// <code><![CDATA[
        /// // Cache for pathfinding results
        /// Cache<(Vector3, Vector3), NavMeshPath> pathCache = CachePresets.HighThroughput<(Vector3, Vector3), NavMeshPath>()
        ///     .Build();
        ///
        /// // Cache for physics overlap queries with custom expiration
        /// Cache<int, Collider[]> overlapCache = CachePresets.HighThroughput<int, Collider[]>()
        ///     .ExpireAfterWrite(60f)
        ///     .Build();
        /// ]]></code>
        /// </example>
        public static CacheBuilder<TKey, TValue> HighThroughput<TKey, TValue>()
        {
            CacheBuilder<TKey, TValue> builder = CacheBuilder<TKey, TValue>
                .NewBuilder()
                .MaximumSize(2000)
                .ExpireAfterWrite(300f)
                .EvictionPolicy(EvictionPolicy.Slru)
                .RecordStatistics()
                .AllowGrowth(1.5f, 4000);
            return builder;
        }

        /// <summary>
        /// Creates a cache builder configured for render-related data.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This preset is ideal for shader parameter caching, material property lookups, render state,
        /// and other graphics-related data that changes frequently but benefits from short-term caching.
        /// </para>
        /// <para>
        /// Configuration:
        /// <list type="bullet">
        /// <item><description>Maximum Size: 200 entries</description></item>
        /// <item><description>Expiration: 30 seconds after write</description></item>
        /// <item><description>Eviction Policy: FIFO (First In First Out) for predictable eviction order</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
        /// <typeparam name="TValue">The type of values in the cache.</typeparam>
        /// <returns>
        /// A <see cref="CacheBuilder{TKey,TValue}"/> configured for render data that can be
        /// further customized before building.
        /// </returns>
        /// <example>
        /// <code><![CDATA[
        /// // Cache for material property blocks
        /// Cache<int, MaterialPropertyBlock> propBlockCache = CachePresets.RenderCache<int, MaterialPropertyBlock>()
        ///     .Build();
        ///
        /// // Cache for computed shader parameters
        /// Cache<string, Vector4> shaderParamCache = CachePresets.RenderCache<string, Vector4>()
        ///     .MaximumSize(50)
        ///     .Build();
        /// ]]></code>
        /// </example>
        public static CacheBuilder<TKey, TValue> RenderCache<TKey, TValue>()
        {
            CacheBuilder<TKey, TValue> builder = CacheBuilder<TKey, TValue>
                .NewBuilder()
                .MaximumSize(200)
                .ExpireAfterWrite(30f)
                .EvictionPolicy(EvictionPolicy.Fifo);
            return builder;
        }

        /// <summary>
        /// Creates a cache builder configured for network and API responses.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This preset is ideal for REST API responses, server data, leaderboard entries, and other
        /// network-fetched content that should be cached temporarily to reduce server load and improve
        /// responsiveness.
        /// </para>
        /// <para>
        /// Configuration:
        /// <list type="bullet">
        /// <item><description>Maximum Size: 100 entries</description></item>
        /// <item><description>Expiration: 2 minutes (120 seconds) after write</description></item>
        /// <item><description>Jitter: Up to 12 seconds to prevent thundering herd on cache refresh</description></item>
        /// <item><description>Eviction Policy: LRU (Least Recently Used)</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
        /// <typeparam name="TValue">The type of values in the cache.</typeparam>
        /// <returns>
        /// A <see cref="CacheBuilder{TKey,TValue}"/> configured for network data that can be
        /// further customized before building.
        /// </returns>
        /// <example>
        /// <code><![CDATA[
        /// // Cache for API responses
        /// Cache<string, JsonResponse> apiCache = CachePresets.NetworkCache<string, JsonResponse>()
        ///     .Build();
        ///
        /// // Cache for leaderboard data with longer expiration
        /// Cache<string, LeaderboardEntry[]> leaderboardCache = CachePresets.NetworkCache<string, LeaderboardEntry[]>()
        ///     .ExpireAfterWrite(300f)
        ///     .Build();
        /// ]]></code>
        /// </example>
        public static CacheBuilder<TKey, TValue> NetworkCache<TKey, TValue>()
        {
            CacheBuilder<TKey, TValue> builder = CacheBuilder<TKey, TValue>
                .NewBuilder()
                .MaximumSize(100)
                .ExpireAfterWrite(120f)
                .WithJitter(12f)
                .EvictionPolicy(EvictionPolicy.Lru);
            return builder;
        }
    }
}
