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

        private readonly List<string> createdAssetPaths = new List<string>();

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
                    "Expected new script to remain hidden until we refresh."
                );

                ManualRecompile.SkipCompilationRequestForTests = true;
                ManualRecompile.AssetsRefreshedForTests = () =>
                {
                    MonoScript refreshedScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                        assetRelativePath
                    );
                    Assert.IsTrue(
                        refreshedScript != null,
                        "Refresh should import the pending script before the compile request."
                    );
                };

                LogAssert.Expect(
                    LogType.Log,
                    new Regex(
                        "Asset database refreshed; compilation request skipped",
                        RegexOptions.IgnoreCase
                    )
                );

                ManualRecompile.RequestFromMenu();

                MonoScript scriptAfterRefresh = AssetDatabase.LoadAssetAtPath<MonoScript>(
                    assetRelativePath
                );
                Assert.IsTrue(
                    scriptAfterRefresh != null,
                    "Manual recompile should make the new script visible."
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
