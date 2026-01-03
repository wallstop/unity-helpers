// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Runtime.Pool
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Utils;
#if !SINGLE_THREADED
    using System.Threading.Tasks;
#endif

    [TestFixture]
    public sealed class WallstopGenericPoolTests
    {
        private sealed class TestPoolItem
        {
            public int Id { get; }
            public bool WasReset { get; set; }
            public bool WasDisposed { get; set; }

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
        }

        [Test]
        public void ConstructorWithNullProducerThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new WallstopGenericPool<TestPoolItem>(null));
        }

        [Test]
        public void ConstructorWithPreWarmCountCreatesItems()
        {
            const int preWarmCount = 5;
            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                preWarmCount
            );

            Assert.AreEqual(preWarmCount, pool.Count);
        }

        [Test]
        public void GetFromEmptyPoolCreatesNewItem()
        {
            using WallstopGenericPool<TestPoolItem> pool = new(() => new TestPoolItem());

            using PooledResource<TestPoolItem> resource = pool.Get(out TestPoolItem item);

            Assert.IsNotNull(item);
            Assert.AreEqual(1, item.Id);
        }

        [Test]
        public void GetReturnsPooledItemWhenAvailable()
        {
            using WallstopGenericPool<TestPoolItem> pool = new(() => new TestPoolItem(), 1);

            using PooledResource<TestPoolItem> resource = pool.Get(out TestPoolItem item);

            Assert.AreEqual(1, item.Id);
            Assert.AreEqual(0, pool.Count);
        }

        [Test]
        public void ReturnAddsItemBackToPool()
        {
            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            TestPoolItem item;
            using (PooledResource<TestPoolItem> resource = pool.Get(out item))
            {
                Assert.AreEqual(0, pool.Count);
            }

            Assert.AreEqual(1, pool.Count);
        }

        [Test]
        public void OnGetCallbackIsInvoked()
        {
            int getCount = 0;
            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                onGet: _ => getCount++
            );

            using PooledResource<TestPoolItem> resource = pool.Get();

            Assert.AreEqual(1, getCount);
        }

        [Test]
        public void OnReleaseCallbackIsInvoked()
        {
            int releaseCount = 0;
            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                onRelease: item =>
                {
                    item.WasReset = true;
                    releaseCount++;
                },
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            TestPoolItem capturedItem;
            using (PooledResource<TestPoolItem> resource = pool.Get(out capturedItem))
            {
                Assert.IsFalse(capturedItem.WasReset);
            }

            Assert.IsTrue(capturedItem.WasReset);
            Assert.AreEqual(1, releaseCount);
        }

        [Test]
        public void DisposeInvokesOnDisposalForAllItems()
        {
            List<TestPoolItem> disposedItems = new();
            WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                preWarmCount: 3,
                onDisposal: item =>
                {
                    item.WasDisposed = true;
                    disposedItems.Add(item);
                },
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            pool.Dispose();

            Assert.AreEqual(3, disposedItems.Count);
            foreach (TestPoolItem item in disposedItems)
            {
                Assert.IsTrue(item.WasDisposed);
            }
        }

        [Test]
        public void DisposeClearsPoolWithoutCallbackIfNoOnDisposal()
        {
            WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                preWarmCount: 3,
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            Assert.AreEqual(3, pool.Count);
            pool.Dispose();
            Assert.AreEqual(0, pool.Count);
        }

        [Test]
        public void DisposeDoubleDisposeIsNoOp()
        {
            int disposeCount = 0;
            WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                preWarmCount: 1,
                onDisposal: _ => disposeCount++,
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            pool.Dispose();
            pool.Dispose();

            Assert.AreEqual(1, disposeCount);
        }

        // Max size enforcement tests
        [Test]
        public void MaxPoolSizeEnforcedOnReturn()
        {
            PoolOptions<TestPoolItem> options = new()
            {
                MaxPoolSize = 2,
                Triggers = PurgeTrigger.OnReturn,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                preWarmCount: 0,
                options: options
            );

            // Get and return 4 items
            for (int i = 0; i < 4; i++)
            {
                using PooledResource<TestPoolItem> resource = pool.Get();
            }

            // Should be capped at MaxPoolSize
            Assert.LessOrEqual(pool.Count, 2);
        }

        [Test]
        public void MaxPoolSizeZeroMeansUnbounded()
        {
            PoolOptions<TestPoolItem> options = new()
            {
                MaxPoolSize = 0,
                Triggers = PurgeTrigger.OnReturn,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            for (int i = 0; i < 10; i++)
            {
                using PooledResource<TestPoolItem> resource = pool.Get();
            }

            Assert.AreEqual(10, pool.Count);
        }

        // Idle timeout purging tests
        [Test]
        public void IdleTimeoutPurgesOldItems()
        {
            List<PurgeReason> purgeReasons = new();
            PoolOptions<TestPoolItem> options = new()
            {
                IdleTimeoutSeconds = 5f,
                Triggers = PurgeTrigger.OnRent,
                OnPurge = (_, reason) => purgeReasons.Add(reason),
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            // Add an item at time 0
            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            Assert.AreEqual(1, pool.Count);

            // Advance time past idle timeout
            _currentTime = 6f;

            // This should trigger purge on rent
            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            Assert.AreEqual(1, purgeReasons.Count);
            Assert.AreEqual(PurgeReason.IdleTimeout, purgeReasons[0]);
        }

        [Test]
        public void IdleTimeoutZeroDisablesPurging()
        {
            List<PurgeReason> purgeReasons = new();
            PoolOptions<TestPoolItem> options = new()
            {
                IdleTimeoutSeconds = 0f,
                Triggers = PurgeTrigger.OnRent,
                OnPurge = (_, reason) => purgeReasons.Add(reason),
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            _currentTime = 1000f; // Very old

            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            Assert.AreEqual(0, purgeReasons.Count);
        }

        // MinRetainCount tests
        [Test]
        public void MinRetainCountPreventsPurgingBelowThreshold()
        {
            PoolOptions<TestPoolItem> options = new()
            {
                MaxPoolSize = 2,
                MinRetainCount = 2,
                Triggers = PurgeTrigger.OnReturn,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                preWarmCount: 5,
                options: options
            );

            // Return more items
            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            Assert.GreaterOrEqual(pool.Count, 2);
        }

        // Purge trigger tests
        [Test]
        public void PurgeTriggerOnRentPurgesWhenRenting()
        {
            int purgeCount = 0;
            PoolOptions<TestPoolItem> options = new()
            {
                IdleTimeoutSeconds = 1f,
                Triggers = PurgeTrigger.OnRent,
                OnPurge = (_, _) => purgeCount++,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            using (PooledResource<TestPoolItem> resource = pool.Get()) { }
            _currentTime = 2f;

            // Purge happens on rent
            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            Assert.AreEqual(1, purgeCount);
        }

        [Test]
        public void PurgeTriggerOnReturnPurgesWhenReturning()
        {
            int purgeCount = 0;
            PoolOptions<TestPoolItem> options = new()
            {
                MaxPoolSize = 1,
                Triggers = PurgeTrigger.OnReturn,
                OnPurge = (_, _) => purgeCount++,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            // Get and return two items, second return should trigger purge
            using (PooledResource<TestPoolItem> resource = pool.Get()) { }
            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            Assert.GreaterOrEqual(purgeCount, 1);
        }

        [Test]
        public void PurgeTriggerExplicitOnlyPurgesWhenCalledExplicitly()
        {
            int purgeCount = 0;
            PoolOptions<TestPoolItem> options = new()
            {
                IdleTimeoutSeconds = 1f,
                Triggers = PurgeTrigger.Explicit,
                OnPurge = (_, _) => purgeCount++,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            using (PooledResource<TestPoolItem> resource = pool.Get()) { }
            _currentTime = 2f;
            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            // No purge yet
            Assert.AreEqual(0, purgeCount);

            // Explicit purge
            pool.Purge();
            Assert.AreEqual(1, purgeCount);
        }

        [Test]
        public void PurgeTriggerCombinedWorksWithMultipleTriggers()
        {
            int purgeCount = 0;
            PoolOptions<TestPoolItem> options = new()
            {
                IdleTimeoutSeconds = 1f,
                Triggers = PurgeTrigger.OnRent | PurgeTrigger.OnReturn,
                OnPurge = (_, _) => purgeCount++,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            using (PooledResource<TestPoolItem> resource = pool.Get()) { }
            _currentTime = 2f;

            // Should trigger on next rent
            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            Assert.GreaterOrEqual(purgeCount, 1);
        }

        [Test]
        public void PurgeTriggerPeriodicPurgesAtInterval()
        {
            int purgeCount = 0;
            PoolOptions<TestPoolItem> options = new()
            {
                IdleTimeoutSeconds = 1f,
                PurgeIntervalSeconds = 5f,
                Triggers = PurgeTrigger.Periodic,
                OnPurge = (_, _) => purgeCount++,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            // Before interval
            _currentTime = 2f;
            using (PooledResource<TestPoolItem> resource = pool.Get()) { }
            Assert.AreEqual(0, purgeCount);

            // After interval
            _currentTime = 6f;
            using (PooledResource<TestPoolItem> resource = pool.Get()) { }
            Assert.GreaterOrEqual(purgeCount, 1);
        }

        // Callback invocation tests
        [Test]
        public void OnPurgeReceivesCorrectReasonIdleTimeout()
        {
            List<PurgeReason> reasons = new();
            PoolOptions<TestPoolItem> options = new()
            {
                IdleTimeoutSeconds = 1f,
                Triggers = PurgeTrigger.OnRent,
                OnPurge = (_, reason) => reasons.Add(reason),
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            using (PooledResource<TestPoolItem> resource = pool.Get()) { }
            _currentTime = 2f;
            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            Assert.Contains(PurgeReason.IdleTimeout, reasons);
        }

        [Test]
        public void OnPurgeReceivesCorrectReasonCapacityExceeded()
        {
            List<PurgeReason> reasons = new();
            PoolOptions<TestPoolItem> options = new()
            {
                MaxPoolSize = 1,
                Triggers = PurgeTrigger.OnReturn,
                OnPurge = (_, reason) => reasons.Add(reason),
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            using (PooledResource<TestPoolItem> resource = pool.Get()) { }
            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            Assert.Contains(PurgeReason.CapacityExceeded, reasons);
        }

        [Test]
        public void OnPurgeReceivesCorrectReasonExplicit()
        {
            List<PurgeReason> reasons = new();
            PoolOptions<TestPoolItem> options = new()
            {
                Triggers = PurgeTrigger.Explicit,
                OnPurge = (_, reason) => reasons.Add(reason),
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            using (PooledResource<TestPoolItem> resource = pool.Get()) { }
            pool.Purge();

            Assert.Contains(PurgeReason.Explicit, reasons);
        }

        [Test]
        public void OnPurgeExceptionDoesNotCorruptPool()
        {
            int purgeCount = 0;
            PoolOptions<TestPoolItem> options = new()
            {
                MaxPoolSize = 1,
                Triggers = PurgeTrigger.OnReturn,
                OnPurge = (_, _) =>
                {
                    purgeCount++;
                    throw new InvalidOperationException("Test exception");
                },
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            Assert.DoesNotThrow(() =>
            {
                using (PooledResource<TestPoolItem> resource = pool.Get()) { }
                using (PooledResource<TestPoolItem> resource = pool.Get()) { }
            });

            Assert.GreaterOrEqual(purgeCount, 1);
            Assert.LessOrEqual(pool.Count, 1);
        }

        // Statistics accuracy tests
        [Test]
        public void GetStatisticsReturnsAccurateRentCount()
        {
            PoolOptions<TestPoolItem> options = new()
            {
                Triggers = PurgeTrigger.Explicit,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            for (int i = 0; i < 5; i++)
            {
                using PooledResource<TestPoolItem> resource = pool.Get();
            }

            PoolStatistics stats = pool.GetStatistics();
            Assert.AreEqual(5, stats.RentCount);
        }

        [Test]
        public void GetStatisticsReturnsAccurateReturnCount()
        {
            PoolOptions<TestPoolItem> options = new()
            {
                Triggers = PurgeTrigger.Explicit,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            for (int i = 0; i < 5; i++)
            {
                using PooledResource<TestPoolItem> resource = pool.Get();
            }

            PoolStatistics stats = pool.GetStatistics();
            Assert.AreEqual(5, stats.ReturnCount);
        }

        [Test]
        public void GetStatisticsReturnsAccuratePurgeCount()
        {
            PoolOptions<TestPoolItem> options = new()
            {
                IdleTimeoutSeconds = 1f,
                Triggers = PurgeTrigger.OnRent,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            // Add 3 items
            for (int i = 0; i < 3; i++)
            {
                using PooledResource<TestPoolItem> resource = pool.Get();
            }

            _currentTime = 2f;

            // Trigger purge by renting
            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            PoolStatistics stats = pool.GetStatistics();
            Assert.AreEqual(3, stats.IdleTimeoutPurges);
        }

        [Test]
        public void GetStatisticsTracksPeakSize()
        {
            PoolOptions<TestPoolItem> options = new()
            {
                Triggers = PurgeTrigger.Explicit,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            for (int i = 0; i < 10; i++)
            {
                using PooledResource<TestPoolItem> resource = pool.Get();
            }

            // Rent some out
            pool.Get();
            pool.Get();

            PoolStatistics stats = pool.GetStatistics();
            Assert.AreEqual(10, stats.PeakSize);
        }

        [Test]
        public void GetStatisticsTracksCurrentSize()
        {
            PoolOptions<TestPoolItem> options = new()
            {
                Triggers = PurgeTrigger.Explicit,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                preWarmCount: 5,
                options: options
            );

            PoolStatistics stats = pool.GetStatistics();
            Assert.AreEqual(5, stats.CurrentSize);
        }

        // Edge case tests
        [Test]
        public void PurgeOnEmptyPoolReturnsZero()
        {
            PoolOptions<TestPoolItem> options = new()
            {
                Triggers = PurgeTrigger.Explicit,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            int purged = pool.Purge();
            Assert.AreEqual(0, purged);
        }

        [Test]
        public void PurgeWithReasonPurgesAllItems()
        {
            List<PurgeReason> reasons = new();
            PoolOptions<TestPoolItem> options = new()
            {
                Triggers = PurgeTrigger.Explicit,
                OnPurge = (_, reason) => reasons.Add(reason),
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                preWarmCount: 3,
                options: options
            );

            int purged = pool.Purge(PurgeReason.Explicit);

            Assert.AreEqual(3, purged);
            Assert.AreEqual(3, reasons.Count);
            foreach (PurgeReason reason in reasons)
            {
                Assert.AreEqual(PurgeReason.Explicit, reason);
            }
        }

        [Test]
        public void NegativeIdleTimeoutDisablesPurging()
        {
            int purgeCount = 0;
            PoolOptions<TestPoolItem> options = new()
            {
                IdleTimeoutSeconds = -1f,
                Triggers = PurgeTrigger.OnRent,
                OnPurge = (_, _) => purgeCount++,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            using (PooledResource<TestPoolItem> resource = pool.Get()) { }
            _currentTime = 1000f;
            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            Assert.AreEqual(0, purgeCount);
        }

        [Test]
        public void GetAfterDisposeCreatesNewItem()
        {
            WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                preWarmCount: 5,
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            pool.Dispose();

            using PooledResource<TestPoolItem> resource = pool.Get(out TestPoolItem item);

            Assert.IsNotNull(item);
        }

        [Test]
        public void ReturnAfterDisposeInvokesOnDisposal()
        {
            int disposeCount = 0;
            WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                onDisposal: _ => disposeCount++,
                options: new PoolOptions<TestPoolItem>
                {
                    Triggers = PurgeTrigger.Explicit,
                    TimeProvider = TestTimeProvider,
                }
            );

            PooledResource<TestPoolItem> resource = pool.Get();
            pool.Dispose();
            resource.Dispose();

            Assert.AreEqual(1, disposeCount);
        }

        [Test]
        public void PoolStatisticsEquality()
        {
            PoolStatistics stats1 = new(1, 2, 3, 4, 5, 6, 7);
            PoolStatistics stats2 = new(1, 2, 3, 4, 5, 6, 7);
            PoolStatistics stats3 = new(1, 2, 3, 4, 5, 6, 8);

            Assert.IsTrue(stats1 == stats2);
            Assert.IsFalse(stats1 == stats3);
            Assert.IsTrue(stats1 != stats3);
            Assert.AreEqual(stats1.GetHashCode(), stats2.GetHashCode());
        }

        [Test]
        public void PoolStatisticsToStringContainsAllFields()
        {
            PoolStatistics stats = new(1, 2, 3, 4, 5, 6, 7);
            string str = stats.ToString();

            Assert.IsTrue(str.Contains("1"));
            Assert.IsTrue(str.Contains("2"));
            Assert.IsTrue(str.Contains("3"));
            Assert.IsTrue(str.Contains("4"));
            Assert.IsTrue(str.Contains("5"));
            Assert.IsTrue(str.Contains("6"));
            Assert.IsTrue(str.Contains("7"));
        }

        [Test]
        public void DynamicPropertyChangesAreRespected()
        {
            PoolOptions<TestPoolItem> options = new()
            {
                MaxPoolSize = 10,
                Triggers = PurgeTrigger.OnReturn,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            // Add items
            for (int i = 0; i < 5; i++)
            {
                using PooledResource<TestPoolItem> resource = pool.Get();
            }

            Assert.AreEqual(5, pool.Count);

            // Change max size
            pool.MaxPoolSize = 2;

            // Add more items to trigger purge
            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            Assert.LessOrEqual(pool.Count, 2);
        }

        // Intelligent purging tests
        [Test]
        public void IntelligentPurgingDefaultsToDisabled()
        {
            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: new PoolOptions<TestPoolItem> { TimeProvider = TestTimeProvider }
            );

            Assert.IsFalse(pool.UseIntelligentPurging);
        }

        [Test]
        public void IntelligentPurgingEnabledViaOptions()
        {
            PoolOptions<TestPoolItem> options = new()
            {
                UseIntelligentPurging = true,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            Assert.IsTrue(pool.UseIntelligentPurging);
        }

        [Test]
        public void IntelligentPurgingSetsIdleTimeoutIfNotConfigured()
        {
            PoolOptions<TestPoolItem> options = new()
            {
                UseIntelligentPurging = true,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            Assert.Greater(pool.IdleTimeoutSeconds, 0f);
        }

        [Test]
        public void IntelligentPurgingRespectsComfortableSize()
        {
            int purgeCount = 0;
            PoolOptions<TestPoolItem> options = new()
            {
                UseIntelligentPurging = true,
                IdleTimeoutSeconds = 1f,
                BufferMultiplier = 1.5f,
                RollingWindowSeconds = 60f,
                Triggers = PurgeTrigger.OnRent,
                OnPurge = (_, _) => purgeCount++,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            // Rent and return several items to establish a usage pattern
            List<PooledResource<TestPoolItem>> resources = new();
            for (int i = 0; i < 5; i++)
            {
                resources.Add(pool.Get());
            }

            // Return them all
            foreach (PooledResource<TestPoolItem> r in resources)
            {
                r.Dispose();
            }
            resources.Clear();

            Assert.AreEqual(5, pool.Count);

            // Advance time past idle timeout
            _currentTime = 2f;

            // Get an item - should not purge all since comfortable size accounts for usage
            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            // With a high water mark of 5 and buffer of 1.5, comfortable size is 7
            // Since we only had 5 items, all should still be there minus one for the rent
            Assert.LessOrEqual(purgeCount, 5);
        }

        [Test]
        public void IntelligentPurgingHysteresisPreventsPurgeAfterSpike()
        {
            int purgeCount = 0;
            PoolOptions<TestPoolItem> options = new()
            {
                UseIntelligentPurging = true,
                IdleTimeoutSeconds = 1f,
                HysteresisSeconds = 30f,
                SpikeThresholdMultiplier = 1.5f,
                Triggers = PurgeTrigger.OnRent,
                OnPurge = (_, _) => purgeCount++,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            // Create a spike - get many items at once
            List<PooledResource<TestPoolItem>> resources = new();
            for (int i = 0; i < 10; i++)
            {
                resources.Add(pool.Get());
            }

            // Return them all
            foreach (PooledResource<TestPoolItem> r in resources)
            {
                r.Dispose();
            }
            resources.Clear();

            // Advance past idle timeout but within hysteresis
            _currentTime = 2f;

            // Get should not trigger purge due to hysteresis
            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            Assert.AreEqual(0, purgeCount);
        }

        [Test]
        public void IntelligentPurgingCanBeDisabledAtRuntime()
        {
            int purgeCount = 0;
            PoolOptions<TestPoolItem> options = new()
            {
                UseIntelligentPurging = true,
                IdleTimeoutSeconds = 1f,
                HysteresisSeconds = 30f,
                Triggers = PurgeTrigger.OnRent,
                OnPurge = (_, _) => purgeCount++,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            // Add items
            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            _currentTime = 2f;

            // Disable intelligent purging
            pool.UseIntelligentPurging = false;

            // Now purge should happen
            using (PooledResource<TestPoolItem> resource = pool.Get()) { }

            Assert.AreEqual(1, purgeCount);
        }

        [Test]
        public void PoolPurgeSettingsConfigureSetsTypeSpecificOptions()
        {
            PoolPurgeSettings.ResetToDefaults();

            try
            {
                PoolPurgeSettings.Configure<TestPoolItem>(opts =>
                {
                    opts.Enabled = true;
                    opts.IdleTimeoutSeconds = 999f;
                });

                PoolPurgeEffectiveOptions effective =
                    PoolPurgeSettings.GetEffectiveOptions<TestPoolItem>();

                Assert.IsTrue(effective.Enabled);
                Assert.AreEqual(999f, effective.IdleTimeoutSeconds);
                Assert.AreEqual(PoolPurgeConfigurationSource.TypeSpecific, effective.Source);
            }
            finally
            {
                PoolPurgeSettings.ResetToDefaults();
            }
        }

        [Test]
        public void PoolPurgeSettingsDisableDisablesForType()
        {
            PoolPurgeSettings.ResetToDefaults();

            try
            {
                PoolPurgeSettings.GlobalEnabled = true;
                PoolPurgeSettings.Disable<TestPoolItem>();

                PoolPurgeEffectiveOptions effective =
                    PoolPurgeSettings.GetEffectiveOptions<TestPoolItem>();

                Assert.IsFalse(effective.Enabled);
                Assert.AreEqual(PoolPurgeConfigurationSource.TypeDisabled, effective.Source);
            }
            finally
            {
                PoolPurgeSettings.ResetToDefaults();
            }
        }

        [Test]
        public void PoolPurgeSettingsGlobalEnabledDefaultsToFalse()
        {
            PoolPurgeSettings.ResetToDefaults();

            Assert.IsFalse(PoolPurgeSettings.GlobalEnabled);
        }

        [Test]
        public void PoolPurgeSettingsConfigureGenericAppliesToGenericTypes()
        {
            PoolPurgeSettings.ResetToDefaults();

            try
            {
                PoolPurgeSettings.ConfigureGeneric(
                    typeof(List<>),
                    opts =>
                    {
                        opts.Enabled = true;
                        opts.IdleTimeoutSeconds = 123f;
                    }
                );

                PoolPurgeEffectiveOptions effective = PoolPurgeSettings.GetEffectiveOptions<
                    List<int>
                >();

                Assert.IsTrue(effective.Enabled);
                Assert.AreEqual(123f, effective.IdleTimeoutSeconds);
                Assert.AreEqual(PoolPurgeConfigurationSource.GenericPattern, effective.Source);
            }
            finally
            {
                PoolPurgeSettings.ResetToDefaults();
            }
        }

        [Test]
        public void PoolPurgeSettingsTypeSpecificOverridesGenericPattern()
        {
            PoolPurgeSettings.ResetToDefaults();

            try
            {
                PoolPurgeSettings.ConfigureGeneric(
                    typeof(List<>),
                    opts =>
                    {
                        opts.IdleTimeoutSeconds = 100f;
                    }
                );

                PoolPurgeSettings.Configure<List<int>>(opts =>
                {
                    opts.IdleTimeoutSeconds = 200f;
                });

                PoolPurgeEffectiveOptions effective = PoolPurgeSettings.GetEffectiveOptions<
                    List<int>
                >();

                Assert.AreEqual(200f, effective.IdleTimeoutSeconds);
                Assert.AreEqual(PoolPurgeConfigurationSource.TypeSpecific, effective.Source);
            }
            finally
            {
                PoolPurgeSettings.ResetToDefaults();
            }
        }

#if !SINGLE_THREADED
        // Thread safety tests
        [Test]
        public void ConcurrentGetAndReturnMaintainsIntegrity()
        {
            PoolOptions<TestPoolItem> options = new()
            {
                Triggers = PurgeTrigger.Explicit,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            const int iterations = 100;
            const int threadCount = 4;

            Task[] tasks = new Task[threadCount];
            for (int t = 0; t < threadCount; t++)
            {
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        using PooledResource<TestPoolItem> resource = pool.Get(
                            out TestPoolItem item
                        );
                        Assert.IsNotNull(item);
                    }
                });
            }

            Task.WaitAll(tasks);

            PoolStatistics stats = pool.GetStatistics();
            Assert.AreEqual(iterations * threadCount, stats.RentCount);
            Assert.AreEqual(iterations * threadCount, stats.ReturnCount);
        }

        [Test]
        public void ConcurrentPurgeDoesNotCorruptPool()
        {
            PoolOptions<TestPoolItem> options = new()
            {
                MaxPoolSize = 5,
                Triggers = PurgeTrigger.OnReturn,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            const int iterations = 50;
            const int threadCount = 4;

            Task[] tasks = new Task[threadCount];
            for (int t = 0; t < threadCount; t++)
            {
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        using PooledResource<TestPoolItem> resource = pool.Get();
                        if (i % 10 == 0)
                        {
                            pool.Purge();
                        }
                    }
                });
            }

            Assert.DoesNotThrow(() => Task.WaitAll(tasks));
        }

        [Test]
        public void ConcurrentGetStatisticsIsThreadSafe()
        {
            PoolOptions<TestPoolItem> options = new()
            {
                Triggers = PurgeTrigger.Explicit,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                preWarmCount: 10,
                options: options
            );

            const int iterations = 100;
            const int threadCount = 4;

            Task[] tasks = new Task[threadCount];
            for (int t = 0; t < threadCount; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        if (threadId % 2 == 0)
                        {
                            using PooledResource<TestPoolItem> resource = pool.Get();
                        }
                        else
                        {
                            PoolStatistics stats = pool.GetStatistics();
                            Assert.GreaterOrEqual(stats.CurrentSize, 0);
                        }
                    }
                });
            }

            Assert.DoesNotThrow(() => Task.WaitAll(tasks));
        }

        [Test]
        public void ConcurrentPropertyAccessIsThreadSafe()
        {
            PoolOptions<TestPoolItem> options = new()
            {
                MaxPoolSize = 10,
                IdleTimeoutSeconds = 5f,
                Triggers = PurgeTrigger.OnRent,
                TimeProvider = TestTimeProvider,
            };

            using WallstopGenericPool<TestPoolItem> pool = new(
                () => new TestPoolItem(),
                options: options
            );

            const int iterations = 100;
            Task[] tasks = new Task[4];

            tasks[0] = Task.Run(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    pool.MaxPoolSize = i % 20;
                    int _ = pool.MaxPoolSize;
                }
            });

            tasks[1] = Task.Run(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    pool.IdleTimeoutSeconds = i * 0.1f;
                    float _ = pool.IdleTimeoutSeconds;
                }
            });

            tasks[2] = Task.Run(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    pool.MinRetainCount = i % 5;
                    int _ = pool.MinRetainCount;
                }
            });

            tasks[3] = Task.Run(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    pool.Triggers = (PurgeTrigger)(i % 15);
                    PurgeTrigger _ = pool.Triggers;
                }
            });

            Assert.DoesNotThrow(() => Task.WaitAll(tasks));
        }
#endif

        // PoolTypeResolver tests
        [Test]
        public void GenericMatchingExactClosedGenericMatches()
        {
            Type concreteType = typeof(List<int>);
            Type patternType = typeof(List<int>);

            Assert.IsTrue(PoolTypeResolver.TypeMatchesPattern(concreteType, patternType));
            Assert.AreEqual(0, PoolTypeResolver.GetMatchPriority(concreteType, patternType));
        }

        [Test]
        public void GenericMatchingOpenGenericMatchesAnyClosed()
        {
            Type concreteType = typeof(List<int>);
            Type patternType = typeof(List<>);

            Assert.IsTrue(PoolTypeResolver.TypeMatchesPattern(concreteType, patternType));

            Type concreteType2 = typeof(List<string>);
            Assert.IsTrue(PoolTypeResolver.TypeMatchesPattern(concreteType2, patternType));

            Type concreteType3 = typeof(List<TestPoolItem>);
            Assert.IsTrue(PoolTypeResolver.TypeMatchesPattern(concreteType3, patternType));
        }

        [Test]
        public void GenericMatchingNestedGenericExactMatch()
        {
            Type concreteType = typeof(List<List<int>>);
            Type patternType = typeof(List<List<int>>);

            Assert.IsTrue(PoolTypeResolver.TypeMatchesPattern(concreteType, patternType));
            Assert.AreEqual(0, PoolTypeResolver.GetMatchPriority(concreteType, patternType));
        }

        [Test]
        public void GenericMatchingNestedGenericOuterOpenMatch()
        {
            Type concreteType = typeof(List<List<int>>);
            Type patternType = typeof(List<>);

            Assert.IsTrue(PoolTypeResolver.TypeMatchesPattern(concreteType, patternType));
            Assert.AreEqual(2, PoolTypeResolver.GetMatchPriority(concreteType, patternType));
        }

        [Test]
        public void GenericMatchingMultipleTypeArgsMatches()
        {
            Type concreteType = typeof(Dictionary<string, int>);
            Type patternType = typeof(Dictionary<,>);

            Assert.IsTrue(PoolTypeResolver.TypeMatchesPattern(concreteType, patternType));
        }

        [Test]
        public void GenericMatchingSimplifiedSyntaxResolvesCorrectly()
        {
            Type resolved = PoolTypeResolver.ResolveType("List<int>");
            Assert.AreEqual(typeof(List<int>), resolved);

            resolved = PoolTypeResolver.ResolveType("Dictionary<string, int>");
            Assert.AreEqual(typeof(Dictionary<string, int>), resolved);

            resolved = PoolTypeResolver.ResolveType("HashSet<string>");
            Assert.AreEqual(typeof(HashSet<string>), resolved);
        }

        [Test]
        public void GenericMatchingOpenGenericSimplifiedSyntaxResolvesCorrectly()
        {
            Type resolved = PoolTypeResolver.ResolveType("List<>");
            Assert.AreEqual(typeof(List<>), resolved);

            resolved = PoolTypeResolver.ResolveType("Dictionary<,>");
            Assert.AreEqual(typeof(Dictionary<,>), resolved);
        }

        [Test]
        public void GenericMatchingNestedGenericSimplifiedSyntaxResolvesCorrectly()
        {
            Type resolved = PoolTypeResolver.ResolveType("List<List<int>>");
            Assert.AreEqual(typeof(List<List<int>>), resolved);

            resolved = PoolTypeResolver.ResolveType("Dictionary<string, List<int>>");
            Assert.AreEqual(typeof(Dictionary<string, List<int>>), resolved);
        }

        [Test]
        public void GenericMatchingFullAssemblyQualifiedNameResolves()
        {
            Type expected = typeof(List<>);
            Type resolved = PoolTypeResolver.ResolveType("System.Collections.Generic.List`1");
            Assert.AreEqual(expected, resolved);
        }

        [Test]
        public void GenericMatchingPriorityOrderMostSpecificWins()
        {
            PoolPurgeSettings.ResetToDefaults();

            try
            {
                // Configure outer open generic with lower timeout
                PoolPurgeSettings.ConfigureGeneric(
                    typeof(List<>),
                    opts =>
                    {
                        opts.Enabled = true;
                        opts.IdleTimeoutSeconds = 100f;
                    }
                );

                // Configure specific closed generic with higher timeout
                PoolPurgeSettings.Configure<List<List<int>>>(opts =>
                {
                    opts.IdleTimeoutSeconds = 500f;
                });

                // List<List<int>> should get the specific config (500s)
                PoolPurgeEffectiveOptions effectiveNested = PoolPurgeSettings.GetEffectiveOptions<
                    List<List<int>>
                >();
                Assert.AreEqual(500f, effectiveNested.IdleTimeoutSeconds);
                Assert.AreEqual(PoolPurgeConfigurationSource.TypeSpecific, effectiveNested.Source);

                // List<int> should get the generic config (100s)
                PoolPurgeEffectiveOptions effectiveSimple = PoolPurgeSettings.GetEffectiveOptions<
                    List<int>
                >();
                Assert.AreEqual(100f, effectiveSimple.IdleTimeoutSeconds);
                Assert.AreEqual(
                    PoolPurgeConfigurationSource.GenericPattern,
                    effectiveSimple.Source
                );
            }
            finally
            {
                PoolPurgeSettings.ResetToDefaults();
            }
        }

        [Test]
        public void GenericMatchingGetAllMatchingPatternsReturnsCorrectOrder()
        {
            Type concreteType = typeof(List<List<int>>);
            List<Type> patterns = new(PoolTypeResolver.GetAllMatchingPatterns(concreteType));

            Assert.GreaterOrEqual(patterns.Count, 2);
            Assert.AreEqual(typeof(List<List<int>>), patterns[0]); // Exact type first
            Assert.AreEqual(typeof(List<>), patterns[patterns.Count - 1]); // Open generic last
        }

        [Test]
        public void GenericMatchingNonGenericTypeDoesNotMatchGenericPattern()
        {
            Type concreteType = typeof(string);
            Type patternType = typeof(List<>);

            Assert.IsFalse(PoolTypeResolver.TypeMatchesPattern(concreteType, patternType));
        }

        [Test]
        public void GenericMatchingDifferentGenericDefinitionDoesNotMatch()
        {
            Type concreteType = typeof(List<int>);
            Type patternType = typeof(HashSet<>);

            Assert.IsFalse(PoolTypeResolver.TypeMatchesPattern(concreteType, patternType));
        }

        [Test]
        public void GenericMatchingGetDisplayNameFormatsCorrectly()
        {
            Assert.AreEqual("List<int>", PoolTypeResolver.GetDisplayName(typeof(List<int>)));
            Assert.AreEqual("List<>", PoolTypeResolver.GetDisplayName(typeof(List<>)));
            Assert.AreEqual(
                "Dictionary<string, int>",
                PoolTypeResolver.GetDisplayName(typeof(Dictionary<string, int>))
            );
            Assert.AreEqual(
                "Dictionary<,>",
                PoolTypeResolver.GetDisplayName(typeof(Dictionary<,>))
            );
            Assert.AreEqual(
                "List<List<int>>",
                PoolTypeResolver.GetDisplayName(typeof(List<List<int>>))
            );
        }

        [Test]
        public void GenericMatchingNullInputsHandleGracefully()
        {
            Assert.IsNull(PoolTypeResolver.ResolveType(null));
            Assert.IsNull(PoolTypeResolver.ResolveType(""));
            Assert.IsNull(PoolTypeResolver.ResolveType("   "));
            Assert.IsFalse(PoolTypeResolver.TypeMatchesPattern(null, typeof(List<>)));
            Assert.IsFalse(PoolTypeResolver.TypeMatchesPattern(typeof(List<int>), (Type)null));
            Assert.AreEqual(int.MaxValue, PoolTypeResolver.GetMatchPriority(null, typeof(List<>)));
        }

        [Test]
        public void GenericMatchingBuiltInTypeAliasesResolveCorrectly()
        {
            Assert.AreEqual(typeof(int), PoolTypeResolver.ResolveType("int"));
            Assert.AreEqual(typeof(string), PoolTypeResolver.ResolveType("string"));
            Assert.AreEqual(typeof(bool), PoolTypeResolver.ResolveType("bool"));
            Assert.AreEqual(typeof(float), PoolTypeResolver.ResolveType("float"));
            Assert.AreEqual(typeof(double), PoolTypeResolver.ResolveType("double"));
        }

        [Test]
        public void GenericMatchingHashSetOpenGenericMatchesCustomClass()
        {
            Type concreteType = typeof(HashSet<TestPoolItem>);
            Type patternType = typeof(HashSet<>);

            Assert.IsTrue(PoolTypeResolver.TypeMatchesPattern(concreteType, patternType));
        }

        // PoolTypeConfiguration tests
        [Test]
        public void PoolTypeConfigurationResolvedTypeCachesResult()
        {
            PoolTypeConfiguration config = new() { TypeName = "List<int>" };

            Type first = config.ResolvedType;
            Type second = config.ResolvedType;

            Assert.IsNotNull(first);
            Assert.AreEqual(typeof(List<int>), first);
            Assert.AreSame(first, second);
        }

        [Test]
        public void PoolTypeConfigurationIsOpenGenericDetectsCorrectly()
        {
            PoolTypeConfiguration openConfig = new() { TypeName = "List<>" };
            Assert.IsTrue(openConfig.IsOpenGeneric);

            PoolTypeConfiguration closedConfig = new() { TypeName = "List<int>" };
            Assert.IsFalse(closedConfig.IsOpenGeneric);

            PoolTypeConfiguration invalidConfig = new() { TypeName = "NotAType<>" };
            Assert.IsFalse(invalidConfig.IsOpenGeneric);
        }

        [Test]
        public void PoolTypeConfigurationMatchesExactType()
        {
            PoolTypeConfiguration config = new() { TypeName = "List<int>" };

            Assert.IsTrue(config.Matches(typeof(List<int>)));
            Assert.IsFalse(config.Matches(typeof(List<string>)));
        }

        [Test]
        public void PoolTypeConfigurationMatchesOpenGeneric()
        {
            PoolTypeConfiguration config = new() { TypeName = "List<>" };

            Assert.IsTrue(config.Matches(typeof(List<int>)));
            Assert.IsTrue(config.Matches(typeof(List<string>)));
            Assert.IsTrue(config.Matches(typeof(List<TestPoolItem>)));
            Assert.IsFalse(config.Matches(typeof(HashSet<int>)));
        }

        [Test]
        public void PoolTypeConfigurationGetMatchPriorityReturnsCorrectValues()
        {
            PoolTypeConfiguration exactConfig = new() { TypeName = "List<int>" };
            PoolTypeConfiguration openConfig = new() { TypeName = "List<>" };

            Assert.AreEqual(0, exactConfig.GetMatchPriority(typeof(List<int>)));
            Assert.AreEqual(2, openConfig.GetMatchPriority(typeof(List<int>)));
            Assert.AreEqual(int.MaxValue, exactConfig.GetMatchPriority(typeof(List<string>)));
        }

        [Test]
        public void PoolTypeConfigurationInvalidateCacheForcesReResolution()
        {
            PoolTypeConfiguration config = new() { TypeName = "List<int>" };

            Type first = config.ResolvedType;
            Assert.IsNotNull(first);

            config.InvalidateCache();
            config.TypeName = "List<string>";

            Type second = config.ResolvedType;
            Assert.IsNotNull(second);
            Assert.AreEqual(typeof(List<string>), second);
            Assert.AreNotEqual(first, second);
        }
    }
}
