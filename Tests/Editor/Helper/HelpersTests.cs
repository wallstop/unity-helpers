namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class HelpersTests : CommonTestBase
    {
        [Test]
        public void EnumeratePrefabsFindsGeneratedPrefab()
        {
            if (Application.isPlaying)
            {
                Assert.Ignore("AssetDatabase access requires edit mode.");
            }

            const string folder = "Assets/TempHelpersPrefabs";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets", "TempHelpersPrefabs");
            }

            string assetPath = Path.Combine(folder, "TestPrefab.prefab").SanitizePath();
            GameObject prefabSource = Track(new GameObject("Helpers_PrefabSource"));
            try
            {
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prefabSource, assetPath);
                Assert.IsTrue(prefab != null);

                HashSet<string> names = Helpers
                    .EnumeratePrefabs(new[] { folder })
                    .Select(go => go.name)
                    .ToHashSet(StringComparer.Ordinal);
                Assert.Contains(prefab.name, names.ToList());
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.DeleteAsset(folder);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void EnumerateScriptableObjectsFindsAsset()
        {
            if (Application.isPlaying)
            {
                Assert.Ignore("AssetDatabase access requires edit mode.");
            }

            const string folder = "Assets/TempHelpersScriptables";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets", "TempHelpersScriptables");
            }

            string assetPath = Path.Combine(folder, "Dummy.asset").SanitizePath();
            // Don't use Track() - asset becomes persistent via CreateAsset and is cleaned up in finally block
            DummyScriptableObject asset = ScriptableObject.CreateInstance<DummyScriptableObject>();
            try
            {
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                HashSet<string> guids = Helpers
                    .EnumerateScriptableObjects<DummyScriptableObject>(new[] { folder })
                    .Select(so => AssetDatabase.GetAssetPath(so))
                    .ToHashSet(StringComparer.Ordinal);
                Assert.Contains(assetPath, guids.ToList());
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.DeleteAsset(folder);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void GetAllLayerNamesMatch()
        {
            Assert.That(Helpers.GetAllLayerNames(), Is.EqualTo(InternalEditorUtility.layers));

            using PooledResource<List<string>> bufferResource = Buffers<string>.List.Get(
                out List<string> buffer
            );
            Helpers.GetAllLayerNames(buffer);
            Assert.That(buffer, Is.EqualTo(InternalEditorUtility.layers));
        }
    }
}
