// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.Runtime.CompilerServices;
    using WallstopStudios.UnityHelpers.Core.Helper;

    /// <summary>
    /// Immutable snapshot of pool performance statistics.
    /// </summary>
    /// <remarks>
    /// Use <see cref="WallstopGenericPool{T}.GetStatistics"/> to retrieve the current snapshot.
    /// Statistics are always recorded regardless of pool configuration.
    /// </remarks>
    public readonly struct PoolStatistics : IEquatable<PoolStatistics>
    {
        /// <summary>
        /// The current number of items in the pool.
        /// </summary>
        public int CurrentSize { get; }

        /// <summary>
        /// The maximum number of items the pool has held at any point.
        /// </summary>
        public int PeakSize { get; }

        /// <summary>
        /// The total number of times items have been rented from the pool.
        /// </summary>
        public long RentCount { get; }

        /// <summary>
        /// The total number of times items have been returned to the pool.
        /// </summary>
        public long ReturnCount { get; }

        /// <summary>
        /// The total number of items purged from the pool for any reason.
        /// </summary>
        public long PurgeCount { get; }

        /// <summary>
        /// The number of items purged due to idle timeout expiration.
        /// </summary>
        public long IdleTimeoutPurges { get; }

        /// <summary>
        /// The number of items purged due to pool capacity being exceeded.
        /// </summary>
        public long CapacityPurges { get; }

        private readonly int _hash;

        /// <summary>
        /// Creates a new statistics snapshot.
        /// </summary>
        /// <param name="currentSize">Current number of items in the pool.</param>
        /// <param name="peakSize">Maximum pool size reached.</param>
        /// <param name="rentCount">Total rent operations.</param>
        /// <param name="returnCount">Total return operations.</param>
        /// <param name="purgeCount">Total purge operations.</param>
        /// <param name="idleTimeoutPurges">Purges due to idle timeout.</param>
        /// <param name="capacityPurges">Purges due to capacity limits.</param>
        public PoolStatistics(
            int currentSize,
            int peakSize,
            long rentCount,
            long returnCount,
            long purgeCount,
            long idleTimeoutPurges,
            long capacityPurges
        )
        {
            CurrentSize = currentSize;
            PeakSize = peakSize;
            RentCount = rentCount;
            ReturnCount = returnCount;
            PurgeCount = purgeCount;
            IdleTimeoutPurges = idleTimeoutPurges;
            CapacityPurges = capacityPurges;
            _hash = Objects.HashCode(
                currentSize,
                peakSize,
                rentCount,
                returnCount,
                purgeCount,
                idleTimeoutPurges,
                capacityPurges
            );
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(PoolStatistics other)
        {
            return _hash == other._hash
                && CurrentSize == other.CurrentSize
                && PeakSize == other.PeakSize
                && RentCount == other.RentCount
                && ReturnCount == other.ReturnCount
                && PurgeCount == other.PurgeCount
                && IdleTimeoutPurges == other.IdleTimeoutPurges
                && CapacityPurges == other.CapacityPurges;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is PoolStatistics other && Equals(other);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return _hash;
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(PoolStatistics left, PoolStatistics right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(PoolStatistics left, PoolStatistics right)
        {
            return !left.Equals(right);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"PoolStatistics(Size={CurrentSize}, Peak={PeakSize}, Rents={RentCount}, "
                + $"Returns={ReturnCount}, Purges={PurgeCount}, IdleTimeout={IdleTimeoutPurges}, Capacity={CapacityPurges})";
        }
    }
}
