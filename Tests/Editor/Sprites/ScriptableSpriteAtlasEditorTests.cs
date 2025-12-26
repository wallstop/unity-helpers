namespace WallstopStudios.UnityHelpers.Tests.Sprites
{
#if UNITY_EDITOR
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.U2D;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.Sprites;
    using WallstopStudios.UnityHelpers.Tests.Core;

    public sealed class ScriptableSpriteAtlasEditorTests : CommonTestBase
    {
        private const string Root = "Assets/Temp/ScriptableSpriteAtlasEditorTests";

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            EnsureFolder(Root);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            // Clean up only tracked folders/assets that this test created
            CleanupTrackedFoldersAndAssets();
        }

        [Test]
        public void GeneratesSpriteAtlasAssetFromConfig()
        {
            // Create a source sprite
            string spritePath = Path.Combine(Root, "icon.png").SanitizePath();
            CreatePng(spritePath, 8, 8, Color.red);
            AssetDatabase.Refresh();

            // Create config asset
            ScriptableSpriteAtlas config = ScriptableObject.CreateInstance<ScriptableSpriteAtlas>(); // UNH-SUPPRESS: Asset becomes persistent via CreateAsset below
            config.name = "TestAtlasConfig";
            config.spritesToPack.Add(AssetDatabase.LoadAssetAtPath<Sprite>(spritePath));
            config.outputSpriteAtlasDirectory = Root;
            config.outputSpriteAtlasName = "TestAtlas";
            string configPath = Path.Combine(Root, "TestAtlasConfig.asset").SanitizePath();
            AssetDatabase.CreateAsset(config, configPath);
            TrackAssetPath(configPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Open window and generate all atlases
            ScriptableSpriteAtlasEditor window = Track(
                ScriptableObject.CreateInstance<ScriptableSpriteAtlasEditor>()
            );
            window.LoadAtlasConfigs();
            window.GenerateAllAtlases();

            AssetDatabase.Refresh();

            string atlasPath = Path.Combine(Root, "TestAtlas.spriteatlas").SanitizePath();
            TrackAssetPath(atlasPath);
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            Assert.IsTrue(atlas != null, ".spriteatlas should be generated");
        }

        private void CreatePng(string relPath, int w, int h, Color c)
        {
            string dir = Path.GetDirectoryName(relPath)?.SanitizePath();
            EnsureFolder(dir);
            Texture2D t = new(w, h, TextureFormat.RGBA32, false);
            try
            {
                Color[] pix = new Color[w * h];
                for (int i = 0; i < pix.Length; i++)
                {
                    pix[i] = c;
                }

                t.SetPixels(pix);
                t.Apply();
                byte[] data = t.EncodeToPNG();
                File.WriteAllBytes(RelToFull(relPath), data);
                TrackAssetPath(relPath);
            }
            finally
            {
                Object.DestroyImmediate(t); // UNH-SUPPRESS: Cleanup temporary texture in finally block
            }
        }

        private static string RelToFull(string rel)
        {
            return Path.Combine(
                    Application.dataPath.Substring(
                        0,
                        Application.dataPath.Length - "Assets".Length
                    ),
                    rel
                )
                .SanitizePath();
        }
    }
#endif
}
