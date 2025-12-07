namespace WallstopStudios.UnityHelpers.Tests.Utils
{
#if UNITY_EDITOR
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using Object = UnityEngine.Object;

    public sealed class ScriptableObjectSingletonCreatorTests : CommonTestBase
    {
        private const string TestRoot = "Assets/Resources/CreatorTests";
        private bool _previousEditorUiSuppress;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Clean up any leftover test folders from previous test runs
            CleanupAllKnownTestFolders();

            // Also clean up duplicate folders that may have been created during previous runs
            // This is especially important for case-mismatch tests on case-insensitive file systems
            TryDeleteFolderAndDuplicates("Assets/Resources", "CreatorTests");
            TryDeleteFolderAndDuplicates("Assets/Resources", "CaseTest");
            TryDeleteFolderAndDuplicates("Assets/Resources", "casetest");
            TryDeleteFolderAndDuplicates("Assets/Resources", "CASETEST");
            TryDeleteFolderAndDuplicates("Assets/Resources", "cASEtest");
            TryDeleteFolderAndDuplicates("Assets/Resources", "CaseTEST");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _previousEditorUiSuppress = EditorUi.Suppress;
            EditorUi.Suppress = true;

            // CRITICAL: Clean up all case-variant folders BEFORE each test
            // This prevents pollution from previous test cases in data-driven tests
            TryDeleteFolderAndDuplicates("Assets/Resources", "CaseTest");
            TryDeleteFolderAndDuplicates("Assets/Resources", "casetest");
            TryDeleteFolderAndDuplicates("Assets/Resources", "CASETEST");
            TryDeleteFolderAndDuplicates("Assets/Resources", "cASEtest");
            TryDeleteFolderAndDuplicates("Assets/Resources", "CaseTEST");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            yield return null;

            ScriptableObjectSingletonCreator.IncludeTestAssemblies = true;
            ScriptableObjectSingletonCreator.VerboseLogging = true;
            // Allow explicit calls to EnsureSingletonAssets during tests
            ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression = true;
            ScriptableObjectSingletonCreator.TypeFilter = static type =>
                type == typeof(CaseMismatch)
                || type == typeof(Duplicate)
                || type == typeof(A.NameCollision)
                || type == typeof(B.NameCollision)
                || type == typeof(RetrySingleton)
                || type == typeof(FileBlockSingleton)
                || type == typeof(NoRetrySingleton);
            EnsureFolder("Assets/Resources");
            EnsureFolder(TestRoot);
            // Ensure the metadata folder exists to prevent modal dialogs
            EnsureFolder("Assets/Resources/Wallstop Studios/Unity Helpers");
            ScriptableObjectSingletonCreator.DisableAutomaticRetries = false;
            ScriptableObjectSingletonCreator.ResetRetryStateForTests();
            yield break;
        }

        [UnityTearDown]
        public override IEnumerator UnityTearDown()
        {
            IEnumerator baseEnumerator = base.UnityTearDown();
            while (baseEnumerator.MoveNext())
            {
                yield return baseEnumerator.Current;
            }

            // Clean up any assets created under our test root
            string[] guids = AssetDatabase.FindAssets("t:Object", new[] { TestRoot });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.DeleteAsset(path);
            }

            // Delete files that may be blocking folder creation (these are actual files, not folders)
            DeleteFileIfExists(TestRoot + "/FileBlock");
            DeleteFileIfExists(TestRoot + "/NoRetry");
            DeleteFileIfExists(TestRoot + "/Retry");

            // Try to delete empty folders bottom-up (subfolders first, then parent)
            TryDeleteFolder(TestRoot + "/Collision");
            TryDeleteFolder(TestRoot + "/Retry");
            TryDeleteFolder(TestRoot + "/Retry 1");
            TryDeleteFolder(TestRoot + "/FileBlock");
            TryDeleteFolder(TestRoot + "/FileBlock 1");
            TryDeleteFolder(TestRoot + "/NoRetry");
            TryDeleteFolder(TestRoot + "/NoRetry 1");
            TryDeleteFolder(TestRoot);
            // Clean up CreatorTests folder and any duplicates (e.g., "CreatorTests 1")
            TryDeleteFolderAndDuplicates("Assets/Resources", "CreatorTests");

            // Clean up all case variants of CaseTest and their duplicates
            // This handles: CaseTest, cASEtest, CASETEST, casetest, CaseTEST
            // AND their duplicates: CaseTest 1, cASEtest 1, etc.
            TryDeleteFolderAndDuplicates("Assets/Resources", "CaseTest");
            TryDeleteFolderAndDuplicates("Assets/Resources", "casetest");
            TryDeleteFolderAndDuplicates("Assets/Resources", "CASETEST");
            TryDeleteFolderAndDuplicates("Assets/Resources", "cASEtest");
            TryDeleteFolderAndDuplicates("Assets/Resources", "CaseTEST");

            // Legacy cleanup (may still be needed for folders without duplicates)
            TryDeleteFolder("Assets/Resources/CaseTest");
            TryDeleteFolderCaseInsensitive("Assets/Resources/CaseTest");
            TryDeleteFolderCaseInsensitive("Assets/Resources/casetest");
            TryDeleteFolderCaseInsensitive("Assets/Resources/CASETEST");
            TryDeleteFolderCaseInsensitive("Assets/Resources/cASEtest");
            TryDeleteFolderCaseInsensitive("Assets/Resources/CaseTEST");
            TryDeleteFolder("Assets/Resources");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ScriptableObjectSingletonCreator.TypeFilter = null;
            ScriptableObjectSingletonCreator.IncludeTestAssemblies = false;
            ScriptableObjectSingletonCreator.DisableAutomaticRetries = false;
            ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression = false;
            ScriptableObjectSingletonCreator.ResetRetryStateForTests();
            EditorUi.Suppress = _previousEditorUiSuppress;

            // Clean up all known test folders including duplicates
            CleanupAllKnownTestFolders();
        }

        public override void OneTimeTearDown()
        {
            base.OneTimeTearDown();
            // Final cleanup of all test folders
            CleanupAllKnownTestFolders();
        }

        [UnityTest]
        public IEnumerator DoesNotCreateDuplicateSubfolderOnCaseMismatch()
        {
            // Arrange: create wrong-cased subfolder under Resources
            EnsureFolder("Assets/Resources/cASEtest");

            // IMPORTANT: Refresh AssetDatabase to ensure the folder is visible to GetSubFolders
            // Without this, the singleton creator may not find the case-mismatched folder
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            yield return null;

            string assetPath = "Assets/Resources/cASEtest/CaseMismatch.asset";
            AssetDatabase.DeleteAsset(assetPath);
            LogAssert.ignoreFailingMessages = true;

            // Verify folder setup before running ensure
            bool wrongCasedFolderExists = AssetDatabase.IsValidFolder("Assets/Resources/cASEtest");
            string[] subFolders = AssetDatabase.GetSubFolders("Assets/Resources");
            string subFolderList = subFolders != null ? string.Join(", ", subFolders) : "null";

            Assert.IsTrue(
                wrongCasedFolderExists,
                $"Setup: Wrong-cased folder 'cASEtest' should exist before EnsureSingletonAssets. "
                    + $"Subfolders of Assets/Resources: [{subFolderList}]"
            );

            // Act: trigger creation for a singleton targeting "CaseTest" path
            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;
            yield return null;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // Check for duplicate folders and asset location
            bool wrongCasedFolderStillExists = AssetDatabase.IsValidFolder(
                "Assets/Resources/cASEtest"
            );
            bool correctCasedFolderExists = AssetDatabase.IsValidFolder(
                "Assets/Resources/CaseTest"
            );
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            string actualAssetPath = asset != null ? AssetDatabase.GetAssetPath(asset) : "null";

            // Build diagnostics
            string[] postSubFolders = AssetDatabase.GetSubFolders("Assets/Resources");
            string postSubFolderList =
                postSubFolders != null ? string.Join(", ", postSubFolders) : "null";

            // Look for any duplicate folder pattern (folder name with " 1", " 2", etc. suffix)
            bool anyDuplicateExists = false;
            string duplicateFound = null;
            foreach (string folder in postSubFolders)
            {
                string folderName = Path.GetFileName(folder);
                // Check if this looks like a duplicate of CaseTest or cASEtest
                if (
                    folderName.StartsWith("CaseTest ", StringComparison.OrdinalIgnoreCase)
                    || folderName.StartsWith("cASEtest ", StringComparison.OrdinalIgnoreCase)
                )
                {
                    // Check if the suffix is a number (indicating a duplicate)
                    string[] parts = folderName.Split(' ');
                    if (parts.Length >= 2 && int.TryParse(parts[^1], out _))
                    {
                        anyDuplicateExists = true;
                        duplicateFound = folder;
                        break;
                    }
                }
            }

            string diagnostics =
                $"wrongCasedFolderStillExists={wrongCasedFolderStillExists}, "
                + $"correctCasedFolderExists={correctCasedFolderExists}, "
                + $"anyDuplicateExists={anyDuplicateExists}, "
                + $"duplicateFound={duplicateFound ?? "none"}, "
                + $"assetExists={asset != null}, actualAssetPath={actualAssetPath}, "
                + $"Subfolders of Assets/Resources: [{postSubFolderList}]";

            // Assert: no duplicate folder created and asset placed in reused folder
            // Note: The folder may have been renamed to correct casing, so either casing is acceptable
            Assert.IsTrue(
                wrongCasedFolderStillExists || correctCasedFolderExists,
                $"Either original or corrected folder should exist. Diagnostics: {diagnostics}"
            );
            Assert.IsFalse(
                anyDuplicateExists,
                $"No duplicate folder should exist. Diagnostics: {diagnostics}"
            );

            // Asset should exist - either in the original wrong-cased folder or in a renamed correct-cased folder
            Object finalAsset =
                AssetDatabase.LoadAssetAtPath<Object>(assetPath)
                ?? AssetDatabase.LoadAssetAtPath<Object>(
                    "Assets/Resources/CaseTest/CaseMismatch.asset"
                );
            Assert.IsNotNull(
                finalAsset,
                $"Asset should exist in either folder. Diagnostics: {diagnostics}"
            );
        }

        [UnityTest]
        public IEnumerator SkipsCreationWhenTargetPathOccupied()
        {
            // Arrange: Create an occupying asset at the target path
            string targetFolder = TestRoot;
            EnsureFolder(targetFolder);
            string occupiedPath = targetFolder + "/Duplicate.asset";
            if (AssetDatabase.LoadAssetAtPath<Object>(occupiedPath) == null)
            {
                TextAsset ta = new("occupied");
                AssetDatabase.CreateAsset(ta, occupiedPath);
            }

            // Act: run ensure and expect a warning about occupied target
            LogAssert.Expect(LogType.Warning, new Regex("target path already occupied"));
            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;

            // Assert: no duplicate asset created alongside
            Assert.IsTrue(
                AssetDatabase.LoadAssetAtPath<Object>(targetFolder + "/Duplicate 1.asset") == null
            );
        }

        [UnityTest]
        public IEnumerator WarnsOnTypeNameCollision()
        {
            // Arrange: ensure collision folder exists
            EnsureFolder("Assets/Resources/CreatorTests/Collision");

            // Act: ensure logs a collision warning and does not create the overlapping asset
            LogAssert.Expect(LogType.Warning, new Regex("Type name collision"));
            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;

            // Assert: no asset created at the ambiguous path
            Assert.IsTrue(
                AssetDatabase.LoadAssetAtPath<Object>(
                    "Assets/Resources/CreatorTests/Collision/NameCollision.asset"
                ) == null
            );
        }

        [UnityTest]
        public IEnumerator EnsureSingletonAssetsIsIdempotent()
        {
            string targetPath = "Assets/Resources/CaseTest/CaseMismatch.asset";
            AssetDatabase.DeleteAsset(targetPath);
            yield return null;

            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;

            string firstGuid = AssetDatabase.AssetPathToGUID(targetPath);
            Assert.IsFalse(string.IsNullOrEmpty(firstGuid));

            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;

            string secondGuid = AssetDatabase.AssetPathToGUID(targetPath);
            Assert.AreEqual(firstGuid, secondGuid);
        }

        [UnityTest]
        public IEnumerator SkipsEnsureInsideAssetImportWorkerProcess()
        {
            string targetPath = "Assets/Resources/CaseTest/CaseMismatch.asset";
            AssetDatabase.DeleteAsset(targetPath);
            yield return null;

            Func<bool> originalDetector =
                ScriptableObjectSingletonCreator.AssetImportWorkerProcessCheck;
            ScriptableObjectSingletonCreator.AssetImportWorkerProcessCheck = static () => true;
            try
            {
                ScriptableObjectSingletonCreator.EnsureSingletonAssets();
                yield return null;
                Assert.IsTrue(AssetDatabase.LoadAssetAtPath<Object>(targetPath) == null);
            }
            finally
            {
                ScriptableObjectSingletonCreator.AssetImportWorkerProcessCheck = originalDetector;
            }

            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<Object>(targetPath) != null);
        }

        [UnityTest]
        public IEnumerator EnvironmentVariablesTriggerWorkerDetection(
            [ValueSource(nameof(AssetImportWorkerEnvironmentScenarios))] string environmentVariable
        )
        {
            string targetPath = "Assets/Resources/CaseTest/CaseMismatch.asset";
            AssetDatabase.DeleteAsset(targetPath);
            yield return null;

            string originalValue = Environment.GetEnvironmentVariable(environmentVariable);
            Func<bool> originalDetector =
                ScriptableObjectSingletonCreator.AssetImportWorkerProcessCheck;
            try
            {
                Environment.SetEnvironmentVariable(environmentVariable, "1");
                ScriptableObjectSingletonCreator.ResetAssetImportWorkerDetectionStateForTests();
                ScriptableObjectSingletonCreator.AssetImportWorkerProcessCheck = null;

                ScriptableObjectSingletonCreator.EnsureSingletonAssets();
                yield return null;

                Assert.IsTrue(AssetDatabase.LoadAssetAtPath<Object>(targetPath) == null);
            }
            finally
            {
                Environment.SetEnvironmentVariable(environmentVariable, originalValue);
                ScriptableObjectSingletonCreator.AssetImportWorkerProcessCheck = originalDetector;
                ScriptableObjectSingletonCreator.ResetAssetImportWorkerDetectionStateForTests();
            }

            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<Object>(targetPath) != null);
        }

        [UnityTest]
        public IEnumerator DetectorExceptionsDoNotBlockEnsure()
        {
            string targetPath = "Assets/Resources/CaseTest/CaseMismatch.asset";
            AssetDatabase.DeleteAsset(targetPath);
            yield return null;

            Func<bool> originalDetector =
                ScriptableObjectSingletonCreator.AssetImportWorkerProcessCheck;
            ScriptableObjectSingletonCreator.AssetImportWorkerProcessCheck = static () =>
                throw new InvalidOperationException("detector failure");

            try
            {
                ScriptableObjectSingletonCreator.EnsureSingletonAssets();
                yield return null;
                Assert.IsTrue(AssetDatabase.LoadAssetAtPath<Object>(targetPath) != null);
            }
            finally
            {
                ScriptableObjectSingletonCreator.AssetImportWorkerProcessCheck = originalDetector;
            }
        }

        [UnityTest]
        public IEnumerator RetriesCreationAfterTemporaryFolderBlock()
        {
            string retryFolder = TestRoot + "/Retry";
            string retryAsset = retryFolder + "/RetrySingleton.asset";
            string blockerMeta = retryFolder + ".meta";
            string retryFolderVariant = retryFolder + " 1";

            // Reset retry state to ensure fresh start
            ScriptableObjectSingletonCreator.ResetRetryStateForTests();

            // Thorough cleanup of all possible states
            CleanupRetryTestState(retryFolder, retryAsset, blockerMeta, retryFolderVariant);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            yield return null;

            EnsureFolder(TestRoot);
            yield return null;

            // Create blocker file that prevents folder creation
            string absoluteBlocker = GetAbsolutePath(retryFolder);
            File.WriteAllText(absoluteBlocker, "block");
            AssetDatabase.ImportAsset(retryFolder, ImportAssetOptions.ForceSynchronousImport);
            yield return null;

            // Verify blocker is in place
            Assert.IsTrue(
                File.Exists(absoluteBlocker),
                "Blocker file should exist before testing folder creation failure"
            );

            LogAssert.Expect(
                LogType.Error,
                new Regex(
                    "(Failed|Expected) to create folder 'Assets/Resources/CreatorTests/Retry'"
                )
            );
            LogAssert.Expect(
                LogType.Error,
                new Regex("Unable to ensure folder 'Assets/Resources/CreatorTests/Retry'")
            );

            ScriptableObjectSingletonCreator.DisableAutomaticRetries = false;
            ScriptableObjectSingletonCreator.EnsureSingletonAssets();

            Assert.IsFalse(
                AssetDatabase.IsValidFolder(retryFolder),
                "Retry folder should not exist while blocker is present"
            );
            Assert.IsTrue(
                AssetDatabase.LoadAssetAtPath<Object>(retryAsset) == null,
                "Retry asset should not exist while blocker is present"
            );
            Assert.IsFalse(
                AssetDatabase.IsValidFolder(retryFolderVariant),
                "Variant folder should not be created"
            );

            // Remove the blocker file completely and reset retry state for fresh retries
            CleanupRetryTestState(retryFolder, retryAsset, blockerMeta, retryFolderVariant);
            ScriptableObjectSingletonCreator.ResetRetryStateForTests();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            yield return null;

            // Verify blocker is gone
            Assert.IsFalse(
                File.Exists(absoluteBlocker),
                "Blocker file should be removed before retry"
            );
            Assert.IsFalse(
                AssetDatabase.IsValidFolder(retryFolder),
                "Retry folder should not exist yet"
            );

            // Manually trigger ensure now that the blocker is gone - should succeed immediately
            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            yield return null;

            bool folderExists = AssetDatabase.IsValidFolder(retryFolder);
            bool assetExists = AssetDatabase.LoadAssetAtPath<Object>(retryAsset) != null;
            bool variantExists = AssetDatabase.IsValidFolder(retryFolderVariant);

            string diagnostics =
                $"folderExists={folderExists}, assetExists={assetExists}, variantExists={variantExists}, "
                + $"blockerOnDisk={File.Exists(absoluteBlocker)}, metaOnDisk={File.Exists(GetAbsolutePath(blockerMeta))}";

            Assert.IsTrue(
                folderExists && assetExists && !variantExists,
                $"Retry singleton should be created once the temporary blocker is removed. Diagnostics: {diagnostics}"
            );
        }

        private void CleanupRetryTestState(
            string retryFolder,
            string retryAsset,
            string blockerMeta,
            string retryFolderVariant
        )
        {
            // Delete assets through AssetDatabase first
            AssetDatabase.DeleteAsset(retryAsset);
            if (AssetDatabase.IsValidFolder(retryFolder))
            {
                AssetDatabase.DeleteAsset(retryFolder);
            }
            if (AssetDatabase.IsValidFolder(retryFolderVariant))
            {
                AssetDatabase.DeleteAsset(retryFolderVariant);
            }

            // Then delete any remaining files on disk
            string absoluteFolder = GetAbsolutePath(retryFolder);
            string absoluteVariant = GetAbsolutePath(retryFolderVariant);
            string absoluteMeta = GetAbsolutePath(blockerMeta);

            // Delete blocker file if it exists
            if (File.Exists(absoluteFolder))
            {
                File.Delete(absoluteFolder);
            }

            // Delete blocker meta if it exists
            if (File.Exists(absoluteMeta))
            {
                File.Delete(absoluteMeta);
            }

            // Delete folder on disk if it somehow exists as a directory
            if (Directory.Exists(absoluteFolder))
            {
                Directory.Delete(absoluteFolder, true);
            }
            if (Directory.Exists(absoluteVariant))
            {
                Directory.Delete(absoluteVariant, true);
            }

            // Delete variant meta if it exists
            string variantMeta = absoluteVariant + ".meta";
            if (File.Exists(variantMeta))
            {
                File.Delete(variantMeta);
            }
        }

        [UnityTest]
        public IEnumerator DoesNotCreateAlternateFolderWhenFileConflicts()
        {
            string conflictFolder = TestRoot + "/FileBlock";
            string conflictAsset = conflictFolder + "/FileBlockSingleton.asset";
            string conflictFile = conflictFolder;
            string conflictVariant = conflictFolder + " 1";

            AssetDatabase.DeleteAsset(conflictAsset);
            if (AssetDatabase.IsValidFolder(conflictFolder))
            {
                AssetDatabase.DeleteAsset(conflictFolder);
            }
            if (AssetDatabase.IsValidFolder(conflictVariant))
            {
                AssetDatabase.DeleteAsset(conflictVariant);
            }

            DeleteFileIfExists(conflictFile);
            EnsureFolder(TestRoot);

            string absoluteParent = Path.GetDirectoryName(GetAbsolutePath(conflictFile));
            if (!string.IsNullOrEmpty(absoluteParent) && !Directory.Exists(absoluteParent))
            {
                Directory.CreateDirectory(absoluteParent);
            }

            File.WriteAllText(GetAbsolutePath(conflictFile), "block");
            AssetDatabase.ImportAsset(conflictFile);
            yield return null;

            LogAssert.Expect(
                LogType.Error,
                new Regex(
                    "(Failed|Expected) to create folder 'Assets/Resources/CreatorTests/FileBlock'"
                )
            );
            LogAssert.Expect(
                LogType.Error,
                new Regex("Unable to ensure folder 'Assets/Resources/CreatorTests/FileBlock'")
            );

            ScriptableObjectSingletonCreator.DisableAutomaticRetries = true;
            try
            {
                ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            }
            finally
            {
                ScriptableObjectSingletonCreator.DisableAutomaticRetries = false;
            }
            yield return null;

            Assert.IsFalse(AssetDatabase.IsValidFolder(conflictFolder));
            Assert.IsFalse(AssetDatabase.IsValidFolder(conflictVariant));
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<Object>(conflictAsset) == null);

            DeleteFileIfExists(conflictFile);
            if (AssetDatabase.IsValidFolder(conflictVariant))
            {
                AssetDatabase.DeleteAsset(conflictVariant);
            }
            AssetDatabase.Refresh();
            yield return null;
        }

        [UnityTest]
        public IEnumerator AutomaticRetriesCanBeDisabled()
        {
            string noRetryFolder = TestRoot + "/NoRetry";
            string noRetryAsset = noRetryFolder + "/NoRetrySingleton.asset";
            string noRetryVariant = noRetryFolder + " 1";
            string blockerMeta = noRetryFolder + ".meta";

            // Reset retry state for clean test
            ScriptableObjectSingletonCreator.ResetRetryStateForTests();

            AssetDatabase.DeleteAsset(noRetryAsset);
            if (AssetDatabase.IsValidFolder(noRetryFolder))
            {
                AssetDatabase.DeleteAsset(noRetryFolder);
            }
            if (AssetDatabase.IsValidFolder(noRetryVariant))
            {
                AssetDatabase.DeleteAsset(noRetryVariant);
            }
            DeleteFileIfExists(noRetryFolder);
            string absoluteBlockerMeta = GetAbsolutePath(blockerMeta);
            if (File.Exists(absoluteBlockerMeta))
            {
                File.Delete(absoluteBlockerMeta);
            }

            EnsureFolder(TestRoot);
            yield return null;

            string absoluteBlocker = GetAbsolutePath(noRetryFolder);
            File.WriteAllText(absoluteBlocker, "block");
            AssetDatabase.ImportAsset(noRetryFolder, ImportAssetOptions.ForceSynchronousImport);
            yield return null;

            LogAssert.Expect(
                LogType.Error,
                new Regex(
                    "(Failed|Expected) to create folder 'Assets/Resources/CreatorTests/NoRetry'"
                )
            );
            LogAssert.Expect(
                LogType.Error,
                new Regex("Unable to ensure folder 'Assets/Resources/CreatorTests/NoRetry'")
            );

            bool originalRetrySetting = ScriptableObjectSingletonCreator.DisableAutomaticRetries;
            ScriptableObjectSingletonCreator.DisableAutomaticRetries = true;
            try
            {
                ScriptableObjectSingletonCreator.EnsureSingletonAssets();
                yield return null;
            }
            finally
            {
                ScriptableObjectSingletonCreator.DisableAutomaticRetries = originalRetrySetting;
            }

            Assert.IsTrue(
                AssetDatabase.LoadAssetAtPath<Object>(noRetryAsset) == null,
                "Asset should not be created while blocker is present"
            );
            Assert.IsFalse(
                AssetDatabase.IsValidFolder(noRetryFolder),
                "Folder should not exist while blocker is present"
            );
            Assert.IsFalse(
                AssetDatabase.IsValidFolder(noRetryVariant),
                "Variant folder should not be created"
            );

            // Remove blocker
            DeleteFileIfExists(noRetryFolder);
            if (File.Exists(absoluteBlockerMeta))
            {
                File.Delete(absoluteBlockerMeta);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            yield return null;

            // Automatic retries are still disabled, so nothing should be created until we run ensure manually.
            Assert.IsTrue(
                AssetDatabase.LoadAssetAtPath<Object>(noRetryAsset) == null,
                "Asset should not be created automatically while retries are disabled"
            );

            // Reset retry state and enable retries, then manually trigger
            ScriptableObjectSingletonCreator.ResetRetryStateForTests();
            ScriptableObjectSingletonCreator.DisableAutomaticRetries = false;

            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            yield return null;

            bool folderExists = AssetDatabase.IsValidFolder(noRetryFolder);
            bool assetExists = AssetDatabase.LoadAssetAtPath<Object>(noRetryAsset) != null;
            bool variantExists = AssetDatabase.IsValidFolder(noRetryVariant);

            ScriptableObjectSingletonCreator.DisableAutomaticRetries = originalRetrySetting;

            string diagnostics =
                $"folderExists={folderExists}, assetExists={assetExists}, variantExists={variantExists}";

            Assert.IsTrue(
                folderExists && assetExists && !variantExists,
                $"NoRetry singleton should be created when manually triggered. Diagnostics: {diagnostics}"
            );
        }

        private static IEnumerable<string> AssetImportWorkerEnvironmentScenarios()
        {
            yield return "UNITY_ASSET_IMPORT_WORKER";
            yield return "UNITY_ASSETIMPORT_WORKER";
            yield return "MY_CUSTOM_UNITY_ASSET_IMPORT_WORKER_FLAG";
        }

        /// <summary>
        /// Data source for case mismatch folder scenarios.
        /// Each tuple contains: (existingFolderName, expectedSingletonPath, description)
        /// </summary>
        private static IEnumerable<TestCaseData> CaseMismatchFolderScenarios()
        {
            yield return new TestCaseData("casetest", "Assets/Resources/casetest")
                .SetName("AllLowercase")
                .SetDescription("Existing folder with all lowercase name");
            yield return new TestCaseData("CASETEST", "Assets/Resources/CASETEST")
                .SetName("AllUppercase")
                .SetDescription("Existing folder with all uppercase name");
            yield return new TestCaseData("cASEtest", "Assets/Resources/cASEtest")
                .SetName("MixedCase1")
                .SetDescription("Existing folder with mixed case (cASEtest)");
            yield return new TestCaseData("CaseTEST", "Assets/Resources/CaseTEST")
                .SetName("MixedCase2")
                .SetDescription("Existing folder with mixed case (CaseTEST)");
        }

        [UnityTest]
        public IEnumerator CaseMismatchFolderIsReused(
            [ValueSource(nameof(CaseMismatchFolderScenarios))] TestCaseData testCase
        )
        {
            string existingFolderName = (string)testCase.Arguments[0];
            string expectedFolderPath = (string)testCase.Arguments[1];

            // Arrange: create wrong-cased subfolder under Resources
            string existingFolder = "Assets/Resources/" + existingFolderName;
            EnsureFolder(existingFolder);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            yield return null;

            LogAssert.ignoreFailingMessages = true;

            // Verify folder setup
            Assert.IsTrue(
                AssetDatabase.IsValidFolder(existingFolder),
                $"Setup: Folder '{existingFolder}' should exist"
            );

            // Act: trigger creation for CaseMismatch singleton targeting "CaseTest" path
            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;
            yield return null;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // Assert: no duplicate folder created
            // Check for any case variant of "CaseTest 1" or "existingFolderName 1"
            string[] resourceSubfolders = AssetDatabase.GetSubFolders("Assets/Resources");
            string subfoldersStr = string.Join(", ", resourceSubfolders);

            // Look for any duplicate folder pattern (folder name with " 1", " 2", etc. suffix)
            bool anyDuplicateExists = false;
            string duplicateFound = null;
            foreach (string folder in resourceSubfolders)
            {
                string folderName = Path.GetFileName(folder);
                // Check if this looks like a duplicate of CaseTest or the existing folder
                if (
                    folderName.StartsWith("CaseTest ", StringComparison.OrdinalIgnoreCase)
                    || folderName.StartsWith(
                        existingFolderName + " ",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    // Check if the suffix is a number (indicating a duplicate)
                    string[] parts = folderName.Split(' ');
                    if (parts.Length >= 2 && int.TryParse(parts[^1], out _))
                    {
                        anyDuplicateExists = true;
                        duplicateFound = folder;
                        break;
                    }
                }
            }

            Assert.IsFalse(
                anyDuplicateExists,
                $"No duplicate folder should exist when '{existingFolderName}' exists. "
                    + $"Found duplicate: '{duplicateFound}'. Subfolders: [{subfoldersStr}]"
            );

            // Asset should exist somewhere under Resources
            Object asset =
                AssetDatabase.LoadAssetAtPath<Object>(existingFolder + "/CaseMismatch.asset")
                ?? AssetDatabase.LoadAssetAtPath<Object>(
                    "Assets/Resources/CaseTest/CaseMismatch.asset"
                );

            Assert.IsNotNull(
                asset,
                $"CaseMismatch singleton should be created. Subfolders: [{subfoldersStr}]"
            );
        }

        /// <summary>
        /// Verifies that duplicate folder cleanup helper correctly identifies duplicate folders.
        /// </summary>
        [Test]
        public void TryDeleteFolderAndDuplicatesIdentifiesDuplicateFolderPatterns()
        {
            // Create a base folder and several "duplicates" with numeric suffixes
            string baseName = "TestDuplicateDetection";
            string basePath = "Assets/Resources/" + baseName;
            string dup1Path = "Assets/Resources/" + baseName + " 1";
            string dup2Path = "Assets/Resources/" + baseName + " 2";
            string notDupPath = "Assets/Resources/" + baseName + "Other"; // Not a duplicate

            EnsureFolder(basePath);
            EnsureFolder(dup1Path);
            EnsureFolder(dup2Path);
            EnsureFolder(notDupPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // Verify all folders exist
            Assert.IsTrue(AssetDatabase.IsValidFolder(basePath), "Base folder should exist");
            Assert.IsTrue(AssetDatabase.IsValidFolder(dup1Path), "Duplicate 1 should exist");
            Assert.IsTrue(AssetDatabase.IsValidFolder(dup2Path), "Duplicate 2 should exist");
            Assert.IsTrue(AssetDatabase.IsValidFolder(notDupPath), "Non-duplicate should exist");

            // Act: delete the base folder and its duplicates
            TryDeleteFolderAndDuplicates("Assets/Resources", baseName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // Assert: base and duplicates should be gone, non-duplicate should remain
            Assert.IsFalse(AssetDatabase.IsValidFolder(basePath), "Base folder should be deleted");
            Assert.IsFalse(AssetDatabase.IsValidFolder(dup1Path), "Duplicate 1 should be deleted");
            Assert.IsFalse(AssetDatabase.IsValidFolder(dup2Path), "Duplicate 2 should be deleted");
            Assert.IsTrue(
                AssetDatabase.IsValidFolder(notDupPath),
                "Non-duplicate should NOT be deleted (it doesn't match the pattern)"
            );

            // Cleanup
            TryDeleteFolder(notDupPath);
        }

        /// <summary>
        /// Verifies that the data-driven CaseMismatchFolderIsReused test doesn't pollute
        /// state between test case executions.
        /// </summary>
        [UnityTest]
        public IEnumerator CaseMismatchFoldersCleanupBetweenTests()
        {
            // This test verifies that after cleanup, no case-variant folders remain
            // Clean up all case variants
            TryDeleteFolderAndDuplicates("Assets/Resources", "CaseTest");
            TryDeleteFolderAndDuplicates("Assets/Resources", "casetest");
            TryDeleteFolderAndDuplicates("Assets/Resources", "CASETEST");
            TryDeleteFolderAndDuplicates("Assets/Resources", "cASEtest");
            TryDeleteFolderAndDuplicates("Assets/Resources", "CaseTEST");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            yield return null;

            // Verify no case-variant folders exist
            string[] subFolders = AssetDatabase.GetSubFolders("Assets/Resources");
            List<string> caseTestFolders = new();

            foreach (string folder in subFolders)
            {
                string name = Path.GetFileName(folder);
                if (
                    name.StartsWith("CaseTest", StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("casetest", StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("CASETEST", StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("cASEtest", StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("CaseTEST", StringComparison.OrdinalIgnoreCase)
                )
                {
                    caseTestFolders.Add(folder);
                }
            }

            Assert.IsEmpty(
                caseTestFolders,
                $"No CaseTest variant folders should exist after cleanup. Found: [{string.Join(", ", caseTestFolders)}]"
            );
        }

        /// <summary>
        /// Verifies diagnostics output for case mismatch scenario.
        /// </summary>
        [UnityTest]
        public IEnumerator CaseMismatchDiagnosticsAreHelpful()
        {
            // Create a folder with non-standard casing
            string existingFolder = "Assets/Resources/cAsEtEsT";
            EnsureFolder(existingFolder);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            yield return null;

            LogAssert.ignoreFailingMessages = true;

            // Run singleton creation
            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // Collect diagnostic info
            string[] subFolders = AssetDatabase.GetSubFolders("Assets/Resources");
            bool foundOriginal = false;
            bool foundDuplicate = false;
            List<string> foundCaseVariants = new();

            foreach (string folder in subFolders)
            {
                string name = Path.GetFileName(folder);
                if (string.Equals(name, "cAsEtEsT", StringComparison.Ordinal))
                {
                    foundOriginal = true;
                }
                if (
                    name.StartsWith("cAsEtEsT ", StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("CaseTest ", StringComparison.OrdinalIgnoreCase)
                )
                {
                    string[] parts = name.Split(' ');
                    if (parts.Length >= 2 && int.TryParse(parts[^1], out _))
                    {
                        foundDuplicate = true;
                    }
                }
                if (name.StartsWith("CaseTest", StringComparison.OrdinalIgnoreCase))
                {
                    foundCaseVariants.Add(folder);
                }
            }

            string diagnostics =
                $"foundOriginal={foundOriginal}, foundDuplicate={foundDuplicate}, "
                + $"caseVariants=[{string.Join(", ", foundCaseVariants)}], "
                + $"allSubfolders=[{string.Join(", ", subFolders)}]";

            // The key assertion: no duplicate should be created
            Assert.IsFalse(
                foundDuplicate,
                $"No duplicate folder should be created for case-insensitive match. {diagnostics}"
            );

            // Clean up
            TryDeleteFolderAndDuplicates("Assets/Resources", "cAsEtEsT");
            TryDeleteFolderAndDuplicates("Assets/Resources", "CaseTest");
        }

        private static void EnsureFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            folderPath = folderPath.Replace('\\', '/');
            string projectRoot = Path.GetDirectoryName(Application.dataPath);

            // Process each path segment to handle case-insensitive folder matching
            string[] parts = folderPath.Split('/');
            string current = parts[0]; // "Assets"

            for (int i = 1; i < parts.Length; i++)
            {
                string desiredName = parts[i];
                string intendedNext = current + "/" + desiredName;

                // First, check if folder already exists in AssetDatabase (exact match)
                if (AssetDatabase.IsValidFolder(intendedNext))
                {
                    current = intendedNext;
                    continue;
                }

                // Check for case-insensitive match on disk first
                string actualFolderName = FindExistingFolderCaseInsensitive(
                    projectRoot,
                    current,
                    desiredName
                );
                if (actualFolderName != null)
                {
                    // Folder exists on disk with potentially different casing
                    string actualPath = current + "/" + actualFolderName;

                    // Import it into AssetDatabase if not already there
                    if (!AssetDatabase.IsValidFolder(actualPath))
                    {
                        AssetDatabase.ImportAsset(
                            actualPath,
                            ImportAssetOptions.ForceSynchronousImport
                        );
                    }

                    current = actualPath;
                    continue;
                }

                // Folder doesn't exist on disk or in AssetDatabase - create it
                // First create on disk
                if (!string.IsNullOrEmpty(projectRoot))
                {
                    string absoluteDirectory = Path.Combine(projectRoot, intendedNext);
                    try
                    {
                        if (!Directory.Exists(absoluteDirectory))
                        {
                            Directory.CreateDirectory(absoluteDirectory);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(
                            $"EnsureFolder: Failed to create directory on disk '{absoluteDirectory}': {ex.Message}"
                        );
                        return;
                    }

                    // Import the newly created folder
                    AssetDatabase.ImportAsset(
                        intendedNext,
                        ImportAssetOptions.ForceSynchronousImport
                    );
                }

                // If it's still not valid, create via AssetDatabase (fallback)
                if (!AssetDatabase.IsValidFolder(intendedNext))
                {
                    AssetDatabase.CreateFolder(current, desiredName);
                }

                current = intendedNext;
            }
        }

        /// <summary>
        /// Finds an existing folder on disk that matches the desired name case-insensitively.
        /// Returns the actual folder name as it exists on disk, or null if not found.
        /// </summary>
        private static string FindExistingFolderCaseInsensitive(
            string projectRoot,
            string parentUnityPath,
            string desiredName
        )
        {
            if (string.IsNullOrEmpty(projectRoot))
            {
                return null;
            }

            string parentAbsolutePath = Path.Combine(projectRoot, parentUnityPath);
            if (!Directory.Exists(parentAbsolutePath))
            {
                return null;
            }

            try
            {
                foreach (string dir in Directory.GetDirectories(parentAbsolutePath))
                {
                    string name = Path.GetFileName(dir);
                    if (string.Equals(name, desiredName, StringComparison.OrdinalIgnoreCase))
                    {
                        return name;
                    }
                }
            }
            catch
            {
                // Ignore enumeration errors
            }

            return null;
        }

        private static void TryDeleteFolder(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string[] contents = AssetDatabase.FindAssets(string.Empty, new[] { folder });
            if (contents == null || contents.Length == 0)
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }

        private static void TryDeleteFolderCaseInsensitive(string intended)
        {
            if (string.IsNullOrWhiteSpace(intended))
            {
                return;
            }

            string[] parts = intended.Replace('\\', '/').Split('/');
            if (parts.Length == 0)
            {
                return;
            }

            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string desired = parts[i];
                string next = current + "/" + desired;
                if (AssetDatabase.IsValidFolder(next))
                {
                    current = next;
                    continue;
                }

                string[] subs = AssetDatabase.GetSubFolders(current);
                if (subs == null || subs.Length == 0)
                {
                    return;
                }

                string match = null;
                for (int s = 0; s < subs.Length; s++)
                {
                    string sub = subs[s];
                    int last = sub.LastIndexOf('/');
                    string name = last >= 0 ? sub.Substring(last + 1) : sub;
                    if (string.Equals(name, desired, StringComparison.OrdinalIgnoreCase))
                    {
                        match = sub;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(match))
                {
                    return;
                }

                current = match;
            }

            TryDeleteFolder(current);
        }

        /// <summary>
        /// Deletes a folder and all its duplicates (e.g., "Folder", "Folder 1", "Folder 2").
        /// This handles the case where Unity creates duplicate folders when case-insensitive
        /// matches aren't detected properly during asset database operations.
        /// </summary>
        private static void TryDeleteFolderAndDuplicates(string parentPath, string folderBaseName)
        {
            if (string.IsNullOrWhiteSpace(parentPath) || string.IsNullOrWhiteSpace(folderBaseName))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(parentPath))
            {
                return;
            }

            string[] subFolders = AssetDatabase.GetSubFolders(parentPath);
            if (subFolders == null || subFolders.Length == 0)
            {
                return;
            }

            foreach (string folder in subFolders)
            {
                string name = Path.GetFileName(folder);
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                // Check exact match (case-insensitive)
                if (string.Equals(name, folderBaseName, StringComparison.OrdinalIgnoreCase))
                {
                    DeleteFolderRecursively(folder);
                    continue;
                }

                // Check duplicate pattern (e.g., "Folder 1", "Folder 2")
                if (name.StartsWith(folderBaseName + " ", StringComparison.OrdinalIgnoreCase))
                {
                    string suffix = name.Substring(folderBaseName.Length + 1);
                    if (int.TryParse(suffix, out _))
                    {
                        DeleteFolderRecursively(folder);
                    }
                }
            }
        }

        /// <summary>
        /// Recursively deletes a folder and all its contents.
        /// </summary>
        private static void DeleteFolderRecursively(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            // First delete all contents
            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { folderPath });
            if (guids != null)
            {
                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(assetPath) && !AssetDatabase.IsValidFolder(assetPath))
                    {
                        AssetDatabase.DeleteAsset(assetPath);
                    }
                }
            }

            // Delete subfolders recursively
            string[] subFolders = AssetDatabase.GetSubFolders(folderPath);
            if (subFolders != null)
            {
                foreach (string sub in subFolders)
                {
                    DeleteFolderRecursively(sub);
                }
            }

            // Finally delete the folder itself
            AssetDatabase.DeleteAsset(folderPath);
        }

        private static string GetAbsolutePath(string assetsRelativePath)
        {
            if (string.IsNullOrWhiteSpace(assetsRelativePath))
            {
                return string.Empty;
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot))
            {
                return string.Empty;
            }

            string normalized = assetsRelativePath.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(projectRoot, normalized);
        }

        private static void DeleteFileIfExists(string assetsRelativePath)
        {
            if (string.IsNullOrWhiteSpace(assetsRelativePath))
            {
                return;
            }

            if (AssetDatabase.DeleteAsset(assetsRelativePath))
            {
                return;
            }

            string absolutePath = GetAbsolutePath(assetsRelativePath);
            if (!string.IsNullOrEmpty(absolutePath) && File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }

            string metaPath = absolutePath + ".meta";
            if (!string.IsNullOrEmpty(metaPath) && File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }
        }
    }
#endif
}

// Name collision types in different namespaces
namespace A
{
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

#if UNITY_EDITOR
    [ScriptableSingletonPath("CreatorTests/Collision")]
    internal sealed class NameCollision : ScriptableObjectSingleton<NameCollision> { }

#endif
}

// Name collision types in different namespaces
namespace B
{
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

#if UNITY_EDITOR
    [ScriptableSingletonPath("CreatorTests/Collision")]
    internal sealed class NameCollision : ScriptableObjectSingleton<NameCollision> { }

#endif
}

namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

#if UNITY_EDITOR
    [ScriptableSingletonPath("CreatorTests/Retry")]
    internal sealed class RetrySingleton : ScriptableObjectSingleton<RetrySingleton> { }

    [ScriptableSingletonPath("CreatorTests/FileBlock")]
    internal sealed class FileBlockSingleton : ScriptableObjectSingleton<FileBlockSingleton> { }

    [ScriptableSingletonPath("CreatorTests/NoRetry")]
    internal sealed class NoRetrySingleton : ScriptableObjectSingleton<NoRetrySingleton> { }

#endif
}
