namespace WallstopStudios.UnityHelpers.Tests.Visuals
{
    using System.Collections.Generic;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Visuals.UGUI;
    using Object = UnityEngine.Object;

    public sealed class EnhancedImageTests
    {
        private readonly List<Object> _tracked = new();

        [TearDown]
        public void Cleanup()
        {
            VisualsTestHelpers.DestroyTracked(_tracked);
        }

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
            Texture2D mask = new Texture2D(4, 4, TextureFormat.RGBA32, false, false);
            _tracked.Add(mask);

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

        private EnhancedImage CreateEnhancedImage(out Material baseMaterial)
        {
            Shader shader = Shader.Find("UI/Default");
            Assert.That(
                shader,
                Is.Not.Null,
                "Expected UI/Default shader to be available for tests."
            );

            baseMaterial = new Material(shader);
            _tracked.Add(baseMaterial);

            GameObject owner = new GameObject("EnhancedImage Test Owner");
            _tracked.Add(owner);

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
