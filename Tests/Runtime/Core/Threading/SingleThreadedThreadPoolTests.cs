namespace WallstopStudios.UnityHelpers.Tests.Core.Threading
{
    using System.Collections;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Threading;

    public sealed class SingleThreadedThreadPoolTests
    {
        [Test]
        public void Disposal()
        {
            using SingleThreadedThreadPool threadPool = new();
        }

        [UnityTest]
        public IEnumerator StuffHappening()
        {
            bool thingHappened = false;
            using SingleThreadedThreadPool threadPool = new();
            for (int i = 0; i < 100; ++i)
            {
                thingHappened = false;
                threadPool.Enqueue(() => thingHappened = true);
                while (!thingHappened)
                {
                    yield return null;
                }
            }
        }

        [UnityTest]
        public IEnumerator Ordering()
        {
            List<int> received = new();
            using SingleThreadedThreadPool threadPool = new();
            for (int i = 0; i < 100; ++i)
            {
                int localValue = i;
                threadPool.Enqueue(() => received.Add(localValue));
            }
            while (received.Count < 100)
            {
                yield return null;
            }

            for (int i = 0; i < 100; ++i)
            {
                Assert.AreEqual(i, received[i]);
            }
        }
    }
}
