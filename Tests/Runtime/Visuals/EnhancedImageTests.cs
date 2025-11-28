namespace WallstopStudios.UnityHelpers.Tests.Visuals
{
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Tests.Utils;
    using WallstopStudios.UnityHelpers.Visuals.UGUI;

    public sealed class EnhancedImageTests : CommonTestBase
    {
        // Tracking handled by CommonTestBase

        [Test]
        public void StartCreatesMaterialInstanceAndRespectsSdrColor()
        {
            EnhancedImage image = CreateEnhancedImage(out Material baseMaterial);
            image.color = new Color(0.2f, 0.4f, 0.6f, 0.8f);

            image.InvokeStartForTests();

            Material cached = image.material;
            Assert.IsTrue(cached != null);
            Assert.AreNotSame(baseMaterial, cached);
            Assert.IsTrue(cached.GetColor("_Color").Approximately(image.color));
        }

        [Test]
        public void HdrColorAboveSdrOverridesMaterialColor()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.InvokeStartForTests();

            Color hdr = new(2f, 0.5f, 0.25f, 1f);
            image.HdrColor = hdr;

            Material cached = image.material;
            Assert.IsTrue(cached != null);
            Assert.IsTrue(cached.GetColor("_Color").Approximately(hdr));
        }

        [Test]
        public void ShapeMaskAssignmentWritesTextureToMaterial()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            Texture2D mask = Track(new Texture2D(4, 4, TextureFormat.RGBA32, false, false));

            image._shapeMask = mask;

            image.InvokeStartForTests();

            Texture maskInMaterial = image.material.GetTexture("_ShapeMask");
            Assert.That(maskInMaterial, Is.SameAs(mask));
        }

        [Test]
        public void OnDestroyReleasesCachedMaterialInstance()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.InvokeStartForTests();

            Material cachedBefore = image.CachedMaterialInstanceForTests;
            Assert.IsTrue(cachedBefore != null);

            image.InvokeOnDestroyForTests();

            Material cachedAfter = image.CachedMaterialInstanceForTests;
            Assert.IsTrue(cachedAfter == null);
            Assert.That(cachedBefore == null, Is.True);
        }

        [Test]
        public void UpdateHandlesNullMaterialGracefully()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.material = null;

            image.InvokeStartForTests();

            Material cached = image.CachedMaterialInstanceForTests;
            Assert.IsTrue(
                cached == null,
                "Expected no instance to be created when material is null."
            );
        }

        [Test]
        public void HdrColorWithinSdrUsesGraphicColor()
        {
            EnhancedImage image = CreateEnhancedImage(out Material baseMaterial);
            image.color = new Color(0.1f, 0.2f, 0.3f, 0.9f);
            image.HdrColor = new Color(0.4f, 0.4f, 0.4f, 0.4f);

            image.InvokeStartForTests();

            Material cached = image.material;
            Assert.IsTrue(cached != null);
            Assert.AreNotSame(baseMaterial, cached);
            Assert.IsTrue(
                cached.GetColor("_Color").Approximately(image.color),
                "Expected SDR color to drive material when HDR values are not used."
            );
        }

        [Test]
        public void MaterialInstanceIsReusedAcrossUpdates()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.InvokeStartForTests();
            Material first = image.material;
            Assert.IsTrue(first != null);

            image.HdrColor = new Color(1.1f, 0.2f, 0.3f, 1f);
            Material second = image.material;
            Assert.That(
                second,
                Is.SameAs(first),
                "Expected material instance to be reused on updates."
            );
        }

        [Test]
        public void StartDoesNotDuplicateExistingInstance()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.InvokeStartForTests();
            Material first = image.material;
            Assert.IsTrue(first != null);

            image.InvokeStartForTests();
            Material second = image.material;
            Assert.That(
                second,
                Is.SameAs(first),
                "Expected repeated start to keep the same instance."
            );
        }

        private EnhancedImage CreateEnhancedImage(out Material baseMaterial)
        {
            Shader shader = Shader.Find("UI/Default");
            Assert.IsTrue(shader != null, "Expected UI/Default shader to be available for tests.");

            baseMaterial = Track(new Material(shader));

            GameObject owner = Track(new GameObject("EnhancedImage Test Owner"));
            EnhancedImage image = owner.AddComponent<EnhancedImage>();
            image.material = baseMaterial;
            return image;
        }

        // No reflection lifecycle helpers needed; use internal wrappers on EnhancedImage
    }
}
