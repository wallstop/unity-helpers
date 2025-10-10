namespace WallstopStudios.UnityHelpers.Tests.Editor.Sprites
{
#if UNITY_EDITOR
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Sprites;
    using Object = UnityEngine.Object;

    public sealed class TextureSettingsApplierWizardAdditionalTests
    {
        private const string Root = "Assets/Temp/TextureSettingsApplierWizardAdditionalTests";

        [SetUp]
        public void SetUp()
        {
            EnsureFolder(Root);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset("Assets/Temp");
            AssetDatabase.Refresh();
        }

        [Test]
        public void AppliesSettingsToExplicitTexturesOnly()
        {
            string dir = Root.Replace('\\', '/');
            string included = (dir + "/inc.png").Replace('\\', '/');
            string other = (dir + "/other.png").Replace('\\', '/');
            CreatePng(included, 16, 16, Color.white);
            CreatePng(other, 16, 16, Color.white);
            AssetDatabase.Refresh();

            // Set initial different values so we can detect changes
            TextureImporter impIncluded = AssetImporter.GetAtPath(included) as TextureImporter;
            impIncluded.isReadable = true;
            impIncluded.wrapMode = TextureWrapMode.Repeat;
            impIncluded.filterMode = FilterMode.Point;
            impIncluded.mipmapEnabled = true;
            impIncluded.SaveAndReimport();

            TextureImporter impOther = AssetImporter.GetAtPath(other) as TextureImporter;
            impOther.isReadable = true;
            impOther.wrapMode = TextureWrapMode.Repeat;
            impOther.filterMode = FilterMode.Point;
            impOther.mipmapEnabled = true;
            impOther.SaveAndReimport();

            TextureSettingsApplier wizard =
                ScriptableObject.CreateInstance<TextureSettingsApplier>();
            wizard.textures = new System.Collections.Generic.List<Texture2D>
            {
                AssetDatabase.LoadAssetAtPath<Texture2D>(included),
            };
            wizard.directories = new System.Collections.Generic.List<Object>(); // none
            wizard.applyReadOnly = true;
            wizard.isReadOnly = true; // expect isReadable = false
            wizard.applyWrapMode = true;
            wizard.wrapMode = TextureWrapMode.Clamp;
            wizard.applyFilterMode = true;
            wizard.filterMode = FilterMode.Bilinear;
            wizard.applyMipMaps = true;
            wizard.generateMipMaps = false;
            wizard.maxTextureSize = 64;

            wizard.OnWizardCreate();
            AssetDatabase.Refresh();

            impIncluded = AssetImporter.GetAtPath(included) as TextureImporter;
            impOther = AssetImporter.GetAtPath(other) as TextureImporter;
            Assert.IsNotNull(impIncluded);
            Assert.IsNotNull(impOther);

            Assert.That(impIncluded.isReadable, Is.False);
            Assert.That(impIncluded.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(impIncluded.filterMode, Is.EqualTo(FilterMode.Bilinear));
            Assert.That(impIncluded.mipmapEnabled, Is.False);
            Assert.That(impIncluded.maxTextureSize, Is.EqualTo(64));

            // Not listed in textures and no directories: should remain unchanged
            Assert.That(impOther.isReadable, Is.True);
            Assert.That(impOther.wrapMode, Is.EqualTo(TextureWrapMode.Repeat));
            Assert.That(impOther.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(impOther.mipmapEnabled, Is.True);
        }

        [Test]
        public void DirectoryRecursionHonorsExtensionFilter()
        {
            string dirA = (Root + "/A").Replace('\\', '/');
            string dirB = (dirA + "/B").Replace('\\', '/');
            EnsureFolder(dirA);
            EnsureFolder(dirB);
            string png = (dirB + "/tex.png").Replace('\\', '/');
            string jpg = (dirB + "/tex.jpg").Replace('\\', '/');
            CreatePng(png, 8, 8, Color.white);
            CreateJpg(jpg, 8, 8, Color.white);
            AssetDatabase.Refresh();

            TextureImporter impPng = AssetImporter.GetAtPath(png) as TextureImporter;
            impPng.isReadable = true;
            impPng.wrapMode = TextureWrapMode.Repeat;
            impPng.filterMode = FilterMode.Point;
            impPng.mipmapEnabled = true;
            impPng.SaveAndReimport();

            TextureImporter impJpg = AssetImporter.GetAtPath(jpg) as TextureImporter;
            impJpg.isReadable = true;
            impJpg.wrapMode = TextureWrapMode.Repeat;
            impJpg.filterMode = FilterMode.Point;
            impJpg.mipmapEnabled = true;
            impJpg.SaveAndReimport();

            TextureSettingsApplier wizard =
                ScriptableObject.CreateInstance<TextureSettingsApplier>();
            wizard.textures = new System.Collections.Generic.List<Texture2D>();
            wizard.directories = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };
            wizard.spriteFileExtensions = new System.Collections.Generic.List<string> { ".png" }; // only png
            wizard.applyReadOnly = true;
            wizard.isReadOnly = true;
            wizard.applyWrapMode = true;
            wizard.wrapMode = TextureWrapMode.Clamp;
            wizard.applyFilterMode = true;
            wizard.filterMode = FilterMode.Bilinear;
            wizard.applyMipMaps = true;
            wizard.generateMipMaps = false;
            wizard.maxTextureSize = 32;

            wizard.OnWizardCreate();
            AssetDatabase.Refresh();

            impPng = AssetImporter.GetAtPath(png) as TextureImporter;
            impJpg = AssetImporter.GetAtPath(jpg) as TextureImporter;

            // png affected
            Assert.That(impPng.isReadable, Is.False);
            Assert.That(impPng.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(impPng.filterMode, Is.EqualTo(FilterMode.Bilinear));
            Assert.That(impPng.mipmapEnabled, Is.False);
            Assert.That(impPng.maxTextureSize, Is.EqualTo(32));

            // jpg not affected due to extension filter
            Assert.That(impJpg.isReadable, Is.True);
            Assert.That(impJpg.wrapMode, Is.EqualTo(TextureWrapMode.Repeat));
            Assert.That(impJpg.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(impJpg.mipmapEnabled, Is.True);
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
            EnsureFolder(Path.GetDirectoryName(relPath).Replace('\\', '/'));
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = c;
            t.SetPixels(pix);
            t.Apply();
            File.WriteAllBytes(RelToFull(relPath), t.EncodeToPNG());
        }

        private static void CreateJpg(string relPath, int w, int h, Color c)
        {
            EnsureFolder(Path.GetDirectoryName(relPath).Replace('\\', '/'));
            Texture2D t = new Texture2D(w, h, TextureFormat.RGB24, false);
            Color[] pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = c;
            t.SetPixels(pix);
            t.Apply();
            File.WriteAllBytes(RelToFull(relPath), t.EncodeToJPG());
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
