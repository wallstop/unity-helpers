namespace WallstopStudios.UnityHelpers.Tests.Tags
{
#if UNITY_EDITOR
    using System.Collections;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Editor.Tags;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Tags;
    using WallstopStudios.UnityHelpers.Tests.Core;

    [TestFixture]
    public sealed class AttributeMetadataCacheGeneratorTests : CommonTestBase
    {
        private const string CacheAssetPath =
            "Assets/Resources/Wallstop Studios/AttributeMetadataCache.asset";
        private const string CacheFolder = "Assets/Resources/Wallstop Studios";

        private bool _assetExistedBefore;
        private string _backupPath;
        private bool _previousAllowAssetCreationDuringSuppression;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Reset asset editing depth in case previous test left it in a bad state
            ScriptableObjectSingletonMetadataUtility.ResetAssetEditingDepthForTesting();

            _previousAllowAssetCreationDuringSuppression =
                ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression;
            ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression = true;

            // Force a refresh to ensure we have the latest state
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            yield return null;

            _assetExistedBefore =
                AssetDatabase.LoadAssetAtPath<AttributeMetadataCache>(CacheAssetPath) != null;

            if (_assetExistedBefore)
            {
                _backupPath =
                    "Assets/Resources/Wallstop Studios/AttributeMetadataCache_Backup.asset";
                bool copySuccess = TryCopyAssetSilent(CacheAssetPath, _backupPath);
                if (!copySuccess)
                {
                    Debug.LogWarning(
                        $"[{nameof(AttributeMetadataCacheGeneratorTests)}] Failed to backup {CacheAssetPath} to {_backupPath}. Test may not properly restore state."
                    );
                }
            }

            yield return null;
        }

        [UnityTearDown]
        public override IEnumerator UnityTearDown()
        {
            yield return base.UnityTearDown();

            if (_assetExistedBefore && !string.IsNullOrEmpty(_backupPath))
            {
                if (AssetDatabase.LoadAssetAtPath<AttributeMetadataCache>(_backupPath) != null)
                {
                    AssetDatabase.DeleteAsset(CacheAssetPath);
                    string moveError = AssetDatabase.MoveAsset(_backupPath, CacheAssetPath);
                    if (!string.IsNullOrEmpty(moveError))
                    {
                        Debug.LogWarning(
                            $"[{nameof(AttributeMetadataCacheGeneratorTests)}] Failed to restore backup: {moveError}"
                        );
                    }
                }
            }

            AssetDatabase.SaveAssets();
            ImportAssetIfExists(CacheAssetPath);
            ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression =
                _previousAllowAssetCreationDuringSuppression;
            yield return null;
        }

        private static void ImportAssetIfExists(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
        }

        [UnityTest]
        public IEnumerator GenerateCacheCreatesAssetWhenMissing()
        {
            if (_assetExistedBefore)
            {
                AssetDatabase.DeleteAsset(CacheAssetPath);
                yield return null;
            }

            Assert.IsTrue(
                AssetDatabase.LoadAssetAtPath<AttributeMetadataCache>(CacheAssetPath) == null,
                "Setup: Cache asset should not exist before test"
            );

            AttributeMetadataCacheGenerator.GenerateCache();
            yield return null;

            AssetDatabase.SaveAssets();
            ImportAssetIfExists(CacheAssetPath);
            yield return null;

            bool folderExists = AssetDatabase.IsValidFolder(CacheFolder);
            AttributeMetadataCache cache = AssetDatabase.LoadAssetAtPath<AttributeMetadataCache>(
                CacheAssetPath
            );
            Assert.IsTrue(
                cache != null,
                $"GenerateCache should create the cache asset when missing. "
                    + $"FolderExists={folderExists}, EditorUiSuppress={EditorUi.Suppress}, "
                    + $"AllowAssetCreationDuringSuppression={ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression}"
            );
        }

        [UnityTest]
        public IEnumerator GenerateCacheCreatesFolderHierarchyWhenMissing()
        {
            if (_assetExistedBefore)
            {
                AssetDatabase.DeleteAsset(CacheAssetPath);
                yield return null;
            }

            DeleteFolderIfEmpty(CacheFolder);
            yield return null;

            AssetDatabase.SaveAssets();
            yield return null;

            AttributeMetadataCacheGenerator.GenerateCache();
            yield return null;

            AssetDatabase.SaveAssets();
            ImportAssetIfExists(CacheAssetPath);
            yield return null;

            Assert.IsTrue(
                AssetDatabase.IsValidFolder(CacheFolder),
                $"GenerateCache should create the folder hierarchy. "
                    + $"EditorUiSuppress={EditorUi.Suppress}, "
                    + $"AllowAssetCreationDuringSuppression={ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression}"
            );

            AttributeMetadataCache cache = AssetDatabase.LoadAssetAtPath<AttributeMetadataCache>(
                CacheAssetPath
            );
            Assert.IsTrue(
                cache != null,
                $"GenerateCache should create the cache asset. FolderExists={AssetDatabase.IsValidFolder(CacheFolder)}"
            );
        }

        [UnityTest]
        public IEnumerator GenerateCacheDoesNotThrowWhenAssetAlreadyExists()
        {
            AttributeMetadataCacheGenerator.GenerateCache();
            yield return null;

            AssetDatabase.SaveAssets();
            ImportAssetIfExists(CacheAssetPath);
            yield return null;

            AttributeMetadataCache firstCache =
                AssetDatabase.LoadAssetAtPath<AttributeMetadataCache>(CacheAssetPath);
            Assert.IsTrue(
                firstCache != null,
                $"First GenerateCache should create the asset. "
                    + $"FolderExists={AssetDatabase.IsValidFolder(CacheFolder)}, "
                    + $"EditorUiSuppress={EditorUi.Suppress}, "
                    + $"AllowAssetCreationDuringSuppression={ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression}"
            );

            Assert.DoesNotThrow(
                () => AttributeMetadataCacheGenerator.GenerateCache(),
                "GenerateCache should not throw when cache already exists"
            );
            yield return null;

            AttributeMetadataCache secondCache =
                AssetDatabase.LoadAssetAtPath<AttributeMetadataCache>(CacheAssetPath);
            Assert.IsTrue(
                secondCache != null,
                "Cache should still exist after second GenerateCache call"
            );
        }

        [UnityTest]
        public IEnumerator GenerateCacheIsIdempotent()
        {
            for (int i = 0; i < 3; i++)
            {
                AttributeMetadataCacheGenerator.GenerateCache();
                yield return null;

                AssetDatabase.SaveAssets();
                ImportAssetIfExists(CacheAssetPath);
                yield return null;

                bool folderExists = AssetDatabase.IsValidFolder(CacheFolder);
                AttributeMetadataCache cache =
                    AssetDatabase.LoadAssetAtPath<AttributeMetadataCache>(CacheAssetPath);
                Assert.IsTrue(
                    cache != null,
                    $"Cache should exist after iteration {i + 1}. FolderExists={folderExists}, "
                        + $"EditorUiSuppress={EditorUi.Suppress}, "
                        + $"AllowAssetCreationDuringSuppression={ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression}"
                );
            }
        }

        [UnityTest]
        public IEnumerator GenerateCachePreservesWallstopStudiosFolder()
        {
            AttributeMetadataCacheGenerator.GenerateCache();
            yield return null;

            AssetDatabase.SaveAssets();
            ImportAssetIfExists(CacheAssetPath);
            yield return null;

            Assert.IsTrue(
                AssetDatabase.IsValidFolder(CacheFolder),
                $"Wallstop Studios folder should be preserved after cache generation. "
                    + $"EditorUiSuppress={EditorUi.Suppress}, "
                    + $"AllowAssetCreationDuringSuppression={ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression}"
            );
        }

        [UnityTest]
        public IEnumerator GenerateCacheRespectsSuppressionFlagWhenNotAllowed()
        {
            // Always delete the cache to ensure clean state for this test
            if (AssetDatabase.LoadAssetAtPath<AttributeMetadataCache>(CacheAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(CacheAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                yield return null;
            }

            AttributeMetadataCache cacheCheck =
                AssetDatabase.LoadAssetAtPath<AttributeMetadataCache>(CacheAssetPath);
            Assert.IsTrue(
                cacheCheck == null,
                $"Setup: Cache asset should not exist before test. Path: '{CacheAssetPath}', "
                    + $"Exists: {cacheCheck != null}, AssetExistedBefore: {_assetExistedBefore}"
            );

            bool originalSuppress = EditorUi.Suppress;
            bool originalAllow =
                ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression;
            try
            {
                EditorUi.Suppress = true;
                ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression = false;

                AttributeMetadataCacheGenerator.GenerateCache();
                yield return null;

                AssetDatabase.SaveAssets();
                ImportAssetIfExists(CacheAssetPath);
                yield return null;

                AttributeMetadataCache cache =
                    AssetDatabase.LoadAssetAtPath<AttributeMetadataCache>(CacheAssetPath);
                Assert.IsTrue(
                    cache == null,
                    "GenerateCache should NOT create cache when EditorUi.Suppress=true and AllowAssetCreationDuringSuppression=false"
                );
            }
            finally
            {
                EditorUi.Suppress = originalSuppress;
                ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression =
                    originalAllow;
            }
        }

        [UnityTest]
        public IEnumerator GenerateCacheCreatesAssetWhenSuppressionAllowed()
        {
            // Always delete the cache to ensure clean state for this test
            if (AssetDatabase.LoadAssetAtPath<AttributeMetadataCache>(CacheAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(CacheAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                yield return null;
            }

            AttributeMetadataCache cacheCheck =
                AssetDatabase.LoadAssetAtPath<AttributeMetadataCache>(CacheAssetPath);
            Assert.IsTrue(
                cacheCheck == null,
                $"Setup: Cache asset should not exist before test. Path: '{CacheAssetPath}', "
                    + $"Exists: {cacheCheck != null}, AssetExistedBefore: {_assetExistedBefore}"
            );

            bool originalSuppress = EditorUi.Suppress;
            bool originalAllow =
                ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression;
            try
            {
                EditorUi.Suppress = true;
                ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression = true;

                AttributeMetadataCacheGenerator.GenerateCache();
                yield return null;

                AssetDatabase.SaveAssets();
                ImportAssetIfExists(CacheAssetPath);
                yield return null;

                AttributeMetadataCache cache =
                    AssetDatabase.LoadAssetAtPath<AttributeMetadataCache>(CacheAssetPath);
                Assert.IsTrue(
                    cache != null,
                    "GenerateCache SHOULD create cache when EditorUi.Suppress=true and AllowAssetCreationDuringSuppression=true"
                );
            }
            finally
            {
                EditorUi.Suppress = originalSuppress;
                ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression =
                    originalAllow;
            }
        }

        private static void DeleteFolderIfEmpty(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
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
