namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System;
    using System.Collections;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Helper;

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
    }
}
