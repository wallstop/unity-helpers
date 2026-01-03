// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Utils
{
    using System;

    /// <summary>
    /// Specifies the reason why an item was purged from a pool.
    /// Used in purge callbacks to distinguish between different removal scenarios.
    /// </summary>
    public enum PurgeReason
    {
        /// <summary>
        /// Reserved for uninitialized state. Do not use directly.
        /// </summary>
        [Obsolete("Use a specific PurgeReason value.")]
        Unknown = 0,

        /// <summary>
        /// The item was purged because it exceeded the idle timeout duration.
        /// This occurs when an item has been sitting unused in the pool for too long.
        /// </summary>
        IdleTimeout = 1,

        /// <summary>
        /// The item was purged because the pool exceeded its maximum size capacity.
        /// Oldest items are typically purged first when capacity is exceeded.
        /// </summary>
        CapacityExceeded = 2,

        /// <summary>
        /// The item was explicitly purged via a call to the Purge method.
        /// </summary>
        Explicit = 3,
    }
}
