namespace WallstopStudios.UnityHelpers.Tests.Editor.AssetProcessors
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.AssetProcessors;

    public sealed class DetectAssetChangeProcessorTests
    {
        private const string Root = "Assets/__DetectAssetChangedTests__";
        private const string HandlerAssetPath = Root + "/Handler.asset";
        private const string PayloadAssetPath = Root + "/Payload.asset";

        [SetUp]
        public void SetUp()
        {
            DetectAssetChangeProcessor.IncludeTestAssets = true;
            EnsureFolder();
            EnsureHandlerAsset();
            TestDetectAssetChangeHandler.Clear();
            DetectAssetChangeProcessor.ResetForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            DetectAssetChangeProcessor.IncludeTestAssets = false;
            if (AssetDatabase.LoadAssetAtPath<Object>(PayloadAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(PayloadAssetPath);
            }

            if (AssetDatabase.LoadAssetAtPath<Object>(HandlerAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(HandlerAssetPath);
            }

            if (AssetDatabase.IsValidFolder(Root))
            {
                AssetDatabase.DeleteAsset(Root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            TestDetectAssetChangeHandler.Clear();
        }

        [Test]
        public void InvokesHandlersWhenAssetsAreCreated()
        {
            CreatePayloadAsset();

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );

            Assert.AreEqual(1, TestDetectAssetChangeHandler.RecordedContexts.Count);
            AssetChangeContext context = TestDetectAssetChangeHandler.RecordedContexts[0];
            Assert.AreEqual(AssetChangeFlags.Created, context.Flags);
            CollectionAssert.Contains(context.CreatedAssetPaths, PayloadAssetPath);
        }

        [Test]
        public void InvokesHandlersWhenAssetsAreDeleted()
        {
            CreatePayloadAsset();

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );

            TestDetectAssetChangeHandler.Clear();

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                null,
                new[] { PayloadAssetPath },
                null,
                null
            );

            Assert.AreEqual(1, TestDetectAssetChangeHandler.RecordedContexts.Count);
            AssetChangeContext context = TestDetectAssetChangeHandler.RecordedContexts[0];
            Assert.AreEqual(AssetChangeFlags.Deleted, context.Flags);
            CollectionAssert.Contains(context.DeletedAssetPaths, PayloadAssetPath);
        }

        private static void CreatePayloadAsset()
        {
            TestDetectableAsset payload = ScriptableObject.CreateInstance<TestDetectableAsset>();
            AssetDatabase.CreateAsset(payload, PayloadAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureHandlerAsset()
        {
            if (
                AssetDatabase.LoadAssetAtPath<TestDetectAssetChangeHandler>(HandlerAssetPath)
                != null
            )
            {
                return;
            }

            TestDetectAssetChangeHandler handler =
                ScriptableObject.CreateInstance<TestDetectAssetChangeHandler>();
            AssetDatabase.CreateAsset(handler, HandlerAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(Root))
            {
                AssetDatabase.CreateFolder("Assets", "__DetectAssetChangedTests__");
            }
        }
    }

    internal sealed class TestDetectableAsset : ScriptableObject { }

    internal sealed class TestDetectAssetChangeHandler : ScriptableObject
    {
        private static readonly List<AssetChangeContext> Recorded = new();

        public static IReadOnlyList<AssetChangeContext> RecordedContexts => Recorded;

        public static void Clear()
        {
            Recorded.Clear();
        }

        [DetectAssetChanged(
            typeof(TestDetectableAsset),
            AssetChangeFlags.Created | AssetChangeFlags.Deleted
        )]
        private void OnTestAssetChanged(AssetChangeContext context)
        {
            Recorded.Add(context);
        }
    }
}
