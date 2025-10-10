namespace WallstopStudios.UnityHelpers.Tests.Utils
{
#if UNITY_EDITOR
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class ScriptableObjectSingletonCreatorEditorTests : CommonTestBase
    {
        private const string ResourcesRoot = "Assets/Resources";
        private const string TargetFolder = ResourcesRoot + "/Tests/CreatorPath";
        private const string TargetAssetPath = TargetFolder + "/CreatorPathSingleton.asset";
        private const string WrongFolder = ResourcesRoot + "/Tests/WrongPath";
        private const string WrongAssetPath = WrongFolder + "/CreatorPathSingleton.asset";

        [SetUp]
        public void SetUp()
        {
            ScriptableObjectSingletonCreator.IncludeTestAssemblies = true;
            DeleteAssetIfExists(TargetAssetPath);
            DeleteAssetIfExists(WrongAssetPath);
            DeleteFolderHierarchy(TargetFolder);
            DeleteFolderHierarchy(WrongFolder);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            DeleteAssetIfExists(TargetAssetPath);
            DeleteAssetIfExists(WrongAssetPath);
            DeleteFolderHierarchy(TargetFolder);
            DeleteFolderHierarchy(WrongFolder);
            ScriptableObjectSingletonCreator.IncludeTestAssemblies = false;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [Test]
        public void CreatesAssetAtAttributePath()
        {
            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            CreatorPathSingleton asset = AssetDatabase.LoadAssetAtPath<CreatorPathSingleton>(
                TargetAssetPath
            );
            Assert.IsTrue(asset != null);
        }

        [Test]
        public void RelocatesExistingAssetToAttributePath()
        {
            EnsureFolder(ResourcesRoot);
            EnsureFolder(WrongFolder);
            CreatorPathSingleton instance = ScriptableObject.CreateInstance<CreatorPathSingleton>();
            AssetDatabase.CreateAsset(instance, WrongAssetPath);
            AssetDatabase.SaveAssets();

            ScriptableObjectSingletonCreator.EnsureSingletonAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            CreatorPathSingleton relocated = AssetDatabase.LoadAssetAtPath<CreatorPathSingleton>(
                TargetAssetPath
            );
            Assert.IsTrue(relocated != null);
            Assert.IsTrue(
                AssetDatabase.LoadAssetAtPath<CreatorPathSingleton>(WrongAssetPath) == null
            );
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] parts = folderPath.Split('/');
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

        private static void DeleteAssetIfExists(string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        private static void DeleteFolderHierarchy(string folderPath)
        {
            string normalized = folderPath.SanitizePath();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            while (
                !string.IsNullOrEmpty(normalized)
                && !string.Equals(
                    normalized,
                    ResourcesRoot,
                    System.StringComparison.OrdinalIgnoreCase
                )
            )
            {
                if (!AssetDatabase.IsValidFolder(normalized))
                {
                    normalized = Path.GetDirectoryName(normalized)?.Replace('\\', '/');
                    continue;
                }

                if (!AssetDatabase.DeleteAsset(normalized))
                {
                    break;
                }

                normalized = Path.GetDirectoryName(normalized)?.Replace('\\', '/');
            }
        }

        [ScriptableSingletonPath("Tests/CreatorPath")]
        private sealed class CreatorPathSingleton
            : ScriptableObjectSingleton<CreatorPathSingleton> { }
    }
#endif
}
