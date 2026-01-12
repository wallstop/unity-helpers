// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;

    /// <summary>
    /// Specifies the reason why an entry was evicted from a <see cref="Cache{TKey,TValue}"/>.
    /// Used in eviction callbacks to distinguish between different removal scenarios.
    /// </summary>
    public enum EvictionReason
    {
        /// <summary>
        /// Reserved for uninitialized state. Do not use directly.
        /// </summary>
        [Obsolete("Use a specific EvictionReason value.")]
        Unknown = 0,

        /// <summary>
        /// The entry was removed because its time-to-live (TTL) expired.
        /// Applies to entries with <see cref="CacheOptions{TKey,TValue}.ExpireAfterWriteSeconds"/>
        /// or <see cref="CacheOptions{TKey,TValue}.ExpireAfterAccessSeconds"/>.
        /// </summary>
        Expired = 1,

        /// <summary>
        /// The entry was removed to make room for new entries when the cache reached its maximum capacity.
        /// The specific entry chosen depends on the configured <see cref="EvictionPolicy"/>.
        /// </summary>
        Capacity = 2,

        /// <summary>
        /// The entry was explicitly removed via <see cref="Cache{TKey,TValue}.TryRemove(TKey)"/>
        /// or <see cref="Cache{TKey,TValue}.Clear"/>.
        /// </summary>
        Explicit = 3,

        /// <summary>
        /// The entry was replaced by a new value via <see cref="Cache{TKey,TValue}.Set(TKey,TValue)"/>
        /// or <see cref="Cache{TKey,TValue}.GetOrAdd(TKey,Func{TKey,TValue})"/>.
        /// </summary>
        Replaced = 4,
    }
}
