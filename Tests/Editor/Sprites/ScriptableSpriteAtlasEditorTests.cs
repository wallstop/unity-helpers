namespace WallstopStudios.UnityHelpers.Tests.Editor.Sprites
{
#if UNITY_EDITOR
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.U2D;
    using WallstopStudios.UnityHelpers.Editor.Sprites;
    using WallstopStudios.UnityHelpers.Tests.Utils;

    public sealed class ScriptableSpriteAtlasEditorTests : CommonTestBase
    {
        private const string Root = "Assets/Temp/ScriptableSpriteAtlasEditorTests";

        [SetUp]
        public void SetUp()
        {
            EnsureFolder(Root);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            AssetDatabase.DeleteAsset("Assets/Temp");
            AssetDatabase.Refresh();
        }

        [Test]
        public void GeneratesSpriteAtlasAssetFromConfig()
        {
            // Create a source sprite
            string spritePath = Path.Combine(Root, "icon.png").Replace('\\', '/');
            CreatePng(spritePath, 8, 8, Color.red);
            AssetDatabase.Refresh();

            // Create config asset
            ScriptableSpriteAtlas config = ScriptableObject.CreateInstance<ScriptableSpriteAtlas>();
            config.name = "TestAtlasConfig";
            config.spritesToPack.Add(AssetDatabase.LoadAssetAtPath<Sprite>(spritePath));
            config.outputSpriteAtlasDirectory = Root;
            config.outputSpriteAtlasName = "TestAtlas";
            string configPath = Path.Combine(Root, "TestAtlasConfig.asset").Replace('\\', '/');
            AssetDatabase.CreateAsset(config, configPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Open window and generate all atlases
            ScriptableSpriteAtlasEditor window = Track(
                ScriptableObject.CreateInstance<ScriptableSpriteAtlasEditor>()
            );
            window.LoadAtlasConfigs();
            window.GenerateAllAtlases();

            AssetDatabase.Refresh();

            string atlasPath = Path.Combine(Root, "TestAtlas.spriteatlas").Replace('\\', '/');
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            Assert.IsNotNull(atlas, ".spriteatlas should be generated");
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

        private static void CreatePng(string relPath, int w, int h, Color c)
        {
            string dir = Path.GetDirectoryName(relPath).Replace('\\', '/');
            EnsureFolder(dir);
            Texture2D t = new(w, h, TextureFormat.RGBA32, false);
            Color[] pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = c;
            }

            t.SetPixels(pix);
            t.Apply();
            byte[] data = t.EncodeToPNG();
            File.WriteAllBytes(RelToFull(relPath), data);
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
                .Replace('\\', '/');
        }
    }
#endif
}
