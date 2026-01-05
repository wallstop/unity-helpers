// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Runtime.Pool
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// Tests for P0.3 - Gradual/Spread Purging feature.
    /// Verifies that large purge operations can be spread across multiple calls
    /// to prevent GC spikes from bulk deallocation.
    /// </summary>
    [TestFixture]
    public sealed class GradualPurgingTests
    {
        private sealed class TestPoolItem
        {
            public int Id { get; }

            private static int _nextId;

            public TestPoolItem()
            {
                Id = Interlocked.Increment(ref _nextId);
            }

            public static void ResetIdCounter()
            {
                _nextId = 0;
            }
        }

        private float _currentTime;

        private float TestTimeProvider()
        {
            return _currentTime;
        }

        [SetUp]
        public void SetUp()
        {
            _currentTime = 0f;
            TestPoolItem.ResetIdCounter();
            PoolPurgeSettings.ResetToDefaults();
        }

        [TearDown]
        public void TearDown()
        {
            PoolPurgeSettings.ResetToDefaults();
        }

        [Test]
        public void MaxPurgesPerOperationLimitsItemsPurgedPerCall()
        {
            const int preWarmCount = 25;
            const int maxPurgesPerOp = 5;
            List<TestPoolItem> purgedItems = new();

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                preWarmCount: preWarmCount,
                options: new PoolOptions<TestPoolItem>
                {
                    IdleTimeoutSeconds = 1f,
                    MaxPurgesPerOperation = maxPurgesPerOp,
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                    OnPurge = (item, _) => purgedItems.Add(item),
                }
            );

            Assert.AreEqual(preWarmCount, pool.Count);

            // Advance time past idle timeout
            _currentTime = 2f;

            // First explicit purge should only purge up to MaxPurgesPerOperation
            int firstPurged = pool.Purge();

            Assert.AreEqual(maxPurgesPerOp, firstPurged);
            Assert.AreEqual(maxPurgesPerOp, purgedItems.Count);
            Assert.AreEqual(preWarmCount - maxPurgesPerOp, pool.Count);
            Assert.IsTrue(pool.HasPendingPurges);
        }

        [Test]
        public void PendingPurgesContinueOnSubsequentOperations()
        {
            const int preWarmCount = 25;
            const int maxPurgesPerOp = 5;

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                preWarmCount: preWarmCount,
                options: new PoolOptions<TestPoolItem>
                {
                    IdleTimeoutSeconds = 1f,
                    MaxPurgesPerOperation = maxPurgesPerOp,
                    Triggers = PurgeTrigger.OnRent,
                    TimeProvider = TestTimeProvider,
                }
            );

            // Advance time past idle timeout
            _currentTime = 2f;

            // Each rent should purge up to maxPurgesPerOp
            int totalPurged = 0;
            int rentsNeeded = 0;

            while (pool.Count > 1)
            {
                using (PooledResource<TestPoolItem> _ = pool.Get())
                {
                    // Get an item
                }

                rentsNeeded++;
                PoolStatistics stats = pool.GetStatistics();
                totalPurged = (int)stats.PurgeCount;

                // Prevent infinite loop
                if (rentsNeeded > 20)
                {
                    break;
                }
            }

            // All items except the one we kept renting should be purged eventually
            Assert.Greater(totalPurged, maxPurgesPerOp, "Should have purged more than one batch");
            Assert.LessOrEqual(pool.Count, 2);
        }

        [Test]
        public void ForceFullPurgeBypassesMaxPurgesPerOperationLimit()
        {
            const int preWarmCount = 25;
            const int maxPurgesPerOp = 5;

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                preWarmCount: preWarmCount,
                options: new PoolOptions<TestPoolItem>
                {
                    IdleTimeoutSeconds = 1f,
                    MaxPurgesPerOperation = maxPurgesPerOp,
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            // Advance time past idle timeout
            _currentTime = 2f;

            // ForceFullPurge should purge all items regardless of limit
            int purged = pool.ForceFullPurge();

            Assert.AreEqual(preWarmCount, purged);
            Assert.AreEqual(0, pool.Count);
            Assert.IsFalse(pool.HasPendingPurges);
        }

        [Test]
        public void ForceFullPurgeWithReasonBypassesLimit()
        {
            const int preWarmCount = 25;
            const int maxPurgesPerOp = 5;
            List<PurgeReason> purgeReasons = new();

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                preWarmCount: preWarmCount,
                options: new PoolOptions<TestPoolItem>
                {
                    IdleTimeoutSeconds = 1f,
                    MaxPurgesPerOperation = maxPurgesPerOp,
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                    OnPurge = (_, reason) => purgeReasons.Add(reason),
                }
            );

            // Advance time past idle timeout
            _currentTime = 2f;

            int purged = pool.ForceFullPurge(PurgeReason.MemoryPressure, ignoreHysteresis: true);

            Assert.AreEqual(preWarmCount, purged);
            Assert.AreEqual(preWarmCount, purgeReasons.Count);

            foreach (PurgeReason reason in purgeReasons)
            {
                Assert.AreEqual(PurgeReason.MemoryPressure, reason);
            }
        }

        [Test]
        public void StatisticsTrackPartialVsFullPurges()
        {
            const int preWarmCount = 12;
            const int maxPurgesPerOp = 5;

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                preWarmCount: preWarmCount,
                options: new PoolOptions<TestPoolItem>
                {
                    IdleTimeoutSeconds = 1f,
                    MaxPurgesPerOperation = maxPurgesPerOp,
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            // Advance time past idle timeout
            _currentTime = 2f;

            // First purge - should be partial (12 items > 5 limit)
            pool.Purge();
            PoolStatistics stats1 = pool.GetStatistics();
            Assert.AreEqual(1, stats1.PartialPurgeOperations);
            Assert.AreEqual(0, stats1.FullPurgeOperations);

            // Second purge - should be partial (7 items > 5 limit)
            pool.Purge();
            PoolStatistics stats2 = pool.GetStatistics();
            Assert.AreEqual(2, stats2.PartialPurgeOperations);
            Assert.AreEqual(0, stats2.FullPurgeOperations);

            // Third purge - should be full (2 items < 5 limit)
            pool.Purge();
            PoolStatistics stats3 = pool.GetStatistics();
            Assert.AreEqual(2, stats3.PartialPurgeOperations);
            Assert.AreEqual(1, stats3.FullPurgeOperations);
        }

        [Test]
        public void MaxPurgesPerOperationOfZeroMeansUnlimited()
        {
            const int preWarmCount = 25;

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                preWarmCount: preWarmCount,
                options: new PoolOptions<TestPoolItem>
                {
                    IdleTimeoutSeconds = 1f,
                    MaxPurgesPerOperation = 0,
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            // Advance time past idle timeout
            _currentTime = 2f;

            int purged = pool.Purge();

            Assert.AreEqual(preWarmCount, purged);
            Assert.AreEqual(0, pool.Count);
            Assert.IsFalse(pool.HasPendingPurges);
        }

        [Test]
        public void MaxPurgesPerOperationPropertyCanBeSetAfterConstruction()
        {
            const int preWarmCount = 15;

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                preWarmCount: preWarmCount,
                options: new PoolOptions<TestPoolItem>
                {
                    IdleTimeoutSeconds = 1f,
                    MaxPurgesPerOperation = 0, // Initially unlimited
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            // Change to limited after construction
            pool.MaxPurgesPerOperation = 3;

            // Advance time past idle timeout
            _currentTime = 2f;

            int purged = pool.Purge();

            Assert.AreEqual(3, purged);
            Assert.AreEqual(preWarmCount - 3, pool.Count);
            Assert.IsTrue(pool.HasPendingPurges);
        }

        [Test]
        public void GlobalDefaultMaxPurgesPerOperationIsUsedWhenNotSpecified()
        {
            const int customGlobalLimit = 7;
            PoolPurgeSettings.DefaultGlobalMaxPurgesPerOperation = customGlobalLimit;

            const int preWarmCount = 20;

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                preWarmCount: preWarmCount,
                options: new PoolOptions<TestPoolItem>
                {
                    IdleTimeoutSeconds = 1f,
                    // MaxPurgesPerOperation not specified - should use global default
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            // Advance time past idle timeout
            _currentTime = 2f;

            int purged = pool.Purge();

            Assert.AreEqual(customGlobalLimit, purged);
        }

        [Test]
        public void HasPendingPurgesIsFalseWhenNoPendingWork()
        {
            const int preWarmCount = 3;
            const int maxPurgesPerOp = 10; // Higher than item count

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                preWarmCount: preWarmCount,
                options: new PoolOptions<TestPoolItem>
                {
                    IdleTimeoutSeconds = 1f,
                    MaxPurgesPerOperation = maxPurgesPerOp,
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            // Advance time past idle timeout
            _currentTime = 2f;

            // Purge all items in one go (limit > count)
            pool.Purge();

            Assert.IsFalse(pool.HasPendingPurges);
            Assert.AreEqual(0, pool.Count);
        }

        [Test]
        public void MinRetainCountIsRespectedDuringGradualPurge()
        {
            const int preWarmCount = 15;
            const int maxPurgesPerOp = 5;
            const int minRetain = 3;

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                preWarmCount: preWarmCount,
                options: new PoolOptions<TestPoolItem>
                {
                    IdleTimeoutSeconds = 1f,
                    MaxPurgesPerOperation = maxPurgesPerOp,
                    MinRetainCount = minRetain,
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            // Advance time past idle timeout
            _currentTime = 2f;

            // Keep purging until no more items can be purged
            int iterations = 0;
            while (pool.Count > minRetain && iterations < 10)
            {
                pool.Purge();
                iterations++;
            }

            Assert.AreEqual(minRetain, pool.Count);
            Assert.IsFalse(pool.HasPendingPurges);
        }

        [Test]
        public void ForceFullPurgeClearsPendingFlag()
        {
            const int preWarmCount = 20;
            const int maxPurgesPerOp = 5;

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                preWarmCount: preWarmCount,
                options: new PoolOptions<TestPoolItem>
                {
                    IdleTimeoutSeconds = 1f,
                    MaxPurgesPerOperation = maxPurgesPerOp,
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            // Advance time past idle timeout
            _currentTime = 2f;

            // Do a partial purge first
            pool.Purge();
            Assert.IsTrue(pool.HasPendingPurges);

            // ForceFullPurge should clear the pending flag
            pool.ForceFullPurge();
            Assert.IsFalse(pool.HasPendingPurges);
            Assert.AreEqual(0, pool.Count);
        }

        [Test]
        public void NegativeMaxPurgesPerOperationIsNormalizedToZero()
        {
            const int preWarmCount = 10;

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                preWarmCount: preWarmCount,
                options: new PoolOptions<TestPoolItem>
                {
                    IdleTimeoutSeconds = 1f,
                    MaxPurgesPerOperation = -5, // Negative value
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            // Should behave as unlimited (0)
            Assert.AreEqual(0, pool.MaxPurgesPerOperation);

            _currentTime = 2f;
            int purged = pool.Purge();

            Assert.AreEqual(preWarmCount, purged);
        }

        [Test]
        public void EffectiveOptionsIncludesMaxPurgesPerOperation()
        {
            const int customLimit = 15;
            PoolPurgeSettings.DefaultGlobalMaxPurgesPerOperation = customLimit;

            PoolPurgeEffectiveOptions options =
                PoolPurgeSettings.GetEffectiveOptions<TestPoolItem>();

            Assert.AreEqual(customLimit, options.MaxPurgesPerOperation);
        }

        [Test]
        public void PoolStatisticsToStringIncludesPurgeOperationCounts()
        {
            PoolStatistics stats = new(
                currentSize: 10,
                peakSize: 20,
                rentCount: 100,
                returnCount: 90,
                purgeCount: 50,
                idleTimeoutPurges: 30,
                capacityPurges: 20,
                fullPurgeOperations: 5,
                partialPurgeOperations: 3
            );

            string str = stats.ToString();

            Assert.That(str, Does.Contain("FullPurgeOps=5"));
            Assert.That(str, Does.Contain("PartialPurgeOps=3"));
        }

        [Test]
        public void PoolStatisticsEqualityIncludesPurgeOperationCounts()
        {
            PoolStatistics stats1 = new(
                currentSize: 10,
                peakSize: 20,
                rentCount: 100,
                returnCount: 90,
                purgeCount: 50,
                idleTimeoutPurges: 30,
                capacityPurges: 20,
                fullPurgeOperations: 5,
                partialPurgeOperations: 3
            );

            PoolStatistics stats2 = new(
                currentSize: 10,
                peakSize: 20,
                rentCount: 100,
                returnCount: 90,
                purgeCount: 50,
                idleTimeoutPurges: 30,
                capacityPurges: 20,
                fullPurgeOperations: 5,
                partialPurgeOperations: 3
            );

            PoolStatistics stats3 = new(
                currentSize: 10,
                peakSize: 20,
                rentCount: 100,
                returnCount: 90,
                purgeCount: 50,
                idleTimeoutPurges: 30,
                capacityPurges: 20,
                fullPurgeOperations: 6, // Different
                partialPurgeOperations: 3
            );

            Assert.AreEqual(stats1, stats2);
            Assert.AreNotEqual(stats1, stats3);
        }
    }
}
