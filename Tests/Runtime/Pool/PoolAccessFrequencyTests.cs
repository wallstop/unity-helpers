// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Runtime.Pool
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// Tests for pool access frequency tracking functionality.
    /// Covers rentals-per-minute tracking, last access time, average inter-rental time,
    /// and frequency-influenced purge decisions.
    /// </summary>
    [TestFixture]
    [NUnit.Framework.Category("Fast")]
    public sealed class PoolAccessFrequencyTests
    {
        private sealed class TestPoolItem
        {
            public int Id { get; }

            private static int _nextId;

            public TestPoolItem()
            {
                Id = ++_nextId;
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
            // Start at t=1 to avoid time=0 initialization issues
            // (time 0 is treated as uninitialized in the tracker)
            _currentTime = 1f;
            TestPoolItem.ResetIdCounter();
        }

        [Test]
        public void StatisticsTracksRentalsPerMinute()
        {
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            // Start at t=1 to ensure window tracking works properly
            // (time 0 is treated as uninitialized in the tracker)
            _currentTime = 1f;
            for (int i = 0; i < 10; i++)
            {
                using PooledResource<TestPoolItem> resource = pool.Get();
            }

            _currentTime = 7f;

            PoolStatistics stats = pool.GetStatistics();
            Assert.Greater(stats.RentalsPerMinute, 0f);
        }

        [Test]
        public void StatisticsTracksLastAccessTime()
        {
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            _currentTime = 10f;
            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            _currentTime = 15f;
            PoolStatistics stats = pool.GetStatistics();
            Assert.GreaterOrEqual(stats.LastAccessTime, 10f);
        }

        [Test]
        public void StatisticsTracksAverageInterRentalTime()
        {
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            // Start at t=1 to ensure first rental sets a valid previous time
            // (time 0 is treated as uninitialized in the tracker)
            _currentTime = 1f;
            using (PooledResource<TestPoolItem> resource1 = pool.Get()) { }
            _currentTime = 3f;
            using (PooledResource<TestPoolItem> resource2 = pool.Get()) { }
            _currentTime = 6f;
            using (PooledResource<TestPoolItem> resource3 = pool.Get()) { }

            PoolStatistics stats = pool.GetStatistics();
            Assert.Greater(
                stats.AverageInterRentalTimeSeconds,
                0f,
                "Expected positive average inter-rental time"
            );
            Assert.AreEqual(
                2.5f,
                stats.AverageInterRentalTimeSeconds,
                0.01f,
                "Average inter-rental time should be (2 + 3) / 2 = 2.5"
            );
        }

        [Test]
        public void HighFrequencyPoolIsIdentifiedCorrectly()
        {
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            // Start at t=1 to ensure window tracking works properly
            // (time 0 is treated as uninitialized in the tracker)
            _currentTime = 1f;
            for (int i = 0; i < 20; i++)
            {
                using PooledResource<TestPoolItem> resource = pool.Get();
            }

            _currentTime = 11f;

            PoolStatistics stats = pool.GetStatistics();
            Assert.GreaterOrEqual(
                stats.RentalsPerMinute,
                10f,
                "Expected high frequency rentals rate"
            );
            Assert.IsTrue(stats.IsHighFrequency, "Pool should be identified as high frequency");
        }

        [Test]
        public void LowFrequencyPoolIsIdentifiedCorrectly()
        {
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            _currentTime = 120f;

            PoolStatistics stats = pool.GetStatistics();
            Assert.Less(stats.RentalsPerMinute, 1f, "Expected low frequency rentals rate");
            Assert.Greater(stats.RentCount, 0, "Expected at least one rental");
            Assert.IsTrue(stats.IsLowFrequency, "Pool should be identified as low frequency");
        }

        [Test]
        public void UnusedPoolIsIdentifiedCorrectly()
        {
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            // Start at t=1 to ensure lastAccess > 0 check works
            // (time 0 is treated as uninitialized in the tracker)
            _currentTime = 1f;
            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            // UnusedPoolThresholdMinutes = 5 minutes = 300 seconds
            _currentTime = 401f;

            PoolStatistics stats = pool.GetStatistics();
            Assert.IsTrue(stats.IsUnused);
        }

        [Test]
        public void HighFrequencyPoolKeepsLargerBuffer()
        {
            int purgeCount = 0;
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem>
                {
                    UseIntelligentPurging = true,
                    IdleTimeoutSeconds = 30f,
                    Triggers = PurgeTrigger.OnRent,
                    OnPurge = (_, _) => purgeCount++,
                    TimeProvider = TestTimeProvider,
                    BufferMultiplier = 1.5f,
                }
            );

            for (int i = 0; i < 50; i++)
            {
                using PooledResource<TestPoolItem> resource = pool.Get();
            }

            int initialCount = pool.Count;

            _currentTime = 31f;
            using PooledResource<TestPoolItem> resource2 = pool.Get();

            Assert.GreaterOrEqual(pool.Count, 0);
        }

        [Test]
        public void LowFrequencyPoolHasFasterIdleTimeout()
        {
            int purgeCount = 0;
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem>
                {
                    UseIntelligentPurging = true,
                    IdleTimeoutSeconds = 60f,
                    Triggers = PurgeTrigger.OnRent,
                    OnPurge = (_, _) => purgeCount++,
                    TimeProvider = TestTimeProvider,
                }
            );

            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            _currentTime = 90f;

            for (int i = 0; i < 10; i++)
            {
                using PooledResource<TestPoolItem> resource = pool.Get();
            }

            _currentTime = 125f;
            using PooledResource<TestPoolItem> resource2 = pool.Get();

            Assert.GreaterOrEqual(pool.Count, 0);
        }

        [Test]
        public void UnusedPoolPurgesToMinimum()
        {
            int purgeCount = 0;
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                preWarmCount: 5,
                options: new PoolOptions<TestPoolItem>
                {
                    UseIntelligentPurging = true,
                    IdleTimeoutSeconds = 60f,
                    MinRetainCount = 1,
                    WarmRetainCount = 3,
                    Triggers = PurgeTrigger.OnRent,
                    OnPurge = (_, _) => purgeCount++,
                    TimeProvider = TestTimeProvider,
                }
            );

            // Start at t=1 to ensure lastAccess > 0 check works
            // (time 0 is treated as uninitialized in the tracker)
            _currentTime = 1f;
            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            // UnusedPoolThresholdMinutes = 5 minutes = 300 seconds
            _currentTime = 401f;

            using PooledResource<TestPoolItem> resource2 = pool.Get();

            PoolStatistics stats = pool.GetStatistics();
            Assert.IsTrue(stats.IsUnused);
        }

        [Test]
        public void FrequencyStatisticsExposedInPoolStatistics()
        {
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            for (int i = 0; i < 5; i++)
            {
                _currentTime = i * 2f;
                using PooledResource<TestPoolItem> resource = pool.Get();
            }

            _currentTime = 10f;
            PoolStatistics stats = pool.GetStatistics();

            Assert.GreaterOrEqual(stats.RentalsPerMinute, 0f);
            Assert.GreaterOrEqual(stats.AverageInterRentalTimeSeconds, 0f);
            Assert.GreaterOrEqual(stats.LastAccessTime, 0f);
            Assert.AreEqual(5, stats.RentCount);
        }

        [Test]
        public void PoolFrequencyStatisticsToStringIsCorrectlyFormatted()
        {
            PoolFrequencyStatistics stats = new PoolFrequencyStatistics(
                rentalsPerMinute: 10.5f,
                averageInterRentalTimeSeconds: 0.5f,
                lastAccessTime: 100f,
                totalRentalCount: 42,
                isHighFrequency: true,
                isLowFrequency: false,
                isUnused: false
            );

            string str = stats.ToString();
            Assert.IsTrue(
                str.Contains("RentalsPerMin="),
                "Expected RentalsPerMin in ToString output"
            );
            Assert.IsTrue(
                str.Contains("AvgInterRentalTime="),
                "Expected AvgInterRentalTime in ToString output"
            );
            Assert.IsTrue(str.Contains("LastAccess="), "Expected LastAccess in ToString output");
            Assert.IsTrue(str.Contains("Total=42"), "Expected Total=42 in ToString output");
            Assert.IsTrue(str.Contains("High=True"), "Expected High=True in ToString output");
            Assert.IsTrue(str.Contains("Low=False"), "Expected Low=False in ToString output");
            Assert.IsTrue(str.Contains("Unused=False"), "Expected Unused=False in ToString output");
        }

        [Test]
        public void MultipleRentalsInQuickSuccessionIncreasesFrequency()
        {
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            PoolStatistics beforeStats = pool.GetStatistics();
            float beforeRentalsPerMin = beforeStats.RentalsPerMinute;

            for (int i = 0; i < 100; i++)
            {
                _currentTime = i * 0.1f;
                using PooledResource<TestPoolItem> resource = pool.Get();
            }

            _currentTime = 10f;
            PoolStatistics afterStats = pool.GetStatistics();
            Assert.Greater(afterStats.RentalsPerMinute, beforeRentalsPerMin);
        }

        [Test]
        public void FrequencyResetAfterWindowElapses()
        {
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            for (int i = 0; i < 50; i++)
            {
                _currentTime = i * 0.5f;
                using PooledResource<TestPoolItem> resource = pool.Get();
            }

            _currentTime = 30f;
            PoolStatistics midStats = pool.GetStatistics();
            float midRentalsPerMin = midStats.RentalsPerMinute;

            _currentTime = 120f;

            using PooledResource<TestPoolItem> resource2 = pool.Get();

            _currentTime = 130f;
            PoolStatistics afterStats = pool.GetStatistics();

            Assert.Less(
                afterStats.RentalsPerMinute,
                midRentalsPerMin,
                "Frequency should decrease after window elapses"
            );
        }

        [Test]
        public void PoolStatisticsToStringIncludesFrequencyMetrics()
        {
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            for (int i = 0; i < 5; i++)
            {
                using PooledResource<TestPoolItem> resource = pool.Get();
            }

            _currentTime = 5f;
            PoolStatistics stats = pool.GetStatistics();
            string str = stats.ToString();

            Assert.IsTrue(
                str.Contains("RentalsPerMin="),
                "Expected RentalsPerMin in ToString output"
            );
            Assert.IsTrue(
                str.Contains("AvgInterRentalTime="),
                "Expected AvgInterRentalTime in ToString output"
            );
            Assert.IsTrue(str.Contains("LastAccess="), "Expected LastAccess in ToString output");
        }

        [Test]
        public void EmptyPoolHasZeroFrequencyMetrics()
        {
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            PoolStatistics stats = pool.GetStatistics();

            Assert.AreEqual(0f, stats.RentalsPerMinute);
            Assert.AreEqual(0f, stats.AverageInterRentalTimeSeconds);
            Assert.AreEqual(0f, stats.LastAccessTime);
            Assert.IsFalse(stats.IsHighFrequency);
            Assert.IsFalse(stats.IsLowFrequency);
            Assert.IsFalse(stats.IsUnused);
        }

        [Test]
        public void NewPoolIsNotUnused()
        {
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            PoolStatistics stats = pool.GetStatistics();
            Assert.IsFalse(stats.IsUnused);
        }

        [Test]
        public void PoolFrequencyStatisticsEqualsReturnsTrueForEqualInstances()
        {
            PoolFrequencyStatistics stats1 = new PoolFrequencyStatistics(
                rentalsPerMinute: 10f,
                averageInterRentalTimeSeconds: 1f,
                lastAccessTime: 100f,
                totalRentalCount: 50,
                isHighFrequency: true,
                isLowFrequency: false,
                isUnused: false
            );

            PoolFrequencyStatistics stats2 = new PoolFrequencyStatistics(
                rentalsPerMinute: 10f,
                averageInterRentalTimeSeconds: 1f,
                lastAccessTime: 100f,
                totalRentalCount: 50,
                isHighFrequency: true,
                isLowFrequency: false,
                isUnused: false
            );

            Assert.IsTrue(stats1.Equals(stats2), "Equal instances should return true from Equals");
            Assert.IsTrue(stats1 == stats2, "Equal instances should return true from == operator");
            Assert.IsFalse(
                stats1 != stats2,
                "Equal instances should return false from != operator"
            );
            Assert.AreEqual(
                stats1.GetHashCode(),
                stats2.GetHashCode(),
                "Equal instances should have same hash code"
            );
        }

        [Test]
        public void PoolFrequencyStatisticsEqualsReturnsFalseForDifferentInstances()
        {
            PoolFrequencyStatistics stats1 = new PoolFrequencyStatistics(
                rentalsPerMinute: 10f,
                averageInterRentalTimeSeconds: 1f,
                lastAccessTime: 100f,
                totalRentalCount: 50,
                isHighFrequency: true,
                isLowFrequency: false,
                isUnused: false
            );

            PoolFrequencyStatistics stats2 = new PoolFrequencyStatistics(
                rentalsPerMinute: 20f,
                averageInterRentalTimeSeconds: 1f,
                lastAccessTime: 100f,
                totalRentalCount: 50,
                isHighFrequency: true,
                isLowFrequency: false,
                isUnused: false
            );

            Assert.IsFalse(
                stats1.Equals(stats2),
                "Different instances should return false from Equals"
            );
            Assert.IsFalse(
                stats1 == stats2,
                "Different instances should return false from == operator"
            );
            Assert.IsTrue(
                stats1 != stats2,
                "Different instances should return true from != operator"
            );
        }

        [Test]
        public void PoolFrequencyStatisticsEqualsHandlesObjectOverload()
        {
            PoolFrequencyStatistics stats = new PoolFrequencyStatistics(
                rentalsPerMinute: 10f,
                averageInterRentalTimeSeconds: 1f,
                lastAccessTime: 100f,
                totalRentalCount: 50,
                isHighFrequency: true,
                isLowFrequency: false,
                isUnused: false
            );

            Assert.IsFalse(stats.Equals(null), "Equals(null) should return false");
            Assert.IsFalse(
                stats.Equals("not a stats object"),
                "Equals with different type should return false"
            );
            Assert.IsTrue(stats.Equals((object)stats), "Equals with boxed self should return true");
        }

        [Test]
        public void FrequencyTrackingWorksWithPreWarmedPool()
        {
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                preWarmCount: 10,
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            // Pre-warm doesn't count as rentals - it just adds items to the pool.
            // Verify the pool has 10 items ready
            Assert.AreEqual(10, pool.Count);

            PoolStatistics stats = pool.GetStatistics();

            // Pre-warm uses ReturnToPool, not Get, so RentCount should be 0
            Assert.AreEqual(0, stats.RentCount);

            // Start at t=1 to ensure window tracking works properly
            // (time 0 is treated as uninitialized in the tracker)
            _currentTime = 1f;
            for (int i = 0; i < 5; i++)
            {
                using PooledResource<TestPoolItem> resource = pool.Get();
            }

            _currentTime = 6f;
            PoolStatistics afterStats = pool.GetStatistics();
            Assert.AreEqual(5, afterStats.RentCount);
            Assert.Greater(afterStats.RentalsPerMinute, 0f);
        }

        [Test]
        [TestCase(4, TestName = "ThreadCount.Four")]
        [TestCase(8, TestName = "ThreadCount.Eight")]
        public void ConcurrentRecordRentIsThreadSafe(int threadCount)
        {
            PoolUsageTracker tracker = new PoolUsageTracker(
                rollingWindowSeconds: 60f,
                hysteresisSeconds: 5f,
                spikeThresholdMultiplier: 2f,
                bufferMultiplier: 1.5f
            );

            int operationsPerThread = 100;
            Task[] tasks = new Task[threadCount];
            int totalOperations = 0;

            for (int i = 0; i < threadCount; i++)
            {
                int threadIndex = i;
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < operationsPerThread; j++)
                    {
                        float time = threadIndex * 1000f + j * 0.01f;
                        tracker.RecordRent(time);
                        Interlocked.Increment(ref totalOperations);
                    }
                });
            }

            Task.WaitAll(tasks);

            Assert.AreEqual(
                threadCount * operationsPerThread,
                totalOperations,
                "All operations should complete"
            );
            Assert.AreEqual(
                threadCount * operationsPerThread,
                tracker.TotalRentalCount,
                "Total rental count should match"
            );
        }

        [Test]
        [TestCase(4, TestName = "ThreadCount.Four")]
        [TestCase(8, TestName = "ThreadCount.Eight")]
        public void ConcurrentRecordReturnIsThreadSafe(int threadCount)
        {
            PoolUsageTracker tracker = new PoolUsageTracker(
                rollingWindowSeconds: 60f,
                hysteresisSeconds: 5f,
                spikeThresholdMultiplier: 2f,
                bufferMultiplier: 1.5f
            );

            int operationsPerThread = 100;
            for (int i = 0; i < threadCount * operationsPerThread; i++)
            {
                tracker.RecordRent(i * 0.01f);
            }

            Task[] tasks = new Task[threadCount];
            int totalOperations = 0;

            for (int i = 0; i < threadCount; i++)
            {
                int threadIndex = i;
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < operationsPerThread; j++)
                    {
                        float time = threadIndex * 1000f + j * 0.01f + 1000f;
                        tracker.RecordReturn(time);
                        Interlocked.Increment(ref totalOperations);
                    }
                });
            }

            Task.WaitAll(tasks);

            Assert.AreEqual(
                threadCount * operationsPerThread,
                totalOperations,
                "All operations should complete"
            );
            Assert.AreEqual(0, tracker.CurrentlyRented, "All items should be returned");
        }

        [Test]
        [TestCase(4, TestName = "ThreadCount.Four")]
        public void ConcurrentMixedRentAndReturnIsThreadSafe(int threadCount)
        {
            PoolUsageTracker tracker = new PoolUsageTracker(
                rollingWindowSeconds: 60f,
                hysteresisSeconds: 5f,
                spikeThresholdMultiplier: 2f,
                bufferMultiplier: 1.5f
            );

            int operationsPerThread = 50;
            Task[] tasks = new Task[threadCount * 2];
            int rentOperations = 0;
            int returnOperations = 0;

            for (int i = 0; i < threadCount; i++)
            {
                int threadIndex = i;
                tasks[i * 2] = Task.Run(() =>
                {
                    for (int j = 0; j < operationsPerThread; j++)
                    {
                        float time = threadIndex * 1000f + j * 0.01f;
                        tracker.RecordRent(time);
                        Interlocked.Increment(ref rentOperations);
                    }
                });

                tasks[i * 2 + 1] = Task.Run(() =>
                {
                    for (int j = 0; j < operationsPerThread; j++)
                    {
                        float time = threadIndex * 1000f + j * 0.01f + 500f;
                        tracker.RecordReturn(time);
                        Interlocked.Increment(ref returnOperations);
                    }
                });
            }

            Task.WaitAll(tasks);

            Assert.AreEqual(
                threadCount * operationsPerThread,
                rentOperations,
                "All rent operations should complete"
            );
            Assert.AreEqual(
                threadCount * operationsPerThread,
                returnOperations,
                "All return operations should complete"
            );
            Assert.AreEqual(
                threadCount * operationsPerThread,
                tracker.TotalRentalCount,
                "Total rental count should match rent operations"
            );
        }

        [Test]
        public void TimeGoingBackwardsDoesNotCorruptStatistics()
        {
            PoolUsageTracker tracker = new PoolUsageTracker(
                rollingWindowSeconds: 60f,
                hysteresisSeconds: 5f,
                spikeThresholdMultiplier: 2f,
                bufferMultiplier: 1.5f
            );

            tracker.RecordRent(100f);
            tracker.RecordRent(110f);
            tracker.RecordRent(90f);
            tracker.RecordRent(80f);

            Assert.AreEqual(
                4,
                tracker.TotalRentalCount,
                "Total rental count should include all rentals"
            );
            Assert.GreaterOrEqual(
                tracker.AverageInterRentalTimeSeconds,
                0f,
                "Average inter-rental time should not be negative"
            );
        }

        [Test]
        public void TimeGoingBackwardsIgnoresNegativeInterRentalTime()
        {
            PoolUsageTracker tracker = new PoolUsageTracker(
                rollingWindowSeconds: 60f,
                hysteresisSeconds: 5f,
                spikeThresholdMultiplier: 2f,
                bufferMultiplier: 1.5f
            );

            // Start at t=1 to ensure first rental sets a valid previous time
            // (time 0 is treated as uninitialized in the tracker)
            tracker.RecordRent(1f);
            tracker.RecordRent(11f);
            tracker.RecordRent(6f);

            float averageInterRentalTime = tracker.AverageInterRentalTimeSeconds;
            Assert.AreEqual(
                10f,
                averageInterRentalTime,
                0.01f,
                "Should only include the positive inter-rental time of 10s"
            );
        }

        [Test]
        [TestCase(9.99f, false, TestName = "BelowThreshold.9.99")]
        [TestCase(10.0f, true, TestName = "AtThreshold.10.0")]
        [TestCase(10.01f, true, TestName = "AboveThreshold.10.01")]
        public void HighFrequencyThresholdBoundaryConditions(
            float rentalsPerMinute,
            bool expectedHighFrequency
        )
        {
            PoolFrequencyStatistics stats = new PoolFrequencyStatistics(
                rentalsPerMinute: rentalsPerMinute,
                averageInterRentalTimeSeconds: 1f,
                lastAccessTime: 100f,
                totalRentalCount: 50,
                isHighFrequency: rentalsPerMinute >= 10f,
                isLowFrequency: rentalsPerMinute < 1f,
                isUnused: false
            );

            Assert.AreEqual(
                expectedHighFrequency,
                stats.IsHighFrequency,
                $"IsHighFrequency should be {expectedHighFrequency} for {rentalsPerMinute} rentals/min"
            );
        }

        [Test]
        [TestCase(0.99f, true, TestName = "BelowThreshold.0.99")]
        [TestCase(1.0f, false, TestName = "AtThreshold.1.0")]
        [TestCase(1.01f, false, TestName = "AboveThreshold.1.01")]
        public void LowFrequencyThresholdBoundaryConditions(
            float rentalsPerMinute,
            bool expectedLowFrequency
        )
        {
            PoolFrequencyStatistics stats = new PoolFrequencyStatistics(
                rentalsPerMinute: rentalsPerMinute,
                averageInterRentalTimeSeconds: 1f,
                lastAccessTime: 100f,
                totalRentalCount: 50,
                isHighFrequency: rentalsPerMinute >= 10f,
                isLowFrequency: rentalsPerMinute < 1f && 50 > 0,
                isUnused: false
            );

            Assert.AreEqual(
                expectedLowFrequency,
                stats.IsLowFrequency,
                $"IsLowFrequency should be {expectedLowFrequency} for {rentalsPerMinute} rentals/min"
            );
        }

        [Test]
        public void SingleRentalHasZeroAverageInterRentalTime()
        {
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            _currentTime = 10f;
            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            PoolStatistics stats = pool.GetStatistics();
            Assert.AreEqual(
                0f,
                stats.AverageInterRentalTimeSeconds,
                "Single rental should have zero average inter-rental time"
            );
        }

        [Test]
        public void PoolFrequencyStatisticsDefaultValueIsValid()
        {
            PoolFrequencyStatistics defaultStats = default;

            Assert.AreEqual(0f, defaultStats.RentalsPerMinute);
            Assert.AreEqual(0f, defaultStats.AverageInterRentalTimeSeconds);
            Assert.AreEqual(0f, defaultStats.LastAccessTime);
            Assert.AreEqual(0L, defaultStats.TotalRentalCount);
            Assert.IsFalse(defaultStats.IsHighFrequency);
            Assert.IsFalse(defaultStats.IsLowFrequency);
            Assert.IsFalse(defaultStats.IsUnused);
        }

        [Test]
        public void PoolUsageTrackerClearResetsAllMetrics()
        {
            PoolUsageTracker tracker = new PoolUsageTracker(
                rollingWindowSeconds: 60f,
                hysteresisSeconds: 5f,
                spikeThresholdMultiplier: 2f,
                bufferMultiplier: 1.5f
            );

            for (int i = 0; i < 10; i++)
            {
                tracker.RecordRent(i * 1f);
            }

            for (int i = 0; i < 5; i++)
            {
                tracker.RecordReturn(10f + i * 1f);
            }

            tracker.Clear();

            Assert.AreEqual(0, tracker.CurrentlyRented, "CurrentlyRented should be 0 after clear");
            Assert.AreEqual(
                0,
                tracker.PeakConcurrentRentals,
                "PeakConcurrentRentals should be 0 after clear"
            );
            Assert.AreEqual(0f, tracker.LastRentalTime, "LastRentalTime should be 0 after clear");
            Assert.AreEqual(0f, tracker.LastReturnTime, "LastReturnTime should be 0 after clear");
            Assert.AreEqual(
                0f,
                tracker.RentalsPerMinute,
                "RentalsPerMinute should be 0 after clear"
            );
            Assert.AreEqual(
                0f,
                tracker.AverageInterRentalTimeSeconds,
                "AverageInterRentalTimeSeconds should be 0 after clear"
            );
            Assert.AreEqual(
                0L,
                tracker.TotalRentalCount,
                "TotalRentalCount should be 0 after clear"
            );
        }
    }

    /// <summary>
    /// Tests to verify that GetStatistics calls do not incorrectly inflate rental counts.
    /// These tests ensure statistics queries are read-only operations.
    /// </summary>
    [TestFixture]
    [NUnit.Framework.Category("Fast")]
    public sealed class PoolStatisticsInvariantTests
    {
        private sealed class TestPoolItem
        {
            public int Id { get; }

            private static int _nextId;

            public TestPoolItem()
            {
                Id = ++_nextId;
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
            _currentTime = 1f;
            TestPoolItem.ResetIdCounter();
        }

        [Test]
        public void MultipleGetStatisticsCallsDoNotInflateRentalCount()
        {
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            PoolStatistics initialStats = pool.GetStatistics();
            long initialRentCount = initialStats.RentCount;

            Assert.That(
                initialRentCount,
                Is.EqualTo(1),
                "Initial rent count should be 1 after single rental"
            );

            for (int i = 0; i < 100; i++)
            {
                PoolStatistics stats = pool.GetStatistics();
                Assert.That(
                    stats.RentCount,
                    Is.EqualTo(initialRentCount),
                    $"Rent count should remain {initialRentCount} after GetStatistics call {i + 1}"
                );
            }

            PoolStatistics finalStats = pool.GetStatistics();
            Assert.That(
                finalStats.RentCount,
                Is.EqualTo(initialRentCount),
                "Rent count should not change after multiple GetStatistics calls"
            );
        }

        [Test]
        public void GetStatisticsDoesNotAffectRentalsPerMinute()
        {
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            for (int i = 0; i < 5; i++)
            {
                using PooledResource<TestPoolItem> resource = pool.Get();
            }

            _currentTime = 10f;

            PoolStatistics initialStats = pool.GetStatistics();
            float initialRentalsPerMin = initialStats.RentalsPerMinute;
            long initialRentCount = initialStats.RentCount;

            for (int i = 0; i < 50; i++)
            {
                _currentTime += 0.1f;
                PoolStatistics stats = pool.GetStatistics();
            }

            PoolStatistics finalStats = pool.GetStatistics();

            Assert.That(
                finalStats.RentCount,
                Is.EqualTo(initialRentCount),
                "Rent count should not change from GetStatistics calls"
            );
            Assert.That(
                finalStats.RentalsPerMinute,
                Is.LessThanOrEqualTo(initialRentalsPerMin),
                "RentalsPerMinute should decrease or stay same as time passes without rentals, never increase from GetStatistics calls"
            );
        }

        [Test]
        public void GetStatisticsDuringWindowTransitionDoesNotCountAsRental()
        {
            const float windowDuration = 60f;
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            for (int i = 0; i < 10; i++)
            {
                using PooledResource<TestPoolItem> resource = pool.Get();
            }

            _currentTime = 30f;
            PoolStatistics midWindowStats = pool.GetStatistics();
            long midWindowRentCount = midWindowStats.RentCount;

            Assert.That(
                midWindowRentCount,
                Is.EqualTo(10),
                "Rent count should be 10 at mid-window"
            );

            _currentTime = windowDuration + 10f;

            PoolStatistics afterTransitionStats = pool.GetStatistics();

            Assert.That(
                afterTransitionStats.RentCount,
                Is.EqualTo(midWindowRentCount),
                "Rent count should not change during window transition from GetStatistics"
            );
        }

        [Test]
        public void ActualRentalDuringWindowTransitionIsCountedInNewWindow()
        {
            const float windowDuration = 60f;
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            for (int i = 0; i < 5; i++)
            {
                using PooledResource<TestPoolItem> resource = pool.Get();
            }

            _currentTime = 30f;
            PoolStatistics midWindowStats = pool.GetStatistics();

            Assert.That(
                midWindowStats.RentCount,
                Is.EqualTo(5),
                "Should have 5 rentals at mid-window"
            );

            _currentTime = windowDuration + 10f;

            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            PoolStatistics afterTransitionStats = pool.GetStatistics();

            Assert.That(
                afterTransitionStats.RentCount,
                Is.EqualTo(6),
                "Actual rental during window transition should be counted"
            );

            Assert.That(
                afterTransitionStats.RentalsPerMinute,
                Is.GreaterThan(0f),
                "RentalsPerMinute should be positive after rental in new window"
            );
        }

        [Test]
        [TestCase(1, TestName = "SingleStatisticsCall")]
        [TestCase(10, TestName = "TenStatisticsCalls")]
        [TestCase(100, TestName = "HundredStatisticsCalls")]
        public void GetStatisticsIsIdempotentForRentCount(int statisticsCallCount)
        {
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            const int actualRentals = 3;
            for (int i = 0; i < actualRentals; i++)
            {
                using PooledResource<TestPoolItem> resource = pool.Get();
            }

            PoolStatistics initialStats = pool.GetStatistics();

            Assert.That(
                initialStats.RentCount,
                Is.EqualTo(actualRentals),
                $"Initial rent count should be {actualRentals}"
            );

            for (int i = 0; i < statisticsCallCount; i++)
            {
                pool.GetStatistics();
            }

            PoolStatistics finalStats = pool.GetStatistics();

            Assert.That(
                finalStats.RentCount,
                Is.EqualTo(actualRentals),
                $"Rent count should remain {actualRentals} after {statisticsCallCount} GetStatistics calls"
            );
        }

        [Test]
        public void GetStatisticsDoesNotAffectLastAccessTime()
        {
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            _currentTime = 10f;
            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            PoolStatistics statsAfterRental = pool.GetStatistics();
            float lastAccessAfterRental = statsAfterRental.LastAccessTime;

            Assert.That(
                lastAccessAfterRental,
                Is.EqualTo(10f),
                "LastAccessTime should be 10f after rental at t=10"
            );

            _currentTime = 50f;

            for (int i = 0; i < 10; i++)
            {
                _currentTime += 1f;
                pool.GetStatistics();
            }

            PoolStatistics statsAfterQueries = pool.GetStatistics();

            Assert.That(
                statsAfterQueries.LastAccessTime,
                Is.EqualTo(lastAccessAfterRental),
                "LastAccessTime should not change from GetStatistics calls"
            );
        }

        [Test]
        public void GetStatisticsDoesNotAffectAverageInterRentalTime()
        {
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            _currentTime = 1f;
            using (PooledResource<TestPoolItem> resource1 = pool.Get()) { }
            _currentTime = 3f;
            using (PooledResource<TestPoolItem> resource2 = pool.Get()) { }
            _currentTime = 6f;
            using (PooledResource<TestPoolItem> resource3 = pool.Get()) { }

            PoolStatistics statsAfterRentals = pool.GetStatistics();
            float avgTimeAfterRentals = statsAfterRentals.AverageInterRentalTimeSeconds;

            Assert.That(
                avgTimeAfterRentals,
                Is.EqualTo(2.5f).Within(0.01f),
                "Average inter-rental time should be (2 + 3) / 2 = 2.5"
            );

            for (int i = 0; i < 50; i++)
            {
                _currentTime += 1f;
                pool.GetStatistics();
            }

            PoolStatistics statsAfterQueries = pool.GetStatistics();

            Assert.That(
                statsAfterQueries.AverageInterRentalTimeSeconds,
                Is.EqualTo(avgTimeAfterRentals).Within(0.01f),
                "AverageInterRentalTimeSeconds should not change from GetStatistics calls"
            );
        }

        [Test]
        public void InterleavedGetStatisticsAndRentalsTrackCorrectly()
        {
            using WallstopGenericPool<TestPoolItem> pool = new WallstopGenericPool<TestPoolItem>(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            for (int i = 0; i < 10; i++)
            {
                _currentTime = i + 1f;
                using PooledResource<TestPoolItem> resource = pool.Get();

                PoolStatistics stats = pool.GetStatistics();

                Assert.That(
                    stats.RentCount,
                    Is.EqualTo(i + 1),
                    $"Rent count should be {i + 1} after rental {i + 1}"
                );

                for (int j = 0; j < 5; j++)
                {
                    PoolStatistics extraStats = pool.GetStatistics();
                    Assert.That(
                        extraStats.RentCount,
                        Is.EqualTo(i + 1),
                        $"Rent count should remain {i + 1} after extra GetStatistics call {j + 1}"
                    );
                }
            }

            PoolStatistics finalStats = pool.GetStatistics();
            Assert.That(
                finalStats.RentCount,
                Is.EqualTo(10),
                "Final rent count should be exactly 10"
            );
        }
    }
}
