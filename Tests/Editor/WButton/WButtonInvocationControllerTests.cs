#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.WButton
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.WButton;

    public sealed class WButtonInvocationControllerTests
    {
        [UnityTest]
        public IEnumerator AsyncTaskInvocationCompletesAndRecordsHistory()
        {
            InvocationTarget target = ScriptableObject.CreateInstance<InvocationTarget>();
            try
            {
                WButtonMethodMetadata metadata = WButtonMetadataCache
                    .GetMetadata(typeof(InvocationTarget))
                    .First(m => m.Method.Name == nameof(InvocationTarget.AsyncTaskButton));
                WButtonTargetState targetState = WButtonStateRepository.GetOrCreate(target);
                WButtonMethodState methodState = targetState.GetOrCreateMethodState(metadata);
                WButtonMethodContext context = new WButtonMethodContext(
                    metadata,
                    new[] { methodState },
                    new UnityEngine.Object[] { target }
                );

                context.MarkTriggered();
                WButtonInvocationController.ProcessTriggeredMethods(
                    new List<WButtonMethodContext> { context }
                );

                yield return WaitUntil(() => methodState.ActiveInvocation == null, 5f);

                Assert.That(methodState.History.Count, Is.GreaterThan(0));
                WButtonResultEntry entry = methodState.History[methodState.History.Count - 1];
                Assert.That(entry.Kind, Is.EqualTo(WButtonResultKind.Success));
                Assert.That(entry.Summary, Does.Contain("Task Complete"));
            }
            finally
            {
                if (target != null)
                {
                    Object.DestroyImmediate(target);
                }
            }
        }

        [UnityTest]
        public IEnumerator EnumeratorInvocationClearsActiveState()
        {
            InvocationTarget target = ScriptableObject.CreateInstance<InvocationTarget>();
            try
            {
                WButtonMethodMetadata metadata = WButtonMetadataCache
                    .GetMetadata(typeof(InvocationTarget))
                    .First(m => m.Method.Name == nameof(InvocationTarget.EnumeratorButton));
                WButtonTargetState targetState = WButtonStateRepository.GetOrCreate(target);
                WButtonMethodState methodState = targetState.GetOrCreateMethodState(metadata);
                WButtonMethodContext context = new WButtonMethodContext(
                    metadata,
                    new[] { methodState },
                    new UnityEngine.Object[] { target }
                );

                context.MarkTriggered();
                WButtonInvocationController.ProcessTriggeredMethods(
                    new List<WButtonMethodContext> { context }
                );

                yield return WaitUntil(() => methodState.ActiveInvocation == null, 2f);

                Assert.That(methodState.History.Count, Is.GreaterThan(0));
                WButtonResultEntry entry = methodState.History[methodState.History.Count - 1];
                Assert.That(entry.Kind, Is.EqualTo(WButtonResultKind.Success));
                Assert.That(entry.Summary, Does.Contain("Enumerator completed"));
            }
            finally
            {
                if (target != null)
                {
                    Object.DestroyImmediate(target);
                }
            }
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float timeoutSeconds)
        {
            float endTime = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > endTime)
                {
                    Assert.Fail("Timed out while waiting for condition.");
                }
                yield return null;
            }
        }

        private sealed class InvocationTarget : ScriptableObject
        {
            [WButton]
            public async Task<string> AsyncTaskButton()
            {
                await Task.Delay(50);
                return "Task Complete";
            }

            [WButton]
            public IEnumerator EnumeratorButton()
            {
                yield return null;
                yield return null;
            }
        }
    }
}
#endif
