namespace WallstopStudios.UnityHelpers.Tests.Visuals
{
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Visuals.UGUI;

    public sealed class EnhancedImageTests : CommonTestBase
    {
        // Tracking handled by CommonTestBase

        [Test]
        public void StartCreatesMaterialInstanceAndRespectsSdrColor()
        {
            EnhancedImage image = CreateEnhancedImage(out Material baseMaterial);
            image.color = new Color(0.2f, 0.4f, 0.6f, 0.8f);

            InvokeLifecycle(image, "Start");

            Material cached = image.material;
            Assert.That(cached, Is.Not.Null);
            Assert.AreNotSame(baseMaterial, cached);
            Assert.IsTrue(cached.GetColor("_Color").Approximately(image.color));
        }

        [Test]
        public void HdrColorAboveSdrOverridesMaterialColor()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            InvokeLifecycle(image, "Start");

            Color hdr = new Color(2f, 0.5f, 0.25f, 1f);
            image.HdrColor = hdr;

            Material cached = image.material;
            Assert.That(cached, Is.Not.Null);
            Assert.IsTrue(cached.GetColor("_Color").Approximately(hdr));
        }

        [Test]
        public void ShapeMaskAssignmentWritesTextureToMaterial()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            Texture2D mask = Track(new Texture2D(4, 4, TextureFormat.RGBA32, false, false));

            typeof(EnhancedImage)
                .GetField("_shapeMask", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(image, mask);

            InvokeLifecycle(image, "Start");

            Texture maskInMaterial = image.material.GetTexture("_ShapeMask");
            Assert.That(maskInMaterial, Is.SameAs(mask));
        }

        [Test]
        public void OnDestroyReleasesCachedMaterialInstance()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            InvokeLifecycle(image, "Start");

            FieldInfo cachedField = typeof(EnhancedImage).GetField(
                "_cachedMaterialInstance",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.That(cachedField, Is.Not.Null);

            Material cachedBefore = (Material)cachedField.GetValue(image);
            Assert.That(cachedBefore, Is.Not.Null);

            InvokeLifecycle(image, "OnDestroy");

            Material cachedAfter = (Material)cachedField.GetValue(image);
            Assert.That(cachedAfter, Is.Null);
            Assert.That(cachedBefore == null, Is.True);
        }

        [Test]
        public void UpdateHandlesNullMaterialGracefully()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.material = null;

            InvokeLifecycle(image, "Start");

            var field = typeof(EnhancedImage).GetField(
                "_cachedMaterialInstance",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.That(field, Is.Not.Null);
            Material cached = (Material)field.GetValue(image);
            Assert.That(
                cached,
                Is.Null,
                "Expected no instance to be created when material is null."
            );
        }

        [Test]
        public void HdrColorWithinSdrUsesGraphicColor()
        {
            EnhancedImage image = CreateEnhancedImage(out Material baseMaterial);
            image.color = new Color(0.1f, 0.2f, 0.3f, 0.9f);
            image.HdrColor = new Color(0.4f, 0.4f, 0.4f, 0.4f);

            InvokeLifecycle(image, "Start");

            Material cached = image.material;
            Assert.That(cached, Is.Not.Null);
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
            InvokeLifecycle(image, "Start");
            Material first = image.material;
            Assert.That(first, Is.Not.Null);

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
            InvokeLifecycle(image, "Start");
            Material first = image.material;
            Assert.That(first, Is.Not.Null);

            InvokeLifecycle(image, "Start");
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
            Assert.That(
                shader,
                Is.Not.Null,
                "Expected UI/Default shader to be available for tests."
            );

            baseMaterial = Track(new Material(shader));

            GameObject owner = Track(new GameObject("EnhancedImage Test Owner"));
            EnhancedImage image = owner.AddComponent<EnhancedImage>();
            image.material = baseMaterial;
            return image;
        }

        private static void InvokeLifecycle(EnhancedImage image, string methodName)
        {
            MethodInfo method = typeof(EnhancedImage).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.That(
                method,
                Is.Not.Null,
                $"Expected method {methodName} to exist on EnhancedImage."
            );
            method.Invoke(image, null);
        }
    }
}
