namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System;
    using System.Collections;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;

    public sealed class UnityMainThreadDispatcherTests : CommonTestBase
    {
        [UnityTest]
        public IEnumerator RunOnMainThreadExecutesQueuedActions()
        {
            UnityMainThreadDispatcher dispatcher = UnityMainThreadDispatcher.Instance;
            Track(dispatcher.gameObject);
            bool executed = false;

            dispatcher.RunOnMainThread(() => executed = true);

            yield return null;
            yield return null;

            Assert.IsTrue(executed);
        }

        [UnityTest]
        public IEnumerator RunOnMainThreadLogsExceptions()
        {
            UnityMainThreadDispatcher dispatcher = UnityMainThreadDispatcher.Instance;
            LogAssert.Expect(
                LogType.Exception,
                "InvalidOperationException: Dispatcher test failure"
            );

            dispatcher.RunOnMainThread(() =>
                throw new InvalidOperationException("Dispatcher test failure")
            );

            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator QueueOverflowDropsExcessActions()
        {
            UnityMainThreadDispatcher dispatcher = UnityMainThreadDispatcher.Instance;
            Track(dispatcher.gameObject);
            dispatcher.PendingActionLimit = 1;

            bool firstExecuted = false;
            bool secondExecuted = false;

            dispatcher.RunOnMainThread(() => firstExecuted = true);

            LogAssert.Expect(
                LogType.Warning,
                new Regex("UnityMainThreadDispatcher queue overflow.*")
            );
            dispatcher.RunOnMainThread(() => secondExecuted = true);

            yield return null;
            yield return null;

            Assert.IsTrue(firstExecuted);
            Assert.IsFalse(secondExecuted);
            Assert.AreEqual(0, dispatcher.PendingActionCount);
        }

        [UnityTest]
        public IEnumerator TryRunOnMainThreadReturnsFalseWhenQueueFull()
        {
            UnityMainThreadDispatcher dispatcher = UnityMainThreadDispatcher.Instance;
            Track(dispatcher.gameObject);
            dispatcher.PendingActionLimit = 1;

            dispatcher.RunOnMainThread(() => { });

            bool overflowActionExecuted = false;
            bool enqueued = dispatcher.TryRunOnMainThread(() => overflowActionExecuted = true);

            Assert.IsFalse(enqueued);
            Assert.IsFalse(overflowActionExecuted);

            yield return null;
            yield return null;

            Assert.AreEqual(0, dispatcher.PendingActionCount);
        }

        [UnityTest]
        public IEnumerator InstanceAccessibleFromWorkerThreadAfterBootstrap()
        {
            UnityMainThreadDispatcher dispatcher = UnityMainThreadDispatcher.Instance;
            Track(dispatcher.gameObject);

            ManualResetEventSlim completed = new(false);
            Exception backgroundException = null;
            UnityMainThreadDispatcher backgroundInstance = null;

            Task.Run(() =>
            {
                try
                {
                    backgroundInstance = UnityMainThreadDispatcher.Instance;
                }
                catch (Exception ex)
                {
                    backgroundException = ex;
                }
                finally
                {
                    completed.Set();
                }
            });

            while (!completed.IsSet)
            {
                yield return null;
            }

            completed.Dispose();

            Assert.IsNull(backgroundException);
            Assert.AreSame(dispatcher, backgroundInstance);
        }

        [UnityTest]
        public IEnumerator BackgroundThreadCanScheduleWorkAfterBootstrap()
        {
            UnityMainThreadDispatcher dispatcher = UnityMainThreadDispatcher.Instance;
            Track(dispatcher.gameObject);

            ManualResetEventSlim completed = new(false);
            Exception backgroundException = null;
            bool executed = false;

            Task.Run(() =>
            {
                try
                {
                    UnityMainThreadDispatcher.Instance.RunOnMainThread(() => executed = true);
                }
                catch (Exception ex)
                {
                    backgroundException = ex;
                }
                finally
                {
                    completed.Set();
                }
            });

            while (!completed.IsSet)
            {
                yield return null;
            }

            completed.Dispose();

            yield return null;
            yield return null;

            Assert.IsNull(backgroundException);
            Assert.IsTrue(executed);
        }

        [UnityTest]
        public IEnumerator RunAsyncCancellationTokenHonorsCancellation()
        {
            UnityMainThreadDispatcher dispatcher = UnityMainThreadDispatcher.Instance;
            Track(dispatcher.gameObject);

            CancellationTokenSource cancellationTokenSource = new();
            Task task = dispatcher.RunAsync(
                async token =>
                {
                    await Task.Delay(1, token);
                },
                cancellationTokenSource.Token
            );

            cancellationTokenSource.Cancel();

            while (!task.IsCompleted)
            {
                yield return null;
            }

            cancellationTokenSource.Dispose();

            Assert.IsTrue(task.IsCanceled);
        }

        [UnityTest]
        public IEnumerator RunAsyncDelegateCompletesAfterAwait()
        {
            UnityMainThreadDispatcher dispatcher = UnityMainThreadDispatcher.Instance;
            Track(dispatcher.gameObject);

            bool awaited = false;

            Task task = dispatcher.RunAsync(async token =>
            {
                await Task.Yield();
                awaited = true;
            });

            while (!task.IsCompleted)
            {
                yield return null;
            }

            Assert.IsTrue(awaited);
            Assert.IsFalse(task.IsFaulted);
            Assert.IsFalse(task.IsCanceled);
        }
    }
}
