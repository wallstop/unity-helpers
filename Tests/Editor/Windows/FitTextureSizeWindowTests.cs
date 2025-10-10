namespace WallstopStudios.UnityHelpers.Tests.Editor.Windows
{
#if UNITY_EDITOR
    using System;
    using System.IO;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;

    public sealed class FitTextureSizeWindowTests
    {
        private const string Root = "Assets/Temp/FitTextureSizeTests";

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
        public void GrowOnlyRaisesToNextPowerOfTwo()
        {
            string path = Path.Combine(Root, "grow.png").Replace('\\', '/');
            CreatePng(path, 300, 100, Color.magenta);
            AssetDatabase.Refresh();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(imp, "Importer should exist");
            imp.maxTextureSize = 128;
            imp.SaveAndReimport();

            var window =
                ScriptableObject.CreateInstance<WallstopStudios.UnityHelpers.Editor.FitTextureSizeWindow>();
            Type t = window.GetType();

            var fitMode = Enum.Parse(
                t.Assembly.GetType("WallstopStudios.UnityHelpers.Editor.FitMode"),
                "GrowOnly"
            );
            t.GetField("_fitMode", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(window, fitMode);

            var listField = t.GetField(
                "_textureSourcePaths",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            Assert.IsNotNull(listField);
            var arr = new System.Collections.Generic.List<UnityEngine.Object>
            {
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Root),
            };
            listField.SetValue(window, arr);

            MethodInfo calc = t.GetMethod(
                "CalculateTextureChanges",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            Assert.IsNotNull(calc, "CalculateTextureChanges not found");

            int count = (int)calc.Invoke(window, new object[] { true });
            Assert.That(count, Is.GreaterThanOrEqualTo(1), "Expected at least one change");

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(imp);
            Assert.That(
                imp.maxTextureSize,
                Is.EqualTo(512),
                "Max size should increase to next POT >= largest dimension"
            );
        }

        [Test]
        public void ShrinkOnlyReducesToTightPowerOfTwo()
        {
            string path = Path.Combine(Root, "shrink.png").Replace('\\', '/');
            CreatePng(path, 300, 100, Color.cyan);
            AssetDatabase.Refresh();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(imp, "Importer should exist");
            imp.maxTextureSize = 2048;
            imp.SaveAndReimport();

            var window =
                ScriptableObject.CreateInstance<WallstopStudios.UnityHelpers.Editor.FitTextureSizeWindow>();
            Type t = window.GetType();

            var fitMode = Enum.Parse(
                t.Assembly.GetType("WallstopStudios.UnityHelpers.Editor.FitMode"),
                "ShrinkOnly"
            );
            t.GetField("_fitMode", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(window, fitMode);

            var listField = t.GetField(
                "_textureSourcePaths",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            Assert.IsNotNull(listField);
            var arr = new System.Collections.Generic.List<UnityEngine.Object>
            {
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Root),
            };
            listField.SetValue(window, arr);

            MethodInfo calc = t.GetMethod(
                "CalculateTextureChanges",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            Assert.IsNotNull(calc, "CalculateTextureChanges not found");

            int count = (int)calc.Invoke(window, new object[] { true });
            Assert.That(count, Is.GreaterThanOrEqualTo(1), "Expected at least one change");

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(imp);
            Assert.That(
                imp.maxTextureSize,
                Is.EqualTo(256),
                "Max size should shrink to tight POT above size"
            );
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
