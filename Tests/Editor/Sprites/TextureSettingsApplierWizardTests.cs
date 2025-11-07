namespace WallstopStudios.UnityHelpers.Tests.Sprites
{
#if UNITY_EDITOR
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Sprites;
    using WallstopStudios.UnityHelpers.Tests.Utils;

    public sealed class TextureSettingsApplierWizardTests : CommonTestBase
    {
        private const string Root = "Assets/Temp/TextureSettingsApplierWizardTests";

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
        public void AppliesImporterSettingsToTexturesAndDirectories()
        {
            string a = Path.Combine(Root, "a.png").Replace('\\', '/');
            string bdir = Path.Combine(Root, "Dir").Replace('\\', '/');
            string b = Path.Combine(bdir, "b.png").Replace('\\', '/');
            EnsureFolder(bdir);
            CreatePng(a, 16, 16, Color.white);
            CreatePng(b, 32, 32, Color.white);
            AssetDatabase.Refresh();

            TextureSettingsApplierWindow window = Track(
                ScriptableObject.CreateInstance<TextureSettingsApplierWindow>()
            );

            // Set explicit texture list
            window.textures = new System.Collections.Generic.List<Texture2D>
            {
                AssetDatabase.LoadAssetAtPath<Texture2D>(a),
            };

            // Set directories list
            window.directories = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            // Configure changes
            window.applyReadOnly = true;
            window.isReadOnly = true;
            window.applyMipMaps = true;
            window.generateMipMaps = false;
            window.applyWrapMode = true;
            window.wrapMode = TextureWrapMode.Clamp;
            window.applyFilterMode = true;
            window.filterMode = FilterMode.Bilinear;
            window.maxTextureSize = 128;

            window.ApplySettings();

            AssetDatabase.Refresh();
            TextureImporter impA = AssetImporter.GetAtPath(a) as TextureImporter;
            TextureImporter impB = AssetImporter.GetAtPath(b) as TextureImporter;
            Assert.IsTrue(impA != null);
            Assert.IsTrue(impB != null);

            // Verify a subset of settings applied
            Assert.That(impA.isReadable, Is.False); // isReadOnly=true → not readable
            Assert.That(impA.mipmapEnabled, Is.False);
            Assert.That(impA.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(impA.filterMode, Is.EqualTo(FilterMode.Bilinear));
            Assert.That(impA.maxTextureSize, Is.EqualTo(128));

            Assert.That(impB.isReadable, Is.False);
            Assert.That(impB.mipmapEnabled, Is.False);
            Assert.That(impB.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(impB.filterMode, Is.EqualTo(FilterMode.Bilinear));
            Assert.That(impB.maxTextureSize, Is.EqualTo(128));
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
