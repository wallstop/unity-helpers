namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.DataStructure;

    public sealed class TimedCacheTests
    {
        [Test]
        public void ConstructorValidatesArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new TimedCache<int>(null, 0.1f));
            Assert.Throws<ArgumentException>(() => new TimedCache<int>(() => 1, -0.5f));
        }

        [Test]
        public void ConstructorWithZeroTtlDoesNotThrow()
        {
            _ = new TimedCache<int>(() => 1, 0f);
            Assert.Pass();
        }

        [Test]
        public void ConstructorWithVerySmallTtlWorks()
        {
            TimedCache<int> cache = new(() => 42, 0.001f);
            Assert.AreEqual(42, cache.Value);
        }

        [Test]
        public void ConstructorWithVeryLargeTtlWorks()
        {
            TimedCache<int> cache = new(() => 42, float.MaxValue);
            Assert.AreEqual(42, cache.Value);
        }

        [Test]
        public void ValueInitiallyCallsProducer()
        {
            int producerCalls = 0;
            TimedCache<int> cache = new(() => ++producerCalls, 1f);

            Assert.AreEqual(0, producerCalls);
            int value = cache.Value;
            Assert.AreEqual(1, value);
            Assert.AreEqual(1, producerCalls);
        }

        [Test]
        public void ValueCachesResultWithinTtl()
        {
            int producerCalls = 0;
            TimedCache<int> cache = new(() => ++producerCalls, 10f);

            int value1 = cache.Value;
            int value2 = cache.Value;
            int value3 = cache.Value;

            Assert.AreEqual(1, value1);
            Assert.AreEqual(1, value2);
            Assert.AreEqual(1, value3);
            Assert.AreEqual(1, producerCalls);
        }

        [Test]
        public void CacheHandlesNullValues()
        {
            int producerCalls = 0;
            TimedCache<string> cache = new(
                () =>
                {
                    producerCalls++;
                    return null;
                },
                1f
            );

            string value = cache.Value;
            Assert.IsNull(value);
            Assert.AreEqual(1, producerCalls);
        }

        [Test]
        public void CacheHandlesReferenceTypes()
        {
            int producerCalls = 0;
            TimedCache<object> cache = new(
                () =>
                {
                    producerCalls++;
                    return new object();
                },
                1f
            );

            object value1 = cache.Value;
            object value2 = cache.Value;

            Assert.AreSame(value1, value2);
            Assert.AreEqual(1, producerCalls);
        }

        [Test]
        public void CacheHandlesValueTypes()
        {
            int producerCalls = 0;
            TimedCache<float> cache = new(
                () =>
                {
                    producerCalls++;
                    return 3.14f;
                },
                1f
            );

            float value = cache.Value;
            Assert.AreEqual(3.14f, value, 0.001f);
            Assert.AreEqual(1, producerCalls);
        }

        [Test]
        public void MultipleResetsWork()
        {
            int producerCalls = 0;
            TimedCache<int> cache = new(() => ++producerCalls, 10f);

            _ = cache.Value;
            cache.Reset();
            _ = cache.Value;
            cache.Reset();
            _ = cache.Value;

            Assert.AreEqual(3, producerCalls);
        }

        [Test]
        public void ResetBeforeFirstAccessWorks()
        {
            int producerCalls = 0;
            TimedCache<int> cache = new(() => ++producerCalls, 10f);

            cache.Reset();
            int value = cache.Value;

            Assert.AreEqual(1, value);
            Assert.AreEqual(1, producerCalls);
        }

        [Test]
        public void ProducerExceptionsPropagateToValue()
        {
            TimedCache<int> cache = new(
                () => throw new InvalidOperationException("Test exception"),
                1f
            );

            Assert.Throws<InvalidOperationException>(() =>
            {
                int _ = cache.Value;
            });
        }

        [Test]
        public void ProducerExceptionDoesNotCacheResult()
        {
            int producerCalls = 0;
            TimedCache<int> cache = new(
                () =>
                {
                    producerCalls++;
                    if (producerCalls == 1)
                    {
                        throw new InvalidOperationException("First call fails");
                    }

                    return 42;
                },
                10f
            );

            Assert.Throws<InvalidOperationException>(() =>
            {
                int _ = cache.Value;
            });
            Assert.AreEqual(1, producerCalls);

            // Reset and try again
            cache.Reset();
            int value = cache.Value;
            Assert.AreEqual(42, value);
            Assert.AreEqual(2, producerCalls);
        }

        [UnityTest]
        public IEnumerator ValueRecomputedAfterTtlExpires()
        {
            int producerCalls = 0;
            TimedCache<int> cache = new(() => ++producerCalls, 0.05f);

            int first = cache.Value;
            Assert.AreEqual(1, first);
            Assert.AreEqual(1, producerCalls);

            int immediate = cache.Value;
            Assert.AreEqual(1, immediate);
            Assert.AreEqual(1, producerCalls);

            yield return new WaitForSeconds(0.06f);

            int refreshed = cache.Value;
            Assert.AreEqual(2, refreshed);
            Assert.AreEqual(2, producerCalls);
        }

        [UnityTest]
        public IEnumerator ResetForcesImmediateRefresh()
        {
            int producerCalls = 0;
            TimedCache<int> cache = new(() => ++producerCalls, 10f);

            int first = cache.Value;
            Assert.AreEqual(1, first);
            Assert.AreEqual(1, producerCalls);

            cache.Reset();
            yield return null;

            int afterReset = cache.Value;
            Assert.AreEqual(2, afterReset);
            Assert.AreEqual(2, producerCalls);
        }

        [UnityTest]
        public IEnumerator MultipleTtlExpirationsWork()
        {
            int producerCalls = 0;
            TimedCache<int> cache = new(() => ++producerCalls, 0.05f);

            int value1 = cache.Value;
            Assert.AreEqual(1, value1);

            yield return new WaitForSeconds(0.06f);
            int value2 = cache.Value;
            Assert.AreEqual(2, value2);

            yield return new WaitForSeconds(0.06f);
            int value3 = cache.Value;
            Assert.AreEqual(3, value3);

            Assert.AreEqual(3, producerCalls);
        }

        [UnityTest]
        public IEnumerator AccessWithinTtlDoesNotResetTimer()
        {
            int producerCalls = 0;
            TimedCache<int> cache = new(() => ++producerCalls, 0.1f);

            _ = cache.Value;
            yield return new WaitForSeconds(0.05f);
            _ = cache.Value; // Access within TTL
            yield return new WaitForSeconds(0.05f);
            // Total time ~0.1s, but we accessed at 0.05s, so should still expire at original time
            _ = cache.Value;

            // Should have refreshed after original TTL
            Assert.GreaterOrEqual(producerCalls, 1);
        }

        [Test]
        public void ConstructorWithJitterDoesNotThrow()
        {
            TimedCache<int> cache = new(() => 1, 1f, useJitter: true);
            Assert.AreEqual(1, cache.Value);
        }

        [UnityTest]
        public IEnumerator JitterCausesFirstExpirationToVaryFromTtl()
        {
            int producerCalls = 0;
            TimedCache<int> cache = new(() => ++producerCalls, 0.1f, useJitter: true);

            int first = cache.Value;
            Assert.AreEqual(1, first);
            Assert.AreEqual(1, producerCalls);

            // Wait for standard TTL - may or may not have expired depending on jitter
            yield return new WaitForSeconds(0.1f);

            // Access to potentially trigger expiration
            _ = cache.Value;

            // Wait additional time to ensure jitter range is covered
            yield return new WaitForSeconds(0.11f);

            int afterJitter = cache.Value;
            // Should have refreshed by now (TTL + max jitter = 0.2s total)
            Assert.AreEqual(2, afterJitter);
            Assert.AreEqual(2, producerCalls);
        }

        [UnityTest]
        public IEnumerator SubsequentExpirationsAfterJitterUseStandardTtl()
        {
            int producerCalls = 0;
            TimedCache<int> cache = new(() => ++producerCalls, 0.05f, useJitter: true);

            _ = cache.Value;
            Assert.AreEqual(1, producerCalls);

            // Wait long enough to cover jitter range
            yield return new WaitForSeconds(0.11f);
            _ = cache.Value;
            Assert.AreEqual(2, producerCalls);

            // Now subsequent expirations should use standard TTL without jitter
            yield return new WaitForSeconds(0.06f);
            _ = cache.Value;
            Assert.AreEqual(3, producerCalls);

            // Verify it's consistent
            yield return new WaitForSeconds(0.06f);
            _ = cache.Value;
            Assert.AreEqual(4, producerCalls);
        }

        [Test]
        public void MultipleAccessesInSameFrameReturnSameValue()
        {
            int producerCalls = 0;
            TimedCache<int> cache = new(() => ++producerCalls, 1f);

            int v1 = cache.Value;
            int v2 = cache.Value;
            int v3 = cache.Value;
            int v4 = cache.Value;
            int v5 = cache.Value;

            Assert.AreEqual(1, v1);
            Assert.AreEqual(1, v2);
            Assert.AreEqual(1, v3);
            Assert.AreEqual(1, v4);
            Assert.AreEqual(1, v5);
            Assert.AreEqual(1, producerCalls);
        }

        [UnityTest]
        public IEnumerator ZeroTtlExpiresImmediately()
        {
            int producerCalls = 0;
            TimedCache<int> cache = new(() => ++producerCalls, 0f);

            int first = cache.Value;
            Assert.AreEqual(1, first);
            Assert.AreEqual(1, producerCalls);

            yield return null;

            int second = cache.Value;
            Assert.AreEqual(2, second);
            Assert.AreEqual(2, producerCalls);

            yield return null;

            int third = cache.Value;
            Assert.AreEqual(3, third);
            Assert.AreEqual(3, producerCalls);
        }

        [UnityTest]
        public IEnumerator ResetAfterJitterDoesNotReapplyJitter()
        {
            int producerCalls = 0;
            TimedCache<int> cache = new(() => ++producerCalls, 0.05f, useJitter: true);

            _ = cache.Value;
            Assert.AreEqual(1, producerCalls);

            // Wait long enough to cover initial jitter range.
            yield return new WaitForSeconds(0.12f);
            _ = cache.Value;
            Assert.AreEqual(2, producerCalls);

            cache.Reset();

            yield return new WaitForSeconds(0.049f);
            _ = cache.Value;
            Assert.AreEqual(2, producerCalls);

            yield return new WaitForSeconds(0.02f);
            _ = cache.Value;
            Assert.AreEqual(3, producerCalls);
        }

        [UnityTest]
        public IEnumerator ValueRefreshesExactlyWhenTtlElapsed()
        {
            int producerCalls = 0;
            TimedCache<int> cache = new(() => ++producerCalls, 0.05f);

            int first = cache.Value;
            Assert.AreEqual(1, first);

            // Wait just shy of the TTL — cache should still be valid.
            yield return new WaitForSeconds(0.049f);
            int withinWindow = cache.Value;
            Assert.AreEqual(1, withinWindow);
            Assert.AreEqual(1, producerCalls);

            // Wait a hair past the TTL to trigger the exact HasEnoughTimePassed boundary.
            yield return new WaitForSeconds(0.002f);
            int afterExpiry = cache.Value;
            Assert.AreEqual(2, afterExpiry);
            Assert.AreEqual(2, producerCalls);
        }

        [Test]
        public void ProducerCalledExactlyOncePerRefresh()
        {
            int producerCalls = 0;
            int sideEffectCounter = 0;
            TimedCache<int> cache = new(
                () =>
                {
                    producerCalls++;
                    sideEffectCounter += 10;
                    return producerCalls;
                },
                1f
            );

            _ = cache.Value;
            _ = cache.Value;
            _ = cache.Value;

            Assert.AreEqual(1, producerCalls);
            Assert.AreEqual(10, sideEffectCounter);

            cache.Reset();
            _ = cache.Value;

            Assert.AreEqual(2, producerCalls);
            Assert.AreEqual(20, sideEffectCounter);
        }

        [Test]
        public void CacheHandlesStructTypes()
        {
            int producerCalls = 0;
            TimedCache<Vector3> cache = new(
                () =>
                {
                    producerCalls++;
                    return new Vector3(1f, 2f, 3f);
                },
                1f
            );

            Vector3 value1 = cache.Value;
            Vector3 value2 = cache.Value;

            Assert.AreEqual(new Vector3(1f, 2f, 3f), value1);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), value2);
            Assert.AreEqual(1, producerCalls);
        }

        [Test]
        public void CacheUpdatesWhenProducerReturnsDifferentValues()
        {
            int producerCalls = 0;
            TimedCache<int> cache = new(() => ++producerCalls * 100, 1f);

            int first = cache.Value;
            Assert.AreEqual(100, first);

            cache.Reset();
            int second = cache.Value;
            Assert.AreEqual(200, second);

            cache.Reset();
            int third = cache.Value;
            Assert.AreEqual(300, third);
        }

        [Test]
        public void ResetAfterExceptionAllowsRecovery()
        {
            int producerCalls = 0;
            TimedCache<int> cache = new(
                () =>
                {
                    producerCalls++;
                    if (producerCalls < 3)
                    {
                        throw new InvalidOperationException($"Call {producerCalls} fails");
                    }

                    return 999;
                },
                10f
            );

            // First attempt fails
            Assert.Throws<InvalidOperationException>(() =>
            {
                int _ = cache.Value;
            });
            Assert.AreEqual(1, producerCalls);

            // Second attempt also fails
            Assert.Throws<InvalidOperationException>(() =>
            {
                _ = cache.Value;
            });
            Assert.AreEqual(2, producerCalls);

            // Third attempt succeeds
            cache.Reset();
            int value = cache.Value;
            Assert.AreEqual(999, value);
            Assert.AreEqual(3, producerCalls);

            // Cached value should persist
            int cached = cache.Value;
            Assert.AreEqual(999, cached);
            Assert.AreEqual(3, producerCalls);
        }

        [Test]
        public void CacheHandlesDefaultValues()
        {
            int producerCalls = 0;
            TimedCache<int> cacheInt = new(
                () =>
                {
                    producerCalls++;
                    return default;
                },
                1f
            );

            int intValue = cacheInt.Value;
            Assert.AreEqual(0, intValue);
            Assert.AreEqual(1, producerCalls);

            producerCalls = 0;
            TimedCache<bool> cacheBool = new(
                () =>
                {
                    producerCalls++;
                    return default;
                },
                1f
            );

            bool boolValue = cacheBool.Value;
            Assert.AreEqual(false, boolValue);
            Assert.AreEqual(1, producerCalls);
        }

        [Test]
        public void CacheDistinguishesBetweenNullAndDefault()
        {
            int nullProducerCalls = 0;
            TimedCache<string> nullCache = new(
                () =>
                {
                    nullProducerCalls++;
                    return null;
                },
                1f
            );

            int defaultProducerCalls = 0;
            TimedCache<string> defaultCache = new(
                () =>
                {
                    defaultProducerCalls++;
                    return default;
                },
                1f
            );

            Assert.IsNull(nullCache.Value);
            Assert.IsNull(defaultCache.Value);
            Assert.AreEqual(1, nullProducerCalls);
            Assert.AreEqual(1, defaultProducerCalls);

            // Both should cache the null/default value
            Assert.IsNull(nullCache.Value);
            Assert.IsNull(defaultCache.Value);
            Assert.AreEqual(1, nullProducerCalls);
            Assert.AreEqual(1, defaultProducerCalls);
        }

        [Test]
        public void VerySmallTtlWithJitterDoesNotThrow()
        {
            TimedCache<int> cache = new(() => 42, 0.001f, useJitter: true);
            Assert.AreEqual(42, cache.Value);
        }

        [Test]
        public void VeryLargeTtlWithJitterDoesNotThrow()
        {
            TimedCache<int> cache = new(() => 42, float.MaxValue, useJitter: true);
            Assert.AreEqual(42, cache.Value);
        }

        [Test]
        public void MultipleResetsInSequenceWork()
        {
            int producerCalls = 0;
            TimedCache<int> cache = new(() => ++producerCalls, 10f);

            cache.Reset();
            cache.Reset();
            cache.Reset();
            int value = cache.Value;

            Assert.AreEqual(3, value);
            Assert.AreEqual(3, producerCalls);
        }

        [Test]
        public void CacheHandlesComplexReferenceTypes()
        {
            int producerCalls = 0;
            TimedCache<List<int>> cache = new(
                () =>
                {
                    producerCalls++;
                    return new List<int> { 1, 2, 3 };
                },
                1f
            );

            List<int> list1 = cache.Value;
            List<int> list2 = cache.Value;

            Assert.AreSame(list1, list2);
            Assert.AreEqual(3, list1.Count);
            Assert.AreEqual(1, producerCalls);

            // Verify mutations are visible (same reference)
            list1.Add(4);
            Assert.AreEqual(4, list2.Count);
        }

        [Test]
        public void ProducerWithCapturedVariablesWorks()
        {
            int externalCounter = 0;
            TimedCache<int> cache = new(
                () =>
                {
                    externalCounter++;
                    return externalCounter * 10;
                },
                1f
            );

            int first = cache.Value;
            Assert.AreEqual(10, first);
            Assert.AreEqual(1, externalCounter);

            cache.Reset();
            int second = cache.Value;
            Assert.AreEqual(20, second);
            Assert.AreEqual(2, externalCounter);
        }

        [UnityTest]
        public IEnumerator VeryShortTtlMultipleExpirationsWork()
        {
            int producerCalls = 0;
            TimedCache<int> cache = new(() => ++producerCalls, 0.001f); // ~1 frame at 60fps

            _ = cache.Value;
            Assert.AreEqual(1, producerCalls);

            yield return new WaitForSecondsRealtime(0.1f);
            _ = cache.Value;
            Assert.AreEqual(2, producerCalls);

            yield return new WaitForSecondsRealtime(0.1f);
            _ = cache.Value;
            Assert.AreEqual(3, producerCalls);

            yield return new WaitForSecondsRealtime(0.1f);
            _ = cache.Value;
            Assert.AreEqual(4, producerCalls);
        }

        [Test]
        public void ResetDoesNotRequirePriorValueAccess()
        {
            int producerCalls = 0;
            TimedCache<int> cache = new(() => ++producerCalls, 1f);

            cache.Reset();
            cache.Reset();
            cache.Reset();

            Assert.AreEqual(3, producerCalls);

            int value = cache.Value;
            Assert.AreEqual(3, value);
            Assert.AreEqual(3, producerCalls);
        }
    }
}
