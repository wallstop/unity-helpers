namespace WallstopStudios.UnityHelpers.Tests.Editor.Utils
{
#if UNITY_EDITOR
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Tags;
    using WallstopStudios.UnityHelpers.Utils;

    [ScriptableSingletonPath("Tests/Singletons")]
    public sealed class MyTestSingleton : ScriptableObjectSingleton<MyTestSingleton>
    {
        public int value;
    }

    public sealed class ScriptableObjectSingletonCreatorTests
    {
        private const string ResourcesRoot = "Assets/Resources";
        private const string TargetFolder = ResourcesRoot + "/Tests/Singletons";
        private const string TargetAsset = TargetFolder + "/MyTestSingleton.asset";

        [SetUp]
        public void SetUp()
        {
            DeleteIfExists("Assets/SomeOther");
            DeleteIfExists(ResourcesRoot);
            AssetDatabase.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            DeleteIfExists("Assets/SomeOther");
            DeleteIfExists(ResourcesRoot);
            AssetDatabase.Refresh();
        }

        [Test]
        public void CreatesMissingSingletonAtExpectedPath()
        {
            SetIncludeTestAssemblies(true);
            InvokeEnsureSingletonAssets();

            MyTestSingleton asset = AssetDatabase.LoadAssetAtPath<MyTestSingleton>(TargetAsset);
            Assert.IsNotNull(asset, "Expected singleton asset to be created at target path");
        }

        [Test]
        public void RelocatesExistingSingletonToTarget()
        {
            string wrongFolder = "Assets/SomeOther";
            EnsureFolder(wrongFolder);

            AttributeMetadataCache inst = ScriptableObject.CreateInstance<AttributeMetadataCache>();
            string wrongPath = wrongFolder + "/AttributeMetadataCache.asset";
            AssetDatabase.CreateAsset(inst, wrongPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SetIncludeTestAssemblies(true);
            InvokeEnsureSingletonAssets();

            string target =
                ResourcesRoot
                + "/Wallstop Studios/AttributeMetadataCache/AttributeMetadataCache.asset";
            AttributeMetadataCache moved = AssetDatabase.LoadAssetAtPath<AttributeMetadataCache>(
                target
            );
            Assert.IsNotNull(moved, "Expected asset to be moved to target path");
            Assert.IsNull(
                AssetDatabase.LoadAssetAtPath<AttributeMetadataCache>(wrongPath),
                "Old location should no longer contain asset"
            );
        }

        private static void SetIncludeTestAssemblies(bool value)
        {
            WallstopStudios
                .UnityHelpers
                .Editor
                .Utils
                .ScriptableObjectSingletonCreator
                .IncludeTestAssemblies = value;
        }

        private static void InvokeEnsureSingletonAssets()
        {
            WallstopStudios.UnityHelpers.Editor.Utils.ScriptableObjectSingletonCreator.EnsureSingletonAssets();
        }

        private static void EnsureFolder(string relPath)
        {
            string[] parts = relPath.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(cur, parts[i]);
                }
                cur = next;
            }
        }

        private static void DeleteIfExists(string relPath)
        {
            if (AssetDatabase.IsValidFolder(relPath))
            {
                AssetDatabase.DeleteAsset(relPath);
            }
        }
    }
#endif
}
