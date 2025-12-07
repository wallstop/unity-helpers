namespace WallstopStudios.UnityHelpers.Tests.Utils
{
#if UNITY_EDITOR
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.Core.TestTypes;
    using WallstopStudios.UnityHelpers.Utils;
    using Object = UnityEngine.Object;

    public sealed class SingletonAssetPollutionPreventionTests : CommonTestBase
    {
        private const string ResourcesRoot = "Assets/Resources";

        private static readonly Type[] AllTestSingletonTypesWithExclusion = new[]
        {
            typeof(TestSingleton),
            typeof(EmptyPathSingleton),
            typeof(CustomPathSingleton),
            typeof(MultipleInstancesSingleton),
            typeof(ResourceBackedSingleton),
            typeof(DeepPathResourceSingleton),
            typeof(WrongPathFallbackSingleton),
            typeof(MultiAssetScriptableSingleton),
            typeof(LifecycleScriptableSingleton),
            typeof(MissingResourceSingleton),
            typeof(AutoScriptableSingleton),
            typeof(ScriptableMismatchSingleton),
            typeof(SingleLevelPathSingleton),
        };

        private static readonly Type[] TestSingletonTypesWithoutExclusion = new[]
        {
            typeof(CreatorPathSingleton),
            typeof(NestedDiskSingleton),
            typeof(CaseMismatch),
            typeof(Duplicate),
        };

        private bool _previousIncludeTestAssemblies;
        private Func<Type, bool> _previousTypeFilter;
        private bool _previousEditorUiSuppress;
        private readonly List<string> _createdAssets = new();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Clean up any leftover test folders from previous test runs
            CleanupAllKnownTestFolders();
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _previousEditorUiSuppress = EditorUi.Suppress;
            EditorUi.Suppress = true;
            _previousIncludeTestAssemblies = ScriptableObjectSingletonCreator.IncludeTestAssemblies;
            _previousTypeFilter = ScriptableObjectSingletonCreator.TypeFilter;
            ScriptableObjectSingletonCreator.IncludeTestAssemblies = false;
            ScriptableObjectSingletonCreator.TypeFilter = null;
            ScriptableObjectSingletonCreator.DisableAutomaticRetries = true;
            yield return CleanupExistingTestSingletonAssets();
        }

        [UnityTearDown]
        public override IEnumerator UnityTearDown()
        {
            yield return base.UnityTearDown();
            foreach (string path in _createdAssets)
            {
                DeleteAssetIfExists(path);
                yield return null;
            }
            _createdAssets.Clear();
            yield return CleanupExistingTestSingletonAssets();
            yield return CleanupTestFolders();
            ScriptableObjectSingletonCreator.IncludeTestAssemblies = _previousIncludeTestAssemblies;
            ScriptableObjectSingletonCreator.TypeFilter = _previousTypeFilter;
            ScriptableObjectSingletonCreator.DisableAutomaticRetries = false;
            ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression = false;
            EditorUi.Suppress = _previousEditorUiSuppress;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Clean up all known test folders including duplicates
            CleanupAllKnownTestFolders();
        }

        public override void OneTimeTearDown()
        {
            base.OneTimeTearDown();
            // Final cleanup of all test folders
            CleanupAllKnownTestFolders();
        }

        [Test]
        public void AllTestSingletonTypesWithExclusionHaveTheAttribute()
        {
            List<string> missingAttribute = new();
            foreach (Type type in AllTestSingletonTypesWithExclusion)
            {
                if (
                    !ReflectionHelpers.TryGetAttributeSafe<ExcludeFromSingletonCreationAttribute>(
                        type,
                        out _,
                        inherit: false
                    )
                )
                {
                    missingAttribute.Add(type.FullName);
                }
            }

            Assert.IsEmpty(
                missingAttribute,
                $"The following test singleton types are missing [ExcludeFromSingletonCreation] attribute:\n{string.Join("\n", missingAttribute)}"
            );
        }

        [Test]
        public void CreatorTestTypesDoNotHaveExclusionAttribute()
        {
            List<string> hasAttribute = new();
            foreach (Type type in TestSingletonTypesWithoutExclusion)
            {
                if (
                    ReflectionHelpers.TryGetAttributeSafe<ExcludeFromSingletonCreationAttribute>(
                        type,
                        out _,
                        inherit: false
                    )
                )
                {
                    hasAttribute.Add(type.FullName);
                }
            }

            Assert.IsEmpty(
                hasAttribute,
                $"The following creator test types should NOT have [ExcludeFromSingletonCreation] attribute as they are used to test creation logic:\n{string.Join("\n", hasAttribute)}"
            );
        }

        [UnityTest]
        public IEnumerator EnsureSingletonAssetsDoesNotCreateAssetsForExcludedTypes()
        {
            ScriptableObjectSingletonCreator.IncludeTestAssemblies = true;
            ScriptableObjectSingletonCreator.TypeFilter = null;
            // Ensure the metadata folder exists to prevent modal dialogs
            EnsureMetadataFolder();
            ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression = true;

            List<string> beforePaths = new();
            foreach (Type type in AllTestSingletonTypesWithExclusion)
            {
                string path = GetExpectedAssetPath(type);
                if (
                    !string.IsNullOrEmpty(path)
                    && AssetDatabase.LoadAssetAtPath<Object>(path) != null
                )
                {
                    beforePaths.Add(path);
                }
            }

            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            yield return null;

            List<string> unexpectedlyCreated = new();
            foreach (Type type in AllTestSingletonTypesWithExclusion)
            {
                string path = GetExpectedAssetPath(type);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                if (beforePaths.Contains(path))
                {
                    continue;
                }

                Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (asset != null)
                {
                    unexpectedlyCreated.Add($"{type.Name} at {path}");
                    _createdAssets.Add(path);
                }
            }

            Assert.IsEmpty(
                unexpectedlyCreated,
                $"EnsureSingletonAssets unexpectedly created assets for excluded types:\n{string.Join("\n", unexpectedlyCreated)}"
            );
        }

        [UnityTest]
        public IEnumerator EnsureSingletonAssetsRespectsExclusionEvenWithIncludeTestAssembliesTrue()
        {
            ScriptableObjectSingletonCreator.IncludeTestAssemblies = true;
            ScriptableObjectSingletonCreator.TypeFilter = null;
            // Ensure the metadata folder exists to prevent modal dialogs
            EnsureMetadataFolder();
            ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression = true;
            yield return CleanupExistingTestSingletonAssets();

            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            yield return null;

            List<string> createdAssets = new();
            foreach (Type type in AllTestSingletonTypesWithExclusion)
            {
                string path = GetExpectedAssetPath(type);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (asset != null)
                {
                    createdAssets.Add($"{type.Name} at {path}");
                    _createdAssets.Add(path);
                }
            }

            Assert.IsEmpty(
                createdAssets,
                $"Excluded singleton types should never have assets created, even with IncludeTestAssemblies=true:\n{string.Join("\n", createdAssets)}"
            );
        }

        [UnityTest]
        public IEnumerator TypeFilterBypassDoesNotOverrideExclusionAttribute()
        {
            ScriptableObjectSingletonCreator.IncludeTestAssemblies = true;
            ScriptableObjectSingletonCreator.TypeFilter = _ => true;
            // Ensure the metadata folder exists to prevent modal dialogs
            EnsureMetadataFolder();
            ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression = true;
            yield return CleanupExistingTestSingletonAssets();

            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            yield return null;

            List<string> createdAssets = new();
            foreach (Type type in AllTestSingletonTypesWithExclusion)
            {
                string path = GetExpectedAssetPath(type);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (asset != null)
                {
                    createdAssets.Add($"{type.Name} at {path}");
                    _createdAssets.Add(path);
                }
            }

            Assert.IsEmpty(
                createdAssets,
                $"Excluded singleton types should never be created even when TypeFilter returns true:\n{string.Join("\n", createdAssets)}"
            );
        }

        [UnityTest]
        public IEnumerator ResourcesFolderRemainsCleanAfterMultipleEnsureCalls()
        {
            // Ensure the metadata folder exists to prevent modal dialogs
            EnsureMetadataFolder();
            yield return CleanupExistingTestSingletonAssets();

            HashSet<string> initialAssetGuids = new(
                AssetDatabase.FindAssets("t:ScriptableObject", new[] { ResourcesRoot })
            );

            for (int i = 0; i < 3; i++)
            {
                ScriptableObjectSingletonCreator.IncludeTestAssemblies = true;
                ScriptableObjectSingletonCreator.TypeFilter = null;
                ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression = true;
                ScriptableObjectSingletonCreator.EnsureSingletonAssets();
                yield return null;
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                yield return null;
            }

            string[] finalGuids = AssetDatabase.FindAssets(
                "t:ScriptableObject",
                new[] { ResourcesRoot }
            );
            List<string> newAssets = new();
            foreach (string guid in finalGuids)
            {
                if (!initialAssetGuids.Contains(guid))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    foreach (Type type in AllTestSingletonTypesWithExclusion)
                    {
                        if (path.Contains(type.Name))
                        {
                            newAssets.Add(path);
                            _createdAssets.Add(path);
                            break;
                        }
                    }
                }
            }

            Assert.IsEmpty(
                newAssets,
                $"Test singleton assets were unexpectedly created in Resources:\n{string.Join("\n", newAssets)}"
            );
        }

        [Test]
        public void ExcludeFromSingletonCreationAttributeIsDefinedCorrectly()
        {
            Type attributeType = typeof(ExcludeFromSingletonCreationAttribute);
            Assert.IsTrue(
                attributeType.IsSealed,
                "ExcludeFromSingletonCreationAttribute should be sealed"
            );

            AttributeUsageAttribute usage =
                attributeType.GetCustomAttribute<AttributeUsageAttribute>();
            Assert.IsNotNull(
                usage,
                "ExcludeFromSingletonCreationAttribute should have AttributeUsage"
            );
            Assert.AreEqual(
                AttributeTargets.Class,
                usage.ValidOn,
                "ExcludeFromSingletonCreationAttribute should target classes"
            );
            Assert.IsFalse(
                usage.Inherited,
                "ExcludeFromSingletonCreationAttribute should not be inherited"
            );
            Assert.IsFalse(
                usage.AllowMultiple,
                "ExcludeFromSingletonCreationAttribute should not allow multiple"
            );
        }

        [UnityTest]
        public IEnumerator TestTypesWithoutExclusionNotCreatedWhenIncludeTestAssembliesIsFalse()
        {
            ScriptableObjectSingletonCreator.IncludeTestAssemblies = false;
            ScriptableObjectSingletonCreator.TypeFilter = null;
            // Ensure the metadata folder exists to prevent modal dialogs
            EnsureMetadataFolder();
            ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression = true;
            yield return CleanupExistingTestSingletonAssets();

            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            yield return null;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            yield return null;

            List<string> createdAssets = new();
            foreach (Type type in TestSingletonTypesWithoutExclusion)
            {
                string path = GetExpectedAssetPath(type);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (asset != null)
                {
                    createdAssets.Add($"{type.Name} at {path}");
                    _createdAssets.Add(path);
                }
            }

            Assert.IsEmpty(
                createdAssets,
                $"Test types should not be created when IncludeTestAssemblies=false (protected by IsTestType):\n{string.Join("\n", createdAssets)}"
            );
        }

        [Test]
        public void AllTestTypesAreDetectedByIsTestTypeHelper()
        {
            List<string> notDetected = new();
            foreach (Type type in AllTestSingletonTypes)
            {
                if (!TestAssemblyHelper.IsTestType(type))
                {
                    notDetected.Add(type.FullName);
                }
            }

            Assert.IsEmpty(
                notDetected,
                $"The following test singleton types are not detected by TestAssemblyHelper.IsTestType:\n{string.Join("\n", notDetected)}"
            );
        }

        private static string GetExpectedAssetPath(Type type)
        {
            if (
                ReflectionHelpers.TryGetAttributeSafe<ScriptableSingletonPathAttribute>(
                    type,
                    out ScriptableSingletonPathAttribute pathAttr,
                    inherit: false
                ) && !string.IsNullOrWhiteSpace(pathAttr.resourcesPath)
            )
            {
                return $"{ResourcesRoot}/{pathAttr.resourcesPath}/{type.Name}.asset";
            }

            return $"{ResourcesRoot}/{type.Name}.asset";
        }

        private static Type[] AllTestSingletonTypes =>
            new List<Type>(AllTestSingletonTypesWithExclusion)
                .Concat(TestSingletonTypesWithoutExclusion)
                .ToArray();

        private IEnumerator CleanupExistingTestSingletonAssets()
        {
            foreach (Type type in AllTestSingletonTypes)
            {
                string path = GetExpectedAssetPath(type);
                if (!string.IsNullOrEmpty(path))
                {
                    DeleteAssetIfExists(path);
                    yield return null;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            yield return null;
        }

        private static void DeleteAssetIfExists(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            Object existing = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (existing != null || !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath)))
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        private IEnumerator CleanupTestFolders()
        {
            string[] testFolders = new[]
            {
                ResourcesRoot + "/Tests/Nested/DeepPath",
                ResourcesRoot + "/Tests/Nested",
                ResourcesRoot + "/Tests/CreatorPath",
                ResourcesRoot + "/Tests",
                ResourcesRoot + "/CreatorTests/Collision",
                ResourcesRoot + "/CreatorTests/Retry",
                ResourcesRoot + "/CreatorTests/FileBlock",
                ResourcesRoot + "/CreatorTests/NoRetry",
                ResourcesRoot + "/CreatorTests",
                ResourcesRoot + "/CaseTest",
                ResourcesRoot + "/CustomPath",
                ResourcesRoot + "/SingleLevel",
                ResourcesRoot + "/Deep/Nested/Singletons",
                ResourcesRoot + "/Deep/Nested",
                ResourcesRoot + "/Deep",
                ResourcesRoot + "/Missing/Subfolder",
                ResourcesRoot + "/Missing",
            };

            foreach (string folder in testFolders)
            {
                TryDeleteEmptyFolder(folder);
                yield return null;
            }

            TryDeleteEmptyFolder(ResourcesRoot);
            yield return null;
        }

        private static void EnsureMetadataFolder()
        {
            string metadataFolder = "Assets/Resources/Wallstop Studios/Unity Helpers";
            // First, ensure the folder exists on disk to prevent Unity's internal
            // "Moving file failed" modal dialog
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (!string.IsNullOrEmpty(projectRoot))
            {
                string absoluteDirectory = Path.Combine(projectRoot, metadataFolder);
                if (!Directory.Exists(absoluteDirectory))
                {
                    Directory.CreateDirectory(absoluteDirectory);
                }
            }

            // Also register in AssetDatabase
            string[] parts = metadataFolder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private static void TryDeleteEmptyFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] subFolders = AssetDatabase.GetSubFolders(folderPath);
            if (subFolders != null && subFolders.Length > 0)
            {
                return;
            }

            string[] assets = AssetDatabase.FindAssets(string.Empty, new[] { folderPath });
            if (assets != null && assets.Length > 0)
            {
                return;
            }

            AssetDatabase.DeleteAsset(folderPath);
        }
    }
#endif
}
