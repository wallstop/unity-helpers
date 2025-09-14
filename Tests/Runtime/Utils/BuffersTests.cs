namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Random;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class BuffersTests
    {
        [Test]
        public void GenericPoolListTests()
        {
            {
                using PooledResource<List<int>> firstList = WallstopGenericPool<List<int>>.Get();
                using PooledResource<List<int>> secondList = WallstopGenericPool<List<int>>.Get();
                Assert.AreNotEqual(firstList, secondList);
                firstList.resource.Add(1);
                Assert.AreEqual(1, firstList.resource.Count);
                Assert.AreEqual(0, secondList.resource.Count);
                secondList.resource.Add(2);
                Assert.AreEqual(1, firstList.resource.Count);
                Assert.AreEqual(1, secondList.resource.Count);
            }
            {
                // Ensure cleared
                using PooledResource<List<int>> firstList = WallstopGenericPool<List<int>>.Get();
                Assert.AreEqual(0, firstList.resource.Count);
            }
        }

        [Test]
        public void ArrayPoolSizeTests()
        {
            for (int i = 0; i < 100; ++i)
            {
                using PooledResource<int[]> resource = WallstopArrayPool<int>.Get(i);
                Assert.AreEqual(i, resource.resource.Length);
                for (int j = 0; j < i; ++j)
                {
                    resource.resource[j] = PRNG.Instance.Next();
                }
            }

            for (int i = 0; i < 100; ++i)
            {
                using PooledResource<int[]> resource = WallstopArrayPool<int>.Get(i);
                Assert.AreEqual(i, resource.resource.Length);
                for (int j = 0; j < i; ++j)
                {
                    Assert.AreEqual(0, resource.resource[j]);
                }
            }
        }
    }
}
