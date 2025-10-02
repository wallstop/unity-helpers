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
    }
}
