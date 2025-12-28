// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Sprites
{
#if UNITY_EDITOR
    using System.Collections.Generic;
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.Sprites;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Tests for <see cref="SpriteSettingsApplierWindow"/> that verify directory-based
    /// texture searching and settings application work correctly.
    /// </summary>
    /// <remarks>
    /// These tests specifically cover edge cases related to array pooling when
    /// passing directory arrays to AssetDatabase.FindAssets.
    /// </remarks>
    [TestFixture]
    public sealed class SpriteSettingsApplierWindowTests : CommonTestBase
    {
        private const string Root = "Assets/Temp/SpriteSettingsApplierWindowTests";

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
            CleanupTrackedFoldersAndAssets();
        }

        [Test]
        public void GetMatchingFilePathsWithSingleDirectorySucceeds()
        {
            string dir = (Root + "/SingleDir").SanitizePath();
            EnsureFolder(dir);
            string texPath = (dir + "/sprite.png").SanitizePath();
            CreatePng(texPath, 8, 8, Color.white);
            AssetDatabase.Refresh();

            // Mark as sprite
            TextureImporter imp = AssetImporter.GetAtPath(texPath) as TextureImporter;
            Assert.IsTrue(imp != null, $"Expected importer at path '{texPath}' to not be null");
            imp.textureType = TextureImporterType.Sprite;
            imp.SaveAndReimport();

            SpriteSettingsApplierWindow window = Track(
                ScriptableObject.CreateInstance<SpriteSettingsApplierWindow>()
            );

            window.sprites = new List<Sprite>();
            window.directories = new List<Object> { AssetDatabase.LoadAssetAtPath<Object>(dir) };
            window.spriteFileExtensions = new List<string> { ".png" };

            // This internally uses GetMatchingFilePaths which had the array pool bug
            Assert.DoesNotThrow(
                () => window.CalculateStats(),
                "CalculateStats with single directory should not throw"
            );
        }

        [Test]
        public void GetMatchingFilePathsWithMultipleDirectoriesSucceeds()
        {
            // This test specifically targets the bug where SystemArrayPool returned larger arrays
            // than requested, causing null values to be passed to AssetDatabase.FindAssets

            string[] dirs = new string[4];
            string[] textures = new string[4];

            for (int i = 0; i < dirs.Length; i++)
            {
                dirs[i] = (Root + "/MultiDir" + i).SanitizePath();
                EnsureFolder(dirs[i]);
                textures[i] = (dirs[i] + "/sprite" + i + ".png").SanitizePath();
                CreatePng(textures[i], 4, 4, Color.white);
            }

            AssetDatabase.Refresh();

            // Mark all as sprites
            for (int i = 0; i < textures.Length; i++)
            {
                TextureImporter imp = AssetImporter.GetAtPath(textures[i]) as TextureImporter;
                Assert.IsTrue(
                    imp != null,
                    $"Expected importer at path '{textures[i]}' to not be null"
                );
                imp.textureType = TextureImporterType.Sprite;
                imp.SaveAndReimport();
            }

            SpriteSettingsApplierWindow window = Track(
                ScriptableObject.CreateInstance<SpriteSettingsApplierWindow>()
            );

            window.sprites = new List<Sprite>();
            window.directories = new List<Object>();

            for (int i = 0; i < dirs.Length; i++)
            {
                Object dirAsset = AssetDatabase.LoadAssetAtPath<Object>(dirs[i]);
                Assert.IsTrue(
                    dirAsset != null,
                    $"Expected directory asset at '{dirs[i]}' to be loaded"
                );
                window.directories.Add(dirAsset);
            }

            window.spriteFileExtensions = new List<string> { ".png" };

            // This was failing before the fix because SystemArrayPool.Get returns larger arrays
            // and the null elements caused AssetDatabase.FindAssets to crash
            Assert.DoesNotThrow(
                () => window.CalculateStats(),
                "CalculateStats with multiple directories should not throw NullReferenceException"
            );
        }

        [Test]
        public void GetMatchingFilePathsWithEmptyDirectoriesListSucceeds()
        {
            string texPath = (Root + "/solo.png").SanitizePath();
            CreatePng(texPath, 8, 8, Color.white);
            AssetDatabase.Refresh();

            TextureImporter imp = AssetImporter.GetAtPath(texPath) as TextureImporter;
            Assert.IsTrue(imp != null, $"Expected importer at path '{texPath}' to not be null");
            imp.textureType = TextureImporterType.Sprite;
            imp.SaveAndReimport();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
            Assert.IsTrue(sprite != null, $"Expected sprite at path '{texPath}' to not be null");

            SpriteSettingsApplierWindow window = Track(
                ScriptableObject.CreateInstance<SpriteSettingsApplierWindow>()
            );

            window.sprites = new List<Sprite> { sprite };
            window.directories = new List<Object>(); // Explicitly empty

            // Should not throw with empty directories
            Assert.DoesNotThrow(
                () => window.CalculateStats(),
                "CalculateStats with empty directories list should not throw"
            );
        }

        [Test]
        public void GetMatchingFilePathsWithNullDirectoryEntriesSucceeds()
        {
            string dir = (Root + "/ValidDir").SanitizePath();
            EnsureFolder(dir);
            string texPath = (dir + "/valid.png").SanitizePath();
            CreatePng(texPath, 8, 8, Color.white);
            AssetDatabase.Refresh();

            TextureImporter imp = AssetImporter.GetAtPath(texPath) as TextureImporter;
            Assert.IsTrue(imp != null, $"Expected importer at path '{texPath}' to not be null");
            imp.textureType = TextureImporterType.Sprite;
            imp.SaveAndReimport();

            SpriteSettingsApplierWindow window = Track(
                ScriptableObject.CreateInstance<SpriteSettingsApplierWindow>()
            );

            window.sprites = new List<Sprite>();
            window.directories = new List<Object>
            {
                null, // Intentionally null
                AssetDatabase.LoadAssetAtPath<Object>(dir),
                null, // Another null
            };
            window.spriteFileExtensions = new List<string> { ".png" };

            // Should not throw - nulls should be skipped
            Assert.DoesNotThrow(
                () => window.CalculateStats(),
                "CalculateStats with null directory entries should not throw"
            );
        }

        [Test]
        public void GetMatchingFilePathsWithEmptyDirectorySucceeds()
        {
            // Tests a directory that contains no textures
            string emptyDir = (Root + "/EmptyDir").SanitizePath();
            EnsureFolder(emptyDir);
            AssetDatabase.Refresh();

            SpriteSettingsApplierWindow window = Track(
                ScriptableObject.CreateInstance<SpriteSettingsApplierWindow>()
            );

            window.sprites = new List<Sprite>();
            window.directories = new List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(emptyDir),
            };
            window.spriteFileExtensions = new List<string> { ".png" };

            // Should not throw even with no textures found
            Assert.DoesNotThrow(
                () => window.CalculateStats(),
                "CalculateStats with empty directory should not throw"
            );
        }

        private void CreatePng(string relPath, int w, int h, Color c)
        {
            EnsureFolder(Path.GetDirectoryName(relPath).SanitizePath());
            Texture2D t = new(w, h, TextureFormat.RGBA32, false);
            Color[] pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = c;
            }

            t.SetPixels(pix);
            t.Apply();
            File.WriteAllBytes(RelToFull(relPath), t.EncodeToPNG());
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
