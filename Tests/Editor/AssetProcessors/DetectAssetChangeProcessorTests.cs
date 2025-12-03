namespace WallstopStudios.UnityHelpers.Tests.Editor.AssetProcessors
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.AssetProcessors;
    using Object = UnityEngine.Object;

    public sealed class DetectAssetChangeProcessorTests
    {
        private const string Root = "Assets/__DetectAssetChangedTests__";
        private const string HandlerAssetPath = Root + "/Handler.asset";
        private const string PayloadAssetPath = Root + "/Payload.asset";
        private const string DetailedHandlerAssetPath = Root + "/DetailedHandler.asset";
        private const string AlternatePayloadAssetPath = Root + "/AlternatePayload.asset";

        [SetUp]
        public void SetUp()
        {
            DetectAssetChangeProcessor.IncludeTestAssets = true;
            EnsureFolder();
            EnsureHandlerAsset<TestDetectAssetChangeHandler>(HandlerAssetPath);
            EnsureHandlerAsset<TestDetailedSignatureHandler>(DetailedHandlerAssetPath);
            ClearTestState();
            DetectAssetChangeProcessor.ResetForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            DetectAssetChangeProcessor.IncludeTestAssets = false;
            DeleteAssetIfExists(PayloadAssetPath);
            DeleteAssetIfExists(AlternatePayloadAssetPath);
            DeleteAssetIfExists(HandlerAssetPath);
            DeleteAssetIfExists(DetailedHandlerAssetPath);

            if (AssetDatabase.IsValidFolder(Root))
            {
                AssetDatabase.DeleteAsset(Root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ClearTestState();
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

        [Test]
        public void StaticHandlersReceiveNotificationsForAssetChanges()
        {
            CreatePayloadAsset();

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );

            Assert.AreEqual(1, TestStaticAssetChangeHandler.RecordedContexts.Count);
            Assert.AreEqual(
                AssetChangeFlags.Created,
                TestStaticAssetChangeHandler.RecordedContexts[0].Flags
            );

            TestStaticAssetChangeHandler.Clear();

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                null,
                new[] { PayloadAssetPath },
                null,
                null
            );

            Assert.AreEqual(1, TestStaticAssetChangeHandler.RecordedContexts.Count);
            Assert.AreEqual(
                AssetChangeFlags.Deleted,
                TestStaticAssetChangeHandler.RecordedContexts[0].Flags
            );
        }

        [Test]
        public void DetailedSignatureReceivesCreatedAssetsAndDeletedPaths()
        {
            CreatePayloadAsset();

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );

            Assert.AreEqual(1, TestDetailedSignatureHandler.LastCreatedAssets.Length);
            Assert.NotNull(TestDetailedSignatureHandler.LastCreatedAssets[0]);
            Assert.AreEqual(
                PayloadAssetPath,
                AssetDatabase.GetAssetPath(TestDetailedSignatureHandler.LastCreatedAssets[0])
            );
            Assert.AreEqual(0, TestDetailedSignatureHandler.LastDeletedPaths.Length);

            TestDetailedSignatureHandler.Clear();

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                null,
                new[] { PayloadAssetPath },
                null,
                null
            );

            Assert.AreEqual(0, TestDetailedSignatureHandler.LastCreatedAssets.Length);
            CollectionAssert.AreEquivalent(
                new[] { PayloadAssetPath },
                TestDetailedSignatureHandler.LastDeletedPaths
            );
        }

        [Test]
        public void SingleMethodCanWatchMultipleAssetTypes()
        {
            CreatePayloadAsset();
            CreateAlternatePayloadAsset();

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );

            Assert.AreEqual(1, TestMultiAttributeHandler.RecordedInvocations.Count);
            Assert.AreEqual(
                typeof(TestDetectableAsset),
                TestMultiAttributeHandler.RecordedInvocations[0].AssetType
            );
            Assert.AreEqual(
                AssetChangeFlags.Created,
                TestMultiAttributeHandler.RecordedInvocations[0].Flags
            );

            TestMultiAttributeHandler.Clear();

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { AlternatePayloadAssetPath },
                null,
                null,
                null
            );

            Assert.AreEqual(0, TestMultiAttributeHandler.RecordedInvocations.Count);

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                null,
                new[] { AlternatePayloadAssetPath },
                null,
                null
            );

            Assert.AreEqual(1, TestMultiAttributeHandler.RecordedInvocations.Count);
            Assert.AreEqual(
                typeof(TestAlternateDetectableAsset),
                TestMultiAttributeHandler.RecordedInvocations[0].AssetType
            );
            Assert.AreEqual(
                AssetChangeFlags.Deleted,
                TestMultiAttributeHandler.RecordedInvocations[0].Flags
            );
        }

        [Test]
        public void LogsErrorWhenMethodReturnsNonVoid()
        {
            Regex expected = new(
                "TestInvalidReturnTypeHandler\\.OnInvalidReturnType.*Supported signatures",
                RegexOptions.Singleline
            );
            LogAssert.Expect(LogType.Error, expected);

            bool isValid = DetectAssetChangeProcessor.ValidateMethodSignatureForTesting(
                typeof(TestInvalidReturnTypeHandler),
                "OnInvalidReturnType"
            );

            Assert.IsFalse(isValid);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LogsErrorWhenMethodHasUnsupportedSingleParameter()
        {
            Regex expected = new(
                "TestInvalidParameterHandler\\.OnInvalidSingleParameter.*Supported signatures",
                RegexOptions.Singleline
            );
            LogAssert.Expect(LogType.Error, expected);

            bool isValid = DetectAssetChangeProcessor.ValidateMethodSignatureForTesting(
                typeof(TestInvalidParameterHandler),
                "OnInvalidSingleParameter"
            );

            Assert.IsFalse(isValid);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LogsErrorWhenCreatedAssetParameterIsNotArray()
        {
            Regex expected = new(
                "TestInvalidCreatedParameterHandler\\.OnInvalidCreated.*Supported signatures",
                RegexOptions.Singleline
            );
            LogAssert.Expect(LogType.Error, expected);

            bool isValid = DetectAssetChangeProcessor.ValidateMethodSignatureForTesting(
                typeof(TestInvalidCreatedParameterHandler),
                "OnInvalidCreated"
            );

            Assert.IsFalse(isValid);
            LogAssert.NoUnexpectedReceived();
        }

        private static void CreatePayloadAsset()
        {
            TestDetectableAsset payload = ScriptableObject.CreateInstance<TestDetectableAsset>();
            AssetDatabase.CreateAsset(payload, PayloadAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateAlternatePayloadAsset()
        {
            TestAlternateDetectableAsset payload =
                ScriptableObject.CreateInstance<TestAlternateDetectableAsset>();
            AssetDatabase.CreateAsset(payload, AlternatePayloadAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureHandlerAsset<T>(string assetPath)
            where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(assetPath) != null)
            {
                return;
            }

            T handler = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(handler, assetPath);
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

        private static void DeleteAssetIfExists(string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        private static void ClearTestState()
        {
            TestDetectAssetChangeHandler.Clear();
            TestDetailedSignatureHandler.Clear();
            TestStaticAssetChangeHandler.Clear();
            TestMultiAttributeHandler.Clear();
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

        [DetectAssetChanged(typeof(TestDetectableAsset))]
        private void OnTestAssetChanged(AssetChangeContext context)
        {
            Recorded.Add(context);
        }
    }

    internal sealed class TestDetailedSignatureHandler : ScriptableObject
    {
        private static TestDetectableAsset[] lastCreatedAssets = Array.Empty<TestDetectableAsset>();
        private static string[] lastDeletedPaths = Array.Empty<string>();

        public static TestDetectableAsset[] LastCreatedAssets => lastCreatedAssets;

        public static string[] LastDeletedPaths => lastDeletedPaths;

        public static void Clear()
        {
            lastCreatedAssets = Array.Empty<TestDetectableAsset>();
            lastDeletedPaths = Array.Empty<string>();
        }

        [DetectAssetChanged(
            typeof(TestDetectableAsset),
            AssetChangeFlags.Created | AssetChangeFlags.Deleted
        )]
        private void OnDetailedChange(TestDetectableAsset[] createdAssets, string[] deletedPaths)
        {
            lastCreatedAssets = createdAssets ?? Array.Empty<TestDetectableAsset>();
            lastDeletedPaths = deletedPaths ?? Array.Empty<string>();
        }
    }

    internal static class TestStaticAssetChangeHandler
    {
        private static readonly List<AssetChangeContext> Recorded = new();

        public static IReadOnlyList<AssetChangeContext> RecordedContexts => Recorded;

        public static void Clear()
        {
            Recorded.Clear();
        }

        [DetectAssetChanged(typeof(TestDetectableAsset))]
        private static void OnTestAssetChanged(AssetChangeContext context)
        {
            Recorded.Add(context);
        }
    }

    internal sealed class TestAlternateDetectableAsset : ScriptableObject { }

    internal static class TestMultiAttributeHandler
    {
        private static readonly List<AssetInvocationRecord> Recorded = new();

        public static IReadOnlyList<AssetInvocationRecord> RecordedInvocations => Recorded;

        public static void Clear()
        {
            Recorded.Clear();
        }

        [DetectAssetChanged(typeof(TestDetectableAsset))]
        [DetectAssetChanged(typeof(TestAlternateDetectableAsset), AssetChangeFlags.Deleted)]
        private static void OnAssetChanged(AssetChangeContext context)
        {
            Recorded.Add(new AssetInvocationRecord(context.AssetType, context.Flags));
        }
    }

    internal readonly struct AssetInvocationRecord
    {
        public AssetInvocationRecord(Type assetType, AssetChangeFlags flags)
        {
            AssetType = assetType;
            Flags = flags;
        }

        public Type AssetType { get; }

        public AssetChangeFlags Flags { get; }
    }

    internal sealed class TestInvalidReturnTypeHandler : ScriptableObject
    {
        private int OnInvalidReturnType()
        {
            return 0;
        }
    }

    internal sealed class TestInvalidParameterHandler : ScriptableObject
    {
        private void OnInvalidSingleParameter(string unexpected)
        {
            _ = unexpected;
        }
    }

    internal sealed class TestInvalidCreatedParameterHandler : ScriptableObject
    {
        private void OnInvalidCreated(TestDetectableAsset created, string[] deletedPaths)
        {
            _ = created;
            _ = deletedPaths;
        }
    }
}
