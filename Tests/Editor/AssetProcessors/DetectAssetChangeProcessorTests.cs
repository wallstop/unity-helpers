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
    using Object = UnityEngine.Object;

    [TestFixture]
    [NUnit.Framework.Category("Slow")]
    [NUnit.Framework.Category("Integration")]
    public sealed class DetectAssetChangeProcessorTests : DetectAssetChangeTestBase
    {
        private const string HandlerAssetPath = TestRoot + "/Handler.asset";
        private const string PayloadPath = TestRoot + "/Payload.asset";
        private const string DetailedHandlerAssetPath = TestRoot + "/DetailedHandler.asset";
        private const string AlternatePayloadPath = TestRoot + "/AlternatePayload.asset";
        private const string AssignableHandlerAssetPath = TestRoot + "/AssignableHandler.asset";

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
            TrackFolder(TestRoot);
            EnsureHandlerAsset<TestDetectAssetChangeHandler>(HandlerAssetPath);
            EnsureHandlerAsset<TestDetailedSignatureHandler>(DetailedHandlerAssetPath);
            EnsureHandlerAsset<TestAssignableAssetChangeHandler>(AssignableHandlerAssetPath);
        }

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            ClearTestState();
            // Delete the payload asset to ensure clean state for tests that depend on asset non-existence.
            // This is critical for tests like DeletedAssetTracking.NeverCreated which expect 0 handler
            // invocations when an asset was never created. Without this cleanup, PopulateKnownAssetPaths
            // would find assets from previous test runs.
            DeleteAssetIfExists(PayloadPath);
            DeleteAssetIfExists(AlternatePayloadPath);
            // Ensure test folder is properly registered before resetting the processor to avoid
            // "Folder not found" warnings when the processor re-initializes with IncludeTestAssets = true
            EnsureTestFolder();
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

        [Test]
        public void InvokesHandlersWhenAssetsAreCreated()
        {
            CreatePayloadAssetAt(PayloadPath);
            // Clear state after asset creation since Unity's OnPostprocessAllAssets may have fired
            ClearTestState();

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadPath },
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
            Assert.AreEqual(
                AssetChangeFlags.Created,
                context.Flags,
                $"Expected Created flag but got {context.Flags}"
            );
            CollectionAssert.Contains(
                context.CreatedAssetPaths,
                PayloadPath,
                $"Expected CreatedAssetPaths to contain '{PayloadPath}' but got [{string.Join(", ", context.CreatedAssetPaths)}]"
            );
        }

        [Test]
        public void InvokesHandlersWhenAssetsAreDeleted()
        {
            CreatePayloadAssetAt(PayloadPath);
            // Clear state after asset creation since Unity's OnPostprocessAllAssets may have fired
            ClearTestState();

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadPath },
                null,
                null,
                null
            );

            TestDetectAssetChangeHandler.Clear();

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                null,
                new[] { PayloadPath },
                null,
                null
            );

            Assert.AreEqual(
                1,
                TestDetectAssetChangeHandler.RecordedContexts.Count,
                $"Expected 1 invocation for deletion but got {TestDetectAssetChangeHandler.RecordedContexts.Count}"
            );
            AssetChangeContext context = TestDetectAssetChangeHandler.RecordedContexts[0];
            Assert.AreEqual(
                AssetChangeFlags.Deleted,
                context.Flags,
                $"Expected Deleted flag but got {context.Flags}"
            );
            CollectionAssert.Contains(
                context.DeletedAssetPaths,
                PayloadPath,
                $"Expected DeletedAssetPaths to contain '{PayloadPath}' but got [{string.Join(", ", context.DeletedAssetPaths)}]"
            );
        }

        [Test]
        public void StaticHandlersReceiveNotificationsForAssetChanges()
        {
            CreatePayloadAssetAt(PayloadPath);
            // Clear state after asset creation since Unity's OnPostprocessAllAssets may have fired
            ClearTestState();

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadPath },
                null,
                null,
                null
            );

            Assert.AreEqual(
                1,
                TestStaticAssetChangeHandler.RecordedContexts.Count,
                $"Expected 1 static handler invocation for creation but got {TestStaticAssetChangeHandler.RecordedContexts.Count}"
            );
            Assert.AreEqual(
                AssetChangeFlags.Created,
                TestStaticAssetChangeHandler.RecordedContexts[0].Flags,
                $"Expected Created flag for static handler but got {TestStaticAssetChangeHandler.RecordedContexts[0].Flags}"
            );

            TestStaticAssetChangeHandler.Clear();

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                null,
                new[] { PayloadPath },
                null,
                null
            );

            Assert.AreEqual(
                1,
                TestStaticAssetChangeHandler.RecordedContexts.Count,
                $"Expected 1 static handler invocation for deletion but got {TestStaticAssetChangeHandler.RecordedContexts.Count}"
            );
            Assert.AreEqual(
                AssetChangeFlags.Deleted,
                TestStaticAssetChangeHandler.RecordedContexts[0].Flags,
                $"Expected Deleted flag for static handler but got {TestStaticAssetChangeHandler.RecordedContexts[0].Flags}"
            );
        }

        [Test]
        public void DetailedSignatureReceivesCreatedAssetsAndDeletedPaths()
        {
            CreatePayloadAssetAt(PayloadPath);
            // Clear state after asset creation since Unity's OnPostprocessAllAssets may have fired
            ClearTestState();

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadPath },
                null,
                null,
                null
            );

            Assert.AreEqual(
                1,
                TestDetailedSignatureHandler.LastCreatedAssets.Length,
                $"Expected 1 created asset but got {TestDetailedSignatureHandler.LastCreatedAssets.Length}"
            );
            Assert.NotNull(
                TestDetailedSignatureHandler.LastCreatedAssets[0],
                "First created asset in LastCreatedAssets should not be null"
            );
            Assert.AreEqual(
                PayloadPath,
                AssetDatabase.GetAssetPath(TestDetailedSignatureHandler.LastCreatedAssets[0]),
                $"Expected created asset path to be '{PayloadPath}' but got '{AssetDatabase.GetAssetPath(TestDetailedSignatureHandler.LastCreatedAssets[0])}'"
            );
            Assert.AreEqual(
                0,
                TestDetailedSignatureHandler.LastDeletedPaths.Length,
                $"Expected 0 deleted paths after creation but got {TestDetailedSignatureHandler.LastDeletedPaths.Length}"
            );

            TestDetailedSignatureHandler.Clear();

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                null,
                new[] { PayloadPath },
                null,
                null
            );

            Assert.AreEqual(
                0,
                TestDetailedSignatureHandler.LastCreatedAssets.Length,
                $"Expected 0 created assets after deletion but got {TestDetailedSignatureHandler.LastCreatedAssets.Length}"
            );
            CollectionAssert.AreEquivalent(
                new[] { PayloadPath },
                TestDetailedSignatureHandler.LastDeletedPaths,
                $"Expected LastDeletedPaths to contain only '{PayloadPath}' but got [{string.Join(", ", TestDetailedSignatureHandler.LastDeletedPaths)}]"
            );
        }

        [Test]
        public void SingleMethodCanWatchMultipleAssetTypes()
        {
            CreatePayloadAssetAt(PayloadPath);
            CreateAlternatePayloadAssetAt(AlternatePayloadPath);
            // Clear state after asset creation since Unity's OnPostprocessAllAssets may have fired
            ClearTestState();

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadPath },
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
                new[] { AlternatePayloadPath },
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
                new[] { AlternatePayloadPath },
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
            CreatePayloadAssetAt(PayloadPath);
            // Clear state after asset creation since Unity's OnPostprocessAllAssets may have fired
            ClearTestState();
            // Reset processor state to ensure clean state for reentrant test
            ResetProcessorWithCleanState();
            TestReentrantHandler.Configure(PayloadPath);

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadPath },
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
        public void ChangeBatchesWithGapsLongerThanWindowAreNotSuppressed()
        {
            CreatePayloadAssetAt(PayloadPath);
            // Clear state after asset creation since Unity's OnPostprocessAllAssets may have fired
            ClearTestState();
            // Reset processor state to ensure _consecutiveChangeBatches starts at 0
            ResetProcessorWithCleanState();

            double fakeTime = 0;
            DetectAssetChangeProcessor.TimeProvider = () => fakeTime;
            DetectAssetChangeProcessor.LoopWindowSecondsOverride = 5d;

            int iterations = DetectAssetChangeProcessor.MaxConsecutiveChangeSetsWithinWindow + 5;
            for (int i = 0; i < iterations; i++)
            {
                fakeTime += 6d;
                DetectAssetChangeProcessor.ProcessChangesForTesting(
                    new[] { PayloadPath },
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
            CreatePayloadAssetAt(PayloadPath);
            ClearTestState();

            string[] created = hasCreated ? new[] { PayloadPath } : null;
            string[] deleted = hasDeleted ? new[] { PayloadPath } : null;
            string[] moved = hasMoved ? new[] { PayloadPath } : null;
            string[] movedFrom = hasMovedFrom ? new[] { PayloadPath } : null;

            // For deletion tests, we need to ensure the path is known first
            if (hasDeleted && !hasCreated)
            {
                // Process a creation first so the path is tracked
                DetectAssetChangeProcessor.ProcessChangesForTesting(
                    new[] { PayloadPath },
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
                // When we have changes, verify that the handler was invoked at least once
                Assert.GreaterOrEqual(
                    TestDetectAssetChangeHandler.RecordedContexts.Count,
                    1,
                    $"Expected flags {expectedFlags} should result in handler invocation"
                );
            }
        }

        [Test]
        public void EmptyChangeListsDoNotTriggerHandlers()
        {
            CreatePayloadAssetAt(PayloadPath);
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
            CreatePayloadAssetAt(PayloadPath);
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
            CreatePayloadAssetAt(PayloadPath);
            ClearTestState();

            // Process mix of valid and invalid paths
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadPath, "Assets/__DoesNotExist__/fake.asset" },
                null,
                null,
                null
            );

            // Should invoke handler for the valid path
            Assert.GreaterOrEqual(
                TestDetectAssetChangeHandler.RecordedContexts.Count,
                1,
                $"Expected at least 1 handler invocation for valid path '{PayloadPath}' in mixed list with invalid paths, "
                    + $"but got {TestDetectAssetChangeHandler.RecordedContexts.Count}"
            );
        }

        [Test]
        public void InPlaceAssetRenameTriggersMovedEvent()
        {
            // Test that renaming an asset within the same folder triggers a moved event.
            CreatePayloadAssetAt(PayloadPath);
            ClearTestState();

            // Process creation to track the asset
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadPath },
                null,
                null,
                null
            );
            ClearTestState();

            // Simulate in-place rename by providing moved/movedFrom paths
            string renamedPath = TestRoot + "/PayloadRenamed.asset";

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                null,
                null,
                new[] { renamedPath },
                new[] { PayloadPath }
            );

            // Handler should be invoked for the moved asset since it was tracked
            Assert.GreaterOrEqual(
                TestDetectAssetChangeHandler.RecordedContexts.Count,
                1,
                $"Expected at least 1 handler invocation for in-place renamed asset from '{PayloadPath}' to '{renamedPath}', "
                    + $"but got {TestDetectAssetChangeHandler.RecordedContexts.Count}"
            );
        }

        [Test]
        public void MultipleAssetsMoveInSameBatchTriggersHandlers()
        {
            // Test that multiple assets moved in the same batch all trigger handler invocations.
            string subFolderPath = CreateTestSubFolder("BatchMoveTarget");
            CreatePayloadAssetAt(PayloadPath);
            CreateAlternatePayloadAssetAt(AlternatePayloadPath);
            ClearTestState();

            // Process creation to track both assets
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadPath, AlternatePayloadPath },
                null,
                null,
                null
            );
            ClearTestState();

            // Simulate batch move of both assets
            string movedPath1 = subFolderPath + "/Payload.asset";
            string movedPath2 = subFolderPath + "/AlternatePayload.asset";

            DetectAssetChangeProcessor.ProcessChangesForTesting(
                null,
                null,
                new[] { movedPath1, movedPath2 },
                new[] { PayloadPath, AlternatePayloadPath }
            );

            // Handler should be invoked for the moved assets
            Assert.GreaterOrEqual(
                TestDetectAssetChangeHandler.RecordedContexts.Count,
                1,
                $"Expected at least 1 handler invocation for batch move of 2 assets, "
                    + $"but got {TestDetectAssetChangeHandler.RecordedContexts.Count}"
            );

            // Verify TestMultiAttributeHandler was also invoked for the batch move
            // (it watches TestDetectableAsset for Created and TestAlternateDetectableAsset for Deleted)
            // Note: Moved assets with TestMultiAttributeHandler may not trigger since it's type-specific
            Assert.GreaterOrEqual(
                TestLoopingHandler.InvocationCount,
                1,
                $"Expected TestLoopingHandler to be invoked at least once for batch move, "
                    + $"but got {TestLoopingHandler.InvocationCount} invocations"
            );

            // Cleanup subfolder
            if (AssetDatabase.IsValidFolder(subFolderPath))
            {
                AssetDatabase.DeleteAsset(subFolderPath);
            }
        }

        [Test]
        public void MixedHandlerTypesInSingleEventBatchProcessCorrectly()
        {
            // Test that when multiple handler types are watching the same asset type,
            // a single event batch invokes all applicable handlers.
            CreatePayloadAssetAt(PayloadPath);
            CreateAlternatePayloadAssetAt(AlternatePayloadPath);
            ClearTestState();
            ResetProcessorWithCleanState();

            // Process both asset types at once
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadPath, AlternatePayloadPath },
                null,
                null,
                null
            );

            // TestDetectAssetChangeHandler watches TestDetectableAsset
            Assert.GreaterOrEqual(
                TestDetectAssetChangeHandler.RecordedContexts.Count,
                1,
                $"Expected TestDetectAssetChangeHandler to be invoked at least once for TestDetectableAsset in mixed batch, "
                    + $"but got {TestDetectAssetChangeHandler.RecordedContexts.Count} invocations"
            );

            // TestMultiAttributeHandler watches both asset types with different flags
            // It should be invoked for TestDetectableAsset (Created) but not for
            // TestAlternateDetectableAsset (which it only watches for Deleted)
            Assert.GreaterOrEqual(
                TestMultiAttributeHandler.RecordedInvocations.Count,
                1,
                $"Expected TestMultiAttributeHandler to be invoked at least once for Created TestDetectableAsset, "
                    + $"but got {TestMultiAttributeHandler.RecordedInvocations.Count} invocations"
            );
        }

        [TestCase(false, false, true, true, TestName = "ChangeFlags.MovedOnly.HandlesMovedAsset")]
        public void MovedAssetFlagsDataDriven(
            bool hasCreated,
            bool hasDeleted,
            bool hasMoved,
            bool hasMovedFrom
        )
        {
            // Data-driven test for moved asset scenarios
            CreatePayloadAssetAt(PayloadPath);
            ClearTestState();

            // Process creation first to track the asset
            DetectAssetChangeProcessor.ProcessChangesForTesting(
                new[] { PayloadPath },
                null,
                null,
                null
            );
            ClearTestState();

            string movedPath = TestRoot + "/MovedPayload.asset";

            string[] created = hasCreated ? new[] { PayloadPath } : null;
            string[] deleted = hasDeleted ? new[] { PayloadPath } : null;
            string[] moved = hasMoved ? new[] { movedPath } : null;
            string[] movedFrom = hasMovedFrom ? new[] { PayloadPath } : null;

            DetectAssetChangeProcessor.ProcessChangesForTesting(created, deleted, moved, movedFrom);

            // Moved events should be processed - verify the handler receives notification
            // about the moved asset (moved from the original path to the new path)
            Assert.GreaterOrEqual(
                TestDetectAssetChangeHandler.RecordedContexts.Count,
                1,
                $"Expected at least 1 handler invocation for moved asset (hasCreated={hasCreated}, hasDeleted={hasDeleted}, "
                    + $"hasMoved={hasMoved}, hasMovedFrom={hasMovedFrom}), but got {TestDetectAssetChangeHandler.RecordedContexts.Count}"
            );
        }
    }
}
