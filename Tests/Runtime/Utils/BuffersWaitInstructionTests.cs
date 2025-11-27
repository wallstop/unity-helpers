namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class BuffersWaitInstructionTests
    {
        private IDisposable waitInstructionScope;

        [SetUp]
        public void SetUp()
        {
            waitInstructionScope = Buffers.BeginWaitInstructionTestScope();
        }

        [TearDown]
        public void TearDown()
        {
            waitInstructionScope?.Dispose();
        }

        [Test]
        public void GetWaitForSecondsCachesByValue()
        {
            WaitForSeconds a1 = Buffers.GetWaitForSeconds(0.25f);
            WaitForSeconds a2 = Buffers.GetWaitForSeconds(0.25f);
            WaitForSeconds b = Buffers.GetWaitForSeconds(0.5f);

            Assert.AreSame(a1, a2);
            Assert.AreNotSame(a1, b);
        }

        [Test]
        public void GetWaitForSecondsRealtimeCachesByValue()
        {
            WaitForSecondsRealtime a1 = Buffers.GetWaitForSecondsRealTime(1f);
            WaitForSecondsRealtime a2 = Buffers.GetWaitForSecondsRealTime(1f);
            WaitForSecondsRealtime b = Buffers.GetWaitForSecondsRealTime(2f);

            Assert.AreSame(a1, a2);
            Assert.AreNotSame(a1, b);
        }

        [TestCase(0.14f, 0.1f)]
        [TestCase(0.149f, 0.1f)]
        [TestCase(0.151f, 0.2f)]
        [TestCase(0.16f, 0.2f)]
        [TestCase(0.26f, 0.3f)]
        public void QuantizationRoundsDurationsToNearestStep(
            float requestedSeconds,
            float expectedQuantizedSeconds
        )
        {
            Buffers.WaitInstructionQuantizationStepSeconds = 0.1f;

            WaitForSeconds baseline = Buffers.GetWaitForSeconds(expectedQuantizedSeconds);
            WaitForSeconds quantized = Buffers.GetWaitForSeconds(requestedSeconds);

            Assert.AreSame(baseline, quantized);
            Assert.IsTrue(Buffers.WaitForSecondsCacheDiagnostics.IsQuantized);
        }

        [TestCase(0.049f, 0.051f)]
        [TestCase(0.14f, 0.16f)]
        [TestCase(1.249f, 1.251f)]
        public void QuantizationSeparatesDurationsAcrossStepBoundaries(
            float firstSeconds,
            float secondSeconds
        )
        {
            Buffers.WaitInstructionQuantizationStepSeconds = 0.1f;

            WaitForSeconds first = Buffers.GetWaitForSeconds(firstSeconds);
            WaitForSeconds second = Buffers.GetWaitForSeconds(secondSeconds);

            Assert.AreNotSame(first, second);
        }

        [TestCase(false, 0.24f, 0.22f)]
        [TestCase(true, 0.36f, 0.39f)]
        public void TryGetQuantizationCollapsesDurations(
            bool useRealtime,
            float firstSeconds,
            float secondSeconds
        )
        {
            Buffers.WaitInstructionQuantizationStepSeconds = 0.1f;
            Buffers.WaitInstructionMaxDistinctEntries = 8;

            if (useRealtime)
            {
                WaitForSecondsRealtime first = Buffers.TryGetWaitForSecondsRealtimePooled(
                    firstSeconds
                );
                WaitForSecondsRealtime second = Buffers.TryGetWaitForSecondsRealtimePooled(
                    secondSeconds
                );

                Assert.NotNull(first);
                Assert.AreSame(first, second);

                WaitInstructionCacheDiagnostics diagnostics =
                    Buffers.WaitForSecondsRealtimeCacheDiagnostics;
                Assert.IsTrue(diagnostics.IsQuantized);
                Assert.AreEqual(1, diagnostics.DistinctEntries);
            }
            else
            {
                WaitForSeconds first = Buffers.TryGetWaitForSecondsPooled(firstSeconds);
                WaitForSeconds second = Buffers.TryGetWaitForSecondsPooled(secondSeconds);

                Assert.NotNull(first);
                Assert.AreSame(first, second);

                WaitInstructionCacheDiagnostics diagnostics =
                    Buffers.WaitForSecondsCacheDiagnostics;
                Assert.IsTrue(diagnostics.IsQuantized);
                Assert.AreEqual(1, diagnostics.DistinctEntries);
            }
        }

        [TestCase(false, 0.24f, 0.26f)]
        [TestCase(true, 0.34f, 0.36f)]
        public void TryGetQuantizationSeparatesDurations(
            bool useRealtime,
            float firstSeconds,
            float secondSeconds
        )
        {
            Buffers.WaitInstructionQuantizationStepSeconds = 0.1f;
            Buffers.WaitInstructionMaxDistinctEntries = 8;

            if (useRealtime)
            {
                WaitForSecondsRealtime first = Buffers.TryGetWaitForSecondsRealtimePooled(
                    firstSeconds
                );
                WaitForSecondsRealtime second = Buffers.TryGetWaitForSecondsRealtimePooled(
                    secondSeconds
                );

                Assert.NotNull(first);
                Assert.NotNull(second);
                Assert.AreNotSame(first, second);

                WaitInstructionCacheDiagnostics diagnostics =
                    Buffers.WaitForSecondsRealtimeCacheDiagnostics;
                Assert.AreEqual(2, diagnostics.DistinctEntries);
            }
            else
            {
                WaitForSeconds first = Buffers.TryGetWaitForSecondsPooled(firstSeconds);
                WaitForSeconds second = Buffers.TryGetWaitForSecondsPooled(secondSeconds);

                Assert.NotNull(first);
                Assert.NotNull(second);
                Assert.AreNotSame(first, second);

                WaitInstructionCacheDiagnostics diagnostics =
                    Buffers.WaitForSecondsCacheDiagnostics;
                Assert.AreEqual(2, diagnostics.DistinctEntries);
            }
        }

        [Test]
        public void CacheLimitPreventsAdditionalEntries()
        {
            Buffers.WaitInstructionMaxDistinctEntries = 1;

            WaitForSeconds first = Buffers.GetWaitForSeconds(0.25f);
            WaitForSeconds second = Buffers.GetWaitForSeconds(0.25f);
            WaitForSeconds third = Buffers.GetWaitForSeconds(0.5f);
            WaitForSeconds fourth = Buffers.GetWaitForSeconds(0.5f);

            Assert.AreSame(first, second, "cached duration should still reuse pooled instance");
            Assert.AreNotSame(
                third,
                fourth,
                "limit hit should prevent caching additional durations"
            );

            WaitInstructionCacheDiagnostics diagnostics = Buffers.WaitForSecondsCacheDiagnostics;
            Assert.AreEqual(1, diagnostics.DistinctEntries);
            Assert.GreaterOrEqual(diagnostics.LimitRefusals, 1);
        }

        [Test]
        public void TryGetWaitForSecondsPooledReturnsNullWhenLimitExceeded()
        {
            Buffers.WaitInstructionMaxDistinctEntries = 1;

            WaitForSeconds cached = Buffers.TryGetWaitForSecondsPooled(0.25f);
            WaitForSeconds shouldBeNull = Buffers.TryGetWaitForSecondsPooled(0.5f);

            Assert.NotNull(cached);
            Assert.IsNull(shouldBeNull);
        }

        [Test]
        public void TryGetWaitForSecondsRealtimePooledReturnsNullWhenLimitExceeded()
        {
            Buffers.WaitInstructionMaxDistinctEntries = 1;

            WaitForSecondsRealtime cached = Buffers.TryGetWaitForSecondsRealtimePooled(0.25f);
            WaitForSecondsRealtime shouldBeNull = Buffers.TryGetWaitForSecondsRealtimePooled(0.5f);

            Assert.NotNull(cached);
            Assert.IsNull(shouldBeNull);
        }

        [Test]
        public void LruEvictsOldestDurationWhenEnabled()
        {
            Buffers.WaitInstructionUseLruEviction = true;
            Buffers.WaitInstructionMaxDistinctEntries = 2;

            WaitForSeconds first = Buffers.GetWaitForSeconds(0.1f);
            WaitForSeconds second = Buffers.GetWaitForSeconds(0.2f);
            WaitForSeconds third = Buffers.GetWaitForSeconds(0.3f); // should evict 0.1

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.NotNull(third);

            WaitForSeconds shouldBeNull = Buffers.TryGetWaitForSecondsPooled(0.1f);
            Assert.IsNull(shouldBeNull, "Least recently used entry should have been evicted.");

            WaitInstructionCacheDiagnostics diagnostics = Buffers.WaitForSecondsCacheDiagnostics;
            Assert.AreEqual(1, diagnostics.Evictions);
            Assert.IsTrue(diagnostics.IsLruEnabled);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TryGetDoesNotTriggerEvictionWhenCacheOnly(bool useRealtime)
        {
            Buffers.WaitInstructionUseLruEviction = true;
            Buffers.WaitInstructionMaxDistinctEntries = 1;

            if (useRealtime)
            {
                WaitForSecondsRealtime cached = Buffers.GetWaitForSecondsRealTime(0.1f);
                Assert.NotNull(cached);

                WaitForSecondsRealtime shouldBeNull = Buffers.TryGetWaitForSecondsRealtimePooled(
                    0.2f
                );
                Assert.IsNull(shouldBeNull);

                WaitForSecondsRealtime shouldStillExist =
                    Buffers.TryGetWaitForSecondsRealtimePooled(0.1f);
                Assert.NotNull(shouldStillExist);

                WaitInstructionCacheDiagnostics diagnostics =
                    Buffers.WaitForSecondsRealtimeCacheDiagnostics;
                Assert.AreEqual(0, diagnostics.Evictions);
            }
            else
            {
                WaitForSeconds cached = Buffers.GetWaitForSeconds(0.1f);
                Assert.NotNull(cached);

                WaitForSeconds shouldBeNull = Buffers.TryGetWaitForSecondsPooled(0.2f);
                Assert.IsNull(shouldBeNull);

                WaitForSeconds shouldStillExist = Buffers.TryGetWaitForSecondsPooled(0.1f);
                Assert.NotNull(shouldStillExist);

                WaitInstructionCacheDiagnostics diagnostics =
                    Buffers.WaitForSecondsCacheDiagnostics;
                Assert.AreEqual(0, diagnostics.Evictions);
            }
        }

        [TestCase(false, 0.25f)]
        [TestCase(true, 0.35f)]
        public void TryGetPopulatesCacheWhenCapacityAvailable(bool useRealtime, float seconds)
        {
            Buffers.WaitInstructionUseLruEviction = true;
            Buffers.WaitInstructionMaxDistinctEntries = 4;

            if (useRealtime)
            {
                WaitForSecondsRealtime initial = Buffers.TryGetWaitForSecondsRealtimePooled(
                    seconds
                );
                Assert.NotNull(initial);

                WaitForSecondsRealtime second = Buffers.TryGetWaitForSecondsRealtimePooled(seconds);
                Assert.AreSame(initial, second);

                WaitInstructionCacheDiagnostics diagnostics =
                    Buffers.WaitForSecondsRealtimeCacheDiagnostics;
                Assert.AreEqual(1, diagnostics.DistinctEntries);
                Assert.AreEqual(0, diagnostics.Evictions);
            }
            else
            {
                WaitForSeconds initial = Buffers.TryGetWaitForSecondsPooled(seconds);
                Assert.NotNull(initial);

                WaitForSeconds second = Buffers.TryGetWaitForSecondsPooled(seconds);
                Assert.AreSame(initial, second);

                WaitInstructionCacheDiagnostics diagnostics =
                    Buffers.WaitForSecondsCacheDiagnostics;
                Assert.AreEqual(1, diagnostics.DistinctEntries);
                Assert.AreEqual(0, diagnostics.Evictions);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TryGetRefreshesLruOrdering(bool useRealtime)
        {
            Buffers.WaitInstructionUseLruEviction = true;
            Buffers.WaitInstructionMaxDistinctEntries = 2;

            if (useRealtime)
            {
                WaitForSecondsRealtime first = Buffers.GetWaitForSecondsRealTime(0.1f);
                WaitForSecondsRealtime second = Buffers.GetWaitForSecondsRealTime(0.2f);
                WaitForSecondsRealtime refreshed = Buffers.TryGetWaitForSecondsRealtimePooled(0.1f);

                Assert.AreSame(first, refreshed);

                WaitForSecondsRealtime third = Buffers.GetWaitForSecondsRealTime(0.3f);
                Assert.NotNull(third);

                WaitForSecondsRealtime shouldRemain = Buffers.TryGetWaitForSecondsRealtimePooled(
                    0.1f
                );
                Assert.NotNull(shouldRemain);

                WaitForSecondsRealtime shouldBeEvicted = Buffers.TryGetWaitForSecondsRealtimePooled(
                    0.2f
                );
                Assert.IsNull(shouldBeEvicted);

                WaitInstructionCacheDiagnostics diagnostics =
                    Buffers.WaitForSecondsRealtimeCacheDiagnostics;
                Assert.AreEqual(1, diagnostics.Evictions);
            }
            else
            {
                WaitForSeconds first = Buffers.GetWaitForSeconds(0.1f);
                WaitForSeconds second = Buffers.GetWaitForSeconds(0.2f);
                WaitForSeconds refreshed = Buffers.TryGetWaitForSecondsPooled(0.1f);

                Assert.AreSame(first, refreshed);

                WaitForSeconds third = Buffers.GetWaitForSeconds(0.3f);
                Assert.NotNull(third);

                WaitForSeconds shouldRemain = Buffers.TryGetWaitForSecondsPooled(0.1f);
                Assert.NotNull(shouldRemain);

                WaitForSeconds shouldBeEvicted = Buffers.TryGetWaitForSecondsPooled(0.2f);
                Assert.IsNull(shouldBeEvicted);

                WaitInstructionCacheDiagnostics diagnostics =
                    Buffers.WaitForSecondsCacheDiagnostics;
                Assert.AreEqual(1, diagnostics.Evictions);
            }
        }
    }
}
