namespace WallstopStudios.UnityHelpers.Tests.Editor.Sprites
{
#if UNITY_EDITOR
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;

    public sealed class TextureResizerWizardTests
    {
        private const string Root = "Assets/Temp/TextureResizerWizardTests";
        private const string OutRoot = "Assets/Temp/TextureResizerWizardTests/Out";

        [SetUp]
        public void SetUp()
        {
            EnsureFolder(Root);
            EnsureFolder(OutRoot);
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
            wizard.textures = new System.Collections.Generic.List<Texture2D>
            {
                AssetDatabase.LoadAssetAtPath<Texture2D>(path),
            };
            wizard.numResizes = 1;
            wizard.pixelsPerUnit = 1;
            wizard.widthMultiplier = 1f;
            wizard.heightMultiplier = 1f;
            wizard.scalingResizeAlgorithm = WallstopStudios
                .UnityHelpers
                .Editor
                .Sprites
                .TextureResizerWizard
                .ResizeAlgorithm
                .Point;
            wizard.OnWizardCreate();

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
            wizard.numResizes = 0;
            wizard.OnWizardCreate();

            AssetDatabase.Refresh();
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.That(tex.width, Is.EqualTo(w0), "Width should remain unchanged");
            Assert.That(tex.height, Is.EqualTo(h0), "Height should remain unchanged");
        }

        [Test]
        public void RespectsDryRunAndDoesNotModifyFile()
        {
            string path = Path.Combine(Root, "dry.png").Replace('\\', '/');
            CreatePng(path, 10, 6, Color.white);
            AssetDatabase.Refresh();

            var wizard =
                ScriptableObject.CreateInstance<WallstopStudios.UnityHelpers.Editor.Sprites.TextureResizerWizard>();
            wizard.textures = new System.Collections.Generic.List<Texture2D>
            {
                AssetDatabase.LoadAssetAtPath<Texture2D>(path),
            };
            wizard.numResizes = 1;
            wizard.pixelsPerUnit = 1;
            wizard.widthMultiplier = 1f;
            wizard.heightMultiplier = 1f;
            wizard.dryRun = true;
            wizard.scalingResizeAlgorithm = WallstopStudios
                .UnityHelpers
                .Editor
                .Sprites
                .TextureResizerWizard
                .ResizeAlgorithm
                .Point;
            wizard.OnWizardCreate();

            AssetDatabase.Refresh();
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.That(tex.width, Is.EqualTo(10));
            Assert.That(tex.height, Is.EqualTo(6));
        }

        [Test]
        public void WritesToOutputFolderLeavingOriginalUnchanged()
        {
            string path = Path.Combine(Root, "out.png").Replace('\\', '/');
            CreatePng(path, 8, 4, Color.black);
            AssetDatabase.Refresh();

            var wizard =
                ScriptableObject.CreateInstance<WallstopStudios.UnityHelpers.Editor.Sprites.TextureResizerWizard>();
            wizard.textures = new System.Collections.Generic.List<Texture2D>
            {
                AssetDatabase.LoadAssetAtPath<Texture2D>(path),
            };
            wizard.numResizes = 1;
            wizard.pixelsPerUnit = 1;
            wizard.widthMultiplier = 1f;
            wizard.heightMultiplier = 1f;
            wizard.scalingResizeAlgorithm = WallstopStudios
                .UnityHelpers
                .Editor
                .Sprites
                .TextureResizerWizard
                .ResizeAlgorithm
                .Point;
            var outAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(OutRoot);
            Assert.IsNotNull(outAsset, "Output folder asset missing");
            wizard.outputFolder = outAsset;
            wizard.OnWizardCreate();

            AssetDatabase.Refresh();
            var orig = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.That(orig.width, Is.EqualTo(8));
            Assert.That(orig.height, Is.EqualTo(4));

            string outPath = Path.Combine(OutRoot, "out.png").Replace('\\', '/');
            var outTex = AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
            Assert.IsNotNull(outTex, "Expected resized texture in output folder");
            Assert.That(outTex.width, Is.EqualTo(16));
            Assert.That(outTex.height, Is.EqualTo(8));
        }

        [Test]
        public void MultiplePassesAccumulateSize()
        {
            string path = Path.Combine(Root, "multi.png").Replace('\\', '/');
            CreatePng(path, 16, 10, Color.gray);
            AssetDatabase.Refresh();

            var wizard =
                ScriptableObject.CreateInstance<WallstopStudios.UnityHelpers.Editor.Sprites.TextureResizerWizard>();
            wizard.textures = new System.Collections.Generic.List<Texture2D>
            {
                AssetDatabase.LoadAssetAtPath<Texture2D>(path),
            };
            wizard.numResizes = 2;
            wizard.pixelsPerUnit = 1;
            wizard.widthMultiplier = 1f;
            wizard.heightMultiplier = 1f;
            wizard.scalingResizeAlgorithm = WallstopStudios
                .UnityHelpers
                .Editor
                .Sprites
                .TextureResizerWizard
                .ResizeAlgorithm
                .Point;
            wizard.OnWizardCreate();

            AssetDatabase.Refresh();
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.That(tex.width, Is.EqualTo(64));
            Assert.That(tex.height, Is.EqualTo(40));
        }

        [Test]
        public void RestoresImporterReadabilityAfterRun()
        {
            string path = Path.Combine(Root, "restore.png").Replace('\\', '/');
            CreatePng(path, 8, 8, Color.red);
            AssetDatabase.Refresh();

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(importer);
            importer.isReadable = false;
            importer.SaveAndReimport();

            var wizard =
                ScriptableObject.CreateInstance<WallstopStudios.UnityHelpers.Editor.Sprites.TextureResizerWizard>();
            wizard.textures = new System.Collections.Generic.List<Texture2D>
            {
                AssetDatabase.LoadAssetAtPath<Texture2D>(path),
            };
            wizard.numResizes = 1;
            wizard.pixelsPerUnit = 1000;
            wizard.widthMultiplier = 1000f;
            wizard.heightMultiplier = 1000f;
            wizard.scalingResizeAlgorithm = WallstopStudios
                .UnityHelpers
                .Editor
                .Sprites
                .TextureResizerWizard
                .ResizeAlgorithm
                .Point;
            wizard.OnWizardCreate();

            AssetDatabase.Refresh();
            importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(importer);
            Assert.IsFalse(importer.isReadable, "Importer readability should be restored");
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
