namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Extension;

    public sealed class AsyncOperationExtensionsTests
    {
        [Test]
        public void WithContinuationOnValueTaskExecutesAction()
        {
            bool invoked = false;
            new ValueTask()
                .WithContinuation(() => invoked = true)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
            Assert.IsTrue(invoked);
        }

        [Test]
        public void WithContinuationOnValueTaskWithNullActionDoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                new ValueTask()
                    .WithContinuation(null)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            });
        }

        [Test]
        public void WithContinuationOnValueTaskWithResultTransformsValue()
        {
            ValueTask<int> valueTask = new(5);
            int result = valueTask
                .WithContinuation(v => v * 2)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
            Assert.AreEqual(10, result);
        }

        [Test]
        public void WithContinuationOnValueTaskWithResultPassesValueToAction()
        {
            ValueTask<int> valueTask = new(7);
            int observed = 0;
            valueTask
                .WithContinuation(v => observed = v)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
            Assert.AreEqual(7, observed);
        }

        [Test]
        public void WithContinuationOnValueTaskWithResultAndNullActionDoesNotThrow()
        {
            ValueTask<int> valueTask = new(42);
            Assert.DoesNotThrow(() =>
            {
                valueTask
                    .WithContinuation((Action<int>)null)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            });
        }

        [UnityTest]
        public IEnumerator AsTaskCompletesWhenOperationIsDone()
        {
            return AsTaskCompletesWhenOperationIsDoneAsync().AsCoroutine();
        }

        [UnityTest]
        public IEnumerator AsTaskReturnsImmediatelyWhenAlreadyDone()
        {
            return AsTaskReturnsImmediatelyWhenAlreadyDoneAsync().AsCoroutine();
        }

        [UnityTest]
        public IEnumerator AsValueTaskCompletesWhenOperationIsDone()
        {
            return AsValueTaskCompletesWhenOperationIsDoneAsync().AsCoroutine();
        }

        [UnityTest]
        public IEnumerator AsValueTaskReturnsImmediatelyWhenAlreadyDone()
        {
            return AsValueTaskReturnsImmediatelyWhenAlreadyDoneAsync().AsCoroutine();
        }

#if !UNITY_2023_1_OR_NEWER
        [Test]
        public void GetAwaiterThrowsOnNullOperation()
        {
            AsyncOperation nullOp = null;
            Assert.Throws<ArgumentNullException>(() =>
            {
                nullOp.GetAwaiter();
            });
        }

        [UnityTest]
        public IEnumerator GetAwaiterReturnsValidAwaiter()
        {
            return GetAwaiterReturnsValidAwaiterAsync().AsCoroutine();
        }

        private static async Task GetAwaiterReturnsValidAwaiterAsync()
        {
            AsyncOperation operation = CreateAsyncOperation();
            AsyncOperationExtensions.AsyncOperationAwaiter awaiter = operation.GetAwaiter();
            Assert.IsNotNull(awaiter);

            // Wait for completion
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            Assert.IsTrue(awaiter.IsCompleted);
        }
#endif

        [UnityTest]
        public IEnumerator AsyncOperationAwaiterIsCompletedReturnsTrueWhenDone()
        {
            return AsyncOperationAwaiterIsCompletedReturnsTrueWhenDoneAsync().AsCoroutine();
        }

        [Test]
        public void AsyncOperationAwaiterConstructorThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new AsyncOperationExtensions.AsyncOperationAwaiter(null);
            });
        }

        [UnityTest]
        public IEnumerator AsyncOperationAwaiterOnCompletedInvokesContinuation()
        {
            return AsyncOperationAwaiterOnCompletedInvokesContinuationAsync().AsCoroutine();
        }

        [Test]
        public void AsyncOperationAwaiterGetResultDoesNotThrow()
        {
            AsyncOperation operation = CreateCompletedAsyncOperation();
            AsyncOperationExtensions.AsyncOperationAwaiter awaiter = new(operation);

            Assert.DoesNotThrow(() => awaiter.GetResult());
        }

        // Tests for Task.AsCoroutine()
        [UnityTest]
        public IEnumerator TaskAsCoroutineCompletesSuccessfully()
        {
            bool completed = false;
            Task task = CreateDelayedTask(() => completed = true);

            yield return task.AsCoroutine();

            Assert.IsTrue(completed);
        }

        [UnityTest]
        public IEnumerator TaskAsCoroutineHandlesCompletedTask()
        {
            Task task = Task.CompletedTask;
            yield return task.AsCoroutine();
            Assert.Pass();
        }

        [UnityTest]
        public IEnumerator TaskAsCoroutineThrowsOnFaultedTask()
        {
            Task task = CreateFaultedTask();

            bool exceptionThrown = false;
            IEnumerator coroutine = task.AsCoroutine();

            while (true)
            {
                try
                {
                    if (!coroutine.MoveNext())
                    {
                        break;
                    }
                }
                catch (InvalidOperationException)
                {
                    exceptionThrown = true;
                    break;
                }
                catch (AggregateException e)
                {
                    exceptionThrown = e.Flatten()
                        .InnerExceptions.Any(inner => inner is InvalidOperationException);
                    break;
                }
                yield return null;
            }

            Assert.IsTrue(exceptionThrown);
        }

        // Tests for Task<T>.AsCoroutine()
        [UnityTest]
        public IEnumerator TaskWithResultAsCoroutineCompletesSuccessfully()
        {
            Task<int> task = CreateDelayedTask(() => 42);

            int result = 0;
            yield return task.AsCoroutine(r => result = r);

            Assert.AreEqual(42, result);
        }

        [UnityTest]
        public IEnumerator TaskWithResultAsCoroutineWorksWithoutCallback()
        {
            Task<string> task = CreateDelayedTask(() => "test");

            yield return task.AsCoroutine();

            Assert.AreEqual("test", task.Result);
        }

        [UnityTest]
        public IEnumerator TaskWithResultAsCoroutineHandlesCompletedTask()
        {
            Task<int> task = Task.FromResult(999);
            int result = 0;
            yield return task.AsCoroutine(r => result = r);
            Assert.AreEqual(999, result);
        }

        [UnityTest]
        public IEnumerator TaskWithResultAsCoroutineThrowsOnFaultedTask()
        {
            Task<int> task = CreateFaultedTask<int>();

            bool exceptionThrown = false;
            IEnumerator coroutine = task.AsCoroutine();

            while (true)
            {
                try
                {
                    if (!coroutine.MoveNext())
                    {
                        break;
                    }
                }
                catch (InvalidOperationException)
                {
                    exceptionThrown = true;
                    break;
                }
                catch (AggregateException e)
                {
                    exceptionThrown = e.Flatten()
                        .InnerExceptions.Any(inner => inner is InvalidOperationException);
                    break;
                }
                yield return null;
            }

            Assert.IsTrue(exceptionThrown);
        }

        // Tests for ValueTask.AsCoroutine()
        [UnityTest]
        public IEnumerator ValueTaskAsCoroutineCompletesSuccessfully()
        {
            bool completed = false;
            ValueTask task = new(CreateDelayedTask(() => completed = true));

            yield return task.AsCoroutine();

            Assert.IsTrue(completed);
        }

        [UnityTest]
        public IEnumerator ValueTaskAsCoroutineCompletesImmediatelyWhenAlreadyDone()
        {
            ValueTask task = new();
            yield return task.AsCoroutine();
            // If we get here without hanging, the test passes
            Assert.Pass();
        }

        [UnityTest]
        public IEnumerator ValueTaskAsCoroutineThrowsOnFaultedTask()
        {
            ValueTask task = new(CreateFaultedTask());

            bool exceptionThrown = false;
            IEnumerator coroutine = task.AsCoroutine();

            while (true)
            {
                try
                {
                    if (!coroutine.MoveNext())
                    {
                        break;
                    }
                }
                catch (InvalidOperationException)
                {
                    exceptionThrown = true;
                    break;
                }
                catch (AggregateException e)
                {
                    exceptionThrown = e.Flatten()
                        .InnerExceptions.Any(inner => inner is InvalidOperationException);
                    break;
                }
                yield return null;
            }

            Assert.IsTrue(exceptionThrown);
        }

        // Tests for ValueTask<T>.AsCoroutine()
        [UnityTest]
        public IEnumerator ValueTaskWithResultAsCoroutineCompletesSuccessfully()
        {
            ValueTask<int> task = new(CreateDelayedTask(() => 99));

            int result = 0;
            yield return task.AsCoroutine(r => result = r);

            Assert.AreEqual(99, result);
        }

        [UnityTest]
        public IEnumerator ValueTaskWithResultAsCoroutineCompletesImmediatelyWhenAlreadyDone()
        {
            ValueTask<string> task = new("immediate");
            string result = null;
            yield return task.AsCoroutine(r => result = r);

            Assert.AreEqual("immediate", result);
        }

        [UnityTest]
        public IEnumerator ValueTaskWithResultAsCoroutineWorksWithoutCallback()
        {
            ValueTask<double> task = new(CreateDelayedTask(() => 3.14));

            yield return task.AsCoroutine();
        }

        [UnityTest]
        public IEnumerator ValueTaskWithResultAsCoroutineThrowsOnFaultedTask()
        {
            ValueTask<int> task = new(CreateFaultedTask<int>());

            bool exceptionThrown = false;
            IEnumerator coroutine = task.AsCoroutine();

            while (true)
            {
                try
                {
                    if (!coroutine.MoveNext())
                    {
                        break;
                    }
                }
                catch (InvalidOperationException)
                {
                    exceptionThrown = true;
                    break;
                }
                catch (AggregateException e)
                {
                    exceptionThrown = e.Flatten()
                        .InnerExceptions.Any(inner => inner is InvalidOperationException);
                    break;
                }
                yield return null;
            }

            Assert.IsTrue(exceptionThrown);
        }

        // Tests for IEnumerator.AsTask()
        [UnityTest]
        public IEnumerator IEnumeratorAsTaskCompletesSuccessfully()
        {
            return IEnumeratorAsTaskCompletesSuccessfullyAsync().AsCoroutine();
        }

        [UnityTest]
        public IEnumerator IEnumeratorAsTaskThrowsOnNull()
        {
            return IEnumeratorAsTaskThrowsOnNullAsync().AsCoroutine();
        }

        // Tests for IEnumerator.AsValueTask()
        [UnityTest]
        public IEnumerator IEnumeratorAsValueTaskCompletesSuccessfully()
        {
            return IEnumeratorAsValueTaskCompletesSuccessfullyAsync().AsCoroutine();
        }

        [UnityTest]
        public IEnumerator IEnumeratorAsValueTaskThrowsOnNull()
        {
            return IEnumeratorAsValueTaskThrowsOnNullAsync().AsCoroutine();
        }

        [UnityTest]
        public IEnumerator IEnumeratorAsValueTaskHandlesComplexCoroutine()
        {
            return IEnumeratorAsValueTaskHandlesComplexCoroutineAsync().AsCoroutine();
        }

        private async Task IEnumeratorAsValueTaskHandlesComplexCoroutineAsync()
        {
            int counter = 0;
            IEnumerator testCoroutine = ComplexTestCoroutine(value => counter = value);

            await testCoroutine.AsValueTask();

            Assert.AreEqual(5, counter);
        }

        // Helper coroutines for testing
        private static IEnumerator TestCoroutine(Action onComplete)
        {
            for (int i = 0; i < 3; i++)
            {
                yield return null;
            }
            onComplete?.Invoke();
        }

        private static IEnumerator ComplexTestCoroutine(Action<int> onComplete)
        {
            int count = 0;
            for (int i = 0; i < 5; i++)
            {
                count++;
                yield return null;
            }
            onComplete?.Invoke(count);
        }

        // Unity-safe Task creation helpers
        private static async Task CreateDelayedTask(Action onComplete)
        {
            // Use Task.Yield instead of Task.Delay to avoid threading issues
            for (int i = 0; i < 3; i++)
            {
                await Task.Yield();
            }
            onComplete?.Invoke();
        }

        private static async Task<T> CreateDelayedTask<T>(Func<T> factory)
        {
            // Use Task.Yield instead of Task.Delay to avoid threading issues
            for (int i = 0; i < 3; i++)
            {
                await Task.Yield();
            }
            return factory();
        }

        private static Task CreateFaultedTask()
        {
            TaskCompletionSource<bool> tcs = new();
            tcs.SetException(new InvalidOperationException("Test exception"));
            return tcs.Task;
        }

        private static Task<T> CreateFaultedTask<T>()
        {
            TaskCompletionSource<T> tcs = new();
            tcs.SetException(new InvalidOperationException("Test exception"));
            return tcs.Task;
        }

        private static AsyncOperation CreateAsyncOperation()
        {
            // Use Resources.LoadAsync as a simple way to create an AsyncOperation
            return Resources.LoadAsync<Texture2D>("NonExistentResource");
        }

        private static AsyncOperation CreateCompletedAsyncOperation()
        {
            AsyncOperation operation = Resources.LoadAsync<Texture2D>("NonExistentResource");
            // Unity AsyncOperations complete on the next frame minimum, but Resources.LoadAsync
            // for non-existent resources completes very quickly. In tests, we rely on the
            // operation completing fast enough that by the time we use it, it's done.
            // If this causes issues, tests should use CreateAsyncOperation() with yield instead.
            return operation;
        }

        private static async Task IEnumeratorAsValueTaskCompletesSuccessfullyAsync()
        {
            bool completed = false;
            IEnumerator testCoroutine = TestCoroutine(() => completed = true);

            await testCoroutine.AsValueTask();

            Assert.IsTrue(completed);
        }

        private static async Task IEnumeratorAsValueTaskThrowsOnNullAsync()
        {
            IEnumerator nullCoroutine = null;
            try
            {
                await nullCoroutine.AsValueTask();
                Assert.Fail("Expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                // Test passed - expected exception was thrown
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(
                    e.Flatten().InnerExceptions.Any(inner => inner is ArgumentNullException),
                    e.ToString()
                );
            }
        }

        private static async Task IEnumeratorAsTaskCompletesSuccessfullyAsync()
        {
            bool completed = false;
            IEnumerator testCoroutine = TestCoroutine(() => completed = true);

            await testCoroutine.AsTask();

            Assert.IsTrue(completed);
        }

        private static async Task IEnumeratorAsTaskThrowsOnNullAsync()
        {
            IEnumerator nullCoroutine = null;
            try
            {
                await nullCoroutine.AsTask();
                Assert.Fail("Expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                // Test passed - expected exception was thrown
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(
                    e.Flatten().InnerExceptions.Any(inner => inner is ArgumentNullException),
                    e.ToString()
                );
            }
        }

        private static async Task AsyncOperationAwaiterOnCompletedInvokesContinuationAsync()
        {
            AsyncOperation operation = CreateAsyncOperation();
            AsyncOperationExtensions.AsyncOperationAwaiter awaiter = new(operation);

            bool continuationInvoked = false;
            awaiter.OnCompleted(() => continuationInvoked = true);

            // Wait for operation to complete
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            // Give it a frame to invoke the continuation
            await Task.Yield();

            Assert.IsTrue(continuationInvoked);
        }

        private static async Task AsyncOperationAwaiterIsCompletedReturnsTrueWhenDoneAsync()
        {
            AsyncOperation operation = CreateAsyncOperation();
            AsyncOperationExtensions.AsyncOperationAwaiter awaiter = new(operation);

            // Initially not completed
            Assert.IsFalse(awaiter.IsCompleted);

            // Wait for operation to complete
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            Assert.IsTrue(awaiter.IsCompleted);
        }

        private static async Task AsValueTaskReturnsImmediatelyWhenAlreadyDoneAsync()
        {
            AsyncOperation operation = CreateCompletedAsyncOperation();
            await operation.AsValueTask();
            Assert.IsTrue(operation.isDone);
        }

        private static async Task AsTaskCompletesWhenOperationIsDoneAsync()
        {
            AsyncOperation operation = CreateAsyncOperation();
            Task task = operation.AsTask();

            // Wait for operation to complete
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            await task;
            Assert.IsTrue(operation.isDone);
        }

        private static async Task AsTaskReturnsImmediatelyWhenAlreadyDoneAsync()
        {
            // First wait for an operation to complete
            AsyncOperation operation = CreateAsyncOperation();
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            // Now test that AsTask returns immediately for already-done operation
            await operation.AsTask();
            Assert.IsTrue(operation.isDone);
        }

        private static async Task AsValueTaskCompletesWhenOperationIsDoneAsync()
        {
            AsyncOperation operation = CreateAsyncOperation();
            ValueTask task = operation.AsValueTask();

            // Wait for operation to complete
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            await task;
            Assert.IsTrue(operation.isDone);
        }
    }
}
