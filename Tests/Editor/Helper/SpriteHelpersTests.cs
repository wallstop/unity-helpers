namespace WallstopStudios.UnityHelpers.Tests.Tests.Editor.Helper
{
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;

    [TestFixture]
    public sealed class SpriteHelpersTests : WallstopStudios.UnityHelpers.Tests.CommonTestBase
    {
        private const string TestFolder = "Assets/TempSpriteHelpersTests";
        private string _testTexturePath;
        private Texture2D _testTexture;

        [SetUp]
        public void SetUp()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(TestFolder))
            {
                AssetDatabase.CreateFolder("Assets", "TempSpriteHelpersTests");
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (_testTexture != null)
            {
                Object.DestroyImmediate(_testTexture, true);
                _testTexture = null;
            }

            if (!string.IsNullOrEmpty(_testTexturePath) && File.Exists(_testTexturePath))
            {
                AssetDatabase.DeleteAsset(_testTexturePath);
                _testTexturePath = null;
            }

            if (AssetDatabase.IsValidFolder(TestFolder))
            {
                AssetDatabase.DeleteAsset(TestFolder);
            }

            AssetDatabase.Refresh();
        }

        [Test]
        public void MakeReadableWithNullTextureDoesNotThrow()
        {
            if (Application.isPlaying)
            {
                Assert.Ignore("AssetDatabase access requires edit mode.");
            }

            Texture2D nullTexture = null;
            Assert.DoesNotThrow(() => nullTexture.MakeReadable());
        }

        [Test]
        public void MakeReadableWithAlreadyReadableTextureDoesNotModify()
        {
            if (Application.isPlaying)
            {
                Assert.Ignore("AssetDatabase access requires edit mode.");
            }

            CreateTestTexture(readable: true);

            bool wasReadableBefore = _testTexture.isReadable;
            _testTexture.MakeReadable();
            bool isReadableAfter = _testTexture.isReadable;

            Assert.IsTrue(
                wasReadableBefore,
                "Texture should be readable before calling MakeReadable"
            );
            Assert.IsTrue(
                isReadableAfter,
                "Texture should still be readable after calling MakeReadable"
            );
        }

        [Test]
        public void MakeReadableMakesNonReadableTextureReadable()
        {
            if (Application.isPlaying)
            {
                Assert.Ignore("AssetDatabase access requires edit mode.");
            }

            CreateTestTexture(readable: false);

            Assert.IsFalse(_testTexture.isReadable, "Texture should not be readable initially");

            _testTexture.MakeReadable();

            AssetDatabase.Refresh();
            _testTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(_testTexturePath);

            Assert.IsTrue(
                _testTexture.isReadable,
                "Texture should be readable after calling MakeReadable"
            );
        }

        [Test]
        public void MakeReadableWithNonReadableTextureUpdatesImporterSettings()
        {
            if (Application.isPlaying)
            {
                Assert.Ignore("AssetDatabase access requires edit mode.");
            }

            CreateTestTexture(readable: false);
            string assetPath = AssetDatabase.GetAssetPath(_testTexture);

            _testTexture.MakeReadable();

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            Assert.IsTrue(importer != null, "Should be able to get TextureImporter");
            Assert.IsTrue(importer.isReadable, "TextureImporter isReadable should be true");
        }

        [Test]
        public void MakeReadableWithRuntimeCreatedTextureWithoutAssetPathDoesNotThrow()
        {
            if (Application.isPlaying)
            {
                Assert.Ignore("AssetDatabase access requires edit mode.");
            }

            Texture2D runtimeTexture = new(2, 2);
            try
            {
                Assert.DoesNotThrow(() => runtimeTexture.MakeReadable());
            }
            finally
            {
                Object.DestroyImmediate(runtimeTexture);
            }
        }

        [Test]
        public void MakeReadableWithMultipleCallsDoesNotCauseIssues()
        {
            if (Application.isPlaying)
            {
                Assert.Ignore("AssetDatabase access requires edit mode.");
            }

            CreateTestTexture(readable: false);

            Assert.DoesNotThrow(() =>
            {
                _testTexture.MakeReadable();
                _testTexture.MakeReadable();
                _testTexture.MakeReadable();
            });

            AssetDatabase.Refresh();
            _testTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(_testTexturePath);
            Assert.IsTrue(
                _testTexture.isReadable,
                "Texture should be readable after multiple calls"
            );
        }

        [Test]
        public void MakeReadablePreservesTextureData()
        {
            if (Application.isPlaying)
            {
                Assert.Ignore("AssetDatabase access requires edit mode.");
            }

            CreateTestTexture(readable: false);
            int originalWidth = _testTexture.width;
            int originalHeight = _testTexture.height;
            TextureFormat originalFormat = _testTexture.format;

            _testTexture.MakeReadable();

            AssetDatabase.Refresh();
            _testTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(_testTexturePath);

            Assert.AreEqual(originalWidth, _testTexture.width, "Width should be preserved");
            Assert.AreEqual(originalHeight, _testTexture.height, "Height should be preserved");
            Assert.AreEqual(originalFormat, _testTexture.format, "Format should be preserved");
        }

        [Test]
        public void MakeReadableWithDifferentTextureSizesWorksCorrectly()
        {
            if (Application.isPlaying)
            {
                Assert.Ignore("AssetDatabase access requires edit mode.");
            }

            int[] sizes = { 1, 2, 4, 16, 64, 256, 512 };

            foreach (int size in sizes)
            {
                CreateTestTexture(readable: false, width: size, height: size);

                _testTexture.MakeReadable();

                AssetDatabase.Refresh();
                _testTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(_testTexturePath);

                Assert.IsTrue(
                    _testTexture.isReadable,
                    $"Texture of size {size}x{size} should be readable"
                );

                AssetDatabase.DeleteAsset(_testTexturePath);
                Object.DestroyImmediate(_testTexture);
                _testTexture = null;
                _testTexturePath = null;
            }
        }

        [Test]
        public void MakeReadableWithNonSquareTexturesWorksCorrectly()
        {
            if (Application.isPlaying)
            {
                Assert.Ignore("AssetDatabase access requires edit mode.");
            }

            CreateTestTexture(readable: false, width: 64, height: 128);

            _testTexture.MakeReadable();

            AssetDatabase.Refresh();
            _testTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(_testTexturePath);

            Assert.IsTrue(_testTexture.isReadable, "Non-square texture should be readable");
            Assert.AreEqual(64, _testTexture.width);
            Assert.AreEqual(128, _testTexture.height);
        }

        [Test]
        public void MakeReadableWithMinimalSizeTextureWorksCorrectly()
        {
            if (Application.isPlaying)
            {
                Assert.Ignore("AssetDatabase access requires edit mode.");
            }

            CreateTestTexture(readable: false, width: 1, height: 1);

            _testTexture.MakeReadable();

            AssetDatabase.Refresh();
            _testTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(_testTexturePath);

            Assert.IsTrue(_testTexture.isReadable, "1x1 texture should be readable");
        }

        [Test]
        public void MakeReadableDoesNotAffectOtherImporterSettings()
        {
            if (Application.isPlaying)
            {
                Assert.Ignore("AssetDatabase access requires edit mode.");
            }

            CreateTestTexture(readable: false);
            string assetPath = AssetDatabase.GetAssetPath(_testTexture);
            TextureImporter importerBefore = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            TextureImporterType originalType = importerBefore.textureType;
            FilterMode originalFilterMode = importerBefore.filterMode;
            int originalAnisoLevel = importerBefore.anisoLevel;

            _testTexture.MakeReadable();

            TextureImporter importerAfter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            Assert.AreEqual(
                originalType,
                importerAfter.textureType,
                "Texture type should not change"
            );
            Assert.AreEqual(
                originalFilterMode,
                importerAfter.filterMode,
                "Filter mode should not change"
            );
            Assert.AreEqual(
                originalAnisoLevel,
                importerAfter.anisoLevel,
                "Aniso level should not change"
            );
        }

        private void CreateTestTexture(bool readable, int width = 4, int height = 4)
        {
            _testTexturePath = Path.Combine(TestFolder, $"TestTexture_{System.Guid.NewGuid()}.png")
                .Replace('\\', '/');

            Texture2D tempTexture = new(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(
                    (float)i / pixels.Length,
                    (float)(pixels.Length - i) / pixels.Length,
                    0.5f,
                    1.0f
                );
            }
            tempTexture.SetPixels(pixels);
            tempTexture.Apply();

            byte[] pngData = tempTexture.EncodeToPNG();
            Object.DestroyImmediate(tempTexture);

            File.WriteAllBytes(_testTexturePath, pngData);
            AssetDatabase.ImportAsset(_testTexturePath);

            TextureImporter importer = AssetImporter.GetAtPath(_testTexturePath) as TextureImporter;
            if (importer != null)
            {
                importer.isReadable = readable;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            AssetDatabase.Refresh();
            _testTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(_testTexturePath);
        }
    }
}
