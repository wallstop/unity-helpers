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
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using Object = UnityEngine.Object;

    public sealed class ScriptableObjectSingletonCreatorEditorTests : CommonTestBase
    {
        private const string ResourcesRoot = "Assets/Resources";
        private const string TargetFolder = ResourcesRoot + "/Tests/CreatorPath";
        private const string TargetAssetPath = TargetFolder + "/CreatorPathSingleton.asset";
        private const string NestedTargetFolder = ResourcesRoot + "/Tests/Nested/DeepPath";
        private const string NestedTargetAssetPath =
            NestedTargetFolder + "/NestedDiskSingleton.asset";
        private const string WrongFolder = ResourcesRoot + "/Tests/WrongPath";
        private const string WrongAssetPath = WrongFolder + "/CreatorPathSingleton.asset";
        private const string WrongFolderCaseVariant = ResourcesRoot + "/TestS/WrongPath";
        private const string WrongAssetPathCaseVariant =
            WrongFolderCaseVariant + "/CreatorPathSingleton.asset";

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            ScriptableObjectSingletonCreator.IncludeTestAssemblies = true;
            ScriptableObjectSingletonCreator.TypeFilter = static type =>
                type == typeof(CreatorPathSingleton) || type == typeof(NestedDiskSingleton);
            ScriptableObjectSingletonCreator.DisableAutomaticRetries = false;
            ScriptableObjectSingletonCreator.ResetRetryStateForTests();
            DeleteAssetIfExists(TargetAssetPath);
            yield return null;
            DeleteAssetIfExists(WrongAssetPath);
            yield return null;
            DeleteAssetIfExists(WrongAssetPathCaseVariant);
            yield return null;
            DeleteAssetIfExists(NestedTargetAssetPath);
            yield return null;
            DeleteFolderHierarchy(TargetFolder);
            yield return null;
            DeleteFolderHierarchy(WrongFolder);
            yield return null;
            DeleteFolderHierarchy(WrongFolderCaseVariant);
            yield return null;
            DeleteFolderHierarchy(NestedTargetFolder);
            yield return null;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            yield return null;
        }

        [UnityTearDown]
        public override IEnumerator UnityTearDown()
        {
            yield return base.UnityTearDown();
            yield return null;
            DeleteAssetIfExists(TargetAssetPath);
            yield return null;
            DeleteAssetIfExists(WrongAssetPath);
            yield return null;
            DeleteAssetIfExists(WrongAssetPathCaseVariant);
            yield return null;
            DeleteAssetIfExists(NestedTargetAssetPath);
            yield return null;
            DeleteFolderHierarchy(TargetFolder);
            yield return null;
            DeleteFolderHierarchy(WrongFolder);
            yield return null;
            DeleteFolderHierarchy(WrongFolderCaseVariant);
            yield return null;
            DeleteFolderHierarchy(NestedTargetFolder);
            yield return null;
            ScriptableObjectSingletonCreator.IncludeTestAssemblies = false;
            ScriptableObjectSingletonCreator.TypeFilter = null;
            ScriptableObjectSingletonCreator.DisableAutomaticRetries = false;
            ScriptableObjectSingletonCreator.ResetRetryStateForTests();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            yield return null;
        }

        [UnityTest]
        public IEnumerator CreatesAssetAtAttributePath()
        {
            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            yield return null;

            CreatorPathSingleton asset = AssetDatabase.LoadAssetAtPath<CreatorPathSingleton>(
                TargetAssetPath
            );
            Assert.IsTrue(asset != null);
        }

        [UnityTest]
        public IEnumerator RelocatesExistingAssetToAttributePath()
        {
            EnsureFolder(ResourcesRoot);
            yield return null;
            EnsureFolder(WrongFolder);
            yield return null;
            CreatorPathSingleton instance = ScriptableObject.CreateInstance<CreatorPathSingleton>();
            AssetDatabase.CreateAsset(instance, WrongAssetPath);
            AssetDatabase.SaveAssets();
            yield return null;

            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            yield return null;
            CreatorPathSingleton relocated = AssetDatabase.LoadAssetAtPath<CreatorPathSingleton>(
                TargetAssetPath
            );
            Assert.IsTrue(relocated != null);
            yield return null;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            yield return null;
            Assert.IsTrue(
                AssetDatabase.LoadAssetAtPath<CreatorPathSingleton>(WrongAssetPath) == null
            );
        }

        private static void EnsureFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            string[] parts = folderPath.Split('/');
            if (parts.Length == 0)
            {
                return;
            }

            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string desired = parts[i];
                string target = current + "/" + desired;

                if (!AssetDatabase.IsValidFolder(target))
                {
                    string[] subs = AssetDatabase.GetSubFolders(current);
                    string match = null;
                    if (subs != null)
                    {
                        for (int s = 0; s < subs.Length; s++)
                        {
                            string sub = subs[s];
                            int last = sub.LastIndexOf('/', sub.Length - 1);
                            string name = last >= 0 ? sub.Substring(last + 1) : sub;
                            if (
                                string.Equals(
                                    name,
                                    desired,
                                    System.StringComparison.OrdinalIgnoreCase
                                )
                            )
                            {
                                match = sub;
                                break;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(match))
                    {
                        AssetDatabase.CreateFolder(current, desired);
                        current = target;
                    }
                    else
                    {
                        current = match;
                    }
                }
                else
                {
                    current = target;
                }
            }
        }

        private static void DeleteAssetIfExists(string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        private static void DeleteFolderHierarchy(string folderPath)
        {
            string path = ResolveExistingFolderPath(folderPath);
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            while (
                !string.IsNullOrEmpty(path)
                && !string.Equals(path, ResourcesRoot, System.StringComparison.OrdinalIgnoreCase)
            )
            {
                if (!AssetDatabase.IsValidFolder(path))
                {
                    path = Path.GetDirectoryName(path)?.Replace('\\', '/');
                    continue;
                }

                if (!AssetDatabase.DeleteAsset(path))
                {
                    break;
                }

                path = Path.GetDirectoryName(path)?.Replace('\\', '/');
            }
        }

        private static string ResolveExistingFolderPath(string intended)
        {
            if (string.IsNullOrWhiteSpace(intended))
            {
                return null;
            }

            intended = intended.SanitizePath();
            string[] parts = intended.Split('/');
            if (parts.Length == 0)
            {
                return null;
            }

            string current = parts[0];
            if (!string.Equals(current, "Assets", System.StringComparison.OrdinalIgnoreCase))
            {
                return intended;
            }

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
                    return intended;
                }

                string match = null;
                for (int s = 0; s < subs.Length; s++)
                {
                    string sub = subs[s];
                    int last = sub.LastIndexOf('/', sub.Length - 1);
                    string name = last >= 0 ? sub.Substring(last + 1) : sub;
                    if (string.Equals(name, desired, System.StringComparison.OrdinalIgnoreCase))
                    {
                        match = sub;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(match))
                {
                    return intended;
                }

                current = match;
            }

            return current;
        }

        [UnityTest]
        public IEnumerator RelocatesExistingAssetToAttributePathFromMismatchedParentCase()
        {
            EnsureFolder(ResourcesRoot);
            yield return null;
            EnsureFolder(WrongFolderCaseVariant);
            yield return null;
            CreatorPathSingleton instance = ScriptableObject.CreateInstance<CreatorPathSingleton>();
            AssetDatabase.CreateAsset(instance, WrongAssetPathCaseVariant);
            AssetDatabase.SaveAssets();
            yield return null;
            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            yield return null;

            CreatorPathSingleton relocated = AssetDatabase.LoadAssetAtPath<CreatorPathSingleton>(
                TargetAssetPath
            );
            Assert.IsTrue(relocated != null);
            yield return null;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            yield return null;
            Assert.IsTrue(
                AssetDatabase.LoadAssetAtPath<CreatorPathSingleton>(WrongAssetPathCaseVariant)
                    == null
            );
        }

        private static IEnumerable<DiskFolderScenario> DiskOnlyFolderScenarios()
        {
            yield return new DiskFolderScenario(
                "CreatorPath",
                TargetFolder,
                TargetAssetPath,
                typeof(CreatorPathSingleton)
            );

            yield return new DiskFolderScenario(
                "NestedDeepPath",
                NestedTargetFolder,
                NestedTargetAssetPath,
                typeof(NestedDiskSingleton)
            );
        }

        [UnityTest]
        public IEnumerator CreatesAssetWhenFolderOnlyExistsOnDisk(
            [ValueSource(nameof(DiskOnlyFolderScenarios))] DiskFolderScenario scenario
        )
        {
            EnsureFolder(ResourcesRoot);
            yield return null;
            DeleteAssetIfExists(scenario.AssetPath);
            yield return null;
            DeleteFolderHierarchy(scenario.FolderPath);
            yield return null;

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string absoluteTarget = Path.Combine(
                projectRoot,
                scenario.FolderPath.Replace('/', Path.DirectorySeparatorChar)
            );

            if (Directory.Exists(absoluteTarget))
            {
                Directory.Delete(absoluteTarget, true);
            }

            string metaPath = absoluteTarget + ".meta";
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }

            Directory.CreateDirectory(absoluteTarget);
            yield return null;

            Assert.IsFalse(AssetDatabase.IsValidFolder(scenario.FolderPath));

            Func<Type, bool> originalFilter = ScriptableObjectSingletonCreator.TypeFilter;
            ScriptableObjectSingletonCreator.TypeFilter = type => type == scenario.SingletonType;
            try
            {
                ScriptableObjectSingletonCreator.EnsureSingletonAssets();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                yield return null;
            }
            finally
            {
                ScriptableObjectSingletonCreator.TypeFilter = originalFilter;
            }

            Assert.IsTrue(AssetDatabase.IsValidFolder(scenario.FolderPath));
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<Object>(scenario.AssetPath) != null);
            Assert.IsFalse(AssetDatabase.IsValidFolder(scenario.FolderPath + " 1"));
        }

        public sealed class DiskFolderScenario
        {
            public DiskFolderScenario(
                string name,
                string folderPath,
                string assetPath,
                Type singletonType
            )
            {
                Name = name;
                FolderPath = folderPath;
                AssetPath = assetPath;
                SingletonType = singletonType;
            }

            public string Name { get; }
            public string FolderPath { get; }
            public string AssetPath { get; }
            public Type SingletonType { get; }

            public override string ToString()
            {
                return string.IsNullOrEmpty(Name) ? base.ToString() : Name;
            }
        }

        [UnityTest]
        public IEnumerator SkipsCreationWhenAssetFileExistsButIsNotImported()
        {
            DeleteAssetIfExists(TargetAssetPath);
            yield return null;

            EnsureFolder(TargetFolder);
            DeleteFileIfExists(TargetAssetPath);
            DeleteFileIfExists(TargetAssetPath + ".meta");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            yield return null;

            string absolutePath = GetAbsolutePath(TargetAssetPath);
            string directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write an invalid asset file - Unity will fail to load it
            File.WriteAllText(absolutePath, "pending import");

            // Capture pre-call state for diagnostics
            string existingGuid = AssetDatabase.AssetPathToGUID(TargetAssetPath);
            bool fileExistsBefore = File.Exists(absolutePath);

            // Unity may or may not log an error when trying to load an invalid asset file
            // (depends on Unity version and whether the file has been indexed).
            // We use ignoreFailingMessages to avoid test failures from Unity's internal errors.
            LogAssert.ignoreFailingMessages = true;

            // Expect the "on-disk asset" warning OR the "target path already occupied" warning
            // (depends on whether Unity has a GUID for the path)
            LogAssert.Expect(
                LogType.Warning,
                new Regex(
                    "(on-disk asset|target path already occupied).*CreatorPathSingleton",
                    RegexOptions.IgnoreCase
                )
            );
            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;

            // Re-enable log assertions
            LogAssert.ignoreFailingMessages = false;

            Assert.IsTrue(
                File.Exists(absolutePath),
                $"The invalid file should still exist. Pre-call state: existingGuid='{existingGuid}', fileExistsBefore={fileExistsBefore}"
            );

            // Suppress the error from LoadAssetAtPath on invalid file during assertion
            LogAssert.ignoreFailingMessages = true;
            Object loadedAsset = AssetDatabase.LoadAssetAtPath<Object>(TargetAssetPath);
            LogAssert.ignoreFailingMessages = false;

            Assert.IsTrue(
                loadedAsset == null,
                "No valid asset should be loaded from the invalid file"
            );

            // Clean up the invalid file and its meta
            File.Delete(absolutePath);
            DeleteFileIfExists(TargetAssetPath + ".meta");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            yield return null;

            // Now EnsureSingletonAssets should create the real asset
            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            yield return null;

            Assert.IsTrue(
                AssetDatabase.LoadAssetAtPath<Object>(TargetAssetPath) != null,
                "Valid singleton asset should be created after removing the invalid file"
            );
        }

        [UnityTest]
        public IEnumerator RecreatesAssetWhenGuidRemainsButFileIsMissing()
        {
            DeleteAssetIfExists(TargetAssetPath);
            yield return null;

            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<Object>(TargetAssetPath) != null);

            string absoluteAsset = GetAbsolutePath(TargetAssetPath);
            if (File.Exists(absoluteAsset))
            {
                File.Delete(absoluteAsset);
            }

            AssetDatabase.Refresh();
            yield return null;

            Assert.IsTrue(
                AssetDatabase.AssetPathToGUID(TargetAssetPath).Length > 0,
                "Meta should still exist after deleting only the asset file."
            );
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<Object>(TargetAssetPath) == null);

            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;

            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<Object>(TargetAssetPath) != null);
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
