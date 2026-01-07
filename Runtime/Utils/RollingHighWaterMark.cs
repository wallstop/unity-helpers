// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using WallstopStudios.UnityHelpers.Core.Helper;

    /// <summary>
    /// Tracks the rolling high-water mark (peak value) within a configurable time window.
    /// Used by intelligent pool purging to determine typical usage patterns.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This data structure maintains a time-windowed maximum value using a sliding window approach.
    /// It stores timestamped samples and efficiently computes the maximum within the window.
    /// </para>
    /// <para>
    /// Thread safety: All operations are protected by a lock for multi-threaded environments.
    /// </para>
    /// </remarks>
    internal sealed class RollingHighWaterMark
    {
        /// <summary>
        /// Multiplier for calculating cleanup interval from window size.
        /// Cleanup runs every 10% of the window duration.
        /// </summary>
        private const float CleanupIntervalMultiplier = 0.1f;

        /// <summary>
        /// Minimum cleanup interval in seconds to avoid excessive cleanup operations.
        /// </summary>
        private const float MinCleanupIntervalSeconds = 1f;

        /// <summary>
        /// Maximum number of samples to store to prevent unbounded growth under extreme load.
        /// When this limit is reached, old samples are removed before adding new ones.
        /// </summary>
        private const int MaxSampleCount = 10000;

        private readonly struct Sample
        {
            public readonly float Time;
            public readonly int Value;

            public Sample(float time, int value)
            {
                Time = time;
                Value = value;
            }
        }

        private readonly List<Sample> _samples = new List<Sample>();
        private readonly object _lock = new object();
        private float _windowSeconds;
        private int _cachedPeak;
        private float _lastCleanupTime;

        /// <summary>
        /// Gets or sets the rolling window duration in seconds.
        /// </summary>
        public float WindowSeconds
        {
            get
            {
                lock (_lock)
                {
                    return _windowSeconds;
                }
            }
            set
            {
                lock (_lock)
                {
                    _windowSeconds =
                        value > 0f ? value : PoolPurgeSettings.DefaultRollingWindowSeconds;
                }
            }
        }

        /// <summary>
        /// Gets the current peak value within the rolling window.
        /// </summary>
        public int Peak
        {
            get
            {
                lock (_lock)
                {
                    return _cachedPeak;
                }
            }
        }

        /// <summary>
        /// Gets the number of samples currently stored.
        /// </summary>
        internal int SampleCount
        {
            get
            {
                lock (_lock)
                {
                    return _samples.Count;
                }
            }
        }

        /// <summary>
        /// Creates a new rolling high-water mark tracker.
        /// </summary>
        /// <param name="windowSeconds">The rolling window duration in seconds.</param>
        public RollingHighWaterMark(float windowSeconds)
        {
            _windowSeconds =
                windowSeconds > 0f ? windowSeconds : PoolPurgeSettings.DefaultRollingWindowSeconds;
            _cachedPeak = 0;
        }

        /// <summary>
        /// Records a new sample value at the specified time.
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        /// <param name="value">The value to record.</param>
        public void Record(float currentTime, int value)
        {
            lock (_lock)
            {
                // Enforce maximum sample limit to prevent unbounded growth
                if (_samples.Count >= MaxSampleCount)
                {
                    int removeCount = _samples.Count - MaxSampleCount + 1;
                    _samples.RemoveRange(0, removeCount);
                    RecalculatePeak();
                }

                _samples.Add(new Sample(currentTime, value));

                if (value > _cachedPeak)
                {
                    _cachedPeak = value;
                }

                CleanupIfNeeded(currentTime);
            }
        }

        /// <summary>
        /// Gets the current peak value within the rolling window, cleaning up expired samples.
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        /// <returns>The peak value within the window.</returns>
        public int GetPeak(float currentTime)
        {
            lock (_lock)
            {
                CleanupIfNeeded(currentTime);
                return _cachedPeak;
            }
        }

        /// <summary>
        /// Gets the average value within the rolling window.
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        /// <returns>The average value, or 0 if no samples exist.</returns>
        public float GetAverage(float currentTime)
        {
            lock (_lock)
            {
                CleanupIfNeeded(currentTime);

                if (_samples.Count == 0)
                {
                    return 0f;
                }

                long sum = 0;
                for (int i = 0; i < _samples.Count; i++)
                {
                    sum += _samples[i].Value;
                }

                return (float)sum / _samples.Count;
            }
        }

        /// <summary>
        /// Clears all recorded samples and resets the peak.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _samples.Clear();
                _cachedPeak = 0;
                _lastCleanupTime = 0f;
            }
        }

        private void CleanupIfNeeded(float currentTime)
        {
            float cleanupInterval = _windowSeconds * CleanupIntervalMultiplier;
            if (cleanupInterval < MinCleanupIntervalSeconds)
            {
                cleanupInterval = MinCleanupIntervalSeconds;
            }

            if (currentTime - _lastCleanupTime < cleanupInterval)
            {
                return;
            }

            _lastCleanupTime = currentTime;

            float cutoff = currentTime - _windowSeconds;
            int removeCount = 0;

            for (int i = 0; i < _samples.Count; i++)
            {
                if (_samples[i].Time < cutoff)
                {
                    removeCount++;
                }
                else
                {
                    break;
                }
            }

            if (removeCount > 0)
            {
                _samples.RemoveRange(0, removeCount);
                RecalculatePeak();
            }
        }

        private void RecalculatePeak()
        {
            int peak = 0;
            for (int i = 0; i < _samples.Count; i++)
            {
                if (_samples[i].Value > peak)
                {
                    peak = _samples[i].Value;
                }
            }
            _cachedPeak = peak;
        }
    }

    /// <summary>
    /// Tracks usage statistics for intelligent pool purging.
    /// </summary>
    /// <remarks>
    /// This class maintains concurrent rental tracking, spike detection,
    /// and access frequency metrics to enable hysteresis-based purge protection
    /// and frequency-informed purge decisions.
    /// </remarks>
    internal sealed class PoolUsageTracker
    {
        /// <summary>
        /// Number of seconds per minute for time conversions.
        /// </summary>
        private const float SecondsPerMinute = 60f;

        /// <summary>
        /// Default duration in seconds for frequency tracking window (1 minute).
        /// </summary>
        private const float DefaultFrequencyWindowSeconds = 60f;

        /// <summary>
        /// Buffer cap applied under medium memory pressure (no additional buffer).
        /// </summary>
        private const float MediumPressureBufferCap = 1.0f;

        /// <summary>
        /// Buffer cap applied under low memory pressure (modest additional buffer).
        /// </summary>
        private const float LowPressureBufferCap = 1.5f;

        /// <summary>
        /// Multiplier for high-frequency pools to increase buffer size.
        /// High-frequency pools get 50% extra buffer.
        /// </summary>
        private const float HighFrequencyBufferBoost = 1.5f;

        /// <summary>
        /// Threshold for rentals-per-minute to be considered high frequency.
        /// Pools with 10+ rentals per minute are high-frequency.
        /// </summary>
        private const float HighFrequencyThreshold = 10f;

        /// <summary>
        /// Multiplier for low-frequency pools idle timeout reduction.
        /// Low-frequency pools purge 50% faster.
        /// </summary>
        private const float LowFrequencyTimeoutMultiplier = 0.5f;

        /// <summary>
        /// Threshold for rentals-per-minute to be considered low frequency.
        /// Pools with less than 1 rental per minute are low-frequency.
        /// </summary>
        private const float LowFrequencyThreshold = 1f;

        /// <summary>
        /// Threshold in minutes for unused pool aggressive purge.
        /// Pools with no access for 5+ minutes are candidates for aggressive purge.
        /// </summary>
        private const float UnusedPoolThresholdMinutes = 5f;

        private readonly RollingHighWaterMark _rollingHighWaterMark;
        private readonly object _lock = new object();

        private int _currentlyRented;
        private int _peakConcurrentRentals;
        private float _lastRentalTime;
        private float _lastReturnTime;
        private float _lastSpikeTime;
        private float _hysteresisSeconds;
        private float _spikeThresholdMultiplier;
        private float _bufferMultiplier;

        private int _rentalCountThisWindow;
        private float _windowStartTime;
        private float _cachedRentalsPerMinute;
        private long _totalRentalCount;
        private double _totalInterRentalTimeSeconds;
        private int _interRentalCount;
        private float _previousRentalTime;

        /// <summary>
        /// Gets the current number of items rented from the pool.
        /// </summary>
        public int CurrentlyRented
        {
            get
            {
                lock (_lock)
                {
                    return _currentlyRented;
                }
            }
        }

        /// <summary>
        /// Gets the all-time peak of concurrent rentals.
        /// </summary>
        public int PeakConcurrentRentals
        {
            get
            {
                lock (_lock)
                {
                    return _peakConcurrentRentals;
                }
            }
        }

        /// <summary>
        /// Gets the time of the last rental.
        /// </summary>
        public float LastRentalTime
        {
            get
            {
                lock (_lock)
                {
                    return _lastRentalTime;
                }
            }
        }

        /// <summary>
        /// Gets the time of the last return.
        /// </summary>
        public float LastReturnTime
        {
            get
            {
                lock (_lock)
                {
                    return _lastReturnTime;
                }
            }
        }

        /// <summary>
        /// Gets the time of the most recent access (rent or return).
        /// </summary>
        public float LastAccessTime
        {
            get
            {
                lock (_lock)
                {
                    return _lastRentalTime > _lastReturnTime ? _lastRentalTime : _lastReturnTime;
                }
            }
        }

        /// <summary>
        /// Gets the current rentals-per-minute rate based on the rolling frequency window.
        /// </summary>
        public float RentalsPerMinute
        {
            get
            {
                lock (_lock)
                {
                    return _cachedRentalsPerMinute;
                }
            }
        }

        /// <summary>
        /// Gets the average time between consecutive rentals in seconds.
        /// This represents the inter-arrival time between rental operations, not the duration items are held.
        /// Returns 0 if fewer than two rentals have occurred.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This metric tracks the time between consecutive rent calls, which can be accurately measured
        /// without per-item state tracking. For concurrent pools with overlapping rentals, this provides
        /// a useful measure of rental frequency (inverse of rentals-per-second).
        /// </para>
        /// <para>
        /// Example: If rentals occur at t=0, t=1, t=3, the average inter-rental time is (1 + 2) / 2 = 1.5 seconds.
        /// </para>
        /// </remarks>
        public float AverageInterRentalTimeSeconds
        {
            get
            {
                lock (_lock)
                {
                    if (_interRentalCount == 0)
                    {
                        return 0f;
                    }
                    return (float)(_totalInterRentalTimeSeconds / _interRentalCount);
                }
            }
        }

        /// <summary>
        /// Gets the total number of rentals since pool creation.
        /// </summary>
        public long TotalRentalCount
        {
            get
            {
                lock (_lock)
                {
                    return _totalRentalCount;
                }
            }
        }

        /// <summary>
        /// Gets or sets the hysteresis duration in seconds.
        /// </summary>
        public float HysteresisSeconds
        {
            get
            {
                lock (_lock)
                {
                    return _hysteresisSeconds;
                }
            }
            set
            {
                lock (_lock)
                {
                    _hysteresisSeconds = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the spike threshold multiplier.
        /// </summary>
        public float SpikeThresholdMultiplier
        {
            get
            {
                lock (_lock)
                {
                    return _spikeThresholdMultiplier;
                }
            }
            set
            {
                lock (_lock)
                {
                    _spikeThresholdMultiplier = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the buffer multiplier.
        /// </summary>
        public float BufferMultiplier
        {
            get
            {
                lock (_lock)
                {
                    return _bufferMultiplier;
                }
            }
            set
            {
                lock (_lock)
                {
                    _bufferMultiplier = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the rolling window duration in seconds.
        /// </summary>
        public float RollingWindowSeconds
        {
            get => _rollingHighWaterMark.WindowSeconds;
            set => _rollingHighWaterMark.WindowSeconds = value;
        }

        /// <summary>
        /// Creates a new pool usage tracker.
        /// </summary>
        /// <param name="rollingWindowSeconds">The rolling window duration for high-water mark tracking.</param>
        /// <param name="hysteresisSeconds">The hysteresis duration after spikes.</param>
        /// <param name="spikeThresholdMultiplier">The multiplier for spike detection.</param>
        /// <param name="bufferMultiplier">The buffer multiplier for comfortable size calculation.</param>
        public PoolUsageTracker(
            float rollingWindowSeconds,
            float hysteresisSeconds,
            float spikeThresholdMultiplier,
            float bufferMultiplier
        )
        {
            _rollingHighWaterMark = new RollingHighWaterMark(rollingWindowSeconds);
            _hysteresisSeconds = hysteresisSeconds;
            _spikeThresholdMultiplier = spikeThresholdMultiplier;
            _bufferMultiplier = bufferMultiplier;
        }

        /// <summary>
        /// Records a rental operation.
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        public void RecordRent(float currentTime)
        {
            lock (_lock)
            {
                _currentlyRented++;
                _totalRentalCount++;

                if (_previousRentalTime > 0f && currentTime >= _previousRentalTime)
                {
                    float interRentalTime = currentTime - _previousRentalTime;
                    _totalInterRentalTimeSeconds += interRentalTime;
                    _interRentalCount++;
                }

                _previousRentalTime = currentTime;
                _lastRentalTime = currentTime;

                if (_currentlyRented > _peakConcurrentRentals)
                {
                    _peakConcurrentRentals = _currentlyRented;
                }

                _rollingHighWaterMark.Record(currentTime, _currentlyRented);

                float average = _rollingHighWaterMark.GetAverage(currentTime);
                if (
                    _spikeThresholdMultiplier > 0f
                    && _currentlyRented > average * _spikeThresholdMultiplier
                )
                {
                    _lastSpikeTime = currentTime;
                }

                UpdateFrequencyTracking(currentTime);
            }
        }

        /// <summary>
        /// Records a return operation.
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        public void RecordReturn(float currentTime)
        {
            lock (_lock)
            {
                if (_currentlyRented > 0)
                {
                    _currentlyRented--;
                }

                _lastReturnTime = currentTime;
                _rollingHighWaterMark.Record(currentTime, _currentlyRented);
            }
        }

        /// <summary>
        /// Gets the rolling high-water mark (peak within the rolling window).
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        /// <returns>The peak concurrent rentals within the rolling window.</returns>
        public int GetRollingHighWaterMark(float currentTime)
        {
            return _rollingHighWaterMark.GetPeak(currentTime);
        }

        /// <summary>
        /// Gets the rolling average of concurrent rentals.
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        /// <returns>The average concurrent rentals within the rolling window.</returns>
        public float GetRollingAverage(float currentTime)
        {
            return _rollingHighWaterMark.GetAverage(currentTime);
        }

        /// <summary>
        /// Calculates the "comfortable" pool size based on usage patterns.
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        /// <param name="effectiveMinRetainCount">The effective minimum retain count (already accounts for warm/min logic).</param>
        /// <returns>The comfortable pool size.</returns>
        public int GetComfortableSize(float currentTime, int effectiveMinRetainCount)
        {
            return GetComfortableSize(
                currentTime,
                effectiveMinRetainCount,
                MemoryPressureLevel.None
            );
        }

        /// <summary>
        /// Calculates the "comfortable" pool size based on usage patterns and memory pressure.
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        /// <param name="effectiveMinRetainCount">The effective minimum retain count (already accounts for warm/min logic).</param>
        /// <param name="pressureLevel">The current memory pressure level.</param>
        /// <returns>The comfortable pool size, adjusted for memory pressure.</returns>
        /// <remarks>
        /// <para>
        /// Memory pressure affects the buffer multiplier used:
        /// <list type="bullet">
        ///   <item><description><see cref="MemoryPressureLevel.None"/>: Uses configured buffer multiplier</description></item>
        ///   <item><description><see cref="MemoryPressureLevel.Low"/>: Caps buffer at 1.5x</description></item>
        ///   <item><description><see cref="MemoryPressureLevel.Medium"/>: Caps buffer at 1.0x</description></item>
        ///   <item><description><see cref="MemoryPressureLevel.High"/> and above: Returns effective min retain count</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public int GetComfortableSize(
            float currentTime,
            int effectiveMinRetainCount,
            MemoryPressureLevel pressureLevel
        )
        {
            if (pressureLevel >= MemoryPressureLevel.High)
            {
                return effectiveMinRetainCount;
            }

            int rollingPeak = _rollingHighWaterMark.GetPeak(currentTime);
            float buffer;
            float rentalsPerMin;
            float lastAccess;
            lock (_lock)
            {
                buffer = _bufferMultiplier;
                rentalsPerMin = _cachedRentalsPerMinute;
                lastAccess = _lastRentalTime > _lastReturnTime ? _lastRentalTime : _lastReturnTime;
            }

            if (rentalsPerMin >= HighFrequencyThreshold)
            {
                buffer *= HighFrequencyBufferBoost;
            }

            if (pressureLevel == MemoryPressureLevel.Medium)
            {
                buffer = buffer > MediumPressureBufferCap ? MediumPressureBufferCap : buffer;
            }
            else if (pressureLevel == MemoryPressureLevel.Low)
            {
                buffer = buffer > LowPressureBufferCap ? LowPressureBufferCap : buffer;
            }

            float unusedThresholdSeconds = UnusedPoolThresholdMinutes * SecondsPerMinute;
            if (lastAccess > 0f && (currentTime - lastAccess) >= unusedThresholdSeconds)
            {
                return effectiveMinRetainCount;
            }

            int bufferedSize = (int)(rollingPeak * buffer);
            return bufferedSize > effectiveMinRetainCount ? bufferedSize : effectiveMinRetainCount;
        }

        /// <summary>
        /// Calculates the effective minimum retain count based on whether the pool is active.
        /// Active pools use WarmRetainCount, idle pools use MinRetainCount.
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        /// <param name="idleTimeoutSeconds">The idle timeout in seconds.</param>
        /// <param name="minRetainCount">The absolute floor (MinRetainCount).</param>
        /// <param name="warmRetainCount">The warm retain count for active pools.</param>
        /// <returns>The effective minimum retain count.</returns>
        public int GetEffectiveMinRetainCount(
            float currentTime,
            float idleTimeoutSeconds,
            int minRetainCount,
            int warmRetainCount
        )
        {
            return GetEffectiveMinRetainCount(
                currentTime,
                idleTimeoutSeconds,
                minRetainCount,
                warmRetainCount,
                MemoryPressureLevel.None
            );
        }

        /// <summary>
        /// Calculates the effective minimum retain count based on pool activity and memory pressure.
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        /// <param name="idleTimeoutSeconds">The idle timeout in seconds.</param>
        /// <param name="minRetainCount">The absolute floor (MinRetainCount).</param>
        /// <param name="warmRetainCount">The warm retain count for active pools.</param>
        /// <param name="pressureLevel">The current memory pressure level.</param>
        /// <returns>The effective minimum retain count, adjusted for memory pressure.</returns>
        /// <remarks>
        /// <para>
        /// Memory pressure affects warm retain count:
        /// <list type="bullet">
        ///   <item><description><see cref="MemoryPressureLevel.None"/> and <see cref="MemoryPressureLevel.Low"/>: Uses full warm retain count for active pools</description></item>
        ///   <item><description><see cref="MemoryPressureLevel.Medium"/> and above: Ignores warm retain count, returns min retain count</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public int GetEffectiveMinRetainCount(
            float currentTime,
            float idleTimeoutSeconds,
            int minRetainCount,
            int warmRetainCount,
            MemoryPressureLevel pressureLevel
        )
        {
            if (pressureLevel >= MemoryPressureLevel.Medium)
            {
                return minRetainCount;
            }

            float lastRental;
            lock (_lock)
            {
                lastRental = _lastRentalTime;
            }

            bool isActive =
                idleTimeoutSeconds > 0f && (currentTime - lastRental) < idleTimeoutSeconds;
            int warmFloor = isActive ? warmRetainCount : 0;
            return warmFloor > minRetainCount ? warmFloor : minRetainCount;
        }

        /// <summary>
        /// Checks if purging should be suppressed due to recent spike activity.
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        /// <returns><c>true</c> if purging should be suppressed; otherwise, <c>false</c>.</returns>
        public bool IsInHysteresisPeriod(float currentTime)
        {
            lock (_lock)
            {
                if (_lastSpikeTime <= 0f)
                {
                    return false;
                }

                return currentTime - _lastSpikeTime < _hysteresisSeconds;
            }
        }

        /// <summary>
        /// Checks if this pool is high-frequency (many rentals per minute).
        /// High-frequency pools benefit from larger buffers.
        /// </summary>
        /// <returns><c>true</c> if the pool is high-frequency; otherwise, <c>false</c>.</returns>
        public bool IsHighFrequency()
        {
            lock (_lock)
            {
                return _cachedRentalsPerMinute >= HighFrequencyThreshold;
            }
        }

        /// <summary>
        /// Checks if this pool is low-frequency (few rentals per minute).
        /// Low-frequency pools can be purged more aggressively.
        /// </summary>
        /// <returns><c>true</c> if the pool is low-frequency; otherwise, <c>false</c>.</returns>
        public bool IsLowFrequency()
        {
            lock (_lock)
            {
                return _cachedRentalsPerMinute < LowFrequencyThreshold && _totalRentalCount > 0;
            }
        }

        /// <summary>
        /// Checks if the pool has been unused for an extended period.
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        /// <returns><c>true</c> if the pool is unused; otherwise, <c>false</c>.</returns>
        public bool IsUnused(float currentTime)
        {
            lock (_lock)
            {
                float lastAccess =
                    _lastRentalTime > _lastReturnTime ? _lastRentalTime : _lastReturnTime;
                if (lastAccess <= 0f)
                {
                    return false;
                }

                float unusedThresholdSeconds = UnusedPoolThresholdMinutes * SecondsPerMinute;
                return (currentTime - lastAccess) >= unusedThresholdSeconds;
            }
        }

        /// <summary>
        /// Gets the effective buffer multiplier adjusted for frequency.
        /// High-frequency pools get a larger buffer, low-frequency pools get standard buffer.
        /// </summary>
        /// <returns>The adjusted buffer multiplier.</returns>
        public float GetFrequencyAdjustedBufferMultiplier()
        {
            lock (_lock)
            {
                if (_cachedRentalsPerMinute >= HighFrequencyThreshold)
                {
                    return _bufferMultiplier * HighFrequencyBufferBoost;
                }

                return _bufferMultiplier;
            }
        }

        /// <summary>
        /// Gets the effective idle timeout adjusted for frequency.
        /// Low-frequency pools have shorter effective timeout for faster purging.
        /// </summary>
        /// <param name="baseIdleTimeoutSeconds">The base idle timeout in seconds.</param>
        /// <returns>The adjusted idle timeout in seconds.</returns>
        public float GetFrequencyAdjustedIdleTimeout(float baseIdleTimeoutSeconds)
        {
            lock (_lock)
            {
                if (
                    _cachedRentalsPerMinute < LowFrequencyThreshold
                    && _cachedRentalsPerMinute > 0f
                    && _totalRentalCount > 0
                )
                {
                    return baseIdleTimeoutSeconds * LowFrequencyTimeoutMultiplier;
                }

                return baseIdleTimeoutSeconds;
            }
        }

        /// <summary>
        /// Gets a snapshot of the current frequency statistics.
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        /// <returns>A snapshot of frequency metrics.</returns>
        public PoolFrequencyStatistics GetFrequencyStatistics(float currentTime)
        {
            lock (_lock)
            {
                UpdateFrequencyTrackingLocked(currentTime);

                float lastAccess =
                    _lastRentalTime > _lastReturnTime ? _lastRentalTime : _lastReturnTime;
                float averageInterRentalTime =
                    _interRentalCount > 0
                        ? (float)(_totalInterRentalTimeSeconds / _interRentalCount)
                        : 0f;

                return new PoolFrequencyStatistics(
                    rentalsPerMinute: _cachedRentalsPerMinute,
                    averageInterRentalTimeSeconds: averageInterRentalTime,
                    lastAccessTime: lastAccess,
                    totalRentalCount: _totalRentalCount,
                    isHighFrequency: _cachedRentalsPerMinute >= HighFrequencyThreshold,
                    isLowFrequency: _cachedRentalsPerMinute < LowFrequencyThreshold
                        && _totalRentalCount > 0,
                    isUnused: lastAccess > 0f
                        && (currentTime - lastAccess)
                            >= UnusedPoolThresholdMinutes * SecondsPerMinute
                );
            }
        }

        /// <summary>
        /// Clears all tracking data.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _currentlyRented = 0;
                _peakConcurrentRentals = 0;
                _lastRentalTime = 0f;
                _lastReturnTime = 0f;
                _lastSpikeTime = 0f;
                _rentalCountThisWindow = 0;
                _windowStartTime = 0f;
                _cachedRentalsPerMinute = 0f;
                _totalRentalCount = 0;
                _totalInterRentalTimeSeconds = 0;
                _interRentalCount = 0;
                _previousRentalTime = 0f;
                _rollingHighWaterMark.Clear();
            }
        }

        private void UpdateFrequencyTracking(float currentTime)
        {
            UpdateFrequencyTrackingLocked(currentTime, incrementRental: true);
        }

        /// <summary>
        /// Updates frequency tracking metrics. Must be called while holding the lock.
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        /// <param name="incrementRental">
        /// When <c>true</c>, increments the rental count for this window (used by actual rental operations).
        /// When <c>false</c>, only updates time-based calculations without counting a rental
        /// (used by read-only statistics queries like <see cref="GetFrequencyStatistics"/>).
        /// </param>
        private void UpdateFrequencyTrackingLocked(float currentTime, bool incrementRental = false)
        {
            if (incrementRental)
            {
                _rentalCountThisWindow++;
            }

            if (_windowStartTime <= 0f)
            {
                _windowStartTime = currentTime;
            }

            float windowElapsed = currentTime - _windowStartTime;

            if (windowElapsed >= DefaultFrequencyWindowSeconds)
            {
                if (windowElapsed > 0f)
                {
                    _cachedRentalsPerMinute =
                        _rentalCountThisWindow * (SecondsPerMinute / DefaultFrequencyWindowSeconds);
                }

                _rentalCountThisWindow = incrementRental ? 1 : 0;
                _windowStartTime = currentTime;
            }
            else if (windowElapsed > 0f)
            {
                float estimatedMinuteRate =
                    _rentalCountThisWindow * (SecondsPerMinute / windowElapsed);
                _cachedRentalsPerMinute = estimatedMinuteRate;
            }
        }
    }

    /// <summary>
    /// Immutable snapshot of pool frequency statistics for debugging and monitoring.
    /// </summary>
    public readonly struct PoolFrequencyStatistics : IEquatable<PoolFrequencyStatistics>
    {
        /// <summary>
        /// Tolerance for floating-point equality comparisons.
        /// </summary>
        private const float FloatEqualityTolerance = 0.0001f;

        /// <summary>
        /// Gets the current rentals-per-minute rate.
        /// </summary>
        public float RentalsPerMinute { get; }

        /// <summary>
        /// Gets the average time between consecutive rentals in seconds.
        /// This represents the inter-arrival time between rental operations, not the duration items are held.
        /// Returns 0 if fewer than two rentals have occurred.
        /// </summary>
        public float AverageInterRentalTimeSeconds { get; }

        /// <summary>
        /// Gets the time of the most recent access (rent or return).
        /// </summary>
        public float LastAccessTime { get; }

        /// <summary>
        /// Gets the total number of rentals since pool creation.
        /// </summary>
        public long TotalRentalCount { get; }

        /// <summary>
        /// Gets whether this pool is considered high-frequency.
        /// </summary>
        public bool IsHighFrequency { get; }

        /// <summary>
        /// Gets whether this pool is considered low-frequency.
        /// </summary>
        public bool IsLowFrequency { get; }

        /// <summary>
        /// Gets whether this pool is considered unused.
        /// </summary>
        public bool IsUnused { get; }

        /// <summary>
        /// Creates a new frequency statistics snapshot.
        /// </summary>
        public PoolFrequencyStatistics(
            float rentalsPerMinute,
            float averageInterRentalTimeSeconds,
            float lastAccessTime,
            long totalRentalCount,
            bool isHighFrequency,
            bool isLowFrequency,
            bool isUnused
        )
        {
            RentalsPerMinute = rentalsPerMinute;
            AverageInterRentalTimeSeconds = averageInterRentalTimeSeconds;
            LastAccessTime = lastAccessTime;
            TotalRentalCount = totalRentalCount;
            IsHighFrequency = isHighFrequency;
            IsLowFrequency = isLowFrequency;
            IsUnused = isUnused;
        }

        /// <inheritdoc />
        public bool Equals(PoolFrequencyStatistics other)
        {
            return Math.Abs(RentalsPerMinute - other.RentalsPerMinute) < FloatEqualityTolerance
                && Math.Abs(AverageInterRentalTimeSeconds - other.AverageInterRentalTimeSeconds)
                    < FloatEqualityTolerance
                && Math.Abs(LastAccessTime - other.LastAccessTime) < FloatEqualityTolerance
                && TotalRentalCount == other.TotalRentalCount
                && IsHighFrequency == other.IsHighFrequency
                && IsLowFrequency == other.IsLowFrequency
                && IsUnused == other.IsUnused;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is PoolFrequencyStatistics other && Equals(other);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return Objects.HashCode(
                RentalsPerMinute,
                AverageInterRentalTimeSeconds,
                LastAccessTime,
                TotalRentalCount,
                IsHighFrequency,
                IsLowFrequency,
                IsUnused
            );
        }

        /// <summary>
        /// Determines whether two <see cref="PoolFrequencyStatistics"/> instances are equal.
        /// </summary>
        public static bool operator ==(PoolFrequencyStatistics left, PoolFrequencyStatistics right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="PoolFrequencyStatistics"/> instances are not equal.
        /// </summary>
        public static bool operator !=(PoolFrequencyStatistics left, PoolFrequencyStatistics right)
        {
            return !left.Equals(right);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"PoolFrequencyStatistics(RentalsPerMin={RentalsPerMinute:F2}, "
                + $"AvgInterRentalTime={AverageInterRentalTimeSeconds:F3}s, "
                + $"LastAccess={LastAccessTime:F2}s, Total={TotalRentalCount}, "
                + $"High={IsHighFrequency}, Low={IsLowFrequency}, Unused={IsUnused})";
        }
    }
}
