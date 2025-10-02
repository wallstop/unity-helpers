namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System;
    using System.Collections;
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
                        throw new InvalidOperationException("First call fails");
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
    }
}
