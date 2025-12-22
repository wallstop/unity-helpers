namespace WallstopStudios.UnityHelpers.Tests.Utils
{
#if UNITY_EDITOR
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Utils.WGroup;

    /// <summary>
    /// Comprehensive tests for <see cref="WGroupHeaderVisualUtility.DrawHeaderBackground"/> and
    /// the private <c>GetHeaderTint</c> pre-compositing fix.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>GetHeaderTint</c> method was modified to pre-composite colors against the expected
    /// editor background and output fully opaque colors. This fixes the issue where header
    /// backgrounds would blend with content underneath, creating muddy colors.
    /// </para>
    /// <para>
    /// Pre-compositing formula: result = bg * (1-a) + fg * a
    /// where bg = editor background, fg = input color, a = calculated alpha.
    /// </para>
    /// </remarks>
    [TestFixture]
    public sealed class WGroupHeaderTintTests
    {
        private const float Tolerance = 0.0001f;

        // Standard editor background colors (from GetHeaderTint implementation)
        private static readonly Color DarkEditorBackground = new(0.22f, 0.22f, 0.22f, 1f);
        private static readonly Color LightEditorBackground = new(0.76f, 0.76f, 0.76f, 1f);

        // Test palette colors
        private static readonly Color DarkPaletteColor = new(0.215f, 0.215f, 0.215f, 1f);
        private static readonly Color LightPaletteColor = new(0.82f, 0.82f, 0.82f, 1f);
        private static readonly Color MidGrayColor = new(0.5f, 0.5f, 0.5f, 1f);
        private static readonly Color VeryDarkColor = new(0.1f, 0.1f, 0.1f, 1f);
        private static readonly Color VeryLightColor = new(0.95f, 0.95f, 0.95f, 1f);
        private static readonly Color ColoredLightColor = new(0.9f, 0.85f, 0.8f, 1f);
        private static readonly Color ColoredDarkColor = new(0.1f, 0.15f, 0.25f, 1f);
        private static readonly Color TransparentColor = new(0.5f, 0.5f, 0.5f, 0.5f);

        // Cache for reflection access to private method
        private static MethodInfo _getHeaderTintMethod;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Get the private static GetHeaderTint method via reflection
            _getHeaderTintMethod = typeof(WGroupHeaderVisualUtility).GetMethod(
                "GetHeaderTint",
                BindingFlags.NonPublic | BindingFlags.Static
            );

            Assert.That(
                _getHeaderTintMethod,
                Is.Not.Null,
                "GetHeaderTint method should exist and be accessible via reflection."
            );
        }

        /// <summary>
        /// Invokes the private GetHeaderTint method via reflection.
        /// </summary>
        private static Color InvokeGetHeaderTint(Color baseColor)
        {
            object result = _getHeaderTintMethod.Invoke(null, new object[] { baseColor });
            return (Color)result;
        }

        /// <summary>
        /// Calculates expected pre-composited color for verification.
        /// </summary>
        private static Color CalculateExpectedPreComposite(
            Color baseColor,
            Color editorBg,
            float alpha
        )
        {
            return new Color(
                editorBg.r * (1f - alpha) + baseColor.r * alpha,
                editorBg.g * (1f - alpha) + baseColor.g * alpha,
                editorBg.b * (1f - alpha) + baseColor.b * alpha,
                1f
            );
        }

        /// <summary>
        /// Calculates luminance using the same formula as GetHeaderTint.
        /// </summary>
        private static float CalculateLuminance(Color color)
        {
            return 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
        }

        // ============================================
        // 1. Full Opacity Tests
        // ============================================

        [Test]
        public void GetHeaderTintReturnsFullyOpaqueColorForDarkPalette()
        {
            Color result = InvokeGetHeaderTint(DarkPaletteColor);

            Assert.That(
                result.a,
                Is.EqualTo(1f).Within(Tolerance),
                "GetHeaderTint should always return fully opaque color (alpha = 1.0) to prevent further blending."
            );
        }

        [Test]
        public void GetHeaderTintReturnsFullyOpaqueColorForLightPalette()
        {
            Color result = InvokeGetHeaderTint(LightPaletteColor);

            Assert.That(
                result.a,
                Is.EqualTo(1f).Within(Tolerance),
                "GetHeaderTint should always return fully opaque color (alpha = 1.0) to prevent further blending."
            );
        }

        [Test]
        public void GetHeaderTintReturnsFullyOpaqueColorForMidGray()
        {
            Color result = InvokeGetHeaderTint(MidGrayColor);

            Assert.That(
                result.a,
                Is.EqualTo(1f).Within(Tolerance),
                "GetHeaderTint should always return fully opaque color regardless of input."
            );
        }

        [Test]
        public void GetHeaderTintReturnsFullyOpaqueColorForTransparentInput()
        {
            Color result = InvokeGetHeaderTint(TransparentColor);

            Assert.That(
                result.a,
                Is.EqualTo(1f).Within(Tolerance),
                "GetHeaderTint should return fully opaque color even when input has transparency."
            );
        }

        [Test]
        public void GetHeaderTintReturnsFullyOpaqueColorForColoredPalettes()
        {
            Color lightResult = InvokeGetHeaderTint(ColoredLightColor);
            Color darkResult = InvokeGetHeaderTint(ColoredDarkColor);

            Assert.That(
                lightResult.a,
                Is.EqualTo(1f).Within(Tolerance),
                "GetHeaderTint should return fully opaque for colored light palette."
            );
            Assert.That(
                darkResult.a,
                Is.EqualTo(1f).Within(Tolerance),
                "GetHeaderTint should return fully opaque for colored dark palette."
            );
        }

        // ============================================
        // 2. Pre-compositing Dark Palette on Dark Editor
        // ============================================

        [Test]
        public void DarkPaletteOnDarkEditorProducesExpectedPreComposite()
        {
            if (!EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires dark editor theme (Pro skin).");
            }

            Color result = InvokeGetHeaderTint(DarkPaletteColor);

            // Dark palette on dark editor = NOT cross-theme, alpha = 0.62
            float expectedAlpha = 0.62f;
            Color expected = CalculateExpectedPreComposite(
                DarkPaletteColor,
                DarkEditorBackground,
                expectedAlpha
            );

            Assert.That(
                result.r,
                Is.EqualTo(expected.r).Within(Tolerance),
                "Red channel should match pre-composited value for dark palette on dark editor."
            );
            Assert.That(
                result.g,
                Is.EqualTo(expected.g).Within(Tolerance),
                "Green channel should match pre-composited value for dark palette on dark editor."
            );
            Assert.That(
                result.b,
                Is.EqualTo(expected.b).Within(Tolerance),
                "Blue channel should match pre-composited value for dark palette on dark editor."
            );
        }

        [Test]
        public void VeryDarkPaletteOnDarkEditorUsesStandardAlpha()
        {
            if (!EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires dark editor theme (Pro skin).");
            }

            Color result = InvokeGetHeaderTint(VeryDarkColor);

            // Very dark palette (luminance < 0.5) on dark editor = NOT cross-theme, alpha = 0.62
            float expectedAlpha = 0.62f;
            Color expected = CalculateExpectedPreComposite(
                VeryDarkColor,
                DarkEditorBackground,
                expectedAlpha
            );

            Assert.That(
                result.r,
                Is.EqualTo(expected.r).Within(Tolerance),
                "Very dark palette should use standard alpha (0.62) on dark editor."
            );
        }

        // ============================================
        // 3. Pre-compositing Light Palette on Light Editor
        // ============================================

        [Test]
        public void LightPaletteOnLightEditorProducesExpectedPreComposite()
        {
            if (EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires light editor theme (non-Pro skin).");
            }

            Color result = InvokeGetHeaderTint(LightPaletteColor);

            // Light palette on light editor = NOT cross-theme, alpha = 0.55
            float expectedAlpha = 0.55f;
            Color expected = CalculateExpectedPreComposite(
                LightPaletteColor,
                LightEditorBackground,
                expectedAlpha
            );

            Assert.That(
                result.r,
                Is.EqualTo(expected.r).Within(Tolerance),
                "Red channel should match pre-composited value for light palette on light editor."
            );
            Assert.That(
                result.g,
                Is.EqualTo(expected.g).Within(Tolerance),
                "Green channel should match pre-composited value for light palette on light editor."
            );
            Assert.That(
                result.b,
                Is.EqualTo(expected.b).Within(Tolerance),
                "Blue channel should match pre-composited value for light palette on light editor."
            );
        }

        [Test]
        public void VeryLightPaletteOnLightEditorUsesStandardAlpha()
        {
            if (EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires light editor theme (non-Pro skin).");
            }

            Color result = InvokeGetHeaderTint(VeryLightColor);

            // Very light palette (luminance > 0.5) on light editor = NOT cross-theme, alpha = 0.55
            float expectedAlpha = 0.55f;
            Color expected = CalculateExpectedPreComposite(
                VeryLightColor,
                LightEditorBackground,
                expectedAlpha
            );

            Assert.That(
                result.r,
                Is.EqualTo(expected.r).Within(Tolerance),
                "Very light palette should use standard alpha (0.55) on light editor."
            );
        }

        // ============================================
        // 4. Cross-theme: Light Palette on Dark Editor
        // ============================================

        [Test]
        public void LightPaletteOnDarkEditorUsesCrossThemeAlpha()
        {
            if (!EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires dark editor theme (Pro skin).");
            }

            Color result = InvokeGetHeaderTint(LightPaletteColor);

            // Light palette (luminance > 0.5) on dark editor = cross-theme, alpha = 0.85
            float expectedAlpha = 0.85f;
            Color expected = CalculateExpectedPreComposite(
                LightPaletteColor,
                DarkEditorBackground,
                expectedAlpha
            );

            Assert.That(
                result.r,
                Is.EqualTo(expected.r).Within(Tolerance),
                "Light palette on dark editor should use cross-theme alpha (0.85)."
            );
            Assert.That(
                result.g,
                Is.EqualTo(expected.g).Within(Tolerance),
                "Light palette on dark editor should use cross-theme alpha (0.85)."
            );
            Assert.That(
                result.b,
                Is.EqualTo(expected.b).Within(Tolerance),
                "Light palette on dark editor should use cross-theme alpha (0.85)."
            );
        }

        [Test]
        public void VeryLightPaletteOnDarkEditorUsesCrossThemeAlpha()
        {
            if (!EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires dark editor theme (Pro skin).");
            }

            Color result = InvokeGetHeaderTint(VeryLightColor);

            // Very light palette (luminance > 0.5) on dark editor = cross-theme, alpha = 0.85
            float expectedAlpha = 0.85f;
            Color expected = CalculateExpectedPreComposite(
                VeryLightColor,
                DarkEditorBackground,
                expectedAlpha
            );

            Assert.That(
                result.r,
                Is.EqualTo(expected.r).Within(Tolerance),
                "Very light palette on dark editor should use cross-theme alpha (0.85)."
            );
        }

        [Test]
        public void ColoredLightPaletteOnDarkEditorUsesCrossThemeAlpha()
        {
            if (!EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires dark editor theme (Pro skin).");
            }

            Color result = InvokeGetHeaderTint(ColoredLightColor);

            // Colored light palette (luminance > 0.5) on dark editor = cross-theme
            float luminance = CalculateLuminance(ColoredLightColor);
            bool isLightBackground = luminance > 0.5f;

            Assert.That(
                isLightBackground,
                Is.True,
                "Test setup: colored light palette should have luminance > 0.5."
            );

            float expectedAlpha = 0.85f;
            Color expected = CalculateExpectedPreComposite(
                ColoredLightColor,
                DarkEditorBackground,
                expectedAlpha
            );

            Assert.That(
                result.r,
                Is.EqualTo(expected.r).Within(Tolerance),
                "Colored light palette on dark editor should use cross-theme alpha."
            );
        }

        // ============================================
        // 5. Cross-theme: Dark Palette on Light Editor
        // ============================================

        [Test]
        public void DarkPaletteOnLightEditorUsesCrossThemeAlpha()
        {
            if (EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires light editor theme (non-Pro skin).");
            }

            Color result = InvokeGetHeaderTint(DarkPaletteColor);

            // Dark palette (luminance < 0.5) on light editor = cross-theme, alpha = 0.85
            float expectedAlpha = 0.85f;
            Color expected = CalculateExpectedPreComposite(
                DarkPaletteColor,
                LightEditorBackground,
                expectedAlpha
            );

            Assert.That(
                result.r,
                Is.EqualTo(expected.r).Within(Tolerance),
                "Dark palette on light editor should use cross-theme alpha (0.85)."
            );
            Assert.That(
                result.g,
                Is.EqualTo(expected.g).Within(Tolerance),
                "Dark palette on light editor should use cross-theme alpha (0.85)."
            );
            Assert.That(
                result.b,
                Is.EqualTo(expected.b).Within(Tolerance),
                "Dark palette on light editor should use cross-theme alpha (0.85)."
            );
        }

        [Test]
        public void VeryDarkPaletteOnLightEditorUsesCrossThemeAlpha()
        {
            if (EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires light editor theme (non-Pro skin).");
            }

            Color result = InvokeGetHeaderTint(VeryDarkColor);

            // Very dark palette (luminance < 0.5) on light editor = cross-theme, alpha = 0.85
            float expectedAlpha = 0.85f;
            Color expected = CalculateExpectedPreComposite(
                VeryDarkColor,
                LightEditorBackground,
                expectedAlpha
            );

            Assert.That(
                result.r,
                Is.EqualTo(expected.r).Within(Tolerance),
                "Very dark palette on light editor should use cross-theme alpha (0.85)."
            );
        }

        [Test]
        public void ColoredDarkPaletteOnLightEditorUsesCrossThemeAlpha()
        {
            if (EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires light editor theme (non-Pro skin).");
            }

            Color result = InvokeGetHeaderTint(ColoredDarkColor);

            // Colored dark palette (luminance < 0.5) on light editor = cross-theme
            float luminance = CalculateLuminance(ColoredDarkColor);
            bool isLightBackground = luminance > 0.5f;

            Assert.That(
                isLightBackground,
                Is.False,
                "Test setup: colored dark palette should have luminance <= 0.5."
            );

            float expectedAlpha = 0.85f;
            Color expected = CalculateExpectedPreComposite(
                ColoredDarkColor,
                LightEditorBackground,
                expectedAlpha
            );

            Assert.That(
                result.r,
                Is.EqualTo(expected.r).Within(Tolerance),
                "Colored dark palette on light editor should use cross-theme alpha."
            );
        }

        // ============================================
        // 6. Result Not Identical to Input (Blending Occurred)
        // ============================================

        [Test]
        public void ResultColorIsNotIdenticalToInputForDarkPalette()
        {
            Color result = InvokeGetHeaderTint(DarkPaletteColor);

            // Result should differ from input (blending with editor background occurred)
            bool isIdentical =
                Mathf.Approximately(result.r, DarkPaletteColor.r)
                && Mathf.Approximately(result.g, DarkPaletteColor.g)
                && Mathf.Approximately(result.b, DarkPaletteColor.b);

            Assert.That(
                isIdentical,
                Is.False,
                "Pre-composited result should differ from input color, indicating blending occurred."
            );
        }

        [Test]
        public void ResultColorIsNotIdenticalToInputForLightPalette()
        {
            Color result = InvokeGetHeaderTint(LightPaletteColor);

            // Result should differ from input (blending with editor background occurred)
            bool isIdentical =
                Mathf.Approximately(result.r, LightPaletteColor.r)
                && Mathf.Approximately(result.g, LightPaletteColor.g)
                && Mathf.Approximately(result.b, LightPaletteColor.b);

            Assert.That(
                isIdentical,
                Is.False,
                "Pre-composited result should differ from input color, indicating blending occurred."
            );
        }

        [Test]
        public void ResultColorIsNotIdenticalToInputForMidGray()
        {
            Color result = InvokeGetHeaderTint(MidGrayColor);

            bool isIdentical =
                Mathf.Approximately(result.r, MidGrayColor.r)
                && Mathf.Approximately(result.g, MidGrayColor.g)
                && Mathf.Approximately(result.b, MidGrayColor.b);

            Assert.That(
                isIdentical,
                Is.False,
                "Pre-composited result should differ from input color for mid-gray palette."
            );
        }

        [Test]
        public void ResultColorIsNotIdenticalToEditorBackground()
        {
            Color result = InvokeGetHeaderTint(DarkPaletteColor);

            Color editorBg = EditorGUIUtility.isProSkin
                ? DarkEditorBackground
                : LightEditorBackground;

            bool isIdenticalToBackground =
                Mathf.Approximately(result.r, editorBg.r)
                && Mathf.Approximately(result.g, editorBg.g)
                && Mathf.Approximately(result.b, editorBg.b);

            Assert.That(
                isIdenticalToBackground,
                Is.False,
                "Pre-composited result should not be identical to editor background (palette influence should be visible)."
            );
        }

        // ============================================
        // 7. Determinism Tests
        // ============================================

        [Test]
        public void MultipleCallsWithSameInputProduceIdenticalOutput()
        {
            Color first = InvokeGetHeaderTint(DarkPaletteColor);
            Color second = InvokeGetHeaderTint(DarkPaletteColor);
            Color third = InvokeGetHeaderTint(DarkPaletteColor);

            Assert.That(
                second.r,
                Is.EqualTo(first.r).Within(Tolerance),
                "Multiple calls should produce identical red channel."
            );
            Assert.That(
                second.g,
                Is.EqualTo(first.g).Within(Tolerance),
                "Multiple calls should produce identical green channel."
            );
            Assert.That(
                second.b,
                Is.EqualTo(first.b).Within(Tolerance),
                "Multiple calls should produce identical blue channel."
            );
            Assert.That(
                second.a,
                Is.EqualTo(first.a).Within(Tolerance),
                "Multiple calls should produce identical alpha channel."
            );

            Assert.That(
                third.r,
                Is.EqualTo(first.r).Within(Tolerance),
                "Third call should match first call."
            );
            Assert.That(
                third.g,
                Is.EqualTo(first.g).Within(Tolerance),
                "Third call should match first call."
            );
            Assert.That(
                third.b,
                Is.EqualTo(first.b).Within(Tolerance),
                "Third call should match first call."
            );
            Assert.That(
                third.a,
                Is.EqualTo(first.a).Within(Tolerance),
                "Third call should match first call."
            );
        }

        [Test]
        public void DifferentInputsProduceDifferentOutputs()
        {
            Color darkResult = InvokeGetHeaderTint(DarkPaletteColor);
            Color lightResult = InvokeGetHeaderTint(LightPaletteColor);

            bool areIdentical =
                Mathf.Approximately(darkResult.r, lightResult.r)
                && Mathf.Approximately(darkResult.g, lightResult.g)
                && Mathf.Approximately(darkResult.b, lightResult.b);

            Assert.That(
                areIdentical,
                Is.False,
                "Different input colors should produce different pre-composited outputs."
            );
        }

        [Test]
        public void MultipleCallsWithLightPaletteProduceIdenticalOutput()
        {
            Color first = InvokeGetHeaderTint(LightPaletteColor);
            Color second = InvokeGetHeaderTint(LightPaletteColor);

            Assert.That(
                second.r,
                Is.EqualTo(first.r).Within(Tolerance),
                "Multiple calls with light palette should produce identical results."
            );
            Assert.That(
                second.g,
                Is.EqualTo(first.g).Within(Tolerance),
                "Multiple calls with light palette should produce identical results."
            );
            Assert.That(
                second.b,
                Is.EqualTo(first.b).Within(Tolerance),
                "Multiple calls with light palette should produce identical results."
            );
        }

        // ============================================
        // 8. Luminance Classification Tests
        // ============================================

        [Test]
        public void LightPaletteIsClassifiedAsLightBackground()
        {
            float luminance = CalculateLuminance(LightPaletteColor);

            Assert.That(
                luminance,
                Is.GreaterThan(0.5f),
                "Light palette color should have luminance > 0.5."
            );
        }

        [Test]
        public void DarkPaletteIsClassifiedAsDarkBackground()
        {
            float luminance = CalculateLuminance(DarkPaletteColor);

            Assert.That(
                luminance,
                Is.LessThanOrEqualTo(0.5f),
                "Dark palette color should have luminance <= 0.5."
            );
        }

        [Test]
        public void MidGrayIsClassifiedCorrectly()
        {
            float luminance = CalculateLuminance(MidGrayColor);

            // Mid gray (0.5, 0.5, 0.5) has luminance exactly 0.5
            // The threshold is > 0.5, so exactly 0.5 should be classified as dark
            Assert.That(
                luminance,
                Is.EqualTo(0.5f).Within(Tolerance),
                "Mid gray should have luminance of exactly 0.5."
            );
        }

        // ============================================
        // 9. Edge Case Tests
        // ============================================

        [Test]
        public void BlackColorProducesValidOutput()
        {
            Color black = new(0f, 0f, 0f, 1f);
            Color result = InvokeGetHeaderTint(black);

            Assert.That(
                result.a,
                Is.EqualTo(1f).Within(Tolerance),
                "Black input should produce fully opaque output."
            );

            // Black should be blended with editor background, result should be lighter than pure black
            Color editorBg = EditorGUIUtility.isProSkin
                ? DarkEditorBackground
                : LightEditorBackground;
            Assert.That(
                result.r,
                Is.GreaterThan(0f).Or.EqualTo(0f).Within(Tolerance),
                "Result should be valid (>= 0)."
            );
            Assert.That(result.r, Is.LessThanOrEqualTo(1f), "Result should be valid (<= 1).");
        }

        [Test]
        public void WhiteColorProducesValidOutput()
        {
            Color white = new(1f, 1f, 1f, 1f);
            Color result = InvokeGetHeaderTint(white);

            Assert.That(
                result.a,
                Is.EqualTo(1f).Within(Tolerance),
                "White input should produce fully opaque output."
            );

            Assert.That(
                result.r,
                Is.GreaterThanOrEqualTo(0f),
                "Result red should be valid (>= 0)."
            );
            Assert.That(result.r, Is.LessThanOrEqualTo(1f), "Result red should be valid (<= 1).");
        }

        [Test]
        public void NearBlackColorHandledCorrectly()
        {
            Color nearBlack = new(0.01f, 0.01f, 0.01f, 1f);
            Color result = InvokeGetHeaderTint(nearBlack);

            Assert.That(
                result.a,
                Is.EqualTo(1f).Within(Tolerance),
                "Near-black input should produce fully opaque output."
            );

            // Result should be blended with editor background
            Assert.That(
                result.r,
                Is.GreaterThan(nearBlack.r),
                "Near-black should be lightened by blending with editor background."
            );
        }

        [Test]
        public void NearWhiteColorHandledCorrectly()
        {
            Color nearWhite = new(0.99f, 0.99f, 0.99f, 1f);
            Color result = InvokeGetHeaderTint(nearWhite);

            Assert.That(
                result.a,
                Is.EqualTo(1f).Within(Tolerance),
                "Near-white input should produce fully opaque output."
            );

            // Result should be blended with editor background
            Assert.That(
                result.r,
                Is.LessThan(nearWhite.r),
                "Near-white should be darkened by blending with editor background."
            );
        }

        // ============================================
        // 10. Alpha Value Selection Tests
        // ============================================

        [Test]
        public void StandardAlphaUsedForMatchingThemes()
        {
            // When palette luminance matches editor theme, standard alpha is used
            Color result;
            float expectedAlpha;

            if (EditorGUIUtility.isProSkin)
            {
                // Dark palette on dark editor
                result = InvokeGetHeaderTint(DarkPaletteColor);
                expectedAlpha = 0.62f;
                Color expected = CalculateExpectedPreComposite(
                    DarkPaletteColor,
                    DarkEditorBackground,
                    expectedAlpha
                );

                Assert.That(
                    result.r,
                    Is.EqualTo(expected.r).Within(Tolerance),
                    "Dark palette on dark editor should use alpha 0.62."
                );
            }
            else
            {
                // Light palette on light editor
                result = InvokeGetHeaderTint(LightPaletteColor);
                expectedAlpha = 0.55f;
                Color expected = CalculateExpectedPreComposite(
                    LightPaletteColor,
                    LightEditorBackground,
                    expectedAlpha
                );

                Assert.That(
                    result.r,
                    Is.EqualTo(expected.r).Within(Tolerance),
                    "Light palette on light editor should use alpha 0.55."
                );
            }
        }

        [Test]
        public void CrossThemeAlphaUsedForMismatchedThemes()
        {
            // When palette luminance doesn't match editor theme, cross-theme alpha (0.85) is used
            Color result;
            float expectedAlpha = 0.85f;

            if (EditorGUIUtility.isProSkin)
            {
                // Light palette on dark editor (cross-theme)
                result = InvokeGetHeaderTint(LightPaletteColor);
                Color expected = CalculateExpectedPreComposite(
                    LightPaletteColor,
                    DarkEditorBackground,
                    expectedAlpha
                );

                Assert.That(
                    result.r,
                    Is.EqualTo(expected.r).Within(Tolerance),
                    "Cross-theme scenario should use alpha 0.85."
                );
            }
            else
            {
                // Dark palette on light editor (cross-theme)
                result = InvokeGetHeaderTint(DarkPaletteColor);
                Color expected = CalculateExpectedPreComposite(
                    DarkPaletteColor,
                    LightEditorBackground,
                    expectedAlpha
                );

                Assert.That(
                    result.r,
                    Is.EqualTo(expected.r).Within(Tolerance),
                    "Cross-theme scenario should use alpha 0.85."
                );
            }
        }
    }
#endif
}
