// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.DataStructure;

    [TestFixture]
    public sealed class CacheTests
    {
        private float _currentTime;

        private float TimeProvider()
        {
            return _currentTime;
        }

        [SetUp]
        public void SetUp()
        {
            _currentTime = 0f;
        }

        [Test]
        public void ConstructorWithDefaultOptionsUsesDefaults()
        {
            // CacheOptions is a struct, so we use default instead of null
            Cache<string, int> cache = new(default);
            Assert.AreEqual(0, cache.Count);
            Assert.AreEqual(CacheOptions<string, int>.DefaultMaximumSize, cache.Capacity);
            cache.Dispose();
        }

        [Test]
        public void ConstructorWithOptionsUsesProvidedValues()
        {
            CacheOptions<string, int> options = new() { MaximumSize = 50 };
            Cache<string, int> cache = new(options);
            Assert.AreEqual(0, cache.Count);
            Assert.AreEqual(50, cache.Capacity);
            cache.Dispose();
        }

        [Test]
        public void SetAndTryGetRetrievesValue()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("key1", 42);

            Assert.IsTrue(cache.TryGet("key1", out int value));
            Assert.AreEqual(42, value);
        }

        [Test]
        public void TryGetReturnsFalseForMissingKey()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            Assert.IsFalse(cache.TryGet("nonexistent", out int value));
            Assert.AreEqual(default(int), value);
        }

        [Test]
        public void TryGetWithNullKeyReturnsFalse()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .Build();

            Assert.IsFalse(cache.TryGet(null, out int value));
            Assert.AreEqual(default(int), value);
        }

        [Test]
        public void SetWithNullKeyDoesNotThrow()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .Build();

            cache.Set(null, 42);
            Assert.AreEqual(0, cache.Count);
        }

        [Test]
        public void SetUpdatesExistingValue()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("key1", 42);
            cache.Set("key1", 100);

            Assert.IsTrue(cache.TryGet("key1", out int value));
            Assert.AreEqual(100, value);
            Assert.AreEqual(1, cache.Count);
        }

        [Test]
        public void CountReturnsCorrectValue()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            Assert.AreEqual(0, cache.Count);

            cache.Set("a", 1);
            Assert.AreEqual(1, cache.Count);

            cache.Set("b", 2);
            Assert.AreEqual(2, cache.Count);

            cache.Set("c", 3);
            Assert.AreEqual(3, cache.Count);
        }

        [Test]
        public void TryRemoveRemovesEntry()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("key1", 42);

            Assert.IsTrue(cache.TryRemove("key1"));
            Assert.AreEqual(0, cache.Count);
            Assert.IsFalse(cache.TryGet("key1", out _));
        }

        [Test]
        public void TryRemoveReturnsFalseForMissingKey()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .Build();

            Assert.IsFalse(cache.TryRemove("nonexistent"));
        }

        [Test]
        public void TryRemoveWithOutParameterReturnsValue()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("key1", 42);

            Assert.IsTrue(cache.TryRemove("key1", out int value));
            Assert.AreEqual(42, value);
        }

        [Test]
        public void ClearRemovesAllEntries()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.Set("b", 2);
            cache.Set("c", 3);

            cache.Clear();

            Assert.AreEqual(0, cache.Count);
            Assert.IsFalse(cache.TryGet("a", out _));
            Assert.IsFalse(cache.TryGet("b", out _));
            Assert.IsFalse(cache.TryGet("c", out _));
        }

        [Test]
        public void ContainsKeyReturnsTrueForExistingKey()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("key1", 42);

            Assert.IsTrue(cache.ContainsKey("key1"));
        }

        [Test]
        public void ContainsKeyReturnsFalseForMissingKey()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .Build();

            Assert.IsFalse(cache.ContainsKey("nonexistent"));
        }

        [Test]
        public void GetOrAddWithFactoryLoadsValue()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            int result = cache.GetOrAdd("key1", static k => 42);

            Assert.AreEqual(42, result);
            Assert.IsTrue(cache.TryGet("key1", out int cached));
            Assert.AreEqual(42, cached);
        }

        [Test]
        public void GetOrAddReturnsCachedValueIfPresent()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("key1", 42);
            int factoryCallCount = 0;

            int result = cache.GetOrAdd(
                "key1",
                k =>
                {
                    factoryCallCount++;
                    return 100;
                }
            );

            Assert.AreEqual(42, result);
            Assert.AreEqual(0, factoryCallCount);
        }

        [Test]
        public void GetOrAddUsesDefaultLoaderWhenFactoryIsNull()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build(static k => k.Length);

            int result = cache.GetOrAdd("hello", null);

            Assert.AreEqual(5, result);
        }

        [Test]
        public void GetAllRetrievesMultipleValues()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.Set("b", 2);
            cache.Set("c", 3);

            Dictionary<string, int> results = new();
            cache.GetAll(new[] { "a", "b", "d" }, results);

            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(1, results["a"]);
            Assert.AreEqual(2, results["b"]);
            Assert.IsFalse(results.ContainsKey("d"));
        }

        [Test]
        public void SetAllAddsMultipleEntries()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            cache.SetAll(
                new[]
                {
                    new KeyValuePair<string, int>("a", 1),
                    new KeyValuePair<string, int>("b", 2),
                    new KeyValuePair<string, int>("c", 3),
                }
            );

            Assert.AreEqual(3, cache.Count);
            Assert.IsTrue(cache.TryGet("a", out int va) && va == 1);
            Assert.IsTrue(cache.TryGet("b", out int vb) && vb == 2);
            Assert.IsTrue(cache.TryGet("c", out int vc) && vc == 3);
        }
    }

    [TestFixture]
    public sealed class CacheLruEvictionTests
    {
        private float _currentTime;

        private float TimeProvider()
        {
            return _currentTime;
        }

        [SetUp]
        public void SetUp()
        {
            _currentTime = 0f;
        }

        [Test]
        public void LruEvictsLeastRecentlyUsedEntry()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(3)
                .EvictionPolicy(EvictionPolicy.Lru)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            _currentTime += 0.1f;
            cache.Set("b", 2);
            _currentTime += 0.1f;
            cache.Set("c", 3);
            _currentTime += 0.1f;

            cache.TryGet("a", out _);
            _currentTime += 0.1f;

            cache.Set("d", 4);

            Assert.IsTrue(cache.ContainsKey("a"));
            Assert.IsFalse(cache.ContainsKey("b"));
            Assert.IsTrue(cache.ContainsKey("c"));
            Assert.IsTrue(cache.ContainsKey("d"));
        }

        [Test]
        public void LruAccessMovesEntryToHead()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(3)
                .EvictionPolicy(EvictionPolicy.Lru)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            _currentTime += 0.1f;
            cache.Set("b", 2);
            _currentTime += 0.1f;
            cache.Set("c", 3);
            _currentTime += 0.1f;

            cache.TryGet("a", out _);
            _currentTime += 0.1f;
            cache.TryGet("b", out _);
            _currentTime += 0.1f;

            cache.Set("d", 4);
            cache.Set("e", 5);

            Assert.IsTrue(cache.ContainsKey("b"));
            Assert.IsFalse(cache.ContainsKey("c"));
            Assert.IsFalse(cache.ContainsKey("a"));
            Assert.IsTrue(cache.ContainsKey("d"));
            Assert.IsTrue(cache.ContainsKey("e"));
        }
    }

    [TestFixture]
    public sealed class CacheFifoEvictionTests
    {
        private float _currentTime;

        private float TimeProvider()
        {
            return _currentTime;
        }

        [SetUp]
        public void SetUp()
        {
            _currentTime = 0f;
        }

        [Test]
        public void FifoEvictsOldestEntry()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(3)
                .EvictionPolicy(EvictionPolicy.Fifo)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.Set("b", 2);
            cache.Set("c", 3);

            cache.Set("d", 4);

            Assert.IsFalse(cache.ContainsKey("a"));
            Assert.IsTrue(cache.ContainsKey("b"));
            Assert.IsTrue(cache.ContainsKey("c"));
            Assert.IsTrue(cache.ContainsKey("d"));
        }

        [Test]
        public void FifoAccessDoesNotChangeEvictionOrder()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(3)
                .EvictionPolicy(EvictionPolicy.Fifo)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.Set("b", 2);
            cache.Set("c", 3);

            cache.TryGet("a", out _);

            cache.Set("d", 4);

            Assert.IsFalse(cache.ContainsKey("a"));
            Assert.IsTrue(cache.ContainsKey("b"));
            Assert.IsTrue(cache.ContainsKey("c"));
            Assert.IsTrue(cache.ContainsKey("d"));
        }
    }

    [TestFixture]
    public sealed class CacheLfuEvictionTests
    {
        private float _currentTime;

        private float TimeProvider()
        {
            return _currentTime;
        }

        [SetUp]
        public void SetUp()
        {
            _currentTime = 0f;
        }

        [Test]
        public void LfuEvictsLeastFrequentlyUsedEntry()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(3)
                .EvictionPolicy(EvictionPolicy.Lfu)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.Set("b", 2);
            cache.Set("c", 3);

            cache.TryGet("a", out _);
            cache.TryGet("a", out _);
            cache.TryGet("b", out _);

            cache.Set("d", 4);

            Assert.IsTrue(cache.ContainsKey("a"));
            Assert.IsTrue(cache.ContainsKey("b"));
            Assert.IsFalse(cache.ContainsKey("c"));
            Assert.IsTrue(cache.ContainsKey("d"));
        }

        [Test]
        public void LfuTiesBrokenByRecency()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(3)
                .EvictionPolicy(EvictionPolicy.Lfu)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            _currentTime += 0.1f;
            cache.Set("b", 2);
            _currentTime += 0.1f;
            cache.Set("c", 3);
            _currentTime += 0.1f;

            cache.Set("d", 4);

            Assert.IsFalse(cache.ContainsKey("a"));
            Assert.IsTrue(cache.ContainsKey("b"));
            Assert.IsTrue(cache.ContainsKey("c"));
            Assert.IsTrue(cache.ContainsKey("d"));
        }
    }

    [TestFixture]
    public sealed class CacheSlruEvictionTests
    {
        private float _currentTime;

        private float TimeProvider()
        {
            return _currentTime;
        }

        [SetUp]
        public void SetUp()
        {
            _currentTime = 0f;
        }

        [Test]
        public void SlruNewEntriesGoToProbation()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(5)
                .EvictionPolicy(EvictionPolicy.Slru)
                .ProtectedRatio(0.6f)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.Set("b", 2);
            cache.Set("c", 3);
            cache.Set("d", 4);
            cache.Set("e", 5);

            Assert.AreEqual(5, cache.Count);
        }

        [Test]
        public void SlruAccessPromotesToProtected()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(4)
                .EvictionPolicy(EvictionPolicy.Slru)
                .ProtectedRatio(0.5f)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.Set("b", 2);

            cache.TryGet("a", out _);

            cache.Set("c", 3);
            cache.Set("d", 4);

            cache.Set("e", 5);

            Assert.IsTrue(cache.ContainsKey("a"));
            Assert.AreEqual(4, cache.Count);
        }

        [Test]
        public void SlruEvictsFromProbationFirst()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(3)
                .EvictionPolicy(EvictionPolicy.Slru)
                .ProtectedRatio(0.67f)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.Set("b", 2);
            cache.Set("c", 3);

            cache.TryGet("a", out _);
            cache.TryGet("b", out _);

            cache.Set("d", 4);

            Assert.IsTrue(cache.ContainsKey("a"));
            Assert.IsTrue(cache.ContainsKey("b"));
            Assert.IsFalse(cache.ContainsKey("c"));
            Assert.IsTrue(cache.ContainsKey("d"));
        }
    }

    [TestFixture]
    public sealed class CacheRandomEvictionTests
    {
        private float _currentTime;

        private float TimeProvider()
        {
            return _currentTime;
        }

        [SetUp]
        public void SetUp()
        {
            _currentTime = 0f;
        }

        [Test]
        public void RandomEvictsAnEntry()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(3)
                .EvictionPolicy(EvictionPolicy.Random)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.Set("b", 2);
            cache.Set("c", 3);

            cache.Set("d", 4);

            Assert.AreEqual(3, cache.Count);
            Assert.IsTrue(cache.ContainsKey("d"));
        }
    }

    [TestFixture]
    public sealed class CacheExpirationTests
    {
        private float _currentTime;

        private float TimeProvider()
        {
            return _currentTime;
        }

        [SetUp]
        public void SetUp()
        {
            _currentTime = 0f;
        }

        [Test]
        public void ExpireAfterWriteEvictsExpiredEntries()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .ExpireAfterWrite(1.0f)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("key1", 42);
            Assert.IsTrue(cache.TryGet("key1", out _));

            _currentTime = 1.1f;

            Assert.IsFalse(cache.TryGet("key1", out _));
        }

        [Test]
        public void ExpireAfterAccessExtendsTtl()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .ExpireAfterAccess(1.0f)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("key1", 42);

            _currentTime = 0.5f;
            Assert.IsTrue(cache.TryGet("key1", out _));

            _currentTime = 1.4f;
            Assert.IsTrue(cache.TryGet("key1", out _));

            _currentTime = 2.5f;
            Assert.IsFalse(cache.TryGet("key1", out _));
        }

        [Test]
        public void CustomTtlOverridesDefaultExpiration()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .ExpireAfterWrite(5.0f)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("key1", 42, 0.5f);

            _currentTime = 0.6f;

            Assert.IsFalse(cache.TryGet("key1", out _));
        }

        [Test]
        public void CleanUpRemovesExpiredEntries()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .ExpireAfterWrite(1.0f)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.Set("b", 2);
            cache.Set("c", 3);

            _currentTime = 1.1f;

            cache.CleanUp();

            Assert.AreEqual(0, cache.Count);
        }

        [Test]
        public void PerEntryExpirationFunction()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .ExpireAfter(static (key, value) => value * 0.1f)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("short", 1);
            cache.Set("long", 10);

            _currentTime = 0.15f;

            Assert.IsFalse(cache.TryGet("short", out _));
            Assert.IsTrue(cache.TryGet("long", out _));
        }
    }

    [TestFixture]
    public sealed class CacheStatisticsTests
    {
        private float _currentTime;

        private float TimeProvider()
        {
            return _currentTime;
        }

        [SetUp]
        public void SetUp()
        {
            _currentTime = 0f;
        }

        [Test]
        public void StatisticsTrackHitsAndMisses()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .RecordStatistics()
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);

            cache.TryGet("a", out _);
            cache.TryGet("a", out _);
            cache.TryGet("b", out _);
            cache.TryGet("c", out _);

            CacheStatistics stats = cache.GetStatistics();

            Assert.AreEqual(2, stats.HitCount);
            Assert.AreEqual(2, stats.MissCount);
            Assert.AreEqual(0.5, stats.HitRate, 0.001);
        }

        [Test]
        public void StatisticsTrackEvictions()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(2)
                .RecordStatistics()
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.Set("b", 2);
            cache.Set("c", 3);

            CacheStatistics stats = cache.GetStatistics();

            Assert.AreEqual(1, stats.EvictionCount);
        }

        [Test]
        public void StatisticsTrackLoads()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .RecordStatistics()
                .TimeProvider(TimeProvider)
                .Build();

            cache.GetOrAdd("a", static k => 1);
            cache.GetOrAdd("b", static k => 2);
            cache.GetOrAdd("a", static k => 100);

            CacheStatistics stats = cache.GetStatistics();

            Assert.AreEqual(2, stats.LoadCount);
        }

        [Test]
        public void StatisticsTrackCurrentAndPeakSize()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(5)
                .RecordStatistics()
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.Set("b", 2);
            cache.Set("c", 3);

            cache.TryRemove("b");

            CacheStatistics stats = cache.GetStatistics();

            Assert.AreEqual(2, stats.CurrentSize);
            Assert.AreEqual(3, stats.PeakSize);
        }

        [Test]
        public void StatisticsNotRecordedWhenDisabled()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.TryGet("a", out _);
            cache.TryGet("b", out _);

            CacheStatistics stats = cache.GetStatistics();

            Assert.AreEqual(0, stats.HitCount);
            Assert.AreEqual(0, stats.MissCount);
        }
    }

    [TestFixture]
    public sealed class CacheCallbackTests
    {
        private float _currentTime;

        private float TimeProvider()
        {
            return _currentTime;
        }

        [SetUp]
        public void SetUp()
        {
            _currentTime = 0f;
        }

        [Test]
        public void OnSetCallbackInvoked()
        {
            List<(string key, int value)> setCalls = new();

            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .OnSet((k, v) => setCalls.Add((k, v)))
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.Set("b", 2);

            Assert.AreEqual(2, setCalls.Count);
            Assert.AreEqual(("a", 1), setCalls[0]);
            Assert.AreEqual(("b", 2), setCalls[1]);
        }

        [Test]
        public void OnGetCallbackInvoked()
        {
            List<(string key, int value)> getCalls = new();

            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .OnGet((k, v) => getCalls.Add((k, v)))
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.Set("b", 2);

            cache.TryGet("a", out _);
            cache.TryGet("b", out _);
            cache.TryGet("c", out _);

            Assert.AreEqual(2, getCalls.Count);
            Assert.AreEqual(("a", 1), getCalls[0]);
            Assert.AreEqual(("b", 2), getCalls[1]);
        }

        [Test]
        public void OnEvictionCallbackInvokedWithReason()
        {
            List<(string key, int value, EvictionReason reason)> evictions = new();

            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(2)
                .OnEviction((k, v, r) => evictions.Add((k, v, r)))
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.Set("b", 2);
            cache.Set("c", 3);

            Assert.AreEqual(1, evictions.Count);
            Assert.AreEqual("a", evictions[0].key);
            Assert.AreEqual(1, evictions[0].value);
            Assert.AreEqual(EvictionReason.Capacity, evictions[0].reason);
        }

        [Test]
        public void OnEvictionCallbackForExplicitRemoval()
        {
            List<(string key, int value, EvictionReason reason)> evictions = new();

            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .OnEviction((k, v, r) => evictions.Add((k, v, r)))
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.TryRemove("a");

            Assert.AreEqual(1, evictions.Count);
            Assert.AreEqual(EvictionReason.Explicit, evictions[0].reason);
        }

        [Test]
        public void OnEvictionCallbackForExpiration()
        {
            List<(string key, int value, EvictionReason reason)> evictions = new();

            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .ExpireAfterWrite(1.0f)
                .OnEviction((k, v, r) => evictions.Add((k, v, r)))
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);

            _currentTime = 1.1f;

            cache.TryGet("a", out _);

            Assert.AreEqual(1, evictions.Count);
            Assert.AreEqual(EvictionReason.Expired, evictions[0].reason);
        }

        [Test]
        public void OnEvictionCallbackForReplacement()
        {
            List<(string key, int value, EvictionReason reason)> evictions = new();

            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .OnEviction((k, v, r) => evictions.Add((k, v, r)))
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.Set("a", 2);

            Assert.AreEqual(1, evictions.Count);
            Assert.AreEqual(1, evictions[0].value);
            Assert.AreEqual(EvictionReason.Replaced, evictions[0].reason);
        }

        [Test]
        public void CallbackExceptionDoesNotPropagate()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .OnSet((k, v) => throw new InvalidOperationException("Test"))
                .OnGet((k, v) => throw new InvalidOperationException("Test"))
                .OnEviction((k, v, r) => throw new InvalidOperationException("Test"))
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.TryGet("a", out _);
            cache.TryRemove("a");

            Assert.Pass("No exceptions propagated");
        }
    }

    [TestFixture]
    public sealed class CacheWeightedTests
    {
        private float _currentTime;

        private float TimeProvider()
        {
            return _currentTime;
        }

        [SetUp]
        public void SetUp()
        {
            _currentTime = 0f;
        }

        [Test]
        public void WeightedCacheUsesMaximumWeight()
        {
            using Cache<string, string> cache = CacheBuilder<string, string>
                .NewBuilder()
                .MaximumWeight(100)
                .Weigher(static (k, v) => v.Length)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", new string('x', 40));
            cache.Set("b", new string('y', 40));

            Assert.AreEqual(2, cache.Count);
            Assert.AreEqual(80, cache.Size);

            cache.Set("c", new string('z', 30));

            Assert.AreEqual(2, cache.Count);
        }

        [Test]
        public void SizeReturnsWeightForWeightedCache()
        {
            using Cache<string, string> cache = CacheBuilder<string, string>
                .NewBuilder()
                .MaximumWeight(100)
                .Weigher(static (k, v) => v.Length)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", "hello");
            cache.Set("b", "world!");

            Assert.AreEqual(11, cache.Size);
        }
    }

    [TestFixture]
    public sealed class CacheDynamicSizingTests
    {
        private float _currentTime;

        private float TimeProvider()
        {
            return _currentTime;
        }

        [SetUp]
        public void SetUp()
        {
            _currentTime = 0f;
        }

        [Test]
        public void CompactForcesEviction()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            for (int i = 0; i < 10; i++)
            {
                cache.Set($"key{i}", i);
            }

            Assert.AreEqual(10, cache.Count);

            cache.Compact(0.5f);

            Assert.AreEqual(5, cache.Count);
        }

        [Test]
        public void ResizeReducesCapacity()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            for (int i = 0; i < 10; i++)
            {
                cache.Set($"key{i}", i);
            }

            cache.Resize(5);

            Assert.AreEqual(5, cache.Count);
            Assert.AreEqual(5, cache.Capacity);
        }
    }

    [TestFixture]
    public sealed class CacheBuilderTests
    {
        [Test]
        public void NewBuilderReturnsBuilder()
        {
            // CacheBuilder is a struct, so it can never be null - verify it can be created
            CacheBuilder<string, int> builder = CacheBuilder<string, int>.NewBuilder();
            // Verify builder can be used by building a cache
            using Cache<string, int> cache = builder.Build();
            Assert.IsNotNull(cache);
        }

        [Test]
        public void BuilderChainsCorrectly()
        {
            Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(100)
                .ExpireAfterWrite(5.0f)
                .ExpireAfterAccess(2.0f)
                .EvictionPolicy(EvictionPolicy.Lfu)
                .RecordStatistics()
                .WithJitter(0.5f)
                .Build();

            Assert.IsTrue(cache != null);
            Assert.AreEqual(100, cache.Capacity);
            cache.Dispose();
        }

        [Test]
        public void BuilderWithLoaderCreatesLoadingCache()
        {
            Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .Build(static k => k.Length);

            int result = cache.GetOrAdd("hello", null);

            Assert.AreEqual(5, result);
            cache.Dispose();
        }

        [Test]
        public void MaximumSizeNormalizesNegativeValues()
        {
            Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(-5)
                .Build();

            Assert.AreEqual(1, cache.Capacity);
            cache.Dispose();
        }

        [Test]
        public void ProtectedRatioClampedToValidRange()
        {
            Cache<string, int> cacheNegative = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .EvictionPolicy(EvictionPolicy.Slru)
                .ProtectedRatio(-0.5f)
                .Build();

            Cache<string, int> cacheExcessive = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .EvictionPolicy(EvictionPolicy.Slru)
                .ProtectedRatio(1.5f)
                .Build();

            Assert.IsTrue(cacheNegative != null);
            Assert.IsTrue(cacheExcessive != null);

            cacheNegative.Dispose();
            cacheExcessive.Dispose();
        }
    }

    [TestFixture]
    public sealed class CacheStatisticsStructTests
    {
        [Test]
        public void DefaultStatisticsAreZero()
        {
            CacheStatistics stats = default;

            Assert.AreEqual(0, stats.HitCount);
            Assert.AreEqual(0, stats.MissCount);
            Assert.AreEqual(0, stats.EvictionCount);
            Assert.AreEqual(0, stats.LoadCount);
            Assert.AreEqual(0, stats.ExpiredCount);
            Assert.AreEqual(0, stats.CurrentSize);
            Assert.AreEqual(0, stats.PeakSize);
            Assert.AreEqual(0, stats.GrowthEvents);
        }

        [Test]
        public void HitRateCalculatedCorrectly()
        {
            CacheStatistics stats = new(
                hitCount: 75,
                missCount: 25,
                evictionCount: 0,
                loadCount: 0,
                expiredCount: 0,
                currentSize: 10,
                peakSize: 10,
                growthEvents: 0
            );

            Assert.AreEqual(0.75, stats.HitRate, 0.001);
            Assert.AreEqual(0.25, stats.MissRate, 0.001);
        }

        [Test]
        public void HitRateZeroWhenNoRequests()
        {
            CacheStatistics stats = new(
                hitCount: 0,
                missCount: 0,
                evictionCount: 0,
                loadCount: 0,
                expiredCount: 0,
                currentSize: 0,
                peakSize: 0,
                growthEvents: 0
            );

            Assert.AreEqual(0.0, stats.HitRate, 0.001);
        }

        [Test]
        public void EqualsReturnsTrueForSameValues()
        {
            CacheStatistics stats1 = new(1, 2, 3, 4, 5, 6, 7, 8);
            CacheStatistics stats2 = new(1, 2, 3, 4, 5, 6, 7, 8);

            Assert.IsTrue(stats1.Equals(stats2));
            Assert.IsTrue(stats1 == stats2);
            Assert.IsFalse(stats1 != stats2);
        }

        [Test]
        public void EqualsReturnsFalseForDifferentValues()
        {
            CacheStatistics stats1 = new(1, 2, 3, 4, 5, 6, 7, 8);
            CacheStatistics stats2 = new(1, 2, 3, 4, 5, 6, 7, 9);

            Assert.IsFalse(stats1.Equals(stats2));
            Assert.IsFalse(stats1 == stats2);
            Assert.IsTrue(stats1 != stats2);
        }

        [Test]
        public void GetHashCodeConsistentWithEquals()
        {
            CacheStatistics stats1 = new(1, 2, 3, 4, 5, 6, 7, 8);
            CacheStatistics stats2 = new(1, 2, 3, 4, 5, 6, 7, 8);

            Assert.AreEqual(stats1.GetHashCode(), stats2.GetHashCode());
        }

        [Test]
        public void ToStringReturnsReadableFormat()
        {
            CacheStatistics stats = new(
                hitCount: 100,
                missCount: 50,
                evictionCount: 10,
                loadCount: 5,
                expiredCount: 3,
                currentSize: 42,
                peakSize: 50,
                growthEvents: 2
            );

            string result = stats.ToString();

            Assert.IsTrue(result.Contains("100"));
            Assert.IsTrue(result.Contains("50"));
            Assert.IsTrue(result.Contains("42"));
        }
    }

    [TestFixture]
    public sealed class CacheEdgeCaseTests
    {
        private float _currentTime;

        private float TimeProvider()
        {
            return _currentTime;
        }

        [SetUp]
        public void SetUp()
        {
            _currentTime = 0f;
        }

        [Test]
        public void OperationsOnDisposedCacheDoNotThrow()
        {
            Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Dispose();

            cache.Set("a", 1);
            Assert.IsFalse(cache.TryGet("a", out _));
            Assert.IsFalse(cache.TryRemove("a"));
            Assert.IsFalse(cache.ContainsKey("a"));

            Assert.Pass("No exceptions thrown");
        }

        [Test]
        public void DoubleDisposeDoesNotThrow()
        {
            Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .Build();

            cache.Dispose();
            cache.Dispose();

            Assert.Pass("No exceptions thrown");
        }

        [Test]
        public void SingleCapacityCacheWorks()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(1)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            Assert.AreEqual(1, cache.Count);

            cache.Set("b", 2);
            Assert.AreEqual(1, cache.Count);
            Assert.IsFalse(cache.ContainsKey("a"));
            Assert.IsTrue(cache.ContainsKey("b"));
        }

        [Test]
        public void KeysEnumeratesNonExpiredEntries()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .ExpireAfterWrite(1.0f)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            _currentTime = 0.5f;
            cache.Set("b", 2);

            _currentTime = 1.1f;

            List<string> keys = new();
            foreach (string key in cache.Keys)
            {
                keys.Add(key);
            }

            Assert.AreEqual(1, keys.Count);
            Assert.AreEqual("b", keys[0]);
        }

        [Test]
        public void FactoryExceptionDoesNotPropagate()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            int result = cache.GetOrAdd(
                "key",
                k => throw new InvalidOperationException("Factory error")
            );

            Assert.AreEqual(default(int), result);
            Assert.IsFalse(cache.ContainsKey("key"));
        }

        [Test]
        public void GetAllWithNullParametersDoesNotThrow()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .Build();

            cache.GetAll(null, new Dictionary<string, int>());
            cache.GetAll(new[] { "a" }, null);

            Assert.Pass("No exceptions thrown");
        }

        [Test]
        public void SetAllWithNullDoesNotThrow()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .Build();

            cache.SetAll(null);

            Assert.Pass("No exceptions thrown");
        }

        [Test]
        public void CompactWithInvalidPercentagesIsHandled()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.Set("b", 2);

            cache.Compact(-0.5f);
            Assert.AreEqual(2, cache.Count);

            cache.Compact(1.5f);
            Assert.AreEqual(0, cache.Count);
        }

        [Test]
        public void ResizeWithInvalidCapacityIsHandled()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);

            cache.Resize(0);
            Assert.AreEqual(1, cache.Count);

            cache.Resize(-5);
            Assert.AreEqual(1, cache.Count);
        }
    }

    [TestFixture]
    public sealed class CacheJitterTests
    {
        private float _currentTime;

        private float TimeProvider()
        {
            return _currentTime;
        }

        [SetUp]
        public void SetUp()
        {
            _currentTime = 0f;
        }

        [Test]
        public void JitterExtendsExpiration()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .ExpireAfterWrite(1.0f)
                .WithJitter(0.5f)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("key", 42);

            _currentTime = 1.0f;

            Assert.IsTrue(cache.TryGet("key", out _));
        }
    }

    [TestFixture]
    public sealed class EvictionPolicyEnumTests
    {
        [Test]
        public void EvictionPolicyHasExpectedValues()
        {
#pragma warning disable CS0618 // Intentionally testing obsolete enum values for backward compatibility
            Assert.AreEqual(0, (int)EvictionPolicy.None);
#pragma warning restore CS0618
            Assert.AreEqual(1, (int)EvictionPolicy.Lru);
            Assert.AreEqual(2, (int)EvictionPolicy.Slru);
            Assert.AreEqual(3, (int)EvictionPolicy.Lfu);
            Assert.AreEqual(4, (int)EvictionPolicy.Fifo);
            Assert.AreEqual(5, (int)EvictionPolicy.Random);
        }
    }

    [TestFixture]
    public sealed class EvictionReasonEnumTests
    {
        [Test]
        public void EvictionReasonHasExpectedValues()
        {
#pragma warning disable CS0618 // Intentionally testing obsolete enum value for backward compatibility
            Assert.AreEqual(0, (int)EvictionReason.Unknown);
#pragma warning restore CS0618
            Assert.AreEqual(1, (int)EvictionReason.Expired);
            Assert.AreEqual(2, (int)EvictionReason.Capacity);
            Assert.AreEqual(3, (int)EvictionReason.Explicit);
            Assert.AreEqual(4, (int)EvictionReason.Replaced);
        }
    }

    [TestFixture]
    public sealed class CacheGetKeysMethodTests
    {
        private float _currentTime;

        private float TimeProvider()
        {
            return _currentTime;
        }

        [SetUp]
        public void SetUp()
        {
            _currentTime = 0f;
        }

        [Test]
        public void GetKeysPopulatesListWithAllKeys()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.Set("b", 2);
            cache.Set("c", 3);

            List<string> keys = new();
            cache.GetKeys(keys);

            Assert.AreEqual(3, keys.Count);
            Assert.IsTrue(keys.Contains("a"));
            Assert.IsTrue(keys.Contains("b"));
            Assert.IsTrue(keys.Contains("c"));
        }

        [Test]
        public void GetKeysClearsPreviousListContents()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);

            List<string> keys = new() { "existing", "items" };
            cache.GetKeys(keys);

            Assert.AreEqual(1, keys.Count);
            Assert.AreEqual("a", keys[0]);
        }

        [Test]
        public void GetKeysWithNullListDoesNotThrow()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .Build();

            cache.GetKeys(null);

            Assert.Pass("No exception thrown");
        }

        [Test]
        public void GetKeysExcludesExpiredEntries()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .ExpireAfterWrite(1.0f)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            _currentTime = 0.5f;
            cache.Set("b", 2);

            _currentTime = 1.1f;

            List<string> keys = new();
            cache.GetKeys(keys);

            Assert.AreEqual(1, keys.Count);
            Assert.AreEqual("b", keys[0]);
        }

        [Test]
        public void GetKeysOnEmptyCacheReturnsEmptyList()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .Build();

            List<string> keys = new();
            cache.GetKeys(keys);

            Assert.AreEqual(0, keys.Count);
        }

        [Test]
        public void GetKeysOnDisposedCacheDoesNotThrow()
        {
            Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.Dispose();

            List<string> keys = new();
            cache.GetKeys(keys);

            Assert.AreEqual(0, keys.Count);
        }
    }

    [TestFixture]
    public sealed class CacheIListOverloadTests
    {
        private float _currentTime;

        private float TimeProvider()
        {
            return _currentTime;
        }

        [SetUp]
        public void SetUp()
        {
            _currentTime = 0f;
        }

        [Test]
        public void GetAllWithIListRetrievesMultipleValues()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.Set("b", 2);
            cache.Set("c", 3);

            List<string> keys = new() { "a", "b", "d" };
            Dictionary<string, int> results = new();
            cache.GetAll(keys, results);

            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(1, results["a"]);
            Assert.AreEqual(2, results["b"]);
            Assert.IsFalse(results.ContainsKey("d"));
        }

        [Test]
        public void GetAllWithIListHandlesNullKeys()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .Build();

            cache.GetAll((IList<string>)null, new Dictionary<string, int>());

            Assert.Pass("No exception thrown");
        }

        [Test]
        public void GetAllWithIListHandlesNullResults()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .Build();

            cache.GetAll(new List<string> { "a" }, null);

            Assert.Pass("No exception thrown");
        }

        [Test]
        public void SetAllWithIListAddsMultipleEntries()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            List<KeyValuePair<string, int>> entries = new()
            {
                new KeyValuePair<string, int>("a", 1),
                new KeyValuePair<string, int>("b", 2),
                new KeyValuePair<string, int>("c", 3),
            };
            cache.SetAll(entries);

            Assert.AreEqual(3, cache.Count);
            Assert.IsTrue(cache.TryGet("a", out int va) && va == 1);
            Assert.IsTrue(cache.TryGet("b", out int vb) && vb == 2);
            Assert.IsTrue(cache.TryGet("c", out int vc) && vc == 3);
        }

        [Test]
        public void SetAllWithIListHandlesNullEntries()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .Build();

            cache.SetAll((IList<KeyValuePair<string, int>>)null);

            Assert.Pass("No exception thrown");
        }

        [Test]
        public void SetAllWithIListUpdatesExistingEntries()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);

            List<KeyValuePair<string, int>> entries = new()
            {
                new KeyValuePair<string, int>("a", 100),
                new KeyValuePair<string, int>("b", 200),
            };
            cache.SetAll(entries);

            Assert.AreEqual(2, cache.Count);
            Assert.IsTrue(cache.TryGet("a", out int va) && va == 100);
            Assert.IsTrue(cache.TryGet("b", out int vb) && vb == 200);
        }
    }

    [TestFixture]
    public sealed class CacheSlruSegmentCountTests
    {
        private float _currentTime;

        private float TimeProvider()
        {
            return _currentTime;
        }

        [SetUp]
        public void SetUp()
        {
            _currentTime = 0f;
        }

        [Test]
        public void SlruEvictionFromProtectedSegmentDecrementsProtectedCount()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(3)
                .EvictionPolicy(EvictionPolicy.Slru)
                .ProtectedRatio(0.67f)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.Set("b", 2);
            cache.Set("c", 3);

            cache.TryGet("a", out _);
            cache.TryGet("b", out _);

            cache.TryRemove("a");

            Assert.AreEqual(2, cache.Count);
            Assert.IsFalse(cache.ContainsKey("a"));
            Assert.IsTrue(cache.ContainsKey("b"));
            Assert.IsTrue(cache.ContainsKey("c"));
        }

        [Test]
        public void SlruEvictionFromProbationSegmentDecrementsProbationCount()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(3)
                .EvictionPolicy(EvictionPolicy.Slru)
                .ProtectedRatio(0.67f)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.Set("b", 2);
            cache.Set("c", 3);

            cache.TryRemove("c");

            Assert.AreEqual(2, cache.Count);
            Assert.IsTrue(cache.ContainsKey("a"));
            Assert.IsTrue(cache.ContainsKey("b"));
            Assert.IsFalse(cache.ContainsKey("c"));
        }

        [Test]
        public void SlruMultipleEvictionsFromBothSegmentsMaintainCorrectState()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(4)
                .EvictionPolicy(EvictionPolicy.Slru)
                .ProtectedRatio(0.5f)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("a", 1);
            cache.Set("b", 2);
            cache.TryGet("a", out _);
            cache.TryGet("b", out _);

            cache.Set("c", 3);
            cache.Set("d", 4);

            cache.TryRemove("a");
            cache.TryRemove("c");

            Assert.AreEqual(2, cache.Count);
            Assert.IsTrue(cache.ContainsKey("b"));
            Assert.IsTrue(cache.ContainsKey("d"));

            cache.Set("e", 5);
            cache.Set("f", 6);

            Assert.AreEqual(4, cache.Count);
        }
    }

    [TestFixture]
    public sealed class CacheExtremeCaseTests
    {
        private float _currentTime;

        private float TimeProvider()
        {
            return _currentTime;
        }

        [SetUp]
        public void SetUp()
        {
            _currentTime = 0f;
        }

        [Test]
        public void LargeCapacityCacheHandles10KEntries()
        {
            using Cache<int, int> cache = CacheBuilder<int, int>
                .NewBuilder()
                .MaximumSize(15000)
                .TimeProvider(TimeProvider)
                .Build();

            for (int i = 0; i < 10000; i++)
            {
                cache.Set(i, i * 2);
            }

            Assert.AreEqual(10000, cache.Count);

            for (int i = 0; i < 10000; i++)
            {
                Assert.IsTrue(cache.TryGet(i, out int value));
                Assert.AreEqual(i * 2, value);
            }
        }

        [Test]
        public void LruEvictionWith10KEntriesEvictsCorrectly()
        {
            using Cache<int, int> cache = CacheBuilder<int, int>
                .NewBuilder()
                .MaximumSize(10000)
                .EvictionPolicy(EvictionPolicy.Lru)
                .TimeProvider(TimeProvider)
                .Build();

            for (int i = 0; i < 10000; i++)
            {
                cache.Set(i, i);
            }

            Assert.AreEqual(10000, cache.Count);

            for (int i = 0; i < 1000; i++)
            {
                _currentTime += 0.001f;
                cache.TryGet(i, out _);
            }

            for (int i = 10000; i < 15000; i++)
            {
                cache.Set(i, i);
            }

            Assert.AreEqual(10000, cache.Count);

            for (int i = 0; i < 1000; i++)
            {
                Assert.IsTrue(cache.ContainsKey(i), $"Key {i} should exist");
            }
        }

        [Test]
        public void FifoEvictionWith10KEntriesEvictsCorrectly()
        {
            using Cache<int, int> cache = CacheBuilder<int, int>
                .NewBuilder()
                .MaximumSize(10000)
                .EvictionPolicy(EvictionPolicy.Fifo)
                .TimeProvider(TimeProvider)
                .Build();

            for (int i = 0; i < 15000; i++)
            {
                cache.Set(i, i);
            }

            Assert.AreEqual(10000, cache.Count);

            for (int i = 0; i < 5000; i++)
            {
                Assert.IsFalse(cache.ContainsKey(i), $"Key {i} should have been evicted");
            }

            for (int i = 5000; i < 15000; i++)
            {
                Assert.IsTrue(cache.ContainsKey(i), $"Key {i} should exist");
            }
        }

        [Test]
        public void GetKeysWithLargeCache()
        {
            using Cache<int, int> cache = CacheBuilder<int, int>
                .NewBuilder()
                .MaximumSize(10000)
                .TimeProvider(TimeProvider)
                .Build();

            for (int i = 0; i < 10000; i++)
            {
                cache.Set(i, i);
            }

            List<int> keys = new(10000);
            cache.GetKeys(keys);

            Assert.AreEqual(10000, keys.Count);
        }

        [Test]
        public void ClearWith10KEntriesWorks()
        {
            using Cache<int, int> cache = CacheBuilder<int, int>
                .NewBuilder()
                .MaximumSize(10000)
                .TimeProvider(TimeProvider)
                .Build();

            for (int i = 0; i < 10000; i++)
            {
                cache.Set(i, i);
            }

            cache.Clear();

            Assert.AreEqual(0, cache.Count);
        }

        [Test]
        public void CompactWith10KEntriesWorks()
        {
            using Cache<int, int> cache = CacheBuilder<int, int>
                .NewBuilder()
                .MaximumSize(10000)
                .TimeProvider(TimeProvider)
                .Build();

            for (int i = 0; i < 10000; i++)
            {
                cache.Set(i, i);
            }

            cache.Compact(0.5f);

            Assert.AreEqual(5000, cache.Count);
        }
    }

    [TestFixture]
    public sealed class CacheDataDrivenTests
    {
        private float _currentTime;

        private float TimeProvider()
        {
            return _currentTime;
        }

        [SetUp]
        public void SetUp()
        {
            _currentTime = 0f;
        }

        [Test]
        [TestCase(1, TestName = "Capacity.1")]
        [TestCase(10, TestName = "Capacity.10")]
        [TestCase(100, TestName = "Capacity.100")]
        [TestCase(1000, TestName = "Capacity.1000")]
        public void CacheWithVariousCapacitiesWorks(int capacity)
        {
            using Cache<int, int> cache = CacheBuilder<int, int>
                .NewBuilder()
                .MaximumSize(capacity)
                .TimeProvider(TimeProvider)
                .Build();

            for (int i = 0; i < capacity; i++)
            {
                cache.Set(i, i);
            }

            Assert.AreEqual(capacity, cache.Count);

            for (int i = 0; i < capacity; i++)
            {
                Assert.IsTrue(cache.TryGet(i, out int value));
                Assert.AreEqual(i, value);
            }
        }

        [Test]
        [TestCaseSource(nameof(EvictionPolicyTestData))]
        public void CacheEvictsWithDifferentPolicies(
            EvictionPolicy policy,
            int capacity,
            int itemsToAdd
        )
        {
            using Cache<int, int> cache = CacheBuilder<int, int>
                .NewBuilder()
                .MaximumSize(capacity)
                .EvictionPolicy(policy)
                .TimeProvider(TimeProvider)
                .Build();

            for (int i = 0; i < itemsToAdd; i++)
            {
                cache.Set(i, i);
            }

            int expectedCount = Math.Min(capacity, itemsToAdd);
            Assert.AreEqual(expectedCount, cache.Count);
        }

        private static IEnumerable<TestCaseData> EvictionPolicyTestData()
        {
            yield return new TestCaseData(EvictionPolicy.Lru, 10, 20).SetName(
                "Policy.Lru.CapacityReached"
            );
            yield return new TestCaseData(EvictionPolicy.Fifo, 10, 20).SetName(
                "Policy.Fifo.CapacityReached"
            );
            yield return new TestCaseData(EvictionPolicy.Lfu, 10, 20).SetName(
                "Policy.Lfu.CapacityReached"
            );
            yield return new TestCaseData(EvictionPolicy.Random, 10, 20).SetName(
                "Policy.Random.CapacityReached"
            );
            yield return new TestCaseData(EvictionPolicy.Slru, 10, 20).SetName(
                "Policy.Slru.CapacityReached"
            );
            yield return new TestCaseData(EvictionPolicy.Lru, 10, 5).SetName(
                "Policy.Lru.UnderCapacity"
            );
            yield return new TestCaseData(EvictionPolicy.Fifo, 10, 5).SetName(
                "Policy.Fifo.UnderCapacity"
            );
        }

        [Test]
        [TestCase(0.5f, TestName = "Ttl.HalfSecond")]
        [TestCase(1.0f, TestName = "Ttl.OneSecond")]
        [TestCase(5.0f, TestName = "Ttl.FiveSeconds")]
        public void ExpirationWorksWithVariousTtls(float ttl)
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .ExpireAfterWrite(ttl)
                .TimeProvider(TimeProvider)
                .Build();

            cache.Set("key", 42);
            Assert.IsTrue(cache.TryGet("key", out _));

            _currentTime = ttl + 0.1f;
            Assert.IsFalse(cache.TryGet("key", out _));
        }

        [Test]
        [TestCaseSource(nameof(CompactPercentageTestData))]
        public void CompactWithVariousPercentages(
            float percentage,
            int initialCount,
            int expectedAfterCompact
        )
        {
            using Cache<int, int> cache = CacheBuilder<int, int>
                .NewBuilder()
                .MaximumSize(initialCount)
                .TimeProvider(TimeProvider)
                .Build();

            for (int i = 0; i < initialCount; i++)
            {
                cache.Set(i, i);
            }

            cache.Compact(percentage);

            Assert.AreEqual(expectedAfterCompact, cache.Count);
        }

        private static IEnumerable<TestCaseData> CompactPercentageTestData()
        {
            yield return new TestCaseData(0.0f, 10, 10).SetName("Compact.0Percent");
            yield return new TestCaseData(0.25f, 100, 75).SetName("Compact.25Percent");
            yield return new TestCaseData(0.5f, 100, 50).SetName("Compact.50Percent");
            yield return new TestCaseData(0.75f, 100, 25).SetName("Compact.75Percent");
            yield return new TestCaseData(1.0f, 100, 0).SetName("Compact.100Percent");
        }
    }

#if !SINGLE_THREADED
    [TestFixture]
    public sealed class CacheConcurrentAccessTests
    {
        private float _currentTime;

        private float TimeProvider()
        {
            return _currentTime;
        }

        [SetUp]
        public void SetUp()
        {
            _currentTime = 0f;
        }

        [Test]
        public void ConcurrentSetsDoNotCorruptCache()
        {
            using Cache<int, int> cache = CacheBuilder<int, int>
                .NewBuilder()
                .MaximumSize(1000)
                .TimeProvider(TimeProvider)
                .Build();

            int threadCount = 4;
            int operationsPerThread = 250;
            System.Threading.CountdownEvent countdownEvent = new(threadCount);
            Exception capturedException = null;

            for (int t = 0; t < threadCount; t++)
            {
                int threadIndex = t;
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        for (int i = 0; i < operationsPerThread; i++)
                        {
                            int key = threadIndex * operationsPerThread + i;
                            cache.Set(key, key);
                        }
                    }
                    catch (Exception ex)
                    {
                        capturedException = ex;
                    }
                    finally
                    {
                        countdownEvent.Signal();
                    }
                });
            }

            countdownEvent.Wait(TimeSpan.FromSeconds(10));

            Assert.IsTrue(
                capturedException == null,
                $"Exception during concurrent sets: {capturedException}"
            );
            Assert.AreEqual(threadCount * operationsPerThread, cache.Count);
        }

        [Test]
        public void ConcurrentGetsDoNotThrow()
        {
            using Cache<int, int> cache = CacheBuilder<int, int>
                .NewBuilder()
                .MaximumSize(100)
                .TimeProvider(TimeProvider)
                .Build();

            for (int i = 0; i < 100; i++)
            {
                cache.Set(i, i);
            }

            int threadCount = 4;
            int operationsPerThread = 1000;
            System.Threading.CountdownEvent countdownEvent = new(threadCount);
            Exception capturedException = null;

            for (int t = 0; t < threadCount; t++)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        for (int i = 0; i < operationsPerThread; i++)
                        {
                            int key = i % 100;
                            cache.TryGet(key, out int _);
                        }
                    }
                    catch (Exception ex)
                    {
                        capturedException = ex;
                    }
                    finally
                    {
                        countdownEvent.Signal();
                    }
                });
            }

            countdownEvent.Wait(TimeSpan.FromSeconds(10));

            Assert.IsTrue(
                capturedException == null,
                $"Exception during concurrent gets: {capturedException}"
            );
        }

        [Test]
        public void ConcurrentSetsAndGetsDoNotCorruptCache()
        {
            using Cache<int, int> cache = CacheBuilder<int, int>
                .NewBuilder()
                .MaximumSize(500)
                .TimeProvider(TimeProvider)
                .Build();

            int threadCount = 4;
            int operationsPerThread = 500;
            System.Threading.CountdownEvent countdownEvent = new(threadCount);
            Exception capturedException = null;

            for (int t = 0; t < threadCount; t++)
            {
                int threadIndex = t;
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        for (int i = 0; i < operationsPerThread; i++)
                        {
                            if (i % 2 == 0)
                            {
                                int key = threadIndex * 100 + (i % 100);
                                cache.Set(key, key);
                            }
                            else
                            {
                                int key = (threadIndex + 1) % threadCount * 100 + (i % 100);
                                cache.TryGet(key, out int _);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        capturedException = ex;
                    }
                    finally
                    {
                        countdownEvent.Signal();
                    }
                });
            }

            countdownEvent.Wait(TimeSpan.FromSeconds(10));

            Assert.IsTrue(
                capturedException == null,
                $"Exception during concurrent sets and gets: {capturedException}"
            );
        }

        [Test]
        public void ConcurrentRemovesDoNotThrow()
        {
            using Cache<int, int> cache = CacheBuilder<int, int>
                .NewBuilder()
                .MaximumSize(1000)
                .TimeProvider(TimeProvider)
                .Build();

            for (int i = 0; i < 1000; i++)
            {
                cache.Set(i, i);
            }

            int threadCount = 4;
            int operationsPerThread = 250;
            System.Threading.CountdownEvent countdownEvent = new(threadCount);
            Exception capturedException = null;

            for (int t = 0; t < threadCount; t++)
            {
                int threadIndex = t;
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        for (int i = 0; i < operationsPerThread; i++)
                        {
                            int key = threadIndex * operationsPerThread + i;
                            cache.TryRemove(key);
                        }
                    }
                    catch (Exception ex)
                    {
                        capturedException = ex;
                    }
                    finally
                    {
                        countdownEvent.Signal();
                    }
                });
            }

            countdownEvent.Wait(TimeSpan.FromSeconds(10));

            Assert.IsTrue(
                capturedException == null,
                $"Exception during concurrent removes: {capturedException}"
            );
            Assert.AreEqual(0, cache.Count);
        }

        [Test]
        public void ConcurrentEvictionDoesNotCorruptCache()
        {
            using Cache<int, int> cache = CacheBuilder<int, int>
                .NewBuilder()
                .MaximumSize(100)
                .EvictionPolicy(EvictionPolicy.Lru)
                .TimeProvider(TimeProvider)
                .Build();

            int threadCount = 4;
            int operationsPerThread = 500;
            System.Threading.CountdownEvent countdownEvent = new(threadCount);
            Exception capturedException = null;

            for (int t = 0; t < threadCount; t++)
            {
                int threadIndex = t;
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        for (int i = 0; i < operationsPerThread; i++)
                        {
                            int key = threadIndex * operationsPerThread + i;
                            cache.Set(key, key);
                        }
                    }
                    catch (Exception ex)
                    {
                        capturedException = ex;
                    }
                    finally
                    {
                        countdownEvent.Signal();
                    }
                });
            }

            countdownEvent.Wait(TimeSpan.FromSeconds(10));

            Assert.IsTrue(
                capturedException == null,
                $"Exception during concurrent eviction: {capturedException}"
            );
            Assert.LessOrEqual(cache.Count, 100);
        }

        [Test]
        public void ConcurrentGetOrAddDoesNotDuplicateEntries()
        {
            using Cache<int, int> cache = CacheBuilder<int, int>
                .NewBuilder()
                .MaximumSize(100)
                .TimeProvider(TimeProvider)
                .Build();

            int threadCount = 4;
            int sharedKeyRange = 50;
            System.Threading.CountdownEvent countdownEvent = new(threadCount);
            Exception capturedException = null;
            int[] loadCounts = new int[sharedKeyRange];

            for (int t = 0; t < threadCount; t++)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        for (int i = 0; i < sharedKeyRange; i++)
                        {
                            cache.GetOrAdd(
                                i,
                                key =>
                                {
                                    System.Threading.Interlocked.Increment(ref loadCounts[key]);
                                    return key * 2;
                                }
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        capturedException = ex;
                    }
                    finally
                    {
                        countdownEvent.Signal();
                    }
                });
            }

            countdownEvent.Wait(TimeSpan.FromSeconds(10));

            Assert.IsTrue(
                capturedException == null,
                $"Exception during concurrent GetOrAdd: {capturedException}"
            );
            Assert.AreEqual(sharedKeyRange, cache.Count);
        }
    }
#endif

    [TestFixture]
    public sealed class CacheOptionsStructTests
    {
        [Test]
        public void DefaultOptionsHasExpectedDefaults()
        {
            CacheOptions<string, int> options = default;
            Assert.AreEqual(0, options.MaximumSize, "Default MaximumSize should be 0 (constructor normalizes)");
            Assert.AreEqual(0f, options.ExpireAfterWriteSeconds, "Default ExpireAfterWriteSeconds should be 0");
            Assert.AreEqual(0f, options.ExpireAfterAccessSeconds, "Default ExpireAfterAccessSeconds should be 0");
        }

        [Test]
        public void DefaultStaticMethodReturnsConfiguredDefaults()
        {
            CacheOptions<string, int> options = CacheOptions<string, int>.Default();
            Assert.AreEqual(CacheOptions<string, int>.DefaultMaximumSize, options.MaximumSize);
        }

        [TestCase(1)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(int.MaxValue)]
        public void CacheOptionsWithVariousMaximumSizes(int maxSize)
        {
            CacheOptions<string, int> options = new() { MaximumSize = maxSize };
            using Cache<string, int> cache = new(options);
            Assert.AreEqual(maxSize, cache.Capacity, $"Cache capacity should match configured MaximumSize {maxSize}");
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-100)]
        [TestCase(int.MinValue)]
        public void CacheOptionsWithInvalidMaximumSizeNormalizesToDefault(int invalidMaxSize)
        {
            CacheOptions<string, int> options = new() { MaximumSize = invalidMaxSize };
            using Cache<string, int> cache = new(options);
            Assert.AreEqual(
                CacheOptions<string, int>.DefaultMaximumSize,
                cache.Capacity,
                $"Invalid MaximumSize {invalidMaxSize} should normalize to default"
            );
        }

        [TestCase(0f, 0f)]
        [TestCase(1f, 0f)]
        [TestCase(0f, 1f)]
        [TestCase(5f, 10f)]
        [TestCase(60f, 120f)]
        public void CacheOptionsWithVariousExpirationSettings(
            float expireAfterWrite,
            float expireAfterAccess
        )
        {
            CacheOptions<string, int> options = new()
            {
                MaximumSize = 10,
                ExpireAfterWriteSeconds = expireAfterWrite,
                ExpireAfterAccessSeconds = expireAfterAccess,
            };
            using Cache<string, int> cache = new(options);
            Assert.IsNotNull(cache);
            Assert.AreEqual(10, cache.Capacity);
        }
    }

    [TestFixture]
    public sealed class EnumValueStabilityTests
    {
        // Data-driven test to ensure enum integer values remain stable for serialization compatibility
        [TestCase(EvictionPolicy.Lru, 1, TestName = "EvictionPolicy.Lru.HasValue.1")]
        [TestCase(EvictionPolicy.Slru, 2, TestName = "EvictionPolicy.Slru.HasValue.2")]
        [TestCase(EvictionPolicy.Lfu, 3, TestName = "EvictionPolicy.Lfu.HasValue.3")]
        [TestCase(EvictionPolicy.Fifo, 4, TestName = "EvictionPolicy.Fifo.HasValue.4")]
        [TestCase(EvictionPolicy.Random, 5, TestName = "EvictionPolicy.Random.HasValue.5")]
        public void EvictionPolicyValueStability(EvictionPolicy policy, int expectedValue)
        {
            Assert.AreEqual(
                expectedValue,
                (int)policy,
                $"EvictionPolicy.{policy} should have stable integer value {expectedValue}"
            );
        }

        [TestCase(EvictionReason.Expired, 1, TestName = "EvictionReason.Expired.HasValue.1")]
        [TestCase(EvictionReason.Capacity, 2, TestName = "EvictionReason.Capacity.HasValue.2")]
        [TestCase(EvictionReason.Explicit, 3, TestName = "EvictionReason.Explicit.HasValue.3")]
        [TestCase(EvictionReason.Replaced, 4, TestName = "EvictionReason.Replaced.HasValue.4")]
        public void EvictionReasonValueStability(EvictionReason reason, int expectedValue)
        {
            Assert.AreEqual(
                expectedValue,
                (int)reason,
                $"EvictionReason.{reason} should have stable integer value {expectedValue}"
            );
        }

        [Test]
        public void EvictionPolicyHasExpectedCount()
        {
            // Verify the enum has expected number of values (including obsolete None)
            int enumCount = Enum.GetValues(typeof(EvictionPolicy)).Length;
            Assert.AreEqual(
                6,
                enumCount,
                "EvictionPolicy should have 6 values (including obsolete None)"
            );
        }

        [Test]
        public void EvictionReasonHasExpectedCount()
        {
            // Verify the enum has expected number of values (including obsolete Unknown)
            int enumCount = Enum.GetValues(typeof(EvictionReason)).Length;
            Assert.AreEqual(
                5,
                enumCount,
                "EvictionReason should have 5 values (including obsolete Unknown)"
            );
        }
    }

    [TestFixture]
    public sealed class CacheBuilderStructTests
    {
        [Test]
        public void BuilderIsValueType()
        {
            // Verify CacheBuilder is a struct (value type)
            Assert.IsTrue(
                typeof(CacheBuilder<string, int>).IsValueType,
                "CacheBuilder should be a value type (struct)"
            );
        }

        [Test]
        public void DefaultBuilderCanBuildCache()
        {
            CacheBuilder<string, int> builder = default;
            // Default struct should still be usable (though may have default values)
            // This verifies the struct doesn't require special initialization
            using Cache<string, int> cache = builder.Build();
            Assert.IsNotNull(cache);
        }

        [TestCase(1)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        public void BuilderWithVariousCapacities(int capacity)
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(capacity)
                .Build();
            Assert.AreEqual(capacity, cache.Capacity);
        }

        [TestCase(EvictionPolicy.Lru)]
        [TestCase(EvictionPolicy.Slru)]
        [TestCase(EvictionPolicy.Lfu)]
        [TestCase(EvictionPolicy.Fifo)]
        [TestCase(EvictionPolicy.Random)]
        public void BuilderWithVariousEvictionPolicies(EvictionPolicy policy)
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .EvictionPolicy(policy)
                .Build();
            Assert.IsNotNull(cache);
            Assert.AreEqual(10, cache.Capacity);
        }
    }

    [TestFixture]
    public sealed class CacheOptionsIsValueTypeTests
    {
        [Test]
        public void CacheOptionsIsValueType()
        {
            // Verify CacheOptions is a struct (value type) - this is the root cause of the null issues
            Assert.IsTrue(
                typeof(CacheOptions<string, int>).IsValueType,
                "CacheOptions should be a value type (struct)"
            );
        }

        [Test]
        public void CacheOptionsDefaultDoesNotEqualNull()
        {
            // Structs can be boxed and compared to null, but default is not null
            CacheOptions<string, int> options = default;
            object boxedOptions = options;
            Assert.IsNotNull(boxedOptions, "Boxed struct should not be null");
        }
    }

    [TestFixture]
    public sealed class TryGetEdgeCaseTests
    {
        private float _currentTime;

        private float TimeProvider()
        {
            return _currentTime;
        }

        [SetUp]
        public void SetUp()
        {
            _currentTime = 0f;
        }

        [Test]
        public void TryGetWithDefaultValueKeyReturnsFalse()
        {
            using Cache<int, string> cache = CacheBuilder<int, string>
                .NewBuilder()
                .MaximumSize(10)
                .Build();

            // Key 0 (default int) is valid and should work
            cache.Set(0, "zero");
            Assert.IsTrue(cache.TryGet(0, out string value));
            Assert.AreEqual("zero", value);
        }

        [Test]
        public void TryGetWithEmptyStringKeyWorks()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .Build();

            // Empty string is a valid key
            cache.Set("", 42);
            Assert.IsTrue(cache.TryGet("", out int value));
            Assert.AreEqual(42, value);
        }

        [Test]
        public void TryGetWithWhitespaceKeyWorks()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .Build();

            // Whitespace is a valid key
            cache.Set(" ", 1);
            cache.Set("  ", 2);
            cache.Set("\t", 3);

            Assert.IsTrue(cache.TryGet(" ", out int v1));
            Assert.AreEqual(1, v1);
            Assert.IsTrue(cache.TryGet("  ", out int v2));
            Assert.AreEqual(2, v2);
            Assert.IsTrue(cache.TryGet("\t", out int v3));
            Assert.AreEqual(3, v3);
        }

        [Test]
        public void TryGetReturnsFalseAfterExpirationWithDiagnostics()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .ExpireAfterWrite(1.0f)
                .TimeProvider(TimeProvider)
                .RecordStatistics()
                .Build();

            cache.Set("key", 42);
            Assert.IsTrue(
                cache.TryGet("key", out int value1),
                "Key should exist immediately after set"
            );
            Assert.AreEqual(42, value1, "Value should be 42");

            CacheStatistics statsBeforeExpiration = cache.GetStatistics();
            Assert.AreEqual(1, statsBeforeExpiration.HitCount, "Should have 1 hit");
            Assert.AreEqual(0, statsBeforeExpiration.MissCount, "Should have 0 misses");

            _currentTime = 2.0f; // Advance time past expiration

            Assert.IsFalse(
                cache.TryGet("key", out int value2),
                "Key should be expired after TTL"
            );
            Assert.AreEqual(default(int), value2, "Value should be default after expiration");

            CacheStatistics statsAfterExpiration = cache.GetStatistics();
            Assert.AreEqual(1, statsAfterExpiration.MissCount, "Should have 1 miss after expiration");
            Assert.AreEqual(1, statsAfterExpiration.ExpiredCount, "Should have 1 expired entry");
        }

        [Test]
        public void TryGetWithValueTypeValueReturnsDefaultOnMiss()
        {
            using Cache<string, int> cache = CacheBuilder<string, int>
                .NewBuilder()
                .MaximumSize(10)
                .Build();

            Assert.IsFalse(cache.TryGet("nonexistent", out int value));
            Assert.AreEqual(0, value, "Value should be default(int) = 0 on miss");
        }

        [Test]
        public void TryGetWithReferenceTypeValueReturnsNullOnMiss()
        {
            using Cache<string, string> cache = CacheBuilder<string, string>
                .NewBuilder()
                .MaximumSize(10)
                .Build();

            Assert.IsFalse(cache.TryGet("nonexistent", out string value));
            Assert.IsNull(value, "Value should be null on miss for reference types");
        }

        [Test]
        public void TryGetWithNullableValueTypeWorks()
        {
            using Cache<string, int?> cache = CacheBuilder<string, int?>
                .NewBuilder()
                .MaximumSize(10)
                .Build();

            cache.Set("withValue", 42);
            cache.Set("withNull", null);

            Assert.IsTrue(cache.TryGet("withValue", out int? value1));
            Assert.AreEqual(42, value1);

            Assert.IsTrue(cache.TryGet("withNull", out int? value2));
            Assert.IsNull(value2, "Should successfully retrieve null value");
        }
    }
}
