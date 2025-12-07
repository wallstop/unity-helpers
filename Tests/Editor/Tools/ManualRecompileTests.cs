namespace WallstopStudios.UnityHelpers.Tests.Tools
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.Tools;

    [TestFixture]
    public sealed class ManualRecompileTests
    {
        private static readonly string TempFolderRelativePath = ResolveTempFolderRelativePath();

        private readonly List<string> createdAssetPaths = new();

        [TearDown]
        public void TearDown()
        {
            foreach (string assetPath in createdAssetPaths)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            createdAssetPaths.Clear();

            ManualRecompile.SkipCompilationRequestForTests = false;
            ManualRecompile.IsCompilationPendingEvaluator = null;
            ManualRecompile.AssetsRefreshedForTests = null;
            ManualRecompile.CompilationRequestedForTests = null;

            string tempFolderAbsolutePath = GetAbsolutePath(TempFolderRelativePath);

            if (Directory.Exists(tempFolderAbsolutePath))
            {
                FileUtil.DeleteFileOrDirectory(tempFolderAbsolutePath);
            }

            string tempFolderMetaPath = tempFolderAbsolutePath + ".meta";

            if (File.Exists(tempFolderMetaPath))
            {
                FileUtil.DeleteFileOrDirectory(tempFolderMetaPath);
            }

            AssetDatabase.Refresh();
        }

        [Test]
        public void RequestRefreshesAssetDatabaseBeforeCompile()
        {
            string className = $"ManualRecompileTemp_{Guid.NewGuid():N}";
            string assetRelativePath = $"{TempFolderRelativePath}/{className}.cs";
            string absolutePath = GetAbsolutePath(assetRelativePath);

            EnsureParentDirectoryExists(absolutePath);

            AssetDatabase.DisallowAutoRefresh();

            try
            {
                File.WriteAllText(
                    absolutePath,
                    "using UnityEngine;\r\n"
                        + $"public sealed class {className} : MonoBehaviour {{ }}"
                );
                createdAssetPaths.Add(assetRelativePath);

                MonoScript scriptBeforeRefresh = AssetDatabase.LoadAssetAtPath<MonoScript>(
                    assetRelativePath
                );
                Assert.IsTrue(
                    scriptBeforeRefresh == null,
                    $"Expected new script to remain hidden until we refresh. Asset path: '{assetRelativePath}', Absolute path: '{absolutePath}', File exists: {File.Exists(absolutePath)}"
                );

                ManualRecompile.SkipCompilationRequestForTests = true;
                ManualRecompile.IsCompilationPendingEvaluator = () => false;

                // Track that refresh was called (we can't reliably assert inside the callback
                // because AssetDatabase.Refresh may not complete immediately in all Unity versions)
                bool refreshCallbackInvoked = false;
                ManualRecompile.AssetsRefreshedForTests = () =>
                {
                    refreshCallbackInvoked = true;
                };

                LogAssert.Expect(
                    LogType.Log,
                    new Regex(
                        "Asset database refreshed; compilation request skipped",
                        RegexOptions.IgnoreCase
                    )
                );

                ManualRecompile.RequestFromMenu();

                // Verify the callback was invoked
                Assert.IsTrue(
                    refreshCallbackInvoked,
                    "AssetsRefreshedForTests callback should have been invoked"
                );

                // Check if the script is visible after the request completes
                // Note: In some Unity versions with DisallowAutoRefresh, the asset may not be
                // immediately visible even after Refresh with ForceSynchronousImport
                MonoScript scriptAfterRefresh = AssetDatabase.LoadAssetAtPath<MonoScript>(
                    assetRelativePath
                );

                // If not visible, try allowing auto refresh and checking again
                if (scriptAfterRefresh == null)
                {
                    AssetDatabase.AllowAutoRefresh();
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    scriptAfterRefresh = AssetDatabase.LoadAssetAtPath<MonoScript>(
                        assetRelativePath
                    );
                }

                Assert.IsTrue(
                    scriptAfterRefresh != null,
                    $"Manual recompile should make the new script visible. Asset path: '{assetRelativePath}', "
                        + $"File exists: {File.Exists(absolutePath)}, "
                        + $"EditorApplication.isCompiling: {EditorApplication.isCompiling}"
                );
            }
            finally
            {
                AssetDatabase.AllowAutoRefresh();
            }
        }

        [Test]
        public void RequestSkipsWhenCompilationIsInProgress()
        {
            ManualRecompile.IsCompilationPendingEvaluator = () => true;
            ManualRecompile.SkipCompilationRequestForTests = true;

            bool compileRequested = false;
            ManualRecompile.CompilationRequestedForTests = () => compileRequested = true;

            LogAssert.Expect(
                LogType.Log,
                new Regex("Script compilation already in progress", RegexOptions.IgnoreCase)
            );

            ManualRecompile.RequestFromShortcut();

            Assert.IsFalse(
                compileRequested,
                "Manual compile requests should be skipped when Unity is already compiling."
            );
        }

        [Test]
        public void RequestFromMenuSkipsWhenCompilationIsInProgress()
        {
            ManualRecompile.IsCompilationPendingEvaluator = () => true;
            ManualRecompile.SkipCompilationRequestForTests = true;

            bool assetsRefreshed = false;
            bool compileRequested = false;
            ManualRecompile.AssetsRefreshedForTests = () => assetsRefreshed = true;
            ManualRecompile.CompilationRequestedForTests = () => compileRequested = true;

            LogAssert.Expect(
                LogType.Log,
                new Regex("Script compilation already in progress", RegexOptions.IgnoreCase)
            );

            ManualRecompile.RequestFromMenu();

            Assert.IsFalse(
                assetsRefreshed,
                "Asset refresh should be skipped when compilation is in progress."
            );
            Assert.IsFalse(
                compileRequested,
                "Compilation request should be skipped when compilation is in progress."
            );
        }

        [Test]
        public void RequestInvokesAssetsRefreshedCallbackWhenNotCompiling()
        {
            ManualRecompile.IsCompilationPendingEvaluator = () => false;
            ManualRecompile.SkipCompilationRequestForTests = true;

            bool assetsRefreshed = false;
            ManualRecompile.AssetsRefreshedForTests = () => assetsRefreshed = true;

            LogAssert.Expect(
                LogType.Log,
                new Regex("Asset database refreshed", RegexOptions.IgnoreCase)
            );

            ManualRecompile.RequestFromMenu();

            Assert.IsTrue(
                assetsRefreshed,
                "AssetsRefreshedForTests callback should be invoked when not compiling."
            );
        }

        [Test]
        public void RequestInvokesCompilationCallbackWhenSkipFlagIsFalse()
        {
            ManualRecompile.IsCompilationPendingEvaluator = () => false;
            ManualRecompile.SkipCompilationRequestForTests = false;

            bool compileRequested = false;
            ManualRecompile.CompilationRequestedForTests = () => compileRequested = true;

            LogAssert.Expect(
                LogType.Log,
                new Regex(
                    "Refreshed assets and requested script compilation",
                    RegexOptions.IgnoreCase
                )
            );

            ManualRecompile.RequestFromMenu();

            Assert.IsTrue(
                compileRequested,
                "CompilationRequestedForTests callback should be invoked when SkipCompilationRequestForTests is false."
            );
        }

        [TestCase(true, false, TestName = "CompilationPendingPreventsRequest")]
        [TestCase(false, true, TestName = "NoCompilationPendingAllowsRequest")]
        public void RequestBehaviorDependsOnCompilationState(bool isCompiling, bool expectRefresh)
        {
            ManualRecompile.IsCompilationPendingEvaluator = () => isCompiling;
            ManualRecompile.SkipCompilationRequestForTests = true;

            bool assetsRefreshed = false;
            ManualRecompile.AssetsRefreshedForTests = () => assetsRefreshed = true;

            if (isCompiling)
            {
                LogAssert.Expect(
                    LogType.Log,
                    new Regex("Script compilation already in progress", RegexOptions.IgnoreCase)
                );
            }
            else
            {
                LogAssert.Expect(
                    LogType.Log,
                    new Regex("Asset database refreshed", RegexOptions.IgnoreCase)
                );
            }

            ManualRecompile.RequestFromMenu();

            Assert.AreEqual(
                expectRefresh,
                assetsRefreshed,
                $"Assets refresh state mismatch. IsCompiling: {isCompiling}, Expected refresh: {expectRefresh}, Actual refresh: {assetsRefreshed}"
            );
        }

        [Test]
        public void IsCompilationPendingEvaluatorSetToNullRestoresDefault()
        {
            Func<bool> customEvaluator = () => true;
            ManualRecompile.IsCompilationPendingEvaluator = customEvaluator;

            ManualRecompile.IsCompilationPendingEvaluator = null;

            ManualRecompile.SkipCompilationRequestForTests = true;

            bool assetsRefreshed = false;
            ManualRecompile.AssetsRefreshedForTests = () => assetsRefreshed = true;

            ManualRecompile.RequestFromMenu();

            bool compilingAtTimeOfTest = EditorApplication.isCompiling;
            if (compilingAtTimeOfTest)
            {
                Assert.IsFalse(
                    assetsRefreshed,
                    "When evaluator reset to default and Unity is compiling, refresh should be skipped."
                );
            }
        }

        private static void EnsureParentDirectoryExists(string absolutePath)
        {
            string directoryPath = Path.GetDirectoryName(absolutePath) ?? string.Empty;

            if (directoryPath.Length > 0)
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        private static string GetAbsolutePath(string relativePath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string normalizedRelativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(projectRoot, normalizedRelativePath);
        }

        private static string ResolveTempFolderRelativePath()
        {
            string testsFolder = DirectoryHelper.FindAbsolutePathToDirectory("Tests/Editor/Tools");
            if (string.IsNullOrEmpty(testsFolder))
            {
                throw new InvalidOperationException(
                    "Unable to resolve Tests/Editor/Tools folder via DirectoryHelper. Ensure the test file lives inside the package."
                );
            }

            return $"{testsFolder.TrimEnd('/')}/TempManualRecompile";
        }
    }
}
