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
                LogType.Error,
                new Regex("UnityMainThreadDispatcher action threw InvalidOperationException.*")
            );

            dispatcher.RunOnMainThread(() =>
                throw new InvalidOperationException("Dispatcher test failure")
            );

            yield return null;
            yield return null;

            LogAssert.NoUnexpectedReceived();
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

        [Test]
        public void RunOnMainThreadNullThrows()
        {
            UnityMainThreadDispatcher dispatcher = UnityMainThreadDispatcher.Instance;
            Assert.Throws<ArgumentNullException>(() => dispatcher.RunOnMainThread(null));
        }

        [Test]
        public void TryRunOnMainThreadNullThrows()
        {
            UnityMainThreadDispatcher dispatcher = UnityMainThreadDispatcher.Instance;
            Assert.Throws<ArgumentNullException>(() => dispatcher.TryRunOnMainThread(null));
        }

        [UnityTest]
        public IEnumerator PendingActionLimitZeroIsUnbounded()
        {
            UnityMainThreadDispatcher dispatcher = UnityMainThreadDispatcher.Instance;
            Track(dispatcher.gameObject);
            dispatcher.PendingActionLimit = 0;

            bool executed = false;
            dispatcher.RunOnMainThread(() => executed = true);

            yield return null;
            yield return null;

            Assert.IsTrue(executed);
            Assert.AreEqual(0, dispatcher.PendingActionCount);
        }

        [UnityTest]
        public IEnumerator OverflowWarningThrottledPerFrame()
        {
            UnityMainThreadDispatcher dispatcher = UnityMainThreadDispatcher.Instance;
            Track(dispatcher.gameObject);
            dispatcher.PendingActionLimit = 1;

            dispatcher.RunOnMainThread(() => { });

            LogAssert.Expect(
                LogType.Warning,
                new Regex("UnityMainThreadDispatcher queue overflow.*")
            );
            dispatcher.RunOnMainThread(() => { });

            // Second overflow same frame should not produce another warning
            dispatcher.RunOnMainThread(() => { });

            yield return null;
            yield return null;

            LogAssert.Expect(
                LogType.Warning,
                new Regex("UnityMainThreadDispatcher queue overflow.*")
            );
            dispatcher.RunOnMainThread(() => { });
            dispatcher.RunOnMainThread(() => { });

            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator RunAsyncFuncThrowsSynchronous()
        {
            UnityMainThreadDispatcher dispatcher = UnityMainThreadDispatcher.Instance;
            Track(dispatcher.gameObject);

            Task task = dispatcher.RunAsync(() =>
                throw new InvalidOperationException("Sync throw")
            );

            while (!task.IsCompleted)
            {
                yield return null;
            }

            Assert.IsTrue(task.IsFaulted);
            Assert.IsInstanceOf<InvalidOperationException>(task.Exception?.GetBaseException());
        }

        [UnityTest]
        public IEnumerator RunAsyncFuncReturnsNull()
        {
            UnityMainThreadDispatcher dispatcher = UnityMainThreadDispatcher.Instance;
            Track(dispatcher.gameObject);

            Task task = dispatcher.RunAsync(token => null);

            while (!task.IsCompleted)
            {
                yield return null;
            }

            Assert.IsTrue(task.IsFaulted);
            StringAssert.Contains(
                "expected the delegate to return a Task",
                task.Exception?.GetBaseException().Message
            );
        }

        [UnityTest]
        public IEnumerator InstanceAccessibleFromWorkerThreadAfterBootstrap()
        {
            UnityMainThreadDispatcher dispatcher = UnityMainThreadDispatcher.Instance;
            Track(dispatcher.gameObject);

            Exception backgroundException = null;
            UnityMainThreadDispatcher backgroundInstance = null;

            using (ManualResetEventSlim completed = new(false))
            {
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
            }

            Assert.IsNull(backgroundException);
            Assert.AreSame(dispatcher, backgroundInstance);
        }

        [UnityTest]
        public IEnumerator BackgroundThreadCanScheduleWorkAfterBootstrap()
        {
            UnityMainThreadDispatcher dispatcher = UnityMainThreadDispatcher.Instance;
            Track(dispatcher.gameObject);

            Exception backgroundException = null;
            bool executed = false;

            using (ManualResetEventSlim completed = new(false))
            {
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
            }

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

            Task task = null;
            using (CancellationTokenSource cancellationTokenSource = new())
            {
                task = dispatcher.RunAsync(
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
            }

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

        [UnityTest]
        public IEnumerator BootstrapEnsuresInstanceExists()
        {
            UnityMainThreadDispatcher dispatcher = UnityMainThreadDispatcher.Instance;
            Track(dispatcher.gameObject);

            yield return null;

            Assert.IsTrue(UnityMainThreadDispatcher.HasInstance);
        }
    }
}
