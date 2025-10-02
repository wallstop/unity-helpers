namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using Object = UnityEngine.Object;

    public sealed class UnityMainThreadDispatcherTests
    {
        private readonly List<Object> _spawned = new();

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            foreach (Object spawned in _spawned)
            {
                if (spawned != null)
                {
                    Object.Destroy(spawned);
                    yield return null;
                }
            }
            _spawned.Clear();
        }

        [UnityTest]
        public IEnumerator RunOnMainThreadExecutesQueuedActions()
        {
            UnityMainThreadDispatcher dispatcher = UnityMainThreadDispatcher.Instance;
            _spawned.Add(dispatcher.gameObject);
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
