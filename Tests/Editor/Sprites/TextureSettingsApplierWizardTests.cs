namespace WallstopStudios.UnityHelpers.Tests.Editor.Sprites
{
#if UNITY_EDITOR
    using System;
    using System.IO;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;

    public sealed class TextureSettingsApplierWizardTests
    {
        private const string Root = "Assets/Temp/TextureSettingsApplierWizardTests";

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
        public void AppliesImporterSettingsToTexturesAndDirectories()
        {
            string a = Path.Combine(Root, "a.png").Replace('\\', '/');
            string bdir = Path.Combine(Root, "Dir").Replace('\\', '/');
            string b = Path.Combine(bdir, "b.png").Replace('\\', '/');
            EnsureFolder(bdir);
            CreatePng(a, 16, 16, Color.white);
            CreatePng(b, 32, 32, Color.white);
            AssetDatabase.Refresh();

            var wizard =
                ScriptableObject.CreateInstance<WallstopStudios.UnityHelpers.Editor.Sprites.TextureSettingsApplier>();

            // Set explicit texture list
            wizard.textures = new System.Collections.Generic.List<Texture2D>
            {
                AssetDatabase.LoadAssetAtPath<Texture2D>(a),
            };

            // Set directories list
            wizard.directories = new System.Collections.Generic.List<UnityEngine.Object>
            {
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Root),
            };

            // Configure changes
            wizard.applyReadOnly = true;
            wizard.isReadOnly = true;
            wizard.applyMipMaps = true;
            wizard.generateMipMaps = false;
            wizard.applyWrapMode = true;
            wizard.wrapMode = TextureWrapMode.Clamp;
            wizard.applyFilterMode = true;
            wizard.filterMode = FilterMode.Bilinear;
            wizard.maxTextureSize = 128;

            wizard.OnWizardCreate();

            AssetDatabase.Refresh();
            TextureImporter impA = AssetImporter.GetAtPath(a) as TextureImporter;
            TextureImporter impB = AssetImporter.GetAtPath(b) as TextureImporter;
            Assert.IsNotNull(impA);
            Assert.IsNotNull(impB);

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
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = c;
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
