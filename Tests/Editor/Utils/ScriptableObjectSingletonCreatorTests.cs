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

            var asset = AssetDatabase.LoadAssetAtPath<MyTestSingleton>(TargetAsset);
            Assert.IsNotNull(asset, "Expected singleton asset to be created at target path");
        }

        [Test]
        public void RelocatesExistingSingletonToTarget()
        {
            string wrongFolder = "Assets/SomeOther";
            EnsureFolder(wrongFolder);

            var inst =
                ScriptableObject.CreateInstance<WallstopStudios.UnityHelpers.Tags.AttributeMetadataCache>();
            string wrongPath = wrongFolder + "/AttributeMetadataCache.asset";
            AssetDatabase.CreateAsset(inst, wrongPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SetIncludeTestAssemblies(true);
            InvokeEnsureSingletonAssets();

            string target =
                ResourcesRoot
                + "/Wallstop Studios/AttributeMetadataCache/AttributeMetadataCache.asset";
            var moved =
                AssetDatabase.LoadAssetAtPath<WallstopStudios.UnityHelpers.Tags.AttributeMetadataCache>(
                    target
                );
            Assert.IsNotNull(moved, "Expected asset to be moved to target path");
            Assert.IsNull(
                AssetDatabase.LoadAssetAtPath<WallstopStudios.UnityHelpers.Tags.AttributeMetadataCache>(
                    wrongPath
                ),
                "Old location should no longer contain asset"
            );
        }

        private static void SetIncludeTestAssemblies(bool value)
        {
            Type t =
                typeof(WallstopStudios.UnityHelpers.Editor.Utils.ScriptableObjectSingletonCreator);
            PropertyInfo p = t.GetProperty(
                "IncludeTestAssemblies",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public
            );
            Assert.IsNotNull(p, "IncludeTestAssemblies property not found");
            p.SetValue(null, value);
        }

        private static void InvokeEnsureSingletonAssets()
        {
            Type t =
                typeof(WallstopStudios.UnityHelpers.Editor.Utils.ScriptableObjectSingletonCreator);
            MethodInfo m = t.GetMethod(
                "EnsureSingletonAssets",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public
            );
            Assert.IsNotNull(m, "EnsureSingletonAssets not found");
            m.Invoke(null, null);
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
