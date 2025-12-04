namespace WallstopStudios.UnityHelpers.Tests.AssetProcessors
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
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using Object = UnityEngine.Object;

    public sealed class DetectAssetChangeProcessorTests
    {
        private const string Root = "Assets/__DetectAssetChangedTests__";
        private const string HandlerAssetPath = Root + "/Handler.asset";
        private const string PayloadAssetPath = Root + "/Payload.asset";
        private const string DetailedHandlerAssetPath = Root + "/DetailedHandler.asset";
        private const string AlternatePayloadAssetPath = Root + "/AlternatePayload.asset";
        private const string AssignableHandlerAssetPath = Root + "/AssignableHandler.asset";

        private Func<double> _originalTimeProvider;
        private float _originalLoopWindowSeconds;

        [SetUp]
        public void SetUp()
        {
            DetectAssetChangeProcessor.IncludeTestAssets = true;
            EnsureFolder();
            EnsureHandlerAsset<TestDetectAssetChangeHandler>(HandlerAssetPath);
            EnsureHandlerAsset<TestDetailedSignatureHandler>(DetailedHandlerAssetPath);
            EnsureHandlerAsset<TestAssignableAssetChangeHandler>(AssignableHandlerAssetPath);
            ClearTestState();
            DetectAssetChangeProcessor.ResetForTesting();
            _originalTimeProvider = DetectAssetChangeProcessor.TimeProvider;
            _originalLoopWindowSeconds = UnityHelpersSettings
                .instance
                .DetectAssetChangeLoopWindowSeconds;
        }

        [TearDown]
        public void TearDown()
        {
            DetectAssetChangeProcessor.IncludeTestAssets = false;
            DeleteAssetIfExists(PayloadAssetPath);
            DeleteAssetIfExists(AlternatePayloadAssetPath);
            DeleteAssetIfExists(HandlerAssetPath);
            DeleteAssetIfExists(DetailedHandlerAssetPath);
            DeleteAssetIfExists(AssignableHandlerAssetPath);

            if (AssetDatabase.IsValidFolder(Root))
            {
                AssetDatabase.DeleteAsset(Root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ClearTestState();
            DetectAssetChangeProcessor.TimeProvider = _originalTimeProvider;
            DetectAssetChangeProcessor.LoopWindowSecondsOverride = null;

            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            if (
                !Mathf.Approximately(
                    settings.DetectAssetChangeLoopWindowSeconds,
                    _originalLoopWindowSeconds
                )
            )
            {
                settings.DetectAssetChangeLoopWindowSeconds = _originalLoopWindowSeconds;
            }
        }

        [Test]
        public void InvokesHandlersWhenAssetsAreCreated()
        {
            CreatePayloadAsset();
            // Clear state after asset creation since Unity's OnPostprocessAllAssets may have fired
            ClearTestState();

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );

            Assert.AreEqual(
                1,
                TestDetectAssetChangeHandler.RecordedContexts.Count,
                $"Expected 1 invocation but got {TestDetectAssetChangeHandler.RecordedContexts.Count}"
            );
            AssetChangeContext context = TestDetectAssetChangeHandler.RecordedContexts[0];
            Assert.AreEqual(AssetChangeFlags.Created, context.Flags);
            CollectionAssert.Contains(context.CreatedAssetPaths, PayloadAssetPath);
        }

        [Test]
        public void InvokesHandlersWhenAssetsAreDeleted()
        {
            CreatePayloadAsset();
            // Clear state after asset creation since Unity's OnPostprocessAllAssets may have fired
            ClearTestState();

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

            Assert.AreEqual(
                1,
                TestDetectAssetChangeHandler.RecordedContexts.Count,
                $"Expected 1 invocation but got {TestDetectAssetChangeHandler.RecordedContexts.Count}"
            );
            AssetChangeContext context = TestDetectAssetChangeHandler.RecordedContexts[0];
            Assert.AreEqual(AssetChangeFlags.Deleted, context.Flags);
            CollectionAssert.Contains(context.DeletedAssetPaths, PayloadAssetPath);
        }

        [Test]
        public void StaticHandlersReceiveNotificationsForAssetChanges()
        {
            CreatePayloadAsset();
            // Clear state after asset creation since Unity's OnPostprocessAllAssets may have fired
            ClearTestState();

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );

            Assert.AreEqual(
                1,
                TestStaticAssetChangeHandler.RecordedContexts.Count,
                $"Expected 1 invocation but got {TestStaticAssetChangeHandler.RecordedContexts.Count}"
            );
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

            Assert.AreEqual(
                1,
                TestStaticAssetChangeHandler.RecordedContexts.Count,
                $"Expected 1 invocation but got {TestStaticAssetChangeHandler.RecordedContexts.Count}"
            );
            Assert.AreEqual(
                AssetChangeFlags.Deleted,
                TestStaticAssetChangeHandler.RecordedContexts[0].Flags
            );
        }

        [Test]
        public void DetailedSignatureReceivesCreatedAssetsAndDeletedPaths()
        {
            CreatePayloadAsset();
            // Clear state after asset creation since Unity's OnPostprocessAllAssets may have fired
            ClearTestState();

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );

            Assert.AreEqual(
                1,
                TestDetailedSignatureHandler.LastCreatedAssets.Length,
                $"Expected 1 created asset but got {TestDetailedSignatureHandler.LastCreatedAssets.Length}"
            );
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
        public void InterfaceHandlersReceiveAssignableAssets()
        {
            CreatePayloadAsset();
            // Clear state after asset creation since Unity's OnPostprocessAllAssets may have fired
            ClearTestState();

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );

            Assert.AreEqual(
                1,
                TestAssignableAssetChangeHandler.RecordedCreated.Count,
                $"Expected 1 created asset but got {TestAssignableAssetChangeHandler.RecordedCreated.Count}"
            );
            Assert.IsInstanceOf<TestDetectableAsset>(
                TestAssignableAssetChangeHandler.RecordedCreated[0]
            );

            TestAssignableAssetChangeHandler.Clear();
            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                null,
                new[] { PayloadAssetPath },
                null,
                null
            );

            CollectionAssert.Contains(
                TestAssignableAssetChangeHandler.RecordedDeletedPaths,
                PayloadAssetPath
            );

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SingleMethodCanWatchMultipleAssetTypes()
        {
            CreatePayloadAsset();
            CreateAlternatePayloadAsset();
            // Clear state after asset creation since Unity's OnPostprocessAllAssets may have fired
            ClearTestState();

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );

            Assert.AreEqual(
                1,
                TestMultiAttributeHandler.RecordedInvocations.Count,
                $"Expected 1 invocation but got {TestMultiAttributeHandler.RecordedInvocations.Count}"
            );
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

            Assert.AreEqual(
                0,
                TestMultiAttributeHandler.RecordedInvocations.Count,
                "TestAlternateDetectableAsset should not trigger Created flag handler"
            );

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                null,
                new[] { AlternatePayloadAssetPath },
                null,
                null
            );

            Assert.AreEqual(
                1,
                TestMultiAttributeHandler.RecordedInvocations.Count,
                $"Expected 1 invocation but got {TestMultiAttributeHandler.RecordedInvocations.Count}"
            );
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
        public void ReentrantHandlersQueueChangesInsteadOfRecursing()
        {
            CreatePayloadAsset();
            // Clear state after asset creation since Unity's OnPostprocessAllAssets may have fired
            ClearTestState();
            TestReentrantHandler.Configure(PayloadAssetPath);

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );

            Assert.AreEqual(
                2,
                TestReentrantHandler.InvocationCount,
                $"Expected 2 invocations (initial + reentrant) but got {TestReentrantHandler.InvocationCount}"
            );
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void InfiniteLoopingHandlersAreSuppressed()
        {
            CreatePayloadAsset();
            // Clear state after asset creation since Unity's OnPostprocessAllAssets may have fired
            ClearTestState();

            double fakeTime = 0;
            DetectAssetChangeProcessor.TimeProvider = () => fakeTime;

            LogAssert.Expect(
                LogType.Error,
                new Regex("potentially infinite asset change loop", RegexOptions.IgnoreCase)
            );

            int limit = DetectAssetChangeProcessor.MaxConsecutiveChangeSetsWithinWindow;
            for (int i = 0; i < limit + 5; i++)
            {
                fakeTime += 0.001;
                DetectAssetChangeProcessor.ProcessChangesForTesting(
                    new[] { PayloadAssetPath },
                    null,
                    null,
                    null
                );
            }

            Assert.AreEqual(
                limit,
                TestLoopingHandler.InvocationCount,
                $"Expected {limit} invocations (loop protection limit) but got {TestLoopingHandler.InvocationCount}"
            );
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ChangeBatchesWithGapsLongerThanWindowAreNotSuppressed()
        {
            CreatePayloadAsset();
            // Clear state after asset creation since Unity's OnPostprocessAllAssets may have fired
            ClearTestState();

            double fakeTime = 0;
            DetectAssetChangeProcessor.TimeProvider = () => fakeTime;
            DetectAssetChangeProcessor.LoopWindowSecondsOverride = 5d;

            int iterations = DetectAssetChangeProcessor.MaxConsecutiveChangeSetsWithinWindow + 5;
            for (int i = 0; i < iterations; i++)
            {
                fakeTime += 6d;
                DetectAssetChangeProcessor.ProcessChangesForTesting(
                    new[] { PayloadAssetPath },
                    null,
                    null,
                    null
                );
            }

            Assert.AreEqual(
                iterations,
                TestLoopingHandler.InvocationCount,
                $"Expected {iterations} invocations (gaps prevent loop detection) but got {TestLoopingHandler.InvocationCount}"
            );
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LoopWindowSettingDetectsSlowlyRecurringChanges()
        {
            CreatePayloadAsset();
            // Clear state after asset creation since Unity's OnPostprocessAllAssets may have fired
            ClearTestState();

            double fakeTime = 0;
            DetectAssetChangeProcessor.TimeProvider = () => fakeTime;

            int limit = DetectAssetChangeProcessor.MaxConsecutiveChangeSetsWithinWindow;
            int iterations = limit + 5;

            for (int i = 0; i < iterations; i++)
            {
                fakeTime += 20d;
                DetectAssetChangeProcessor.ProcessChangesForTesting(
                    new[] { PayloadAssetPath },
                    null,
                    null,
                    null
                );
            }

            Assert.AreEqual(
                iterations,
                TestLoopingHandler.InvocationCount,
                $"Expected {iterations} invocations (default window allows) but got {TestLoopingHandler.InvocationCount}"
            );
            LogAssert.NoUnexpectedReceived();

            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;
            ClearTestState();

            UnityHelpersSettings.instance.DetectAssetChangeLoopWindowSeconds = 45f;
            fakeTime = 0d;
            DetectAssetChangeProcessor.TimeProvider = () => fakeTime;

            LogAssert.Expect(
                LogType.Error,
                new Regex("potentially infinite asset change loop", RegexOptions.IgnoreCase)
            );

            for (int i = 0; i < iterations; i++)
            {
                fakeTime += 20d;
                DetectAssetChangeProcessor.ProcessChangesForTesting(
                    new[] { PayloadAssetPath },
                    null,
                    null,
                    null
                );
            }

            Assert.AreEqual(
                limit,
                TestLoopingHandler.InvocationCount,
                $"Expected {limit} invocations (longer window triggers limit) but got {TestLoopingHandler.InvocationCount}"
            );
            LogAssert.NoUnexpectedReceived();
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

        // Data-driven tests for method signature validation
        [TestCase(
            typeof(TestValidNoParametersHandler),
            "OnValidNoParameters",
            true,
            TestName = "SignatureValidation.NoParameters.Valid"
        )]
        [TestCase(
            typeof(TestValidContextHandler),
            "OnValidContext",
            true,
            TestName = "SignatureValidation.ContextParameter.Valid"
        )]
        [TestCase(
            typeof(TestValidDetailedHandler),
            "OnValidDetailed",
            true,
            TestName = "SignatureValidation.DetailedSignature.Valid"
        )]
        [TestCase(
            typeof(TestInvalidReturnTypeHandler),
            "OnInvalidReturnType",
            false,
            TestName = "SignatureValidation.NonVoidReturn.Invalid"
        )]
        [TestCase(
            typeof(TestInvalidParameterHandler),
            "OnInvalidSingleParameter",
            false,
            TestName = "SignatureValidation.WrongSingleParam.Invalid"
        )]
        [TestCase(
            typeof(TestInvalidCreatedParameterHandler),
            "OnInvalidCreated",
            false,
            TestName = "SignatureValidation.NonArrayCreated.Invalid"
        )]
        public void MethodSignatureValidationDataDriven(
            Type declaringType,
            string methodName,
            bool expectedValid
        )
        {
            if (!expectedValid)
            {
                // Expect error log for invalid signatures
                LogAssert.Expect(
                    LogType.Error,
                    new Regex(
                        $"{declaringType.Name}\\.{methodName}.*Supported signatures",
                        RegexOptions.Singleline
                    )
                );
            }

            bool isValid = DetectAssetChangeProcessor.ValidateMethodSignatureForTesting(
                declaringType,
                methodName
            );

            Assert.AreEqual(
                expectedValid,
                isValid,
                $"Method {declaringType.Name}.{methodName} should be {(expectedValid ? "valid" : "invalid")}"
            );
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void EmptyChangeListsDoNotTriggerHandlers()
        {
            CreatePayloadAsset();
            ClearTestState();

            // Process empty changes
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>()
            );

            Assert.AreEqual(
                0,
                TestDetectAssetChangeHandler.RecordedContexts.Count,
                "Empty change lists should not trigger handlers"
            );
        }

        [Test]
        public void NullChangeListsDoNotTriggerHandlers()
        {
            CreatePayloadAsset();
            ClearTestState();

            // Process null changes
            DetectAssetChangeProcessor.ProcessChangesForTesting(null, null, null, null);

            Assert.AreEqual(
                0,
                TestDetectAssetChangeHandler.RecordedContexts.Count,
                "Null change lists should not trigger handlers"
            );
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
            TestReentrantHandler.Clear();
            TestLoopingHandler.Clear();
            TestAssignableAssetChangeHandler.Clear();
        }
    }

    internal interface ITestDetectableContract { }

    internal sealed class TestDetectableAsset : ScriptableObject, ITestDetectableContract { }

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
        private static TestDetectableAsset[] _lastCreatedAssets =
            Array.Empty<TestDetectableAsset>();
        private static string[] _lastDeletedPaths = Array.Empty<string>();

        public static TestDetectableAsset[] LastCreatedAssets => _lastCreatedAssets;

        public static string[] LastDeletedPaths => _lastDeletedPaths;

        public static void Clear()
        {
            _lastCreatedAssets = Array.Empty<TestDetectableAsset>();
            _lastDeletedPaths = Array.Empty<string>();
        }

        [DetectAssetChanged(
            typeof(TestDetectableAsset),
            AssetChangeFlags.Created | AssetChangeFlags.Deleted
        )]
        private void OnDetailedChange(TestDetectableAsset[] createdAssets, string[] deletedPaths)
        {
            _lastCreatedAssets = createdAssets ?? Array.Empty<TestDetectableAsset>();
            _lastDeletedPaths = deletedPaths ?? Array.Empty<string>();
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

    internal sealed class TestAssignableAssetChangeHandler : ScriptableObject
    {
        private static readonly List<ITestDetectableContract> recordedCreated =
            new List<ITestDetectableContract>();
        private static readonly List<string> recordedDeletedPaths = new();

        public static IReadOnlyList<ITestDetectableContract> RecordedCreated => recordedCreated;

        public static IReadOnlyList<string> RecordedDeletedPaths => recordedDeletedPaths;

        public static void Clear()
        {
            recordedCreated.Clear();
            recordedDeletedPaths.Clear();
        }

        [DetectAssetChanged(
            typeof(ITestDetectableContract),
            AssetChangeFlags.Created | AssetChangeFlags.Deleted,
            DetectAssetChangedOptions.IncludeAssignableTypes
        )]
        private void OnAssignableAssetChanged(
            ITestDetectableContract[] createdAssets,
            string[] deletedPaths
        )
        {
            recordedCreated.Clear();
            recordedDeletedPaths.Clear();

            if (createdAssets != null)
            {
                for (int i = 0; i < createdAssets.Length; i++)
                {
                    if (createdAssets[i] != null)
                    {
                        recordedCreated.Add(createdAssets[i]);
                    }
                }
            }

            if (deletedPaths != null)
            {
                for (int i = 0; i < deletedPaths.Length; i++)
                {
                    recordedDeletedPaths.Add(deletedPaths[i]);
                }
            }
        }
    }

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

    internal static class TestReentrantHandler
    {
        private static int _invocationCount;
        private static bool _triggerNestedChange;
        private static string _watchedPath;

        public static int InvocationCount => _invocationCount;

        public static void Configure(string assetPath)
        {
            _watchedPath = assetPath;
            _triggerNestedChange = true;
        }

        public static void Clear()
        {
            _invocationCount = 0;
            _triggerNestedChange = false;
            _watchedPath = null;
        }

        [DetectAssetChanged(typeof(TestDetectableAsset))]
        private static void OnReentrantChange(AssetChangeContext context)
        {
            _invocationCount++;
            if (
                _triggerNestedChange
                && _invocationCount == 1
                && !string.IsNullOrEmpty(_watchedPath)
            )
            {
                DetectAssetChangeProcessor.ProcessChangesForTesting(
                    new[] { _watchedPath },
                    null,
                    null,
                    null
                );
            }
        }
    }

    internal static class TestLoopingHandler
    {
        private static int _invocationCount;

        public static int InvocationCount => _invocationCount;

        public static void Clear()
        {
            _invocationCount = 0;
        }

        [DetectAssetChanged(typeof(TestDetectableAsset))]
        private static void OnLoopingChange(AssetChangeContext context)
        {
            _invocationCount++;
        }
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

    // Valid handler signatures for data-driven tests
    internal sealed class TestValidNoParametersHandler : ScriptableObject
    {
        private void OnValidNoParameters()
        {
            // No-op handler with valid no-parameter signature
        }
    }

    internal sealed class TestValidContextHandler : ScriptableObject
    {
        private void OnValidContext(AssetChangeContext context)
        {
            _ = context;
        }
    }

    internal sealed class TestValidDetailedHandler : ScriptableObject
    {
        private void OnValidDetailed(TestDetectableAsset[] createdAssets, string[] deletedPaths)
        {
            _ = createdAssets;
            _ = deletedPaths;
        }
    }
}
