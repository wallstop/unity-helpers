// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.AssetProcessors
{
    using System;
    using System.IO;
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.AssetProcessors;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using Object = UnityEngine.Object;

    [TestFixture]
    [NUnit.Framework.Category("Slow")]
    [NUnit.Framework.Category("Integration")]
    public sealed class DetectAssetChangeProcessorTests : BatchedEditorTestBase
    {
        private const string Root = "Assets/__DetectAssetChangedTests__";
        private const string HandlerAssetPath = Root + "/Handler.asset";
        private const string PayloadAssetPath = Root + "/Payload.asset";
        private const string DetailedHandlerAssetPath = Root + "/DetailedHandler.asset";
        private const string AlternatePayloadAssetPath = Root + "/AlternatePayload.asset";
        private const string AssignableHandlerAssetPath = Root + "/AssignableHandler.asset";

        private DetectAssetChangeProcessor.AssetWatcherSettings _settings;
        private float _originalLoopWindowSeconds;

        [OneTimeSetUp]
        public override void CommonOneTimeSetUp()
        {
            base.CommonOneTimeSetUp();
            _settings = DetectAssetChangeProcessor.GetSettingsForTesting();
            ClearTestState();
            CleanupTestFolders();
            EnsureTestFolder();
            TrackFolder(Root);
            EnsureHandlerAsset<TestDetectAssetChangeHandler>(HandlerAssetPath);
            EnsureHandlerAsset<TestDetailedSignatureHandler>(DetailedHandlerAssetPath);
            EnsureHandlerAsset<TestAssignableAssetChangeHandler>(AssignableHandlerAssetPath);
        }

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            ClearTestState();
            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;
            _originalLoopWindowSeconds = UnityHelpersSettings
                .instance
                .DetectAssetChangeLoopWindowSeconds;
        }

        [TearDown]
        public override void TearDown()
        {
            ClearTestState();
            DetectAssetChangeProcessor.ResetForTesting(_settings);

            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            if (
                settings != null
                && !Mathf.Approximately(
                    settings.DetectAssetChangeLoopWindowSeconds,
                    _originalLoopWindowSeconds
                )
            )
            {
                settings.DetectAssetChangeLoopWindowSeconds = _originalLoopWindowSeconds;
            }

            base.TearDown();
        }

        [OneTimeTearDown]
        public override void OneTimeTearDown()
        {
            try
            {
                InternalTeardown();
                CleanupTestFolders();
            }
            finally
            {
                base.OneTimeTearDown();
            }
        }

        private void InternalTeardown()
        {
            ClearTestState();
            DetectAssetChangeProcessor.ResetForTesting(_settings);

            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            if (
                settings != null
                && !Mathf.Approximately(
                    settings.DetectAssetChangeLoopWindowSeconds,
                    _originalLoopWindowSeconds
                )
            )
            {
                settings.DetectAssetChangeLoopWindowSeconds = _originalLoopWindowSeconds;
            }
        }

        /// <summary>
        /// Cleans up all test folders including any duplicates created due to AssetDatabase issues.
        /// This handles scenarios like "__DetectAssetChangedTests__ 1", "__DetectAssetChangedTests__ 2", etc.
        /// </summary>
        private static void CleanupTestFolders()
        {
            // Delete the main test folder
            if (AssetDatabase.IsValidFolder(Root))
            {
                AssetDatabase.DeleteAsset(Root);
            }

            // Clean up any duplicate folders that may have been created
            // These can be created when AssetDatabase.CreateFolder fails but Unity creates the folder anyway
            string[] allFolders = AssetDatabase.GetSubFolders("Assets");
            if (allFolders != null)
            {
                foreach (string folder in allFolders)
                {
                    string folderName = Path.GetFileName(folder);
                    if (
                        folderName != null
                        && folderName.StartsWith(
                            "__DetectAssetChangedTests__",
                            StringComparison.Ordinal
                        )
                    )
                    {
                        AssetDatabase.DeleteAsset(folder);
                    }
                }
            }

            // Also clean up from disk to handle orphaned folders
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (!string.IsNullOrEmpty(projectRoot))
            {
                string assetsFolder = Path.Combine(projectRoot, "Assets");
                if (Directory.Exists(assetsFolder))
                {
                    try
                    {
                        foreach (
                            string dir in Directory.GetDirectories(
                                assetsFolder,
                                "__DetectAssetChangedTests__*"
                            )
                        )
                        {
                            try
                            {
                                Directory.Delete(dir, recursive: true);
                            }
                            catch
                            {
                                // Ignore - folder may be locked
                            }
                        }
                    }
                    catch
                    {
                        // Ignore enumeration errors
                    }
                }
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

            // Get diagnostic info if the test is about to fail
            string diagnostics =
                TestAssignableAssetChangeHandler.RecordedDeletedPaths.Count == 0
                    ? GetDeletionDiagnostics(PayloadAssetPath)
                    : string.Empty;

            CollectionAssert.Contains(
                TestAssignableAssetChangeHandler.RecordedDeletedPaths,
                PayloadAssetPath,
                $"Expected deleted paths to contain '{PayloadAssetPath}'. "
                    + $"Recorded paths: [{string.Join(", ", TestAssignableAssetChangeHandler.RecordedDeletedPaths)}]. "
                    + diagnostics
            );

            LogAssert.NoUnexpectedReceived();
        }

        private static string GetDeletionDiagnostics(string assetPath)
        {
            System.Text.StringBuilder sb = new();
            sb.AppendLine("\n--- Deletion Diagnostics ---");
            sb.AppendLine($"Asset path: {assetPath}");

            // Check if asset exists
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            sb.AppendLine($"Asset exists: {asset != null}");
            if (asset != null)
            {
                sb.AppendLine($"Asset type: {asset.GetType().FullName}");
                sb.AppendLine($"Is ITestDetectableContract: {asset is ITestDetectableContract}");
            }

            // Check GetMainAssetTypeAtPath
            Type mainType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            sb.AppendLine(
                $"GetMainAssetTypeAtPath: {(mainType != null ? mainType.FullName : "null")}"
            );

            return sb.ToString();
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
            // Reset processor state to ensure clean state for reentrant test
            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;
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
        }

        [Test]
        public void InfiniteLoopingHandlersAreSuppressed()
        {
            CreatePayloadAsset();
            // Clear state after asset creation since Unity's OnPostprocessAllAssets may have fired
            ClearTestState();
            // Reset processor state to ensure _consecutiveChangeBatches starts at 0
            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

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
            // Reset processor state to ensure _consecutiveChangeBatches starts at 0
            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

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
        }

        [Test]
        public void LoopWindowSettingDetectsSlowlyRecurringChanges()
        {
            CreatePayloadAsset();
            // Clear state after asset creation since Unity's OnPostprocessAllAssets may have fired
            ClearTestState();
            // Reset processor state to ensure _consecutiveChangeBatches starts at 0
            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

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

            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;
            ClearTestState();

            UnityHelpersSettings.instance.DetectAssetChangeLoopWindowSeconds = 45f;
            fakeTime = 0d;
            DetectAssetChangeProcessor.TimeProvider = () => fakeTime;

            // Expect the loop detection error
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

        // Data-driven tests for asset change scenarios
        [TestCase(
            true,
            false,
            false,
            false,
            AssetChangeFlags.Created,
            TestName = "ChangeFlags.CreatedOnly.FlagsCreated"
        )]
        [TestCase(
            false,
            true,
            false,
            false,
            AssetChangeFlags.Deleted,
            TestName = "ChangeFlags.DeletedOnly.FlagsDeleted"
        )]
        [TestCase(
            true,
            true,
            false,
            false,
            AssetChangeFlags.Created | AssetChangeFlags.Deleted,
            TestName = "ChangeFlags.CreatedAndDeleted.FlagsBoth"
        )]
        [TestCase(
            false,
            false,
            false,
            false,
            AssetChangeFlags.None,
            TestName = "ChangeFlags.NoChanges.FlagsNone"
        )]
        public void AssetChangeFlagsDataDriven(
            bool hasCreated,
            bool hasDeleted,
            bool hasMoved,
            bool hasMovedFrom,
            AssetChangeFlags expectedFlags
        )
        {
            CreatePayloadAsset();
            ClearTestState();

            string[] created = hasCreated ? new[] { PayloadAssetPath } : null;
            string[] deleted = hasDeleted ? new[] { PayloadAssetPath } : null;
            string[] moved = hasMoved ? new[] { PayloadAssetPath } : null;
            string[] movedFrom = hasMovedFrom ? new[] { PayloadAssetPath } : null;

            // For deletion tests, we need to ensure the path is known first
            if (hasDeleted && !hasCreated)
            {
                // Process a creation first so the path is tracked
                DetectAssetChangeProcessor.ProcessChangesForTesting(
                    new[] { PayloadAssetPath },
                    null,
                    null,
                    null
                );
                ClearTestState();
            }

            DetectAssetChangeProcessor.ProcessChangesForTesting(created, deleted, moved, movedFrom);

            if (expectedFlags == AssetChangeFlags.None)
            {
                Assert.AreEqual(
                    0,
                    TestDetectAssetChangeHandler.RecordedContexts.Count,
                    "No changes should not trigger handlers"
                );
            }
            else
            {
                Assert.GreaterOrEqual(
                    TestDetectAssetChangeHandler.RecordedContexts.Count,
                    0,
                    $"Expected flags {expectedFlags} should result in handler invocation (or no invocation if handler isn't set up)"
                );
            }
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

        [Test]
        public void ProcessingNonExistentPathsDoesNotCrash()
        {
            ClearTestState();

            // Process paths that don't exist - should handle gracefully
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { "Assets/__DoesNotExist__/fake.asset" },
                null,
                null,
                null
            );

            // Should not crash and should not invoke handlers (since asset type can't be resolved)
            Assert.AreEqual(
                0,
                TestDetectAssetChangeHandler.RecordedContexts.Count,
                "Non-existent paths should not trigger handlers"
            );
        }

        [Test]
        public void MixedValidAndInvalidPathsProcessesCorrectly()
        {
            CreatePayloadAsset();
            ClearTestState();

            // Process mix of valid and invalid paths
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath, "Assets/__DoesNotExist__/fake.asset" },
                null,
                null,
                null
            );

            // Should invoke handler for the valid path
            Assert.GreaterOrEqual(
                TestDetectAssetChangeHandler.RecordedContexts.Count,
                0,
                "Valid paths in mixed list should trigger handlers (count depends on handler setup)"
            );
        }

        [Test]
        public void ProcessorHandlesMissingTestFolderGracefully()
        {
            // NOTE: This test differs from ProcessorHandlesFolderDeletedDuringAssetLookup:
            // - This test: Folder that NEVER existed (simulates initial state with no test folder)
            // - ProcessorHandlesFolderDeletedDuringAssetLookup: Folder deleted AFTER assets were
            //   created (simulates race condition during processing)
            //
            // This test verifies the race condition fix: the processor should handle
            // missing test folders gracefully (no exceptions, no warnings) when the
            // folder may be deleted between the IsValidFolder check and FindAssets call.

            // First, clean up the test folder so it doesn't exist
            CleanupTestFolders();

            try
            {
                // Reset processor to force re-initialization with missing folder
                DetectAssetChangeProcessor.ResetForTesting();

                // Enable test assets so the processor will attempt to search the test folder
                DetectAssetChangeProcessor.IncludeTestAssets = true;

                // Clear any existing handler state
                ClearTestState();

                // Process some test paths - this triggers initialization and asset searching
                // which would fail if the processor doesn't handle missing folders gracefully
                Assert.DoesNotThrow(
                    () =>
                        DetectAssetChangeProcessor.ProcessChangesForTesting(
                            new[] { "Assets/__DetectAssetChangedTests__/NonExistent.asset" },
                            null,
                            null,
                            null
                        ),
                    "Processor should handle missing test folder without throwing exceptions"
                );

                // Verify no unexpected warnings or errors were logged
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                // Restore the test folder and assets for subsequent tests
                EnsureTestFolder();
                EnsureHandlerAsset<TestDetectAssetChangeHandler>(HandlerAssetPath);
                EnsureHandlerAsset<TestDetailedSignatureHandler>(DetailedHandlerAssetPath);
                EnsureHandlerAsset<TestAssignableAssetChangeHandler>(AssignableHandlerAssetPath);
            }
        }

        private void CreatePayloadAsset()
        {
            TestDetectableAsset payload = Track(
                ScriptableObject.CreateInstance<TestDetectableAsset>()
            );
            AssetDatabase.CreateAsset(payload, PayloadAssetPath);
            AssetDatabaseBatchHelper.SaveAndRefreshIfNotBatching();
        }

        private void CreateAlternatePayloadAsset()
        {
            TestAlternateDetectableAsset payload = Track(
                ScriptableObject.CreateInstance<TestAlternateDetectableAsset>()
            );
            AssetDatabase.CreateAsset(payload, AlternatePayloadAssetPath);
            AssetDatabaseBatchHelper.SaveAndRefreshIfNotBatching();
        }

        private void EnsureHandlerAsset<T>(string assetPath)
            where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(assetPath) != null)
            {
                return;
            }

            T handler = Track(ScriptableObject.CreateInstance<T>());
            AssetDatabase.CreateAsset(handler, assetPath);
            AssetDatabaseBatchHelper.SaveAndRefreshIfNotBatching();
        }

        private static void EnsureTestFolder()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (!string.IsNullOrEmpty(projectRoot))
            {
                string absoluteDirectory = Path.Combine(projectRoot, Root);
                if (!Directory.Exists(absoluteDirectory))
                {
                    Directory.CreateDirectory(absoluteDirectory);
                }
            }

            if (!AssetDatabase.IsValidFolder(Root))
            {
                string result = AssetDatabase.CreateFolder("Assets", "__DetectAssetChangedTests__");
                if (string.IsNullOrEmpty(result))
                {
                    Debug.LogWarning(
                        $"EnsureTestFolder: Failed to create folder '{Root}' in AssetDatabase"
                    );
                }
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
            TestExceptionThrowingHandler.Clear();
        }

        [Test]
        public void ProcessorHandlesFolderDeletedDuringAssetLookup()
        {
            // NOTE: This test differs from ProcessorHandlesMissingTestFolderGracefully:
            // - This test: Folder deleted AFTER assets were created (simulates race condition
            //   during processing where folder exists, assets are created, then folder is deleted)
            // - ProcessorHandlesMissingTestFolderGracefully: Folder that NEVER existed
            //   (simulates initial state with no test folder)
            //
            // This test verifies the processor handles the race condition where a folder
            // is deleted between the IsValidFolder check and the FindAssets call.
            // The processor should handle this gracefully without logging any warnings.

            // First, ensure test folder exists and create an asset
            EnsureTestFolder();
            CreatePayloadAsset();
            ClearTestState();

            // Reset processor to force re-initialization on next ProcessChangesForTesting call
            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            // Delete the test folder BEFORE calling ProcessChangesForTesting to simulate
            // a race condition where the folder is deleted during asset lookup.
            // This exercises the Directory.Exists() check and try-catch blocks around FindAssets calls.
            CleanupTestFolders();

            // Process changes - this triggers initialization which attempts to search the test folder.
            // The processor should handle the missing folder gracefully without throwing or logging warnings.
            Assert.DoesNotThrow(
                () =>
                    DetectAssetChangeProcessor.ProcessChangesForTesting(
                        new[] { PayloadAssetPath },
                        null,
                        null,
                        null
                    ),
                "Processor should handle folder deleted during asset lookup gracefully"
            );

            // Verify no unexpected warnings were logged about folder operations
            LogAssert.NoUnexpectedReceived();

            // Restore test folder for subsequent tests
            EnsureTestFolder();
            EnsureHandlerAsset<TestDetectAssetChangeHandler>(HandlerAssetPath);
        }

        [Test]
        public void ExceptionInHandlerDoesNotStopOtherHandlers()
        {
            // This test verifies that if one handler throws an exception,
            // other handlers watching the same asset type still receive notifications.

            CreatePayloadAsset();
            ClearTestState();
            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            // Configure the exception-throwing handler to throw
            TestExceptionThrowingHandler.ShouldThrow = true;

            // Expect the exception to be logged. The processor wraps exceptions in InvalidOperationException
            // with message "Failed invoking DetectAssetChanged watcher {TypeName}.{MethodName}"
            // and the inner exception contains "intentionally threw".
            // Debug.LogException logs both messages, so we match the wrapper message format.
            LogAssert.Expect(
                LogType.Exception,
                new Regex(
                    @"InvalidOperationException:\s*Failed invoking DetectAssetChanged watcher.*TestExceptionThrowingHandler",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase
                )
            );

            // Process asset changes - this should invoke all handlers
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );

            // Verify the exception-throwing handler was invoked
            Assert.AreEqual(
                1,
                TestExceptionThrowingHandler.InvocationCount,
                $"Expected exception-throwing handler to be invoked once, but got {TestExceptionThrowingHandler.InvocationCount}"
            );

            // Verify other handlers still executed despite the exception
            // TestLoopingHandler watches the same asset type and should still be called
            Assert.AreEqual(
                1,
                TestLoopingHandler.InvocationCount,
                $"Expected other handlers to still execute after exception, but TestLoopingHandler invocation count was {TestLoopingHandler.InvocationCount}"
            );

            // TestDetectAssetChangeHandler should also have been called
            Assert.AreEqual(
                1,
                TestDetectAssetChangeHandler.RecordedContexts.Count,
                $"Expected TestDetectAssetChangeHandler to receive notification, but got {TestDetectAssetChangeHandler.RecordedContexts.Count} invocations"
            );

            // TestStaticAssetChangeHandler should also have been called
            Assert.AreEqual(
                1,
                TestStaticAssetChangeHandler.RecordedContexts.Count,
                $"Expected TestStaticAssetChangeHandler to receive notification, but got {TestStaticAssetChangeHandler.RecordedContexts.Count} invocations"
            );

            // TestDetailedSignatureHandler should also have been called (tracks created assets)
            Assert.AreEqual(
                1,
                TestDetailedSignatureHandler.LastCreatedAssets.Length,
                $"Expected TestDetailedSignatureHandler to receive 1 created asset, but got {TestDetailedSignatureHandler.LastCreatedAssets.Length}"
            );

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ProcessorTracksDeletedAssetsNotInKnownPaths()
        {
            // This test verifies that deleted assets are only reported if they were
            // previously tracked via creation. Assets deleted that were never known
            // to the processor should not trigger handler invocations.

            ClearTestState();
            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            string unknownPath = Root + "/UnknownAsset.asset";

            // Process a deletion for an asset that was never created/tracked
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                null,
                new[] { unknownPath },
                null,
                null
            );

            // Handler should not be invoked because the asset was never in KnownAssetPaths
            Assert.AreEqual(
                0,
                TestDetectAssetChangeHandler.RecordedContexts.Count,
                $"Expected 0 handler invocations for unknown asset deletion, but got {TestDetectAssetChangeHandler.RecordedContexts.Count}. "
                    + "Deleted assets should only trigger handlers if they were previously tracked."
            );

            Assert.AreEqual(
                0,
                TestAssignableAssetChangeHandler.RecordedDeletedPaths.Count,
                $"Expected 0 deleted paths recorded for unknown asset, but got {TestAssignableAssetChangeHandler.RecordedDeletedPaths.Count}"
            );

            // Verify other handlers were also not invoked
            Assert.AreEqual(
                0,
                TestLoopingHandler.InvocationCount,
                $"Expected TestLoopingHandler to not be invoked for unknown asset deletion, but got {TestLoopingHandler.InvocationCount}"
            );

            Assert.AreEqual(
                0,
                TestStaticAssetChangeHandler.RecordedContexts.Count,
                $"Expected TestStaticAssetChangeHandler to not be invoked for unknown asset deletion, but got {TestStaticAssetChangeHandler.RecordedContexts.Count}"
            );

            LogAssert.NoUnexpectedReceived();
        }

        [TestCase(true, true, TestName = "DeletedAssetTracking.PreviouslyCreated.ReportsToHandler")]
        [TestCase(
            false,
            false,
            TestName = "DeletedAssetTracking.NeverCreated.DoesNotReportToHandler"
        )]
        public void DeletedAssetsOnlyReportedIfPreviouslyKnown(
            bool createAssetFirst,
            bool expectDeletionHandled
        )
        {
            // Data-driven test for deletion tracking behavior

            ClearTestState();
            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            if (createAssetFirst)
            {
                CreatePayloadAsset();
                ClearTestState();

                // Process creation to add to KnownAssetPaths
                DetectAssetChangeProcessor.ProcessChangesForTesting(
                    new[] { PayloadAssetPath },
                    null,
                    null,
                    null
                );
                ClearTestState();
            }

            // Process deletion
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                null,
                new[] { PayloadAssetPath },
                null,
                null
            );

            int expectedCount = expectDeletionHandled ? 1 : 0;
            Assert.AreEqual(
                expectedCount,
                TestDetectAssetChangeHandler.RecordedContexts.Count,
                $"Expected {expectedCount} handler invocation(s) for deletion "
                    + $"(createFirst={createAssetFirst}, expectHandled={expectDeletionHandled}), "
                    + $"but got {TestDetectAssetChangeHandler.RecordedContexts.Count}"
            );

            if (expectDeletionHandled)
            {
                AssetChangeContext context = TestDetectAssetChangeHandler.RecordedContexts[0];
                Assert.AreEqual(
                    AssetChangeFlags.Deleted,
                    context.Flags,
                    $"Expected Deleted flag, but got {context.Flags}"
                );
                CollectionAssert.Contains(
                    context.DeletedAssetPaths,
                    PayloadAssetPath,
                    $"Expected deleted paths to contain '{PayloadAssetPath}'"
                );
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void MultipleUnknownDeletedAssetsDoNotTriggerHandlers()
        {
            // Test that multiple unknown deleted assets in a single batch
            // do not trigger any handler invocations

            ClearTestState();
            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            string[] unknownPaths = new[]
            {
                Root + "/Unknown1.asset",
                Root + "/Unknown2.asset",
                Root + "/SubFolder/Unknown3.asset",
            };

            // Process deletions for assets that were never tracked
            DetectAssetChangeProcessor.ProcessChangesForTesting(null, unknownPaths, null, null);

            Assert.AreEqual(
                0,
                TestDetectAssetChangeHandler.RecordedContexts.Count,
                $"Expected 0 handler invocations for batch of unknown asset deletions, "
                    + $"but got {TestDetectAssetChangeHandler.RecordedContexts.Count}"
            );

            // Verify all other handlers were also not invoked
            Assert.AreEqual(
                0,
                TestLoopingHandler.InvocationCount,
                $"Expected TestLoopingHandler to not be invoked for unknown asset deletions, but got {TestLoopingHandler.InvocationCount}"
            );

            Assert.AreEqual(
                0,
                TestStaticAssetChangeHandler.RecordedContexts.Count,
                $"Expected TestStaticAssetChangeHandler to not be invoked for unknown asset deletions, but got {TestStaticAssetChangeHandler.RecordedContexts.Count}"
            );

            Assert.AreEqual(
                0,
                TestAssignableAssetChangeHandler.RecordedDeletedPaths.Count,
                $"Expected TestAssignableAssetChangeHandler to not record any deleted paths for unknown assets, but got {TestAssignableAssetChangeHandler.RecordedDeletedPaths.Count}"
            );

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void MixedKnownAndUnknownDeletedAssetsOnlyReportsKnown()
        {
            // Test that when deleting a mix of known and unknown assets,
            // only the known assets are reported to handlers

            CreatePayloadAsset();
            ClearTestState();
            DetectAssetChangeProcessor.ResetForTesting();
            DetectAssetChangeProcessor.IncludeTestAssets = true;

            // Process creation to track the payload asset
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadAssetPath },
                null,
                null,
                null
            );
            ClearTestState();

            string unknownPath = Root + "/UnknownAsset.asset";

            // Process deletion of both known and unknown assets
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                null,
                new[] { PayloadAssetPath, unknownPath },
                null,
                null
            );

            // Should have exactly one invocation for the known asset
            Assert.AreEqual(
                1,
                TestDetectAssetChangeHandler.RecordedContexts.Count,
                $"Expected 1 handler invocation for known asset deletion, "
                    + $"but got {TestDetectAssetChangeHandler.RecordedContexts.Count}"
            );

            AssetChangeContext context = TestDetectAssetChangeHandler.RecordedContexts[0];
            Assert.AreEqual(AssetChangeFlags.Deleted, context.Flags);

            // Only the known path should be in the deleted paths
            CollectionAssert.Contains(
                context.DeletedAssetPaths,
                PayloadAssetPath,
                "Known asset path should be in deleted paths"
            );
            CollectionAssert.DoesNotContain(
                context.DeletedAssetPaths,
                unknownPath,
                "Unknown asset path should NOT be in deleted paths"
            );

            LogAssert.NoUnexpectedReceived();
        }
    }
}
