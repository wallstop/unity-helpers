#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.WButton
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils.WButton;
    using WallstopStudios.UnityHelpers.Tests.Utils;

    public sealed class WButtonInvocationControllerTests : CommonTestBase
    {
        [UnityTest]
        public IEnumerator AsyncTaskInvocationCompletesAndRecordsHistory()
        {
            InvocationTarget target = CreateScriptableObject<InvocationTarget>();

            WButtonMethodMetadata metadata = WButtonMetadataCache
                .GetMetadata(typeof(InvocationTarget))
                .First(m => m.Method.Name == nameof(InvocationTarget.AsyncTaskButton));
            WButtonTargetState targetState = WButtonStateRepository.GetOrCreate(target);
            WButtonMethodState methodState = targetState.GetOrCreateMethodState(metadata);
            WButtonMethodContext context = new(
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
            WButtonResultEntry entry = methodState.History[^1];
            Assert.That(entry.Kind, Is.EqualTo(WButtonResultKind.Success));
            Assert.That(entry.Summary, Does.Contain("Task Complete"));
        }

        [UnityTest]
        public IEnumerator EnumeratorInvocationClearsActiveState()
        {
            InvocationTarget target = CreateScriptableObject<InvocationTarget>();

            WButtonMethodMetadata metadata = WButtonMetadataCache
                .GetMetadata(typeof(InvocationTarget))
                .First(m => m.Method.Name == nameof(InvocationTarget.EnumeratorButton));
            WButtonTargetState targetState = WButtonStateRepository.GetOrCreate(target);
            WButtonMethodState methodState = targetState.GetOrCreateMethodState(metadata);
            WButtonMethodContext context = new(
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
            WButtonResultEntry entry = methodState.History[^1];
            Assert.That(entry.Kind, Is.EqualTo(WButtonResultKind.Success));
            Assert.That(entry.Summary, Does.Contain("Enumerator completed"));
        }

        [Test]
        public void ClearHistoryRemovesRecordedEntries()
        {
            InvocationTarget target = CreateScriptableObject<InvocationTarget>();

            WButtonMethodMetadata metadata = WButtonMetadataCache
                .GetMetadata(typeof(InvocationTarget))
                .First(m => m.Method.Name == nameof(InvocationTarget.AsyncTaskButton));
            WButtonTargetState targetState = WButtonStateRepository.GetOrCreate(target);
            WButtonMethodState methodState = targetState.GetOrCreateMethodState(metadata);

            int capacity = UnityHelpersSettings.GetWButtonHistorySize();
            methodState.AddResult(
                new WButtonResultEntry(
                    WButtonResultKind.Success,
                    DateTime.UtcNow,
                    value: "Sample",
                    summary: "Sample Result",
                    objectReference: null
                ),
                capacity
            );

            Assert.IsTrue(methodState.HasHistory, "History should be populated after AddResult.");

            methodState.ClearHistory();

            Assert.IsFalse(methodState.HasHistory, "History should be empty after ClearHistory.");
            Assert.That(methodState.History, Is.Empty);
        }

        private static IEnumerator WaitUntil(Func<bool> condition, float timeoutSeconds)
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
