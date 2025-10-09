namespace WallstopStudios.UnityHelpers.Tests.Helper
{
#if UNITY_EDITOR
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Tests.Utils;

    public sealed class ObjectHelpersEditorTests : CommonTestBase
    {
        private const string TempFolder = "Assets/TempObjectHelpersEditorTests";
        private const string AssetName = "TestSO.asset";
        private string _assetPath;

        private sealed class TestSO : ScriptableObject { }

        [SetUp]
        public void Setup()
        {
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
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void SmartDestroyDoesNotDeleteAssetsInEditMode()
        {
            TestSO instance = ScriptableObject.CreateInstance<TestSO>();
            AssetDatabase.CreateAsset(instance, _assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            TestSO loaded = AssetDatabase.LoadAssetAtPath<TestSO>(_assetPath);
            Assert.IsTrue(loaded != null, "Expected asset to be created and loadable");

            // Should not log errors and should not delete the on-disk asset
            loaded.Destroy();

            TestSO reloaded = AssetDatabase.LoadAssetAtPath<TestSO>(_assetPath);
            Assert.IsTrue(reloaded != null, "Expected asset to remain after SmartDestroy");
        }
    }
#endif
}
