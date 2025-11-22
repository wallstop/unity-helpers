namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System;
    using System.Collections;
    using System.Reflection;
    using System.Threading;
    using NUnit.Framework;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;

    public sealed class UnityMainThreadGuardTests : CommonTestBase
    {
        [UnityTest]
        public IEnumerator EnsureMainThreadThrowsWhenOffThread()
        {
            InvalidOperationException captured = null;
            using (ManualResetEventSlim done = new(false))
            {
                Thread thread = new(() =>
                {
                    try
                    {
                        UnityMainThreadGuard.EnsureMainThread();
                    }
                    catch (InvalidOperationException ex)
                    {
                        captured = ex;
                    }
                    finally
                    {
                        done.Set();
                    }
                })
                {
                    IsBackground = true,
                };

                thread.Start();

                while (!done.IsSet)
                {
                    yield return null;
                }
            }

            Assert.IsNotNull(captured);
            StringAssert.Contains(nameof(EnsureMainThreadThrowsWhenOffThread), captured.Message);
        }

        [Test]
        public void EnsureMainThreadNoOpWhenOnMainThread()
        {
            Assert.DoesNotThrow(() => UnityMainThreadGuard.EnsureMainThread());
        }

#if UNITY_EDITOR
        [Test]
        public void EditorInitializeDoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                MethodInfo method = typeof(UnityMainThreadGuard).GetMethod(
                    "CaptureEditorThread",
                    BindingFlags.NonPublic | BindingFlags.Static
                );
                method?.Invoke(null, null);
            });
        }
#endif
    }
}
