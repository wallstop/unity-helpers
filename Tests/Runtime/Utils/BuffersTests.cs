namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using NUnit.Framework;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Random;
    using WallstopStudios.UnityHelpers.Utils;
#if !SINGLE_THREADED
    using System.Collections.Concurrent;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
#endif

    public sealed class BuffersTests
    {
        private IDisposable _waitInstructionScope;
        private readonly WallstopGenericPool<List<int>> _intPool = new(
            () => new List<int>(),
            onRelease: list => list.Clear()
        );

        [SetUp]
        public void SetUp()
        {
            _waitInstructionScope = Buffers.BeginWaitInstructionTestScope();
        }

        [TearDown]
        public void TearDown()
        {
            _waitInstructionScope?.Dispose();
        }

        [Test]
        public void PreWarmCount()
        {
            int retrieved = 0;
            int released = 0;
            using WallstopGenericPool<int> pool = new(
                () => 0,
                preWarmCount: 10,
                onGet: _ => ++retrieved,
                onRelease: _ => ++released
            );
            Assert.AreEqual(10, retrieved);
            Assert.AreEqual(10, released);
            Assert.AreEqual(10, pool.Count);
        }

        [Test]
        public void GenericPoolListTests()
        {
            {
                using PooledResource<List<int>> firstList = _intPool.Get();
                using PooledResource<List<int>> secondList = _intPool.Get();
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
                using PooledResource<List<int>> firstList = _intPool.Get();
                Assert.AreEqual(0, firstList.resource.Count);
            }
        }

        [Test]
        public void ArrayPoolSizeTests()
        {
            for (int i = 0; i < 100; ++i)
            {
                using PooledResource<int[]> resource = WallstopArrayPool<int>.Get(
                    i,
                    out int[] buffer
                );
                Assert.AreEqual(i, buffer.Length);
                for (int j = 0; j < i; ++j)
                {
                    buffer[j] = PRNG.Instance.Next();
                }
            }

            for (int i = 0; i < 100; ++i)
            {
                using PooledResource<int[]> resource = WallstopArrayPool<int>.Get(
                    i,
                    out int[] buffer
                );
                Assert.AreEqual(i, buffer.Length);
                for (int j = 0; j < i; ++j)
                {
                    Assert.AreEqual(0, buffer[j]);
                }
            }
        }

        [Test]
        public void WallstopFastArrayPoolGetNegativeSizeThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                WallstopFastArrayPool<int>.Get(-1, out _)
            );
        }

        [Test]
        public void WallstopFastArrayPoolGetZeroSizeReturnsEmptyArrayWithNoOpDispose()
        {
            using PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(
                0,
                out int[] buffer
            );
            Assert.NotNull(buffer);
            Assert.AreEqual(0, buffer.Length);
            Assert.AreSame(Array.Empty<int>(), buffer);
        }

        [Test]
        public void WallstopFastArrayPoolGetPositiveSizeReturnsArrayWithCorrectLength()
        {
            const int size = 10;
            using PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(
                size,
                out int[] buffer
            );

            Assert.NotNull(buffer);
            Assert.AreEqual(size, buffer.Length);
        }

        [Test]
        public void WallstopFastArrayPoolGetSameSizeReusesArrayAfterDispose()
        {
            const int size = 5;
            int[] firstArray;

            using (
                PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(
                    size,
                    out int[] buffer
                )
            )
            {
                firstArray = buffer;
                firstArray[0] = 42;
            }

            using PooledResource<int[]> pooledReused = WallstopFastArrayPool<int>.Get(
                size,
                out int[] reused
            );
            Assert.AreSame(firstArray, reused);
            Assert.AreEqual(42, reused[0]);
        }

        [Test]
        public void WallstopFastArrayPoolZeroLengthAlwaysSharedInstance()
        {
            using PooledResource<int[]> first = WallstopFastArrayPool<int>.Get(0, out int[] zeroA);
            using PooledResource<int[]> second = WallstopFastArrayPool<int>.Get(0, out int[] zeroB);

            Assert.AreSame(Array.Empty<int>(), zeroA);
            Assert.AreSame(zeroA, zeroB);

            for (int i = 0; i < 32; i++)
            {
                using PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(
                    0,
                    out int[] zeroBuffer
                );
                Assert.AreSame(
                    zeroA,
                    zeroBuffer,
                    $"Zero-length iteration {i} should reuse the shared empty array."
                );
            }
        }

        [Test]
        public void WallstopFastArrayPoolDoesNotCrossContaminateSizes()
        {
            const int smallSize = 16;
            const int largeSize = 64;
            const int iterations = 20;
            HashSet<int> smallHashes = new();
            HashSet<int> largeHashes = new();

            for (int i = 0; i < iterations; i++)
            {
                using PooledResource<int[]> small = WallstopFastArrayPool<int>.Get(
                    smallSize,
                    out int[] smallArray
                );
                using PooledResource<int[]> large = WallstopFastArrayPool<int>.Get(
                    largeSize,
                    out int[] largeArray
                );
                Assert.AreNotSame(smallArray, largeArray);

                smallHashes.Add(RuntimeHelpers.GetHashCode(smallArray));
                largeHashes.Add(RuntimeHelpers.GetHashCode(largeArray));
            }

            for (int i = 0; i < iterations; i++)
            {
                using PooledResource<int[]> small = WallstopFastArrayPool<int>.Get(
                    smallSize,
                    out int[] smallArray
                );
                int hash = RuntimeHelpers.GetHashCode(smallArray);
                Assert.IsTrue(
                    smallHashes.Contains(hash),
                    $"Small-size fetch {i} returned unexpected buffer hash {hash}."
                );
                Assert.IsFalse(
                    largeHashes.Contains(hash),
                    $"Small-size fetch {i} reused buffer previously issued for large size."
                );
            }

            for (int i = 0; i < iterations; i++)
            {
                using PooledResource<int[]> large = WallstopFastArrayPool<int>.Get(
                    largeSize,
                    out int[] largeArray
                );
                int hash = RuntimeHelpers.GetHashCode(largeArray);
                Assert.IsTrue(
                    largeHashes.Contains(hash),
                    $"Large-size fetch {i} returned unexpected buffer hash {hash}."
                );
                Assert.IsFalse(
                    smallHashes.Contains(hash),
                    $"Large-size fetch {i} reused buffer previously issued for small size."
                );
            }
        }

#if SINGLE_THREADED
        [Test]
        public void WallstopFastArrayPoolReleaseHandlesSparseBuckets()
        {
            const int maxSize = 128;

            using (
                PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(
                    maxSize,
                    out int[] maxArray
                )
            )
            {
                Assert.AreEqual(maxSize, maxArray.Length);
            }

            Assert.DoesNotThrow(() =>
            {
                for (int size = 1; size <= maxSize; size += 5)
                {
                    using PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(
                        size,
                        out int[] array
                    );
                    Assert.AreEqual(size, array.Length);
                }
            });

            Assert.DoesNotThrow(() =>
            {
                for (int size = maxSize - 1; size >= 1; size -= 7)
                {
                    using PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(
                        size,
                        out int[] array
                    );
                    Assert.AreEqual(size, array.Length);
                }
            });
        }
#endif

        [Test]
        public void WallstopFastArrayPoolGetDifferentSizesReturnsCorrectArrays()
        {
            int[] sizes = { 1, 5, 10, 100, 1000 };
            List<PooledResource<int[]>> pooledArrays = new();

            try
            {
                foreach (int size in sizes)
                {
                    PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(
                        size,
                        out int[] array
                    );
                    pooledArrays.Add(pooled);
                    Assert.AreEqual(size, array.Length);
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

            using (
                PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(size, out int[] array)
            )
            {
                for (int i = 0; i < size; i++)
                {
                    array[i] = i + 1;
                }
            }

            using PooledResource<int[]> pooledReused = WallstopFastArrayPool<int>.Get(
                size,
                out int[] reused
            );
            for (int i = 0; i < size; i++)
            {
                Assert.AreEqual(i + 1, reused[i]);
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
                    PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(
                        size,
                        out int[] array
                    );
                    pooledArrays.Add(pooled);
                    Assert.AreEqual(size, array.Length);
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

            using PooledResource<int[]> intPooled = WallstopFastArrayPool<int>.Get(
                size,
                out int[] intBuffer
            );
            using PooledResource<long[]> longPooled = WallstopFastArrayPool<long>.Get(
                size,
                out long[] longBuffer
            );
            using PooledResource<float[]> floatPooled = WallstopFastArrayPool<float>.Get(
                size,
                out float[] floatBuffer
            );

            Assert.AreEqual(size, intBuffer.Length);
            Assert.AreEqual(size, longBuffer.Length);
            Assert.AreEqual(size, floatBuffer.Length);
        }

        [Test]
        public void WallstopFastArrayPoolLargeArraysWork()
        {
            const int size = 100000;
            using PooledResource<byte[]> pooled = WallstopFastArrayPool<byte>.Get(
                size,
                out byte[] array
            );

            Assert.AreEqual(size, array.Length);
        }

        [Test]
        public void WallstopFastArrayPoolNestedUsageWorks()
        {
            const int outerSize = 5;
            const int innerSize = 3;

            using PooledResource<int[]> outer = WallstopFastArrayPool<int>.Get(
                outerSize,
                out int[] outerArray
            );
            Assert.AreEqual(outerSize, outerArray.Length);
            Array.Clear(outerArray, 0, outerArray.Length);
            outerArray[0] = 1;

            using (
                PooledResource<int[]> inner = WallstopFastArrayPool<int>.Get(
                    innerSize,
                    out int[] innerArray
                )
            )
            {
                innerArray[0] = 2;
                Assert.AreEqual(innerSize, innerArray.Length);
                Assert.AreEqual(1, outerArray[0]);
                Assert.AreEqual(2, innerArray[0]);
            }

            Assert.AreEqual(outerSize, outerArray.Length);
            Assert.AreEqual(1, outerArray[0]);
        }

        [UnityTest]
        public IEnumerator WallstopFastArrayPoolStressTest()
        {
            const int iterations = 1000;
            const int maxSize = 100;
            IRandom random = new PcgRandom(42);

            for (int i = 0; i < iterations; i++)
            {
                int size = random.Next(1, maxSize);
                using PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(
                    size,
                    out int[] array
                );

                Assert.AreEqual(size, array.Length);

                for (int j = 0; j < Math.Min(10, size); j++)
                {
                    array[j] = random.Next();
                }

                if (i % 100 == 0)
                {
                    yield return null;
                }
            }
        }

#if !SINGLE_THREADED
        private enum ConcurrentScenarioName
        {
            WallstopFastArrayPoolConcurrentAccessDifferentSizes,
            WallstopFastArrayPoolConcurrentAccessSameSize,
            WallstopFastArrayPoolConcurrentAccessMixedSizes,
            WallstopFastArrayPoolConcurrentAccessRapidAllocationDeallocation,
            WallstopFastArrayPoolConcurrentOutOfOrderDispose,
            WallstopArrayPoolClearOnReuse,
            WallstopArrayPoolConcurrentMixedSizesClearCheck,
        }

        [TestCaseSource(nameof(WallstopFastArrayPoolConcurrentScenarioCases))]
        public void WallstopFastArrayPoolConcurrentAccessScenarios(ConcurrentScenario scenario)
        {
            RunConcurrentScenario(scenario);
        }

        [TestCaseSource(nameof(WallstopArrayPoolConcurrentScenarioCases))]
        public void WallstopArrayPoolConcurrentAccessScenarios(ConcurrentScenario scenario)
        {
            RunConcurrentScenario(scenario);
        }

        private static IEnumerable<TestCaseData> WallstopFastArrayPoolConcurrentScenarioCases()
        {
            yield return CreateDifferentSizesScenario();
            yield return CreateSameSizeScenario();
            yield return CreateMixedSizesScenario();
            yield return CreateRapidAllocationScenario();
            yield return CreateOutOfOrderDisposeScenario();
        }

        private static IEnumerable<TestCaseData> WallstopArrayPoolConcurrentScenarioCases()
        {
            yield return CreateArrayPoolClearScenario();
            yield return CreateArrayPoolMixedSizesScenario();
        }

        private static void RunConcurrentScenario(ConcurrentScenario scenario)
        {
            ConcurrentQueue<ScenarioException> exceptions = new();
            Task[] tasks = new Task[scenario.ThreadCount];

            for (int t = 0; t < scenario.ThreadCount; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(async () =>
                {
                    try
                    {
                        Task work = scenario.Work(threadId);
                        if (work == null)
                        {
                            throw new InvalidOperationException(
                                $"Scenario '{scenario.Name}' returned a null task for thread {threadId}."
                            );
                        }

                        await work.ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Enqueue(new ScenarioException(threadId, ex));
                    }
                });
            }

            Task.WaitAll(tasks);

            ScenarioException[] failures = exceptions.ToArray();
            if (failures.Length == 0)
            {
                return;
            }

            StringBuilder builder = new();
            foreach (ScenarioException failure in failures)
            {
                builder.AppendLine(
                    $"Thread {failure.ThreadId}: {failure.Exception.GetType().Name} - {failure.Exception.Message}"
                );

                if (failure.Exception.Data != null && failure.Exception.Data.Count > 0)
                {
                    foreach (DictionaryEntry entry in failure.Exception.Data)
                    {
                        builder.AppendLine($"    {entry.Key}: {entry.Value}");
                    }
                }

                if (!string.IsNullOrEmpty(failure.Exception.StackTrace))
                {
                    builder.AppendLine(failure.Exception.StackTrace);
                }

                builder.AppendLine();
            }

            Assert.Fail(
                $"Scenario '{scenario.Name}' captured {failures.Length} exception(s).{Environment.NewLine}{builder}"
            );
        }

        private static TestCaseData CreateDifferentSizesScenario()
        {
            const int threadCount = 10;
            const int operationsPerThread = 100;

            ConcurrentScenario scenario = new(
                nameof(ConcurrentScenarioName.WallstopFastArrayPoolConcurrentAccessDifferentSizes),
                threadCount,
                async threadId =>
                {
                    PcgRandom random = new(threadId);
                    foreach (int i in Enumerable.Range(0, operationsPerThread).Shuffled(random))
                    {
                        int size = random.Next(1, 50) + threadId;
                        using PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(
                            size,
                            out int[] array
                        );

                        Assert.AreEqual(
                            size,
                            array.Length,
                            $"DifferentSizes thread {threadId} iteration {i} expected length {size}"
                        );

                        for (int j = 0; j < Math.Min(5, size); j++)
                        {
                            array[j] = threadId * 1000 + i * 10 + j;
                        }

                        await Task.Delay(TimeSpan.FromMilliseconds(random.NextDouble()))
                            .ConfigureAwait(false);
                    }
                }
            );

            return new TestCaseData(scenario).SetName(scenario.Name);
        }

        private static TestCaseData CreateSameSizeScenario()
        {
            const int threadCount = 8;
            const int operationsPerThread = 200;
            const int arraySize = 25;

            ConcurrentScenario scenario = new(
                nameof(ConcurrentScenarioName.WallstopFastArrayPoolConcurrentAccessSameSize),
                threadCount,
                threadId =>
                {
                    PcgRandom random = new(threadId);
                    foreach (int i in Enumerable.Range(0, operationsPerThread).Shuffled(random))
                    {
                        using PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(
                            arraySize,
                            out int[] array
                        );

                        Array.Clear(array, 0, array.Length);
                        Assert.AreEqual(
                            arraySize,
                            array.Length,
                            $"SameSize thread {threadId} iteration {i} expected length {arraySize}"
                        );

                        for (int j = 0; j < arraySize; j++)
                        {
                            Assert.AreEqual(
                                0,
                                array[j],
                                $"SameSize thread {threadId} iteration {i} index {j} expected zero"
                            );
                            array[j] = threadId * 1000 + i;
                        }
                    }

                    return Task.CompletedTask;
                }
            );

            return new TestCaseData(scenario).SetName(scenario.Name);
        }

        private static TestCaseData CreateMixedSizesScenario()
        {
            const int threadCount = 6;
            const int operationsPerThread = 150;
            int[] sizes = { 1, 5, 10, 20, 50, 100 };

            ConcurrentScenario scenario = new(
                nameof(ConcurrentScenarioName.WallstopFastArrayPoolConcurrentAccessMixedSizes),
                threadCount,
                async threadId =>
                {
                    PcgRandom random = new(threadId + 100);
                    foreach (int i in Enumerable.Range(0, operationsPerThread).Shuffled(random))
                    {
                        int size = random.NextOf(sizes);
                        using PooledResource<long[]> pooled = WallstopFastArrayPool<long>.Get(
                            size,
                            out long[] array
                        );
                        Array.Clear(array, 0, array.Length);

                        Assert.AreEqual(
                            size,
                            array.Length,
                            $"MixedSizes thread {threadId} iteration {i} expected length {size}"
                        );

                        for (int j = 0; j < size; j++)
                        {
                            Assert.AreEqual(
                                0,
                                array[j],
                                $"MixedSizes thread {threadId} iteration {i} index {j} expected zero"
                            );
                            long packed = ((long)threadId << 32) | ((long)i << 16) | (uint)j;
                            array[j] = packed;
                        }

                        if (i % 50 == 0)
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(random.NextDouble()))
                                .ConfigureAwait(false);
                        }
                    }
                }
            );

            return new TestCaseData(scenario).SetName(scenario.Name);
        }

        private static TestCaseData CreateRapidAllocationScenario()
        {
            const int threadCount = 12;
            const int operationsPerThread = 500;

            ConcurrentScenario scenario = new(
                nameof(
                    ConcurrentScenarioName.WallstopFastArrayPoolConcurrentAccessRapidAllocationDeallocation
                ),
                threadCount,
                threadId =>
                {
                    PcgRandom random = new(threadId + 100);
                    foreach (int i in Enumerable.Range(0, operationsPerThread).Shuffled(random))
                    {
                        int size = random.Next(1, 30);
                        using PooledResource<byte[]> pooled = WallstopFastArrayPool<byte>.Get(
                            size,
                            out byte[] array
                        );

                        Assert.AreEqual(
                            size,
                            array.Length,
                            $"RapidAllocation thread {threadId} iteration {i} expected length {size}"
                        );

                        for (int j = 0; j < size; j++)
                        {
                            array[j] = (byte)(threadId + i + j);
                        }
                    }

                    return Task.CompletedTask;
                }
            );

            return new TestCaseData(scenario).SetName(scenario.Name);
        }

        private static TestCaseData CreateOutOfOrderDisposeScenario()
        {
            const int threadCount = 4;
            const int allocationsPerThread = 64;

            ConcurrentScenario scenario = new(
                nameof(ConcurrentScenarioName.WallstopFastArrayPoolConcurrentOutOfOrderDispose),
                threadCount,
                threadId =>
                {
                    int baseSize = 12 + threadId * 24;
                    int[] threadSizes = { baseSize, baseSize + 3, baseSize + 6 };
                    List<PooledResource<int[]>> rentals = new();

                    for (int i = 0; i < allocationsPerThread; i++)
                    {
                        int size = threadSizes[i % threadSizes.Length];
                        PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(
                            size,
                            out int[] array
                        );
                        rentals.Add(pooled);
                        array[0] = threadId;
                        array[size - 1] = threadId;
                    }

                    Dictionary<int, Queue<int[]>> expectedOrder = new();

                    for (int i = rentals.Count - 1; i >= 0; i--)
                    {
                        PooledResource<int[]> pooled = rentals[i];
                        int size = pooled.resource.Length;
                        if (!expectedOrder.TryGetValue(size, out Queue<int[]> queue))
                        {
                            queue = new Queue<int[]>();
                            expectedOrder[size] = queue;
                        }

                        queue.Enqueue(pooled.resource);
                        pooled.Dispose();
                    }

                    foreach (KeyValuePair<int, Queue<int[]>> pair in expectedOrder)
                    {
                        while (pair.Value.Count > 0)
                        {
                            int[] expected = pair.Value.Dequeue();
                            using PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(
                                pair.Key,
                                out int[] array
                            );

                            Assert.AreSame(
                                expected,
                                array,
                                $"OutOfOrderDispose thread {threadId} expected LIFO for size {pair.Key}"
                            );
                        }
                    }

                    return Task.CompletedTask;
                }
            );

            return new TestCaseData(scenario).SetName(scenario.Name);
        }

        private static TestCaseData CreateArrayPoolClearScenario()
        {
            const int threadCount = 6;
            const int operationsPerThread = 120;
            const int arraySize = 32;

            ConcurrentScenario scenario = new(
                nameof(ConcurrentScenarioName.WallstopArrayPoolClearOnReuse),
                threadCount,
                threadId =>
                {
                    PcgRandom random = new(threadId + 400);
                    foreach (int i in Enumerable.Range(0, operationsPerThread).Shuffled(random))
                    {
                        using PooledResource<int[]> pooled = WallstopArrayPool<int>.Get(
                            arraySize,
                            out int[] arrayBuffer
                        );
                        Assert.AreEqual(
                            arraySize,
                            arrayBuffer.Length,
                            $"ArrayPoolClear thread {threadId} iteration {i} expected length {arraySize}"
                        );

                        for (int j = 0; j < arraySize; j++)
                        {
                            Assert.AreEqual(
                                0,
                                arrayBuffer[j],
                                $"ArrayPoolClear thread {threadId} iteration {i} index {j} expected zero"
                            );
                            arrayBuffer[j] = threadId + i + j;
                        }
                    }

                    return Task.CompletedTask;
                }
            );

            return new TestCaseData(scenario).SetName(scenario.Name);
        }

        private static TestCaseData CreateArrayPoolMixedSizesScenario()
        {
            const int threadCount = 5;
            const int operationsPerThread = 80;
            int[] sizes = { 2, 7, 13, 21, 34 };

            ConcurrentScenario scenario = new(
                nameof(ConcurrentScenarioName.WallstopArrayPoolConcurrentMixedSizesClearCheck),
                threadCount,
                async threadId =>
                {
                    PcgRandom random = new(threadId + 700);
                    foreach (int i in Enumerable.Range(0, operationsPerThread).Shuffled(random))
                    {
                        int size = random.NextOf(sizes);
                        using PooledResource<byte[]> pooled = WallstopArrayPool<byte>.Get(
                            size,
                            out byte[] byteBuffer
                        );

                        Assert.AreEqual(
                            size,
                            byteBuffer.Length,
                            $"ArrayPoolMixedSizes thread {threadId} iteration {i} expected length {size}"
                        );

                        for (int j = 0; j < size; j++)
                        {
                            Assert.AreEqual(
                                0,
                                byteBuffer[j],
                                $"ArrayPoolMixedSizes thread {threadId} iteration {i} index {j} expected zero"
                            );
                            byteBuffer[j] = (byte)(threadId + i + j);
                        }

                        if (i % 25 == 0)
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(random.NextDouble() * 2.0))
                                .ConfigureAwait(false);
                        }
                    }
                }
            );

            return new TestCaseData(scenario).SetName(scenario.Name);
        }

        public sealed class ConcurrentScenario
        {
            public ConcurrentScenario(string name, int threadCount, Func<int, Task> work)
            {
                Name = name;
                ThreadCount = threadCount;
                Work = work;
            }

            public string Name { get; }
            public int ThreadCount { get; }
            public Func<int, Task> Work { get; }

            public override string ToString()
            {
                return Name;
            }
        }

        private sealed class ScenarioException
        {
            public ScenarioException(int threadId, Exception exception)
            {
                ThreadId = threadId;
                Exception = exception;
            }

            public int ThreadId { get; }
            public Exception Exception { get; }
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
                                size,
                                out int[] array
                            );
                            Assert.AreEqual(size, array.Length);

                            array[0] = threadId;
                            array[size - 1] = threadId;
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
                                size,
                                out float[] array
                            );

                            Assert.AreEqual(size, array.Length);

                            for (int j = 0; j < Math.Min(10, size); j++)
                            {
                                array[j] = threadId * 1000.0f + i + j * 0.1f;
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
            using PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(
                veryLargeSize,
                out int[] array
            );

            Assert.AreEqual(veryLargeSize, array.Length);
            Assert.AreEqual(0, array[0]);
            Assert.AreEqual(0, array[veryLargeSize - 1]);
        }

        [Test]
        public void WallstopFastArrayPoolEdgeCaseSizeOne()
        {
            using PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(1, out int[] array);
            Array.Clear(array, 0, array.Length);
            Assert.AreEqual(1, array.Length);
            Assert.AreEqual(0, array[0]);

            array[0] = 42;
            Assert.AreEqual(42, array[0]);
        }

        [Test]
        public void WallstopFastArrayPoolPoolingBehaviorLifo()
        {
            const int size = 15;
            int[][] arrays = new int[3][];
            {
                using PooledResource<int[]> pooled1 = WallstopFastArrayPool<int>.Get(
                    size,
                    out int[] arr1
                );
                arrays[0] = arr1;
                using PooledResource<int[]> pooled2 = WallstopFastArrayPool<int>.Get(
                    size,
                    out int[] arr2
                );
                arrays[1] = arr2;
                using PooledResource<int[]> pooled3 = WallstopFastArrayPool<int>.Get(
                    size,
                    out int[] arr3
                );
                arrays[2] = arr3;
            }

            using PooledResource<int[]> pooledReuse1 = WallstopFastArrayPool<int>.Get(
                size,
                out int[] reuse1
            );
            Assert.AreSame(arrays[0], reuse1);

            using PooledResource<int[]> pooledReuse2 = WallstopFastArrayPool<int>.Get(
                size,
                out int[] reuse2
            );
            Assert.AreSame(arrays[1], reuse2);

            using PooledResource<int[]> pooledReuse3 = WallstopFastArrayPool<int>.Get(
                size,
                out int[] reuse3
            );
            Assert.AreSame(arrays[2], reuse3);
        }

        [Test]
        public void WallstopArrayPoolGetNegativeSizeThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => WallstopArrayPool<int>.Get(-1, out _));
        }

        [Test]
        public void WallstopArrayPoolGetZeroSizeReturnsEmptyArrayWithNoOpDispose()
        {
            using PooledResource<int[]> pooled = WallstopArrayPool<int>.Get(0, out int[] buffer);
            Assert.NotNull(buffer);
            Assert.AreEqual(0, buffer.Length);
            Assert.AreSame(Array.Empty<int>(), buffer);
        }

        [Test]
        public void WallstopArrayPoolGetPositiveSizeReturnsArrayWithCorrectLength()
        {
            const int size = 10;
            using PooledResource<int[]> pooled = WallstopArrayPool<int>.Get(size, out int[] buffer);

            Assert.NotNull(buffer);
            Assert.AreEqual(size, buffer.Length);
        }

        [Test]
        public void WallstopArrayPoolGetSameSizeReusesArrayAfterDispose()
        {
            const int size = 5;
            int[] firstArray;

            using (
                PooledResource<int[]> pooled = WallstopArrayPool<int>.Get(size, out int[] buffer)
            )
            {
                firstArray = buffer;
                firstArray[0] = 42;
            }

            using PooledResource<int[]> pooledReused = WallstopArrayPool<int>.Get(
                size,
                out int[] reused
            );
            Assert.AreSame(firstArray, reused);
            Assert.AreEqual(0, reused[0]);
        }

        [Test]
        public void WallstopGenericPoolGetReturnsValidPooledResource()
        {
            using PooledResource<List<int>> pooled = _intPool.Get();

            Assert.NotNull(pooled.resource);
            Assert.AreEqual(0, pooled.resource.Count);
        }

        [Test]
        public void WallstopGenericPoolGetReusesInstanceAfterDispose()
        {
            List<int> firstList;

            using (PooledResource<List<int>> pooled = _intPool.Get())
            {
                firstList = pooled.resource;
                firstList.Add(42);
                firstList.Add(100);
            }

            using PooledResource<List<int>> pooledReused = _intPool.Get();
            Assert.AreSame(firstList, pooledReused.resource);
            Assert.AreEqual(0, pooledReused.resource.Count);
        }

        [Test]
        public void WallstopGenericPoolClearActionWorksWithCustomType()
        {
            using WallstopGenericPool<HashSet<string>> pool = new(() => new HashSet<string>());
            using PooledResource<HashSet<string>> pooled = pool.Get();

            pooled.resource.Add("test1");
            pooled.resource.Add("test2");
            Assert.AreEqual(2, pooled.resource.Count);
        }

        [Test]
        public void PooledResourceDisposeCallsOnDisposeAction()
        {
            bool clearCalled = false;
            bool disposeCalled = false;
            {
                using WallstopGenericPool<List<int>> pool = new(
                    () => new List<int>(),
                    onRelease: list =>
                    {
                        list.Clear();
                        clearCalled = true;
                    },
                    onDisposal: _ => disposeCalled = true
                );

                using (PooledResource<List<int>> pooled = pool.Get())
                {
                    Assert.NotNull(pooled.resource);
                    Assert.IsFalse(clearCalled);
                    Assert.IsFalse(disposeCalled);
                }

                Assert.IsTrue(clearCalled);
            }

            Assert.IsTrue(disposeCalled);
        }

        [Test]
        public void BuffersGetWaitForSecondsReturnsCachedInstance()
        {
            const float seconds = 1.5f;
            UnityEngine.WaitForSeconds first = Buffers.GetWaitForSeconds(seconds);
            UnityEngine.WaitForSeconds second = Buffers.GetWaitForSeconds(seconds);

            Assert.AreSame(first, second);
        }

        [Test]
        public void BuffersGetWaitForSecondsRealTimeReturnsCachedInstance()
        {
            const float seconds = 2.5f;
            UnityEngine.WaitForSecondsRealtime first = Buffers.GetWaitForSecondsRealTime(seconds);
            UnityEngine.WaitForSecondsRealtime second = Buffers.GetWaitForSecondsRealTime(seconds);

            Assert.AreSame(first, second);
        }

        [Test]
        public void BuffersWaitForFixedUpdateIsSingleton()
        {
            Assert.NotNull(Buffers.WaitForFixedUpdate);
            Assert.AreSame(Buffers.WaitForFixedUpdate, Buffers.WaitForFixedUpdate);
        }

        [Test]
        public void BuffersWaitForEndOfFrameIsSingleton()
        {
            Assert.NotNull(Buffers.WaitForEndOfFrame);
            Assert.AreSame(Buffers.WaitForEndOfFrame, Buffers.WaitForEndOfFrame);
        }

        [Test]
        public void BuffersStringBuilderPoolWorks()
        {
            using PooledResource<System.Text.StringBuilder> pooled = Buffers.StringBuilder.Get();

            Assert.NotNull(pooled.resource);
            pooled.resource.Append("test");
            Assert.AreEqual("test", pooled.resource.ToString());
        }

        [Test]
        public void BuffersStringBuilderPoolClearsOnRelease()
        {
            using (PooledResource<System.Text.StringBuilder> pooled = Buffers.StringBuilder.Get())
            {
                pooled.resource.Append("test content");
            }

            using PooledResource<System.Text.StringBuilder> pooledReused =
                Buffers.StringBuilder.Get();
            Assert.AreEqual(0, pooledReused.resource.Length);
        }

        [Test]
        public void BuffersGenericListPoolWorks()
        {
            using PooledResource<List<int>> pooled = Buffers<int>.List.Get(out List<int> list);

            Assert.NotNull(list);
            list.Add(42);
            Assert.AreEqual(1, list.Count);
        }

        [Test]
        public void BuffersGenericHashSetPoolWorks()
        {
            using PooledResource<HashSet<string>> pooled = Buffers<string>.HashSet.Get(
                out HashSet<string> set
            );

            Assert.NotNull(set);
            set.Add("test");
            Assert.AreEqual(1, set.Count);
        }

        [Test]
        public void BuffersGenericQueuePoolWorks()
        {
            using PooledResource<Queue<int>> pooled = Buffers<int>.Queue.Get(out Queue<int> queue);

            Assert.NotNull(queue);
            queue.Enqueue(1);
            queue.Enqueue(2);
            Assert.AreEqual(2, queue.Count);
            Assert.AreEqual(1, queue.Dequeue());
        }

        [Test]
        public void BuffersGenericStackPoolWorks()
        {
            using PooledResource<Stack<int>> pooled = Buffers<int>.Stack.Get(out Stack<int> stack);

            Assert.NotNull(stack);
            stack.Push(1);
            stack.Push(2);
            Assert.AreEqual(2, stack.Count);
            Assert.AreEqual(2, stack.Pop());
        }

        [Test]
        public void SetBuffersSortedSetPoolWorks()
        {
            using PooledResource<SortedSet<int>> pooled = SetBuffers<int>.SortedSet.Get(
                out SortedSet<int> sortedSet
            );

            Assert.NotNull(sortedSet);
            sortedSet.Add(3);
            sortedSet.Add(1);
            sortedSet.Add(2);
            Assert.AreEqual(3, sortedSet.Count);
            Assert.AreEqual(1, sortedSet.Min);
        }

        [Test]
        public void SetBuffersGetSortedSetPoolWithCustomComparer()
        {
            IComparer<int> reverseComparer = Comparer<int>.Create((a, b) => b.CompareTo(a));
            WallstopGenericPool<SortedSet<int>> pool = SetBuffers<int>.GetSortedSetPool(
                reverseComparer
            );

            using PooledResource<SortedSet<int>> pooled = pool.Get();
            pooled.resource.Add(1);
            pooled.resource.Add(2);
            pooled.resource.Add(3);

            Assert.AreEqual(3, pooled.resource.Min);
        }

        [Test]
        public void SetBuffersGetHashSetPoolWithCustomComparer()
        {
            IEqualityComparer<string> caseInsensitiveComparer = StringComparer.OrdinalIgnoreCase;
            WallstopGenericPool<HashSet<string>> pool = SetBuffers<string>.GetHashSetPool(
                caseInsensitiveComparer
            );

            using PooledResource<HashSet<string>> pooled = pool.Get();
            pooled.resource.Add("Test");
            Assert.IsFalse(pooled.resource.Add("test"));
            Assert.AreEqual(1, pooled.resource.Count);
        }

        [Test]
        public void SetBuffersGetSortedSetPoolThrowsOnNullComparer()
        {
            Assert.Throws<ArgumentNullException>(() => SetBuffers<int>.GetSortedSetPool(null));
        }

        [Test]
        public void SetBuffersGetHashSetPoolThrowsOnNullComparer()
        {
            Assert.Throws<ArgumentNullException>(() => SetBuffers<int>.GetHashSetPool(null));
        }

        [Test]
        public void SetBuffersHasHashSetPoolWorks()
        {
            IEqualityComparer<int> comparer = EqualityComparer<int>.Default;
            Assert.IsFalse(SetBuffers<int>.HasHashSetPool(comparer));

            SetBuffers<int>.GetHashSetPool(comparer);
            Assert.IsTrue(SetBuffers<int>.HasHashSetPool(comparer));
        }

        [Test]
        public void SetBuffersHasSortedSetPoolWorks()
        {
            IComparer<int> comparer = Comparer<int>.Default;
            Assert.IsFalse(SetBuffers<int>.HasSortedSetPool(comparer));

            SetBuffers<int>.GetSortedSetPool(comparer);
            Assert.IsTrue(SetBuffers<int>.HasSortedSetPool(comparer));
        }

        [Test]
        public void SetBuffersDestroyHashSetPoolWorks()
        {
            IEqualityComparer<int> comparer = EqualityComparer<int>.Default;
            SetBuffers<int>.GetHashSetPool(comparer);
            Assert.IsTrue(SetBuffers<int>.HasHashSetPool(comparer));

            Assert.IsTrue(SetBuffers<int>.DestroyHashSetPool(comparer));
            Assert.IsFalse(SetBuffers<int>.HasHashSetPool(comparer));
        }

        [Test]
        public void SetBuffersDestroySortedSetPoolWorks()
        {
            IComparer<int> comparer = Comparer<int>.Default;
            SetBuffers<int>.GetSortedSetPool(comparer);
            Assert.IsTrue(SetBuffers<int>.HasSortedSetPool(comparer));

            Assert.IsTrue(SetBuffers<int>.DestroySortedSetPool(comparer));
            Assert.IsFalse(SetBuffers<int>.HasSortedSetPool(comparer));
        }

        [Test]
        public void LinkedListBufferWorks()
        {
            using PooledResource<LinkedList<int>> pooled = LinkedListBuffer<int>.LinkedList.Get();

            Assert.NotNull(pooled.resource);
            pooled.resource.AddLast(1);
            pooled.resource.AddLast(2);
            Assert.AreEqual(2, pooled.resource.Count);
        }

        [Test]
        public void DictionaryBufferDictionaryPoolWorks()
        {
            using PooledResource<Dictionary<string, int>> pooled = DictionaryBuffer<
                string,
                int
            >.Dictionary.Get();

            Assert.NotNull(pooled.resource);
            pooled.resource["key"] = 42;
            Assert.AreEqual(1, pooled.resource.Count);
            Assert.AreEqual(42, pooled.resource["key"]);
        }

        [Test]
        public void DictionaryBufferSortedDictionaryPoolWorks()
        {
            using PooledResource<SortedDictionary<int, string>> pooled = DictionaryBuffer<
                int,
                string
            >.SortedDictionary.Get();

            Assert.NotNull(pooled.resource);
            pooled.resource[3] = "three";
            pooled.resource[1] = "one";
            pooled.resource[2] = "two";
            Assert.AreEqual(3, pooled.resource.Count);
            Assert.AreEqual("one", pooled.resource.First().Value);
        }

        [Test]
        public void DictionaryBufferGetDictionaryPoolWithCustomComparer()
        {
            IEqualityComparer<string> caseInsensitiveComparer = StringComparer.OrdinalIgnoreCase;
            WallstopGenericPool<Dictionary<string, int>> pool = DictionaryBuffer<
                string,
                int
            >.GetDictionaryPool(caseInsensitiveComparer);

            using PooledResource<Dictionary<string, int>> pooled = pool.Get();
            pooled.resource["Test"] = 1;
            pooled.resource["test"] = 2;
            Assert.AreEqual(1, pooled.resource.Count);
            Assert.AreEqual(2, pooled.resource["TEST"]);
        }

        [Test]
        public void DictionaryBufferGetSortedDictionaryPoolWithCustomComparer()
        {
            IComparer<int> reverseComparer = Comparer<int>.Create((a, b) => b.CompareTo(a));
            WallstopGenericPool<SortedDictionary<int, string>> pool = DictionaryBuffer<
                int,
                string
            >.GetSortedDictionaryPool(reverseComparer);

            using PooledResource<SortedDictionary<int, string>> pooled = pool.Get();
            pooled.resource[1] = "one";
            pooled.resource[2] = "two";
            pooled.resource[3] = "three";

            Assert.AreEqual("three", pooled.resource.First().Value);
        }

        [Test]
        public void DictionaryBufferHasDictionaryPoolWorks()
        {
            IEqualityComparer<string> comparer = EqualityComparer<string>.Default;
            Assert.IsFalse(DictionaryBuffer<string, int>.HasDictionaryPool(comparer));

            DictionaryBuffer<string, int>.GetDictionaryPool(comparer);
            Assert.IsTrue(DictionaryBuffer<string, int>.HasDictionaryPool(comparer));
        }

        [Test]
        public void DictionaryBufferHasSortedDictionaryPoolWorks()
        {
            IComparer<int> comparer = Comparer<int>.Default;
            Assert.IsFalse(DictionaryBuffer<int, string>.HasSortedDictionaryPool(comparer));

            DictionaryBuffer<int, string>.GetSortedDictionaryPool(comparer);
            Assert.IsTrue(DictionaryBuffer<int, string>.HasSortedDictionaryPool(comparer));
        }

        [Test]
        public void DictionaryBufferDestroyDictionaryPoolWorks()
        {
            IEqualityComparer<string> comparer = EqualityComparer<string>.Default;
            DictionaryBuffer<string, int>.GetDictionaryPool(comparer);
            Assert.IsTrue(DictionaryBuffer<string, int>.HasDictionaryPool(comparer));

            Assert.IsTrue(DictionaryBuffer<string, int>.DestroyDictionaryPool(comparer));
            Assert.IsFalse(DictionaryBuffer<string, int>.HasDictionaryPool(comparer));
        }

        [Test]
        public void DictionaryBufferDestroySortedDictionaryPoolWorks()
        {
            IComparer<int> comparer = Comparer<int>.Default;
            DictionaryBuffer<int, string>.GetSortedDictionaryPool(comparer);
            Assert.IsTrue(DictionaryBuffer<int, string>.HasSortedDictionaryPool(comparer));

            Assert.IsTrue(DictionaryBuffer<int, string>.DestroySortedDictionaryPool(comparer));
            Assert.IsFalse(DictionaryBuffer<int, string>.HasSortedDictionaryPool(comparer));
        }

        [Test]
        public void WallstopGenericPoolProducerNullThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new WallstopGenericPool<int>(null));
        }

        [Test]
        public void WallstopGenericPoolOnGetIsCalledWhenRetrieving()
        {
            bool getCalled = false;
            using WallstopGenericPool<int> pool = new(() => 42, onGet: _ => getCalled = true);

            using PooledResource<int> pooled = pool.Get();
            Assert.IsTrue(getCalled);
        }

        [Test]
        public void WallstopGenericPoolMultipleGetReturnsDifferentInstancesWhenEmpty()
        {
            using WallstopGenericPool<List<int>> pool = new(() => new List<int>());

            List<int> first;
            List<int> second;

            using (PooledResource<List<int>> pooled = pool.Get(out first)) { }

            using (PooledResource<List<int>> pooled = pool.Get(out second)) { }

            Assert.AreSame(first, second);
        }

        [Test]
        public void PooledResourceDefaultIsNotInitialized()
        {
            PooledResource<int> defaultResource = default;
            defaultResource.Dispose();
        }

        [Test]
        public void WallstopArrayPoolArraysAreClearedOnReturn()
        {
            const int size = 10;
            using (PooledResource<int[]> pooled = WallstopArrayPool<int>.Get(size, out int[] array))
            {
                for (int i = 0; i < size; i++)
                {
                    array[i] = i + 1;
                }
            }

            using PooledResource<int[]> pooledReused = WallstopArrayPool<int>.Get(
                size,
                out int[] reused
            );
            for (int i = 0; i < size; i++)
            {
                Assert.AreEqual(0, reused[i]);
            }
        }

        [Test]
        public void WallstopArrayPoolDifferentSizesReturnDifferentArrays()
        {
            using PooledResource<int[]> pooled5 = WallstopArrayPool<int>.Get(5, out int[] a5);
            using PooledResource<int[]> pooled10 = WallstopArrayPool<int>.Get(10, out int[] a10);

            Assert.AreNotSame(a5, a10);
            Assert.AreEqual(5, a5.Length);
            Assert.AreEqual(10, a10.Length);
        }

        [Test]
        public void WallstopFastArrayPoolMultipleGetsReturnDistinctArrays()
        {
            const int size = 7;
            List<int[]> arrays = new();

            try
            {
                for (int i = 0; i < 3; i++)
                {
                    PooledResource<int[]> pooled = WallstopFastArrayPool<int>.Get(
                        size,
                        out int[] array
                    );
                    arrays.Add(array);
                }

                Assert.AreEqual(3, arrays.Distinct().Count());
            }
            finally
            {
                foreach (int[] array in arrays)
                {
                    WallstopFastArrayPool<int>.Get(0, out _).Dispose();
                }
            }
        }
    }
}
