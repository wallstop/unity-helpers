// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Helper
{
#if UNITY_EDITOR
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Tests.Core;

    [TestFixture]
    [NUnit.Framework.Category("Slow")]
    [NUnit.Framework.Category("Integration")]
    public sealed class ObjectHelpersEditorTests : CommonTestBase
    {
        private const string TempFolder = "Assets/TempObjectHelpersEditorTests";
        private const string AssetName = "TestMaterial.mat";
        private string _assetPath;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            if (!AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.CreateFolder("Assets", "TempObjectHelpersEditorTests");
            }
            _assetPath = TempFolder + "/" + AssetName;
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            if (AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.DeleteAsset(_assetPath);
                AssetDatabase.DeleteAsset(TempFolder);
                AssetDatabaseBatchHelper.SaveAndRefreshIfNotBatching();
            }
        }

        [Test]
        public void SmartDestroyDoesNotDeleteAssetsInEditMode()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Assert.Inconclusive("Required shader not found.");
            }
            Material mat = new(shader);
            AssetDatabase.CreateAsset(mat, _assetPath);
            AssetDatabaseBatchHelper.SaveAndRefreshIfNotBatching();

            Material loaded = AssetDatabase.LoadAssetAtPath<Material>(_assetPath);
            Assert.IsTrue(loaded != null, "Expected asset to be created and loadable");

            // Should not log errors and should not delete the on-disk asset
            loaded.Destroy();

            Material reloaded = AssetDatabase.LoadAssetAtPath<Material>(_assetPath);
            Assert.IsTrue(reloaded != null, "Expected asset to remain after SmartDestroy");
        }
    }
#endif
}
