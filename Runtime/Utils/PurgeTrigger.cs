// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Utils
{
    using System;

    /// <summary>
    /// Specifies when automatic purge operations should be triggered on a pool.
    /// This is a flags enum allowing multiple triggers to be combined.
    /// </summary>
    /// <remarks>
    /// Combine triggers using bitwise OR to enable multiple purge conditions:
    /// <code>
    /// PurgeTrigger triggers = PurgeTrigger.OnRent | PurgeTrigger.OnReturn;
    /// </code>
    /// </remarks>
    [Flags]
    public enum PurgeTrigger
    {
        /// <summary>
        /// Reserved for uninitialized state. Do not use directly.
        /// </summary>
        [Obsolete("Use a specific PurgeTrigger value or combination of values.")]
        None = 0,

        /// <summary>
        /// Trigger purge checks when an item is rented from the pool.
        /// This is the default behavior providing lazy cleanup during normal usage.
        /// </summary>
        OnRent = 1,

        /// <summary>
        /// Trigger purge checks when an item is returned to the pool.
        /// Useful for immediate cleanup when pool size limits are exceeded.
        /// </summary>
        OnReturn = 2,

        /// <summary>
        /// Purge only occurs when explicitly requested via the Purge method.
        /// Use this for manual control over purge timing.
        /// </summary>
        Explicit = 4,

        /// <summary>
        /// Enable periodic purge checks based on a time interval.
        /// Requires setting PurgeIntervalSeconds in PoolOptions.
        /// </summary>
        Periodic = 8,
    }
}
