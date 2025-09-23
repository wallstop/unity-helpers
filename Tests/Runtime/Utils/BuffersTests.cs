namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Random;
    using WallstopStudios.UnityHelpers.Utils;
#if !SINGLETHREADED
    using System.Threading;
    using System.Threading.Tasks;
#endif

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

        [Test]
        public void WallstopFastArrayPoolGetNegativeSizeThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => WallstopFastArrayPool<int>.Get(-1));
        }

        [Test]
        public void WallstopFastArrayPoolGetZeroSizeReturnsEmptyArrayWithNoOpDispose()
        {
            using PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(0);
            Assert.NotNull(pooled.resource);
            Assert.AreEqual(0, pooled.resource.Length);
            Assert.AreSame(Array.Empty<int>(), pooled.resource);
        }

        [Test]
        public void WallstopFastArrayPoolGetPositiveSizeReturnsArrayWithCorrectLength()
        {
            const int size = 10;
            using PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(size);

            Assert.NotNull(pooled.resource);
            Assert.AreEqual(size, pooled.resource.Length);
        }

        [Test]
        public void WallstopFastArrayPoolGetSameSizeReusesArrayAfterDispose()
        {
            const int size = 5;
            int[] firstArray;

            using (PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(size))
            {
                firstArray = pooled.resource;
                firstArray[0] = 42;
            }

            using PooledResource<int[]> pooledReused = WallstopFastArrayPool<int>.Get(size);
            Assert.AreSame(firstArray, pooledReused.resource);
            Assert.AreEqual(42, pooledReused.resource[0]);
        }

        [Test]
        public void WallstopFastArrayPoolGetDifferentSizesReturnsCorrectArrays()
        {
            int[] sizes = { 1, 5, 10, 100, 1000 };
            List<PooledResource<int[]>> pooledArrays = new();

            try
            {
                foreach (int size in sizes)
                {
                    PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(size);
                    pooledArrays.Add(pooled);
                    Assert.AreEqual(size, pooled.resource.Length);
                }
            }
            finally
            {
                foreach (PooledResource<int[]> pooled in pooledArrays)
                {
                    pooled.Dispose();
                }
            }
        }

        [Test]
        public void WallstopFastArrayPoolArraysNotClearedOnRelease()
        {
            const int size = 10;

            using (PooledResource<string[]> pooled = WallstopFastArrayPool<string>.Get(size))
            {
                for (int i = 0; i < size; i++)
                {
                    pooled.resource[i] = $"test{i}";
                }
            }

            using PooledResource<string[]> pooledReused = WallstopFastArrayPool<string>.Get(size);
            for (int i = 0; i < size; i++)
            {
                Assert.AreEqual($"test{i}", pooledReused.resource[i]);
            }
        }

        [Test]
        public void WallstopFastArrayPoolPoolGrowsAsNeeded()
        {
            const int size = 7;
            const int count = 5;
            List<PooledResource<int[]>> pooledArrays = new();

            try
            {
                for (int i = 0; i < count; i++)
                {
                    PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(size);
                    pooledArrays.Add(pooled);
                    Assert.AreEqual(size, pooled.resource.Length);
                }

                HashSet<int[]> distinctArrays = pooledArrays.Select(p => p.resource).ToHashSet();
                Assert.AreEqual(count, distinctArrays.Count);
            }
            finally
            {
                foreach (PooledResource<int[]> pooled in pooledArrays)
                {
                    pooled.Dispose();
                }
            }
        }

        [Test]
        public void WallstopFastArrayPoolDifferentTypesUseDifferentPools()
        {
            const int size = 10;

            using PooledResource<int[]> intPooled = WallstopFastArrayPool<int>.Get(size);
            using PooledResource<string[]> stringPooled = WallstopFastArrayPool<string>.Get(size);
            using PooledResource<float[]> floatPooled = WallstopFastArrayPool<float>.Get(size);

            Assert.AreEqual(size, intPooled.resource.Length);
            Assert.AreEqual(size, stringPooled.resource.Length);
            Assert.AreEqual(size, floatPooled.resource.Length);
        }

        [Test]
        public void WallstopFastArrayPoolLargeArraysWork()
        {
            const int size = 100000;
            using PooledResource<byte[]> pooled = WallstopFastArrayPool<byte>.Get(size);

            Assert.AreEqual(size, pooled.resource.Length);
        }

        [Test]
        public void WallstopFastArrayPoolNestedUsageWorks()
        {
            const int outerSize = 5;
            const int innerSize = 3;

            using PooledResource<int[]> outer = WallstopFastArrayPool<int>.Get(outerSize);
            Assert.AreEqual(outerSize, outer.resource.Length);
            Array.Clear(outer.resource, 0, outer.resource.Length);
            outer.resource[0] = 1;

            using (PooledResource<int[]> inner = WallstopFastArrayPool<int>.Get(innerSize))
            {
                inner.resource[0] = 2;
                Assert.AreEqual(innerSize, inner.resource.Length);
                Assert.AreEqual(1, outer.resource[0]);
                Assert.AreEqual(2, inner.resource[0]);
            }

            Assert.AreEqual(outerSize, outer.resource.Length);
            Assert.AreEqual(1, outer.resource[0]);
        }

        [UnityTest]
        public IEnumerator WallstopFastArrayPoolStressTest()
        {
            const int iterations = 1000;
            const int maxSize = 100;
            Random random = new(42);

            for (int i = 0; i < iterations; i++)
            {
                int size = random.Next(1, maxSize);
                using PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(size);

                Assert.AreEqual(size, pooled.resource.Length);

                for (int j = 0; j < Math.Min(10, size); j++)
                {
                    pooled.resource[j] = random.Next();
                }

                if (i % 100 == 0)
                {
                    yield return null;
                }
            }
        }

#if !SINGLETHREADED
        [Test]
        public void WallstopFastArrayPoolConcurrentAccessDifferentSizes()
        {
            const int threadCount = 10;
            const int operationsPerThread = 100;
            Task[] tasks = new Task[threadCount];
            List<Exception> exceptions = new();

            for (int t = 0; t < threadCount; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(async () =>
                {
                    try
                    {
                        PcgRandom random = new(threadId);
                        foreach (int i in Enumerable.Range(0, operationsPerThread).Shuffled(random))
                        {
                            int size = random.Next(1, 50) + threadId;
                            using PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(
                                size
                            );

                            Assert.AreEqual(size, pooled.resource.Length);

                            for (int j = 0; j < Math.Min(5, size); j++)
                            {
                                pooled.resource[j] = threadId * 1000 + i * 10 + j;
                            }

                            await Task.Delay(TimeSpan.FromMilliseconds(random.NextDouble()));
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
            }

            Task.WaitAll(tasks);

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        [Test]
        public void WallstopFastArrayPoolConcurrentAccessSameSize()
        {
            const int threadCount = 8;
            const int operationsPerThread = 200;
            const int arraySize = 25;
            Task[] tasks = new Task[threadCount];
            List<Exception> exceptions = new();

            for (int t = 0; t < threadCount; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(() =>
                {
                    try
                    {
                        PcgRandom random = new(threadId);
                        foreach (int i in Enumerable.Range(0, operationsPerThread).Shuffled(random))
                        {
                            using PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(
                                arraySize
                            );

                            Array.Clear(pooled.resource, 0, pooled.resource.Length);
                            Assert.AreEqual(arraySize, pooled.resource.Length);

                            for (int j = 0; j < arraySize; j++)
                            {
                                Assert.AreEqual(0, pooled.resource[j]);
                                pooled.resource[j] = threadId * 1000 + i;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
            }

            Task.WaitAll(tasks);

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        [Test]
        public void WallstopFastArrayPoolConcurrentAccessMixedSizes()
        {
            const int threadCount = 6;
            const int operationsPerThread = 150;
            Task[] tasks = new Task[threadCount];
            List<Exception> exceptions = new();
            int[] sizes = { 1, 5, 10, 20, 50, 100 };

            for (int t = 0; t < threadCount; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(async () =>
                {
                    try
                    {
                        PcgRandom random = new(threadId + 100);
                        foreach (int i in Enumerable.Range(0, operationsPerThread).Shuffled(random))
                        {
                            int size = random.NextOf(sizes);
                            using PooledResource<string[]> pooled =
                                WallstopFastArrayPool<string>.Get(size);
                            Array.Clear(pooled.resource, 0, pooled.resource.Length);

                            Assert.AreEqual(size, pooled.resource.Length);

                            for (int j = 0; j < size; j++)
                            {
                                Assert.IsNull(pooled.resource[j]);
                                pooled.resource[j] = $"T{threadId}-I{i}-J{j}";
                            }

                            if (i % 50 == 0)
                            {
                                await Task.Delay(TimeSpan.FromMilliseconds(random.NextDouble()));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
            }

            Task.WaitAll(tasks);

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        [Test]
        public void WallstopFastArrayPoolConcurrentAccessRapidAllocationDeallocation()
        {
            const int threadCount = 12;
            const int operationsPerThread = 500;
            Task[] tasks = new Task[threadCount];
            List<Exception> exceptions = new();

            for (int t = 0; t < threadCount; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(() =>
                {
                    try
                    {
                        PcgRandom random = new(threadId + 100);
                        foreach (int i in Enumerable.Range(0, operationsPerThread).Shuffled(random))
                        {
                            int size = random.Next(1, 30);
                            using PooledResource<byte[]> pooled = WallstopFastArrayPool<byte>.Get(
                                size
                            );

                            Assert.AreEqual(size, pooled.resource.Length);

                            for (int j = 0; j < size; j++)
                            {
                                pooled.resource[j] = (byte)(threadId + i + j);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
            }

            Task.WaitAll(tasks);

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        [Test]
        public void WallstopFastArrayPoolThreadSafetyPoolExpansion()
        {
            const int threadCount = 8;
            const int maxPoolSize = 1000;
            Task[] tasks = new Task[threadCount];
            List<Exception> exceptions = new();
            Barrier barrier = new(threadCount);

            for (int t = 0; t < threadCount; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();

                        for (int size = threadId + 1; size <= maxPoolSize; size += threadCount)
                        {
                            using PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(
                                size
                            );
                            Assert.AreEqual(size, pooled.resource.Length);

                            pooled.resource[0] = threadId;
                            pooled.resource[size - 1] = threadId;
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
            }

            Task.WaitAll(tasks);

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        [UnityTest]
        public IEnumerator WallstopFastArrayPoolConcurrentStressTest()
        {
            const int threadCount = 4;
            const int operationsPerThread = 250;
            Task[] tasks = new Task[threadCount];
            List<Exception> exceptions = new();
            int completedThreads = 0;

            for (int t = 0; t < threadCount; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(() =>
                {
                    try
                    {
                        PcgRandom random = new(threadId + 100);
                        foreach (int i in Enumerable.Range(0, operationsPerThread).Shuffled(random))
                        {
                            int size = random.Next(1, 100);
                            using PooledResource<float[]> pooled = WallstopFastArrayPool<float>.Get(
                                size
                            );

                            Assert.AreEqual(size, pooled.resource.Length);

                            for (int j = 0; j < Math.Min(10, size); j++)
                            {
                                pooled.resource[j] = threadId * 1000.0f + i + j * 0.1f;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                    finally
                    {
                        Interlocked.Increment(ref completedThreads);
                    }
                });
            }

            while (completedThreads < threadCount)
            {
                yield return null;
            }

            Task.WaitAll(tasks);

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }
#endif

        [Test]
        public void WallstopFastArrayPoolEdgeCaseVeryLargeSize()
        {
            const int veryLargeSize = 1000000;
            using PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(veryLargeSize);

            Assert.AreEqual(veryLargeSize, pooled.resource.Length);
            Assert.AreEqual(0, pooled.resource[0]);
            Assert.AreEqual(0, pooled.resource[veryLargeSize - 1]);
        }

        [Test]
        public void WallstopFastArrayPoolEdgeCaseSizeOne()
        {
            using PooledResource<string[]> pooled = WallstopFastArrayPool<string>.Get(1);
            Array.Clear(pooled.resource, 0, pooled.resource.Length);
            Assert.AreEqual(1, pooled.resource.Length);
            Assert.IsNull(pooled.resource[0]);

            pooled.resource[0] = "test";
            Assert.AreEqual("test", pooled.resource[0]);
        }

        [Test]
        public void WallstopFastArrayPoolPoolingBehaviorLifo()
        {
            const int size = 15;
            int[][] arrays = new int[3][];
            {
                using PooledResource<int[]> pooled1 = WallstopFastArrayPool<int>.Get(size);
                arrays[0] = pooled1.resource;
                using PooledResource<int[]> pooled2 = WallstopFastArrayPool<int>.Get(size);
                arrays[1] = pooled2.resource;
                using PooledResource<int[]> pooled3 = WallstopFastArrayPool<int>.Get(size);
                arrays[2] = pooled3.resource;
            }

            using PooledResource<int[]> pooledReuse1 = WallstopFastArrayPool<int>.Get(size);
            Assert.AreSame(arrays[0], pooledReuse1.resource);

            using PooledResource<int[]> pooledReuse2 = WallstopFastArrayPool<int>.Get(size);
            Assert.AreSame(arrays[1], pooledReuse2.resource);

            using PooledResource<int[]> pooledReuse3 = WallstopFastArrayPool<int>.Get(size);
            Assert.AreSame(arrays[2], pooledReuse3.resource);
        }

        [Test]
        public void WallstopArrayPoolGetNegativeSizeThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => WallstopArrayPool<int>.Get(-1));
        }

        [Test]
        public void WallstopArrayPoolGetZeroSizeReturnsEmptyArrayWithNoOpDispose()
        {
            using PooledResource<int[]> pooled = WallstopArrayPool<int>.Get(0);
            Assert.NotNull(pooled.resource);
            Assert.AreEqual(0, pooled.resource.Length);
            Assert.AreSame(Array.Empty<int>(), pooled.resource);
        }

        [Test]
        public void WallstopArrayPoolGetPositiveSizeReturnsArrayWithCorrectLength()
        {
            const int size = 10;
            using PooledResource<int[]> pooled = WallstopArrayPool<int>.Get(size);

            Assert.NotNull(pooled.resource);
            Assert.AreEqual(size, pooled.resource.Length);
        }

        [Test]
        public void WallstopArrayPoolGetSameSizeReusesArrayAfterDispose()
        {
            const int size = 5;
            int[] firstArray;

            using (PooledResource<int[]> pooled = WallstopArrayPool<int>.Get(size))
            {
                firstArray = pooled.resource;
                firstArray[0] = 42;
            }

            using PooledResource<int[]> pooledReused = WallstopArrayPool<int>.Get(size);
            Assert.AreSame(firstArray, pooledReused.resource);
            Assert.AreEqual(0, pooledReused.resource[0]);
        }

        [Test]
        public void WallstopGenericPoolGetReturnsValidPooledResource()
        {
            using PooledResource<List<int>> pooled = WallstopGenericPool<List<int>>.Get();

            Assert.NotNull(pooled.resource);
            Assert.AreEqual(0, pooled.resource.Count);
        }

        [Test]
        public void WallstopGenericPoolGetReusesInstanceAfterDispose()
        {
            List<int> firstList;

            using (PooledResource<List<int>> pooled = WallstopGenericPool<List<int>>.Get())
            {
                firstList = pooled.resource;
                firstList.Add(42);
                firstList.Add(100);
            }

            using PooledResource<List<int>> pooledReused = WallstopGenericPool<List<int>>.Get();
            Assert.AreSame(firstList, pooledReused.resource);
            Assert.AreEqual(0, pooledReused.resource.Count);
        }

        [Test]
        public void WallstopGenericPoolClearActionWorksWithCustomType()
        {
            using PooledResource<HashSet<string>> pooled = WallstopGenericPool<
                HashSet<string>
            >.Get();

            pooled.resource.Add("test1");
            pooled.resource.Add("test2");
            Assert.AreEqual(2, pooled.resource.Count);
        }

        [Test]
        public void PooledResourceDisposeCallsOnDisposeAction()
        {
            bool disposeCalled = false;
            WallstopGenericPool<List<int>>.clearAction ??= list => list.Clear();
            WallstopGenericPool<List<int>>.clearAction += Callback;
            try
            {
                using (PooledResource<List<int>> pooled = WallstopGenericPool<List<int>>.Get())
                {
                    Assert.NotNull(pooled.resource);
                    Assert.IsFalse(disposeCalled);
                }

                Assert.IsTrue(disposeCalled);
            }
            finally
            {
                WallstopGenericPool<List<int>>.clearAction -= Callback;
            }

            return;
            void Callback(List<int> list)
            {
                disposeCalled = true;
            }
        }
    }
}
