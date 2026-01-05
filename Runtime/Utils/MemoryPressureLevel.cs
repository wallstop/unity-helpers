// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Utils
{
    /// <summary>
    /// Represents the current level of memory pressure detected by the <see cref="MemoryPressureMonitor"/>.
    /// Higher levels indicate greater memory stress and trigger more aggressive pool purging.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Memory pressure is determined by analyzing:
    /// <list type="bullet">
    ///   <item><description>Absolute memory usage compared to <see cref="MemoryPressureMonitor.MemoryPressureThresholdBytes"/></description></item>
    ///   <item><description>GC collection frequency (rapid collections indicate memory stress)</description></item>
    ///   <item><description>Memory growth rate between checks</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Purge behavior at each level:
    /// <list type="bullet">
    ///   <item><description><see cref="None"/>: Normal purging (respects hysteresis, buffer multiplier, warm retain count)</description></item>
    ///   <item><description><see cref="Low"/>: Reduces buffer multiplier to 1.5x</description></item>
    ///   <item><description><see cref="Medium"/>: Reduces buffer multiplier to 1.0x, ignores warm retain count</description></item>
    ///   <item><description><see cref="High"/>: Ignores hysteresis, purges to min retain count</description></item>
    ///   <item><description><see cref="Critical"/>: Emergency purge - purges everything to min retain count immediately</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public enum MemoryPressureLevel
    {
        /// <summary>
        /// No memory pressure detected. Normal purging behavior applies.
        /// Pools respect hysteresis, buffer multiplier, and warm retain count.
        /// </summary>
        // None is intentionally not [Obsolete] as it represents a valid state (no memory pressure)
        None = 0,

        /// <summary>
        /// Low memory pressure - approaching the configured threshold.
        /// Reduces buffer multiplier to 1.5x to start freeing memory conservatively.
        /// </summary>
        Low = 1,

        /// <summary>
        /// Medium memory pressure - at or near the configured threshold.
        /// Reduces buffer multiplier to 1.0x and ignores warm retain count.
        /// </summary>
        Medium = 2,

        /// <summary>
        /// High memory pressure - exceeding the configured threshold.
        /// Ignores hysteresis protection and purges to min retain count.
        /// </summary>
        High = 3,

        /// <summary>
        /// Critical memory pressure - emergency situation.
        /// Triggers immediate purge of all pools to min retain count, bypassing all protections.
        /// </summary>
        Critical = 4,
    }
}
