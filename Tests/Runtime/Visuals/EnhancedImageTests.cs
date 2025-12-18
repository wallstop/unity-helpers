namespace WallstopStudios.UnityHelpers.Tests.Visuals
{
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Tests.Core;
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

        [Test]
        public void HdrColorUpdateAfterStartActuallyUpdatesTheMaterialColor()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.InvokeStartForTests();

            Color initialHdr = new(1.5f, 0.8f, 0.4f, 1f);
            image.HdrColor = initialHdr;

            Material cached = image.material;
            Assert.IsTrue(cached != null);
            Color materialColorAfterFirstUpdate = cached.GetColor("_Color");
            Assert.IsTrue(
                materialColorAfterFirstUpdate.Approximately(initialHdr),
                $"Expected material color {materialColorAfterFirstUpdate} to match initial HDR color {initialHdr}"
            );

            Color updatedHdr = new(3.0f, 2.0f, 1.0f, 1f);
            image.HdrColor = updatedHdr;

            Color materialColorAfterSecondUpdate = cached.GetColor("_Color");
            Assert.IsTrue(
                materialColorAfterSecondUpdate.Approximately(updatedHdr),
                $"Expected material color {materialColorAfterSecondUpdate} to match updated HDR color {updatedHdr} after second update"
            );
        }

        [Test]
        public void MultipleConsecutiveHdrColorChangesAllReflectInMaterial()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.InvokeStartForTests();

            Color[] hdrColors = new Color[]
            {
                new(1.1f, 0.5f, 0.2f, 1f),
                new(2.5f, 1.5f, 0.8f, 1f),
                new(5.0f, 3.0f, 2.0f, 0.9f),
                new(1.01f, 0.99f, 0.5f, 0.5f),
                new(10f, 10f, 10f, 1f),
            };

            Material cached = image.material;
            Assert.IsTrue(cached != null);

            for (int i = 0; i < hdrColors.Length; i++)
            {
                Color expected = hdrColors[i];
                image.HdrColor = expected;

                Color actual = cached.GetColor("_Color");
                Assert.IsTrue(
                    actual.Approximately(expected),
                    $"Iteration {i}: Expected material color {actual} to match HDR color {expected}"
                );
            }
        }

        [Test]
        public void TransitionFromHdrToSdrRangeUpdatesMaterialToGraphicColor()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.color = new Color(0.3f, 0.4f, 0.5f, 1f);
            image.InvokeStartForTests();

            Color hdrColor = new(2.0f, 1.5f, 1.2f, 1f);
            image.HdrColor = hdrColor;

            Material cached = image.material;
            Assert.IsTrue(cached != null);
            Assert.IsTrue(
                cached.GetColor("_Color").Approximately(hdrColor),
                "Expected HDR color to be applied"
            );

            Color sdrColor = new(0.5f, 0.6f, 0.7f, 1f);
            image.HdrColor = sdrColor;

            Color materialColor = cached.GetColor("_Color");
            Assert.IsTrue(
                materialColor.Approximately(image.color),
                $"Expected material color {materialColor} to match Graphic.color {image.color} when HdrColor is within SDR range"
            );
        }

        [Test]
        public void TransitionFromSdrToHdrRangeUpdatesMaterialToHdrColor()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.color = new Color(0.2f, 0.3f, 0.4f, 1f);
            image.HdrColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            image.InvokeStartForTests();

            Material cached = image.material;
            Assert.IsTrue(cached != null);
            Assert.IsTrue(
                cached.GetColor("_Color").Approximately(image.color),
                "Expected Graphic.color to be applied initially when HdrColor is SDR"
            );

            Color hdrColor = new(2.5f, 1.8f, 1.2f, 1f);
            image.HdrColor = hdrColor;

            Color materialColor = cached.GetColor("_Color");
            Assert.IsTrue(
                materialColor.Approximately(hdrColor),
                $"Expected material color {materialColor} to match HdrColor {hdrColor} after transition to HDR range"
            );
        }

        [Test]
        public void HdrColorAtExactlyOneUsesGraphicColor()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.color = new Color(0.1f, 0.2f, 0.3f, 1f);
            image.HdrColor = new Color(1f, 1f, 1f, 1f);
            image.InvokeStartForTests();

            Material cached = image.material;
            Assert.IsTrue(cached != null);

            Color materialColor = cached.GetColor("_Color");
            Assert.IsTrue(
                materialColor.Approximately(image.color),
                $"Expected material color {materialColor} to use Graphic.color {image.color} when HdrColor maxComponent equals 1"
            );
        }

        [Test]
        public void HdrColorSlightlyAboveOneUsesHdrColor()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.color = new Color(0.1f, 0.2f, 0.3f, 1f);
            Color hdrColor = new(1.001f, 0.5f, 0.5f, 1f);
            image.HdrColor = hdrColor;
            image.InvokeStartForTests();

            Material cached = image.material;
            Assert.IsTrue(cached != null);

            Color materialColor = cached.GetColor("_Color");
            Assert.IsTrue(
                materialColor.Approximately(hdrColor),
                $"Expected material color {materialColor} to match HdrColor {hdrColor} when maxComponent > 1"
            );
        }

        [Test]
        public void HdrColorWithZeroAlphaStillUpdatesCorrectly()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.InvokeStartForTests();

            Color hdrWithZeroAlpha = new(2f, 1.5f, 1f, 0f);
            image.HdrColor = hdrWithZeroAlpha;

            Material cached = image.material;
            Assert.IsTrue(cached != null);

            Color materialColor = cached.GetColor("_Color");
            Assert.IsTrue(
                materialColor.Approximately(hdrWithZeroAlpha),
                $"Expected material color {materialColor} to match HDR color with zero alpha {hdrWithZeroAlpha}"
            );
        }

        [Test]
        public void HdrColorWithVeryHighIntensityUpdatesCorrectly()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.InvokeStartForTests();

            Color extremeHdr = new(100f, 50f, 25f, 1f);
            image.HdrColor = extremeHdr;

            Material cached = image.material;
            Assert.IsTrue(cached != null);

            Color materialColor = cached.GetColor("_Color");
            Assert.IsTrue(
                materialColor.Approximately(extremeHdr),
                $"Expected material color {materialColor} to match extreme HDR color {extremeHdr}"
            );
        }

        [Test]
        public void HdrColorWithNegativeValuesHandledGracefully()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            image.InvokeStartForTests();

            Color negativeColor = new(-0.5f, -0.2f, 0.1f, 1f);
            image.HdrColor = negativeColor;

            Material cached = image.material;
            Assert.IsTrue(cached != null);

            Color materialColor = cached.GetColor("_Color");
            Assert.IsTrue(
                materialColor.Approximately(image.color),
                $"Expected material color {materialColor} to use Graphic.color when HdrColor has maxComponent <= 1"
            );
        }

        [Test]
        public void SettingSameHdrColorDoesNotTriggerUpdate()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.InvokeStartForTests();

            Color hdrColor = new(2f, 1.5f, 1f, 1f);
            image.HdrColor = hdrColor;

            Material firstMaterial = image.material;
            Assert.IsTrue(firstMaterial != null);

            image.HdrColor = hdrColor;

            Material secondMaterial = image.material;
            Assert.That(
                secondMaterial,
                Is.SameAs(firstMaterial),
                "Setting the same HdrColor should not create a new material instance"
            );
        }

        [Test]
        public void MaterialInstancePreservedAcrossManyRapidHdrColorChanges()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.InvokeStartForTests();

            Material originalInstance = image.CachedMaterialInstanceForTests;
            Assert.IsTrue(originalInstance != null);

            for (int i = 0; i < 100; i++)
            {
                float intensity = 1.1f + (i * 0.1f);
                image.HdrColor = new Color(intensity, 0.5f, 0.3f, 1f);
            }

            Material finalInstance = image.CachedMaterialInstanceForTests;
            Assert.That(
                finalInstance,
                Is.SameAs(originalInstance),
                "Material instance should be preserved across many rapid updates"
            );
        }

        [Test]
        public void HdrColorPropertyGetReturnsCurrentValue()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.InvokeStartForTests();

            Color expected = new(3f, 2f, 1f, 0.8f);
            image.HdrColor = expected;

            Color actual = image.HdrColor;
            Assert.AreEqual(
                expected,
                actual,
                "HdrColor getter should return the value that was set"
            );
        }

        [Test]
        public void HdrColorDefaultsToWhite()
        {
            EnhancedImage image = CreateEnhancedImage(out _);

            Color defaultColor = image.HdrColor;
            Assert.AreEqual(Color.white, defaultColor, "HdrColor should default to Color.white");
        }

        [Test]
        public void ChangingHdrColorBeforeStartStillAppliesCorrectlyOnStart()
        {
            EnhancedImage image = CreateEnhancedImage(out _);

            Color hdrColor = new(2.5f, 1.8f, 0.9f, 1f);
            image.HdrColor = hdrColor;

            image.InvokeStartForTests();

            Material cached = image.material;
            Assert.IsTrue(cached != null);

            Color materialColor = cached.GetColor("_Color");
            Assert.IsTrue(
                materialColor.Approximately(hdrColor),
                $"Expected material color {materialColor} to match HdrColor {hdrColor} set before Start"
            );
        }

        [Test]
        public void GraphicColorChangeDoesNotAffectMaterialWhenHdrIsActive()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.InvokeStartForTests();

            Color hdrColor = new(2f, 1.5f, 1f, 1f);
            image.HdrColor = hdrColor;

            Material cached = image.material;
            Assert.IsTrue(cached != null);

            image.color = new Color(0.1f, 0.1f, 0.1f, 1f);

            Color materialColor = cached.GetColor("_Color");
            Assert.IsTrue(
                materialColor.Approximately(hdrColor),
                $"Changing Graphic.color should not affect material when HDR is active. Got {materialColor}, expected {hdrColor}"
            );
        }

        [Test]
        public void MultipleEnhancedImagesHaveIndependentMaterialInstances()
        {
            EnhancedImage image1 = CreateEnhancedImage(out Material baseMaterial1);
            EnhancedImage image2 = CreateEnhancedImageWithMaterial(baseMaterial1);

            image1.InvokeStartForTests();
            image2.InvokeStartForTests();

            Material cached1 = image1.CachedMaterialInstanceForTests;
            Material cached2 = image2.CachedMaterialInstanceForTests;

            Assert.IsTrue(cached1 != null);
            Assert.IsTrue(cached2 != null);
            Assert.AreNotSame(
                cached1,
                cached2,
                "Each EnhancedImage should have its own material instance"
            );

            Color hdr1 = new(2f, 0.5f, 0.3f, 1f);
            Color hdr2 = new(1.5f, 3f, 0.8f, 1f);
            image1.HdrColor = hdr1;
            image2.HdrColor = hdr2;

            Assert.IsTrue(
                cached1.GetColor("_Color").Approximately(hdr1),
                "Image1 material should have its own HDR color"
            );
            Assert.IsTrue(
                cached2.GetColor("_Color").Approximately(hdr2),
                "Image2 material should have its own HDR color"
            );
        }

        [Test]
        public void HdrColorChangeDoesNotAffectBaseMaterial()
        {
            EnhancedImage image = CreateEnhancedImage(out Material baseMaterial);
            Color originalBaseColor = baseMaterial.GetColor("_Color");

            image.InvokeStartForTests();

            image.HdrColor = new Color(5f, 3f, 2f, 1f);

            Color baseColorAfterUpdate = baseMaterial.GetColor("_Color");
            Assert.AreEqual(
                originalBaseColor,
                baseColorAfterUpdate,
                "Base material color should not be affected by EnhancedImage HDR color changes"
            );
        }

        [Test]
        public void AlternatingBetweenHdrAndSdrMultipleTimes()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.color = new Color(0.2f, 0.3f, 0.4f, 1f);
            image.InvokeStartForTests();

            Material cached = image.material;
            Assert.IsTrue(cached != null);

            for (int i = 0; i < 5; i++)
            {
                Color hdr = new(2f + i, 1.5f, 1f, 1f);
                image.HdrColor = hdr;
                Assert.IsTrue(
                    cached.GetColor("_Color").Approximately(hdr),
                    $"Iteration {i}: HDR mode should use HdrColor"
                );

                Color sdr = new(0.5f, 0.6f, 0.7f, 1f);
                image.HdrColor = sdr;
                Assert.IsTrue(
                    cached.GetColor("_Color").Approximately(image.color),
                    $"Iteration {i}: SDR mode should use Graphic.color"
                );
            }
        }

        [Test]
        public void HdrColorWithOnlyAlphaAboveOneUsesHdrColor()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            Color hdrAlphaOnly = new(0.5f, 0.5f, 0.5f, 1.5f);
            image.HdrColor = hdrAlphaOnly;
            image.InvokeStartForTests();

            Material cached = image.material;
            Assert.IsTrue(cached != null);

            Color materialColor = cached.GetColor("_Color");
            Assert.IsTrue(
                materialColor.Approximately(hdrAlphaOnly),
                $"Expected material color {materialColor} to match HdrColor {hdrAlphaOnly} when alpha > 1"
            );
        }

        [Test]
        public void ShapeMaskCanBeChangedAfterStart()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.InvokeStartForTests();

            Texture2D mask1 = Track(new Texture2D(4, 4, TextureFormat.RGBA32, false, false));
            image._shapeMask = mask1;

            image.HdrColor = new Color(1.1f, 0.5f, 0.3f, 1f);

            Material cached = image.material;
            Shader supportShader = Shader.Find("Hidden/Wallstop/EnhancedImageSupport");
            if (supportShader != null && cached.HasProperty("_ShapeMask"))
            {
                Texture maskInMaterial = cached.GetTexture("_ShapeMask");
                Assert.That(
                    maskInMaterial,
                    Is.SameAs(mask1),
                    "Shape mask should be updated when HdrColor triggers material update"
                );
            }
        }

        [Test]
        public void DestroyAndRecreatePreservesExpectedBehavior()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.InvokeStartForTests();

            Color hdrColor = new(2f, 1.5f, 1f, 1f);
            image.HdrColor = hdrColor;

            Material cachedBefore = image.CachedMaterialInstanceForTests;
            Assert.IsTrue(cachedBefore != null);

            image.InvokeOnDestroyForTests();

            Assert.IsTrue(
                image.CachedMaterialInstanceForTests == null,
                "Cached material should be null after destroy"
            );

            image.InvokeStartForTests();

            Material cachedAfter = image.CachedMaterialInstanceForTests;
            Assert.IsTrue(
                cachedAfter != null,
                "New material instance should be created on restart"
            );
            Assert.AreNotSame(
                cachedBefore,
                cachedAfter,
                "New material instance should be different from destroyed one"
            );

            Color materialColor = cachedAfter.GetColor("_Color");
            Assert.IsTrue(
                materialColor.Approximately(hdrColor),
                $"New material should reflect current HdrColor. Got {materialColor}, expected {hdrColor}"
            );
        }

        [Test]
        public void MaterialColorPropertyExistsAndIsAccessible()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.InvokeStartForTests();

            Material cached = image.material;
            Assert.IsTrue(cached != null);
            Assert.IsTrue(
                cached.HasProperty("_Color"),
                "Material should have _Color property for HDR color application"
            );
        }

        [Test]
        public void HdrColorChangesAreImmediatelyReflectedInMaterial()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.InvokeStartForTests();

            Material cached = image.material;
            Assert.IsTrue(cached != null);

            Color before = cached.GetColor("_Color");

            Color newHdr = new(3f, 2f, 1f, 1f);
            image.HdrColor = newHdr;

            Color after = cached.GetColor("_Color");

            Assert.AreNotEqual(
                before,
                after,
                "Material color should change immediately when HdrColor is set"
            );
            Assert.IsTrue(
                after.Approximately(newHdr),
                $"Material color {after} should match new HdrColor {newHdr}"
            );
        }

        [Test]
        public void UpdateWithDefaultUIMaterialDoesNotCreateInstance()
        {
            GameObject owner = Track(new GameObject("Default Material Test"));
            EnhancedImage image = owner.AddComponent<EnhancedImage>();

            image.InvokeStartForTests();

            Material cached = image.CachedMaterialInstanceForTests;
            Assert.IsTrue(
                cached == null,
                "Should not create material instance when using default UI material"
            );
        }

        [Test]
        public void MaterialInstanceCreatedOnlyOnce()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.InvokeStartForTests();

            Material instance1 = image.CachedMaterialInstanceForTests;
            Assert.IsTrue(instance1 != null);

            image.HdrColor = new Color(1.5f, 0.5f, 0.3f, 1f);
            Material instance2 = image.CachedMaterialInstanceForTests;

            image.HdrColor = new Color(2.5f, 1.5f, 0.8f, 1f);
            Material instance3 = image.CachedMaterialInstanceForTests;

            image.HdrColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            Material instance4 = image.CachedMaterialInstanceForTests;

            Assert.That(instance2, Is.SameAs(instance1));
            Assert.That(instance3, Is.SameAs(instance1));
            Assert.That(instance4, Is.SameAs(instance1));
        }

        [Test]
        public void ColorChannelsUpdateIndependently()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.InvokeStartForTests();

            Material cached = image.material;
            Assert.IsTrue(cached != null);

            Color redHdr = new(2f, 0f, 0f, 1f);
            image.HdrColor = redHdr;
            Color redResult = cached.GetColor("_Color");
            Assert.IsTrue(Mathf.Approximately(redResult.r, 2f), "Red channel should be 2");
            Assert.IsTrue(Mathf.Approximately(redResult.g, 0f), "Green channel should be 0");
            Assert.IsTrue(Mathf.Approximately(redResult.b, 0f), "Blue channel should be 0");

            Color greenHdr = new(0f, 1.5f, 0f, 1f);
            image.HdrColor = greenHdr;
            Color greenResult = cached.GetColor("_Color");
            Assert.IsTrue(Mathf.Approximately(greenResult.r, 0f), "Red channel should be 0");
            Assert.IsTrue(Mathf.Approximately(greenResult.g, 1.5f), "Green channel should be 1.5");
            Assert.IsTrue(Mathf.Approximately(greenResult.b, 0f), "Blue channel should be 0");

            Color blueHdr = new(0f, 0f, 3f, 1f);
            image.HdrColor = blueHdr;
            Color blueResult = cached.GetColor("_Color");
            Assert.IsTrue(Mathf.Approximately(blueResult.r, 0f), "Red channel should be 0");
            Assert.IsTrue(Mathf.Approximately(blueResult.g, 0f), "Green channel should be 0");
            Assert.IsTrue(Mathf.Approximately(blueResult.b, 3f), "Blue channel should be 3");
        }

        [Test]
        public void AlphaChannelPreservedInHdrColor()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.InvokeStartForTests();

            Material cached = image.material;
            Assert.IsTrue(cached != null);

            float[] alphaValues = new float[] { 0f, 0.25f, 0.5f, 0.75f, 1f };

            foreach (float alpha in alphaValues)
            {
                Color hdr = new(2f, 1.5f, 1f, alpha);
                image.HdrColor = hdr;

                Color result = cached.GetColor("_Color");
                Assert.IsTrue(
                    Mathf.Approximately(result.a, alpha),
                    $"Alpha channel should be {alpha}, got {result.a}"
                );
            }
        }

        [Test]
        public void VerySmallHdrValuesAboveOneAreRecognizedAsHdr()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            image.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            image.InvokeStartForTests();

            Material cached = image.material;
            Assert.IsTrue(cached != null);

            Color barelyHdr = new(1.0001f, 0.5f, 0.5f, 1f);
            image.HdrColor = barelyHdr;

            Color result = cached.GetColor("_Color");
            Assert.IsTrue(
                result.Approximately(barelyHdr),
                $"Color just above 1.0 should be treated as HDR. Got {result}, expected {barelyHdr}"
            );
        }

        [Test]
        public void VerySmallHdrValuesBelowOneUsesGraphicColor()
        {
            EnhancedImage image = CreateEnhancedImage(out _);
            Color graphicColor = new(0.3f, 0.4f, 0.5f, 1f);
            image.color = graphicColor;
            image.InvokeStartForTests();

            Material cached = image.material;
            Assert.IsTrue(cached != null);

            Color barelySdr = new(0.9999f, 0.9999f, 0.9999f, 1f);
            image.HdrColor = barelySdr;

            Color result = cached.GetColor("_Color");
            Assert.IsTrue(
                result.Approximately(graphicColor),
                $"Color just below 1.0 should use Graphic.color. Got {result}, expected {graphicColor}"
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

        private EnhancedImage CreateEnhancedImageWithMaterial(Material material)
        {
            GameObject owner = Track(new GameObject("EnhancedImage Test Owner"));
            EnhancedImage image = owner.AddComponent<EnhancedImage>();
            image.material = material;
            return image;
        }
    }
}
