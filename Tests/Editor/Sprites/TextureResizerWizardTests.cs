namespace WallstopStudios.UnityHelpers.Tests.Editor.Sprites
{
#if UNITY_EDITOR
    using System;
    using System.IO;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;

    public sealed class TextureResizerWizardTests
    {
        private const string Root = "Assets/Temp/TextureResizerWizardTests";

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
        public void ResizesTextureAccordingToMultipliers()
        {
            string path = Path.Combine(Root, "tex.png").Replace('\\', '/');
            CreatePng(path, 16, 10, Color.green);
            AssetDatabase.Refresh();

            var wizard =
                ScriptableObject.CreateInstance<WallstopStudios.UnityHelpers.Editor.Sprites.TextureResizerWizard>();
            Type t = wizard.GetType();

            var texturesField = t.GetField("textures", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(texturesField);
            var list = new System.Collections.Generic.List<Texture2D>
            {
                AssetDatabase.LoadAssetAtPath<Texture2D>(path),
            };
            texturesField.SetValue(wizard, list);

            t.GetField("numResizes", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(wizard, 1);
            t.GetField("pixelsPerUnit", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(wizard, 1);
            t.GetField("widthMultiplier", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(wizard, 1f);
            t.GetField("heightMultiplier", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(wizard, 1f);

            t.GetField("scalingResizeAlgorithm", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(
                    wizard,
                    Enum.Parse(t.GetNestedType("ResizeAlgorithm", BindingFlags.Public)!, "Point")
                );

            MethodInfo onCreate = t.GetMethod(
                "OnWizardCreate",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            Assert.IsNotNull(onCreate, "OnWizardCreate not found");
            onCreate.Invoke(wizard, null);

            AssetDatabase.Refresh();
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.IsNotNull(tex, "Texture should exist after resize");
            Assert.That(tex.width, Is.EqualTo(32), "Width should double");
            Assert.That(tex.height, Is.EqualTo(20), "Height should double");
        }

        [Test]
        public void DoesNothingWhenNumResizesIsZero()
        {
            string path = Path.Combine(Root, "nochange.png").Replace('\\', '/');
            CreatePng(path, 12, 7, Color.blue);
            AssetDatabase.Refresh();
            int w0 = AssetDatabase.LoadAssetAtPath<Texture2D>(path).width;
            int h0 = AssetDatabase.LoadAssetAtPath<Texture2D>(path).height;

            var wizard =
                ScriptableObject.CreateInstance<WallstopStudios.UnityHelpers.Editor.Sprites.TextureResizerWizard>();
            Type t = wizard.GetType();
            t.GetField("numResizes", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(wizard, 0);
            MethodInfo onCreate = t.GetMethod(
                "OnWizardCreate",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            Assert.IsNotNull(onCreate, "OnWizardCreate not found");
            onCreate.Invoke(wizard, null);

            AssetDatabase.Refresh();
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.That(tex.width, Is.EqualTo(w0), "Width should remain unchanged");
            Assert.That(tex.height, Is.EqualTo(h0), "Height should remain unchanged");
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
