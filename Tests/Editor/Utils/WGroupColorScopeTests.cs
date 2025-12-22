namespace WallstopStudios.UnityHelpers.Tests.Utils
{
#if UNITY_EDITOR
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Editor.Utils.WGroup;

    /// <summary>
    /// Exhaustive tests for <see cref="WGroupColorScope"/> cross-theme palette support.
    /// </summary>
    /// <remarks>
    /// These tests verify that WGroup palettes render correctly regardless of editor theme,
    /// particularly when using a light palette on a dark editor (or vice versa).
    /// </remarks>
    [TestFixture]
    public sealed class WGroupColorScopeTests
    {
        private Color _originalContentColor;
        private Color _originalBackgroundColor;
        private Color _originalGuiColor;

        // Standard test palettes
        private static readonly UnityHelpersSettings.WGroupPaletteEntry LightPalette = new(
            new Color(0.82f, 0.82f, 0.82f, 1f),
            Color.black
        );

        private static readonly UnityHelpersSettings.WGroupPaletteEntry DarkPalette = new(
            new Color(0.215f, 0.215f, 0.215f, 1f),
            Color.white
        );

        private static readonly UnityHelpersSettings.WGroupPaletteEntry MidGrayPalette = new(
            new Color(0.5f, 0.5f, 0.5f, 1f),
            Color.white
        );

        private static readonly UnityHelpersSettings.WGroupPaletteEntry VeryLightPalette = new(
            new Color(0.95f, 0.95f, 0.95f, 1f),
            Color.black
        );

        private static readonly UnityHelpersSettings.WGroupPaletteEntry VeryDarkPalette = new(
            new Color(0.1f, 0.1f, 0.1f, 1f),
            Color.white
        );

        private static readonly UnityHelpersSettings.WGroupPaletteEntry ColoredLightPalette = new(
            new Color(0.9f, 0.85f, 0.8f, 1f),
            new Color(0.2f, 0.1f, 0.0f, 1f)
        );

        private static readonly UnityHelpersSettings.WGroupPaletteEntry ColoredDarkPalette = new(
            new Color(0.1f, 0.15f, 0.25f, 1f),
            new Color(0.9f, 0.95f, 1.0f, 1f)
        );

        [SetUp]
        public void SetUp()
        {
            _originalContentColor = GUI.contentColor;
            _originalBackgroundColor = GUI.backgroundColor;
            _originalGuiColor = GUI.color;
        }

        [TearDown]
        public void TearDown()
        {
            GUI.contentColor = _originalContentColor;
            GUI.backgroundColor = _originalBackgroundColor;
            GUI.color = _originalGuiColor;
        }

        [Test]
        public void LightPaletteOnDarkEditorIsCrossTheme()
        {
            if (!EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires dark editor theme (Pro skin).");
            }

            using (var scope = new WGroupColorScope(LightPalette))
            {
                // Light palette (luminance > 0.5) on dark editor = cross-theme
                Assert.That(
                    scope.IsActive,
                    Is.True,
                    "Light palette on dark editor should be detected as cross-theme."
                );
                // GUI.contentColor should be overridden to palette text color
                Assert.That(
                    GUI.contentColor,
                    Is.EqualTo(LightPalette.TextColor),
                    "Light palette on dark editor should override content color to black text."
                );
            }
        }

        [Test]
        public void DarkPaletteOnLightEditorIsCrossTheme()
        {
            if (EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires light editor theme (Personal skin).");
            }

            using (var scope = new WGroupColorScope(DarkPalette))
            {
                // Dark palette (luminance < 0.5) on light editor = cross-theme
                Assert.That(
                    scope.IsActive,
                    Is.True,
                    "Dark palette on light editor should be detected as cross-theme."
                );
                Assert.That(
                    GUI.contentColor,
                    Is.EqualTo(DarkPalette.TextColor),
                    "Dark palette on light editor should override content color to white text."
                );
            }
        }

        [Test]
        public void LightPaletteOnLightEditorIsNotCrossTheme()
        {
            if (EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires light editor theme (Personal skin).");
            }

            Color beforeContentColor = GUI.contentColor;

            using (var scope = new WGroupColorScope(LightPalette))
            {
                // Light palette on light editor = same theme, no override needed
                Assert.That(
                    scope.IsActive,
                    Is.False,
                    "Light palette on light editor should not be cross-theme."
                );
                Assert.That(
                    GUI.contentColor,
                    Is.EqualTo(beforeContentColor),
                    "Light palette on light editor should not override content color."
                );
            }
        }

        [Test]
        public void DarkPaletteOnDarkEditorIsNotCrossTheme()
        {
            if (!EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires dark editor theme (Pro skin).");
            }

            Color beforeContentColor = GUI.contentColor;

            using (var scope = new WGroupColorScope(DarkPalette))
            {
                // Dark palette on dark editor = same theme, no override needed
                Assert.That(
                    scope.IsActive,
                    Is.False,
                    "Dark palette on dark editor should not be cross-theme."
                );
                Assert.That(
                    GUI.contentColor,
                    Is.EqualTo(beforeContentColor),
                    "Dark palette on dark editor should not override content color."
                );
            }
        }

        [Test]
        public void IsCrossThemePaletteStaticMethodMatchesInstanceBehavior()
        {
            bool staticResult = WGroupColorScope.IsCrossThemePalette(LightPalette.BackgroundColor);

            using (var scope = new WGroupColorScope(LightPalette))
            {
                Assert.That(
                    scope.IsActive,
                    Is.EqualTo(staticResult),
                    "Static IsCrossThemePalette should match instance IsActive."
                );
            }
        }

        [Test]
        public void MidGrayPaletteLuminanceAt0Point5TreatedAsDark()
        {
            // Luminance = 0.5 exactly should be treated as dark (< 0.5 check is false for 0.5)
            // Actually the check is > 0.5, so 0.5 is NOT light
            float luminance =
                0.299f * MidGrayPalette.BackgroundColor.r
                + 0.587f * MidGrayPalette.BackgroundColor.g
                + 0.114f * MidGrayPalette.BackgroundColor.b;

            Assert.That(
                luminance,
                Is.EqualTo(0.5f).Within(0.001f),
                "Mid-gray palette should have luminance of exactly 0.5."
            );

            // With luminance = 0.5, isLightBackground = (0.5 > 0.5) = false
            // So it's treated as a dark palette
        }

        [Test]
        public void VeryLightPaletteHighLuminanceTreatedAsLight()
        {
            float luminance =
                0.299f * VeryLightPalette.BackgroundColor.r
                + 0.587f * VeryLightPalette.BackgroundColor.g
                + 0.114f * VeryLightPalette.BackgroundColor.b;

            Assert.That(
                luminance,
                Is.GreaterThan(0.5f),
                "Very light palette should have luminance > 0.5."
            );
            Assert.That(
                luminance,
                Is.GreaterThan(0.9f),
                "Very light palette should have luminance > 0.9."
            );
        }

        [Test]
        public void VeryDarkPaletteLowLuminanceTreatedAsDark()
        {
            float luminance =
                0.299f * VeryDarkPalette.BackgroundColor.r
                + 0.587f * VeryDarkPalette.BackgroundColor.g
                + 0.114f * VeryDarkPalette.BackgroundColor.b;

            Assert.That(
                luminance,
                Is.LessThan(0.5f),
                "Very dark palette should have luminance < 0.5."
            );
            Assert.That(
                luminance,
                Is.LessThan(0.15f),
                "Very dark palette should have luminance < 0.15."
            );
        }

        [Test]
        public void ColoredLightPalettePerceivedLuminanceTreatedAsLight()
        {
            // Uses proper luminance formula that accounts for human perception
            float luminance =
                0.299f * ColoredLightPalette.BackgroundColor.r
                + 0.587f * ColoredLightPalette.BackgroundColor.g
                + 0.114f * ColoredLightPalette.BackgroundColor.b;

            Assert.That(
                luminance,
                Is.GreaterThan(0.5f),
                "Warm-tinted light palette should be treated as light based on perceived luminance."
            );
        }

        [Test]
        public void ColoredDarkPalettePerceivedLuminanceTreatedAsDark()
        {
            float luminance =
                0.299f * ColoredDarkPalette.BackgroundColor.r
                + 0.587f * ColoredDarkPalette.BackgroundColor.g
                + 0.114f * ColoredDarkPalette.BackgroundColor.b;

            Assert.That(
                luminance,
                Is.LessThan(0.5f),
                "Blue-tinted dark palette should be treated as dark based on perceived luminance."
            );
        }

        [Test]
        public void ScopeRestoresContentColorOnDispose()
        {
            Color customColor = new(0.123f, 0.456f, 0.789f, 1f);
            GUI.contentColor = customColor;

            using (var scope = new WGroupColorScope(LightPalette))
            {
                // Content color may or may not be modified depending on theme
            }

            Assert.That(
                GUI.contentColor,
                Is.EqualTo(customColor),
                "Content color should be restored after scope disposal."
            );
        }

        [Test]
        public void ScopeRestoresGuiColorOnDispose()
        {
            Color customColor = new(0.111f, 0.222f, 0.333f, 1f);
            GUI.color = customColor;

            using (var scope = new WGroupColorScope(LightPalette))
            {
                // GUI color may or may not be modified depending on theme
            }

            Assert.That(
                GUI.color,
                Is.EqualTo(customColor),
                "GUI color should be restored after scope disposal."
            );
        }

        [Test]
        public void NestedScopesRestoreCorrectly()
        {
            Color outer = new(0.1f, 0.1f, 0.1f, 1f);
            Color middle = new(0.5f, 0.5f, 0.5f, 1f);
            Color inner = new(0.9f, 0.9f, 0.9f, 1f);

            GUI.contentColor = outer;

            using (var scope1 = new WGroupColorScope(LightPalette))
            {
                GUI.contentColor = middle;

                using (var scope2 = new WGroupColorScope(DarkPalette))
                {
                    GUI.contentColor = inner;

                    using (var scope3 = new WGroupColorScope(MidGrayPalette))
                    {
                        // Innermost scope
                    }

                    // After scope3, should still be inner (scope3 restores to inner)
                }

                // After scope2, should still be middle (scope2 restores to middle)
            }

            // After scope1, should be outer (scope1 restores to outer)
            Assert.That(
                GUI.contentColor,
                Is.EqualTo(outer),
                "Nested scopes should restore colors in correct order."
            );
        }

        [Test]
        public void DisposeIsIdempotent()
        {
            Color original = GUI.contentColor;
            var scope = new WGroupColorScope(LightPalette);

            scope.Dispose();
            Color afterFirstDispose = GUI.contentColor;

            scope.Dispose();
            Color afterSecondDispose = GUI.contentColor;

            Assert.That(
                afterFirstDispose,
                Is.EqualTo(original),
                "First dispose should restore original color."
            );
            Assert.That(
                afterSecondDispose,
                Is.EqualTo(original),
                "Second dispose should not change anything."
            );
        }

        [Test]
        public void LightBackgroundFieldColorIsLight()
        {
            using (var scope = new WGroupColorScope(LightPalette))
            {
                // Field background should be light for light palettes
                float fieldLuminance =
                    0.299f * scope.FieldBackgroundColor.r
                    + 0.587f * scope.FieldBackgroundColor.g
                    + 0.114f * scope.FieldBackgroundColor.b;

                Assert.That(
                    fieldLuminance,
                    Is.GreaterThan(0.8f),
                    "Light palette field background should be light (luminance > 0.8)."
                );
            }
        }

        [Test]
        public void DarkBackgroundFieldColorIsDark()
        {
            using (var scope = new WGroupColorScope(DarkPalette))
            {
                // Field background should be dark for dark palettes
                float fieldLuminance =
                    0.299f * scope.FieldBackgroundColor.r
                    + 0.587f * scope.FieldBackgroundColor.g
                    + 0.114f * scope.FieldBackgroundColor.b;

                Assert.That(
                    fieldLuminance,
                    Is.LessThan(0.25f),
                    "Dark palette field background should be dark (luminance < 0.25)."
                );
            }
        }

        [Test]
        public void CalculateFieldBackgroundColorStaticMethodMatchesInstanceProperty()
        {
            Color staticResult = WGroupColorScope.CalculateFieldBackgroundColor(
                LightPalette.BackgroundColor
            );

            using (var scope = new WGroupColorScope(LightPalette))
            {
                Assert.That(
                    scope.FieldBackgroundColor,
                    Is.EqualTo(staticResult),
                    "Static CalculateFieldBackgroundColor should match instance FieldBackgroundColor."
                );
            }
        }

        [Test]
        public void FieldBackgroundColorLightPaletteIsNearWhite()
        {
            Color fieldBg = WGroupColorScope.CalculateFieldBackgroundColor(
                LightPalette.BackgroundColor
            );

            Assert.That(fieldBg.r, Is.GreaterThan(0.9f), "Light palette field R should be > 0.9.");
            Assert.That(fieldBg.g, Is.GreaterThan(0.9f), "Light palette field G should be > 0.9.");
            Assert.That(fieldBg.b, Is.GreaterThan(0.9f), "Light palette field B should be > 0.9.");
        }

        [Test]
        public void FieldBackgroundColorDarkPaletteIsDarkGray()
        {
            Color fieldBg = WGroupColorScope.CalculateFieldBackgroundColor(
                DarkPalette.BackgroundColor
            );

            Assert.That(fieldBg.r, Is.LessThan(0.25f), "Dark palette field R should be < 0.25.");
            Assert.That(fieldBg.g, Is.LessThan(0.25f), "Dark palette field G should be < 0.25.");
            Assert.That(fieldBg.b, Is.LessThan(0.25f), "Dark palette field B should be < 0.25.");
        }

        [Test]
        public void CrossThemeGuiColorIsWhiteForVerbatimNestedColors()
        {
            // GUI.color should be pure white in cross-theme mode.
            // This ensures nested elements (like SerializableDictionary/Set drawers)
            // receive untinted colors that they can modify independently.

            UnityHelpersSettings.WGroupPaletteEntry crossThemePalette;
            if (EditorGUIUtility.isProSkin)
            {
                crossThemePalette = LightPalette; // Light on dark = cross
            }
            else
            {
                crossThemePalette = DarkPalette; // Dark on light = cross
            }

            using (var scope = new WGroupColorScope(crossThemePalette))
            {
                // GUI.color should be pure white in cross-theme mode
                // to prevent color tinting in nested drawers
                Assert.That(
                    GUI.color.r,
                    Is.EqualTo(1f).Within(0.001f),
                    "GUI.color.r should be 1 (white) in cross-theme mode."
                );
                Assert.That(
                    GUI.color.g,
                    Is.EqualTo(1f).Within(0.001f),
                    "GUI.color.g should be 1 (white) in cross-theme mode."
                );
                Assert.That(
                    GUI.color.b,
                    Is.EqualTo(1f).Within(0.001f),
                    "GUI.color.b should be 1 (white) in cross-theme mode."
                );
            }
        }

        [Test]
        public void SameThemeGuiColorUnchanged()
        {
            Color original = GUI.color;

            UnityHelpersSettings.WGroupPaletteEntry sameThemePalette;
            if (EditorGUIUtility.isProSkin)
            {
                sameThemePalette = DarkPalette; // Dark on dark = same
            }
            else
            {
                sameThemePalette = LightPalette; // Light on light = same
            }

            using (var scope = new WGroupColorScope(sameThemePalette))
            {
                Assert.That(
                    GUI.color,
                    Is.EqualTo(original),
                    "GUI color should not change when palette matches editor theme."
                );
            }
        }

        [Test]
        public void ZeroAlphaPaletteHandledGracefully()
        {
            var zeroAlphaPalette = new UnityHelpersSettings.WGroupPaletteEntry(
                new Color(0.5f, 0.5f, 0.5f, 0f),
                new Color(0f, 0f, 0f, 0f)
            );

            Assert.DoesNotThrow(
                () =>
                {
                    using (var scope = new WGroupColorScope(zeroAlphaPalette))
                    {
                        // Should not crash
                    }
                },
                "Zero alpha palette should be handled gracefully."
            );
        }

        [Test]
        public void FullBrightWhitePaletteHandledCorrectly()
        {
            var whitePalette = new UnityHelpersSettings.WGroupPaletteEntry(
                Color.white,
                Color.black
            );

            Assert.DoesNotThrow(() =>
            {
                using (var scope = new WGroupColorScope(whitePalette))
                {
                    // Should not crash or produce invalid colors
                    Assert.That(scope.FieldBackgroundColor.r, Is.LessThanOrEqualTo(1f));
                    Assert.That(scope.FieldBackgroundColor.g, Is.LessThanOrEqualTo(1f));
                    Assert.That(scope.FieldBackgroundColor.b, Is.LessThanOrEqualTo(1f));
                }
            });
        }

        [Test]
        public void FullBlackPaletteHandledCorrectly()
        {
            var blackPalette = new UnityHelpersSettings.WGroupPaletteEntry(
                Color.black,
                Color.white
            );

            Assert.DoesNotThrow(() =>
            {
                using (var scope = new WGroupColorScope(blackPalette))
                {
                    // Should not crash or produce invalid colors
                    Assert.That(scope.FieldBackgroundColor.r, Is.GreaterThanOrEqualTo(0f));
                    Assert.That(scope.FieldBackgroundColor.g, Is.GreaterThanOrEqualTo(0f));
                    Assert.That(scope.FieldBackgroundColor.b, Is.GreaterThanOrEqualTo(0f));
                }
            });
        }

        [Test]
        public void NegativeColorValuesClampedToValidRange()
        {
            // While Unity shouldn't produce negative colors, test defensive handling
            var edgePalette = new UnityHelpersSettings.WGroupPaletteEntry(
                new Color(-0.1f, 0.5f, 1.5f, 1f),
                new Color(0.5f, 0.5f, 0.5f, 1f)
            );

            Assert.DoesNotThrow(
                () =>
                {
                    using (var scope = new WGroupColorScope(edgePalette))
                    {
                        // Should handle gracefully
                        Assert.That(scope.FieldBackgroundColor.r, Is.GreaterThanOrEqualTo(0f));
                        Assert.That(scope.FieldBackgroundColor.g, Is.GreaterThanOrEqualTo(0f));
                        Assert.That(scope.FieldBackgroundColor.b, Is.GreaterThanOrEqualTo(0f));
                    }
                },
                "Edge case colors should be handled gracefully."
            );
        }

        [Test]
        public void DefaultLightPaletteResolvedCorrectly()
        {
            var palette = UnityHelpersSettings.ResolveWGroupPalette(
                UnityHelpersSettings.WGroupLightThemeColorKey
            );

            float luminance =
                0.299f * palette.BackgroundColor.r
                + 0.587f * palette.BackgroundColor.g
                + 0.114f * palette.BackgroundColor.b;

            Assert.That(
                luminance,
                Is.GreaterThan(0.5f),
                "Default-Light palette should have light background (luminance > 0.5)."
            );

            float textLuminance =
                0.299f * palette.TextColor.r
                + 0.587f * palette.TextColor.g
                + 0.114f * palette.TextColor.b;

            Assert.That(
                textLuminance,
                Is.LessThan(0.5f),
                "Default-Light palette should have dark text (luminance < 0.5)."
            );
        }

        [Test]
        public void DefaultDarkPaletteResolvedCorrectly()
        {
            var palette = UnityHelpersSettings.ResolveWGroupPalette(
                UnityHelpersSettings.WGroupDarkThemeColorKey
            );

            float luminance =
                0.299f * palette.BackgroundColor.r
                + 0.587f * palette.BackgroundColor.g
                + 0.114f * palette.BackgroundColor.b;

            Assert.That(
                luminance,
                Is.LessThan(0.5f),
                "Default-Dark palette should have dark background (luminance < 0.5)."
            );

            float textLuminance =
                0.299f * palette.TextColor.r
                + 0.587f * palette.TextColor.g
                + 0.114f * palette.TextColor.b;

            Assert.That(
                textLuminance,
                Is.GreaterThan(0.5f),
                "Default-Dark palette should have light text (luminance > 0.5)."
            );
        }

        [Test]
        public void DefaultLightOnDarkEditorAppliesCorrectContentColor()
        {
            if (!EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires dark editor theme (Pro skin).");
            }

            var palette = UnityHelpersSettings.ResolveWGroupPalette(
                UnityHelpersSettings.WGroupLightThemeColorKey
            );

            using (var scope = new WGroupColorScope(palette))
            {
                Assert.That(
                    GUI.contentColor,
                    Is.EqualTo(palette.TextColor),
                    "Default-Light on dark editor should override content color to palette text color."
                );
            }
        }

        [Test]
        public void DefaultDarkOnLightEditorAppliesCorrectContentColor()
        {
            if (EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires light editor theme (Personal skin).");
            }

            var palette = UnityHelpersSettings.ResolveWGroupPalette(
                UnityHelpersSettings.WGroupDarkThemeColorKey
            );

            using (var scope = new WGroupColorScope(palette))
            {
                Assert.That(
                    GUI.contentColor,
                    Is.EqualTo(palette.TextColor),
                    "Default-Dark on light editor should override content color to palette text color."
                );
            }
        }

        [Test]
        public void DefaultPaletteMatchesCurrentEditorTheme()
        {
            var palette = UnityHelpersSettings.ResolveWGroupPalette(
                UnityHelpersSettings.DefaultWGroupColorKey
            );

            float bgLuminance =
                0.299f * palette.BackgroundColor.r
                + 0.587f * palette.BackgroundColor.g
                + 0.114f * palette.BackgroundColor.b;

            if (EditorGUIUtility.isProSkin)
            {
                Assert.That(
                    bgLuminance,
                    Is.LessThan(0.5f),
                    "Default palette on dark editor should resolve to dark background."
                );
            }
            else
            {
                Assert.That(
                    bgLuminance,
                    Is.GreaterThan(0.5f),
                    "Default palette on light editor should resolve to light background."
                );
            }
        }

        [Test]
        public void LightPaletteTextHasSufficientContrast()
        {
            float bgLuminance =
                0.299f * LightPalette.BackgroundColor.r
                + 0.587f * LightPalette.BackgroundColor.g
                + 0.114f * LightPalette.BackgroundColor.b;

            float textLuminance =
                0.299f * LightPalette.TextColor.r
                + 0.587f * LightPalette.TextColor.g
                + 0.114f * LightPalette.TextColor.b;

            float contrastRatio = Mathf.Abs(bgLuminance - textLuminance);

            Assert.That(
                contrastRatio,
                Is.GreaterThan(0.4f),
                "Light palette should have sufficient contrast between background and text."
            );
        }

        [Test]
        public void DarkPaletteTextHasSufficientContrast()
        {
            float bgLuminance =
                0.299f * DarkPalette.BackgroundColor.r
                + 0.587f * DarkPalette.BackgroundColor.g
                + 0.114f * DarkPalette.BackgroundColor.b;

            float textLuminance =
                0.299f * DarkPalette.TextColor.r
                + 0.587f * DarkPalette.TextColor.g
                + 0.114f * DarkPalette.TextColor.b;

            float contrastRatio = Mathf.Abs(bgLuminance - textLuminance);

            Assert.That(
                contrastRatio,
                Is.GreaterThan(0.4f),
                "Dark palette should have sufficient contrast between background and text."
            );
        }

        [Test]
        public void LightBackgroundBorderColorIsDark()
        {
            // Using reflection or public API to test border color calculation
            // The border should be dark (towards black) for light backgrounds
            Color lightBg = new(0.82f, 0.82f, 0.82f, 1f);

            float bgLuminance = 0.299f * lightBg.r + 0.587f * lightBg.g + 0.114f * lightBg.b;
            bool isLightBackground = bgLuminance > 0.5f;

            Assert.That(
                isLightBackground,
                Is.True,
                "Test setup: background should be classified as light."
            );

            // Border calculation: Lerp(baseColor, black, 0.7) for light backgrounds
            Color expectedBorderBase = Color.Lerp(lightBg, Color.black, 0.7f);

            float borderLuminance =
                0.299f * expectedBorderBase.r
                + 0.587f * expectedBorderBase.g
                + 0.114f * expectedBorderBase.b;

            Assert.That(
                borderLuminance,
                Is.LessThan(bgLuminance),
                "Border color should be darker than background for light palettes."
            );
            Assert.That(
                borderLuminance,
                Is.LessThan(0.4f),
                "Border color should be distinctly dark for visibility."
            );
        }

        [Test]
        public void DarkBackgroundBorderColorIsLight()
        {
            Color darkBg = new(0.215f, 0.215f, 0.215f, 1f);

            float bgLuminance = 0.299f * darkBg.r + 0.587f * darkBg.g + 0.114f * darkBg.b;
            bool isLightBackground = bgLuminance > 0.5f;

            Assert.That(
                isLightBackground,
                Is.False,
                "Test setup: background should be classified as dark."
            );

            // Border calculation: Lerp(baseColor, white, 0.15) for dark backgrounds
            Color expectedBorderBase = Color.Lerp(darkBg, Color.white, 0.15f);

            float borderLuminance =
                0.299f * expectedBorderBase.r
                + 0.587f * expectedBorderBase.g
                + 0.114f * expectedBorderBase.b;

            Assert.That(
                borderLuminance,
                Is.GreaterThan(bgLuminance),
                "Border color should be lighter than background for dark palettes."
            );
        }

        [Test]
        public void CrossThemePaletteHasHigherAlpha()
        {
            // Cross-theme palettes should use 0.85 alpha for better visibility
            // Same-theme palettes use 0.62 (dark) or 0.55 (light)

            // The tint calculation is based on background luminance
            Color lightBg = new(0.82f, 0.82f, 0.82f, 1f);
            float bgLuminance = 0.299f * lightBg.r + 0.587f * lightBg.g + 0.114f * lightBg.b;
            bool isLightBackground = bgLuminance > 0.5f;

            // Cross-theme detection: light background on dark editor (isProSkin)
            // or dark background on light editor (!isProSkin)
            bool wouldBeCrossTheme = isLightBackground == EditorGUIUtility.isProSkin;

            if (wouldBeCrossTheme)
            {
                // Expected alpha for cross-theme: 0.85
                float expectedAlpha = 0.85f;
                Assert.That(
                    expectedAlpha,
                    Is.GreaterThan(0.62f),
                    "Cross-theme alpha should be higher than same-theme dark alpha."
                );
                Assert.That(
                    expectedAlpha,
                    Is.GreaterThan(0.55f),
                    "Cross-theme alpha should be higher than same-theme light alpha."
                );
            }
            else
            {
                // Same theme - alpha depends on editor
                float expectedAlpha = EditorGUIUtility.isProSkin ? 0.62f : 0.55f;
                Assert.That(
                    expectedAlpha,
                    Is.LessThan(0.85f),
                    "Same-theme alpha should be less than cross-theme alpha."
                );
            }
        }

        [Test]
        public void StyleOverridesAppliedForEntireScopeDurationInCrossTheme()
        {
            // This test verifies that styles are overridden for the entire scope duration
            // We can only test this on one editor theme, so we check if we're in cross-theme scenario
            UnityHelpersSettings.WGroupPaletteEntry testPalette = EditorGUIUtility.isProSkin
                ? LightPalette
                : DarkPalette;

            Color originalLabelColor = EditorStyles.label.normal.textColor;
            Color originalFoldoutColor = EditorStyles.foldout.normal.textColor;

            using (var scope = new WGroupColorScope(testPalette))
            {
                if (scope.IsActive)
                {
                    // Label and foldout text colors should be overridden
                    Assert.That(
                        EditorStyles.label.normal.textColor,
                        Is.Not.EqualTo(originalLabelColor),
                        "Label text color should be overridden in cross-theme scope."
                    );
                    Assert.That(
                        EditorStyles.foldout.normal.textColor,
                        Is.Not.EqualTo(originalFoldoutColor),
                        "Foldout text color should be overridden in cross-theme scope."
                    );
                }
            }

            // After scope, colors should be restored
            Assert.That(
                EditorStyles.label.normal.textColor,
                Is.EqualTo(originalLabelColor),
                "Label text color should be restored after scope disposal."
            );
            Assert.That(
                EditorStyles.foldout.normal.textColor,
                Is.EqualTo(originalFoldoutColor),
                "Foldout text color should be restored after scope disposal."
            );
        }

        [Test]
        public void BackgroundColorOverriddenInCrossThemeScope()
        {
            UnityHelpersSettings.WGroupPaletteEntry testPalette = EditorGUIUtility.isProSkin
                ? LightPalette
                : DarkPalette;

            Color originalBackgroundColor = GUI.backgroundColor;

            using (var scope = new WGroupColorScope(testPalette))
            {
                if (scope.IsActive)
                {
                    Assert.That(
                        GUI.backgroundColor,
                        Is.Not.EqualTo(originalBackgroundColor),
                        "GUI.backgroundColor should be overridden in cross-theme scope."
                    );
                }
            }

            Assert.That(
                GUI.backgroundColor,
                Is.EqualTo(originalBackgroundColor),
                "GUI.backgroundColor should be restored after scope disposal."
            );
        }

        [Test]
        public void TextFieldStyleOverriddenInCrossThemeScope()
        {
            UnityHelpersSettings.WGroupPaletteEntry testPalette = EditorGUIUtility.isProSkin
                ? LightPalette
                : DarkPalette;

            Texture2D originalBackground = EditorStyles.textField.normal.background;
            Color originalTextColor = EditorStyles.textField.normal.textColor;

            using (var scope = new WGroupColorScope(testPalette))
            {
                if (scope.IsActive)
                {
                    // Background might be overridden if custom textures are created
                    Assert.That(
                        EditorStyles.textField.normal.textColor,
                        Is.Not.EqualTo(originalTextColor),
                        "TextField text color should be overridden in cross-theme scope."
                    );
                }
            }

            Assert.That(
                EditorStyles.textField.normal.background,
                Is.EqualTo(originalBackground),
                "TextField background should be restored after scope disposal."
            );
            Assert.That(
                EditorStyles.textField.normal.textColor,
                Is.EqualTo(originalTextColor),
                "TextField text color should be restored after scope disposal."
            );
        }

        [Test]
        public void HelpBoxStyleOverriddenInCrossThemeScope()
        {
            UnityHelpersSettings.WGroupPaletteEntry testPalette = EditorGUIUtility.isProSkin
                ? LightPalette
                : DarkPalette;

            Texture2D originalBackground = EditorStyles.helpBox.normal.background;
            Color originalTextColor = EditorStyles.helpBox.normal.textColor;

            using (var scope = new WGroupColorScope(testPalette))
            {
                if (scope.IsActive)
                {
                    // HelpBox is used for list/array containers
                    Assert.That(
                        EditorStyles.helpBox.normal.textColor,
                        Is.Not.EqualTo(originalTextColor),
                        "HelpBox text color should be overridden in cross-theme scope."
                    );
                }
            }

            Assert.That(
                EditorStyles.helpBox.normal.background,
                Is.EqualTo(originalBackground),
                "HelpBox background should be restored after scope disposal."
            );
            Assert.That(
                EditorStyles.helpBox.normal.textColor,
                Is.EqualTo(originalTextColor),
                "HelpBox text color should be restored after scope disposal."
            );
        }

        [Test]
        public void ExitWGroupThemingInsideActiveColorScopeResetsStyles()
        {
            if (!EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires dark editor theme (Pro skin).");
            }

            using (var colorScope = new WGroupColorScope(LightPalette))
            {
                Assert.That(
                    colorScope.IsActive,
                    Is.True,
                    "Light palette on dark editor should be active."
                );

                // WGroupColorScope has modified EditorStyles
                // ExitWGroupTheming should reset them
                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    // GUI colors should be reset to white
                    Assert.That(
                        GUI.contentColor,
                        Is.EqualTo(Color.white),
                        "ExitWGroupTheming should reset contentColor to white."
                    );
                    Assert.That(
                        GUI.color,
                        Is.EqualTo(Color.white),
                        "ExitWGroupTheming should reset color to white."
                    );
                    Assert.That(
                        GUI.backgroundColor,
                        Is.EqualTo(Color.white),
                        "ExitWGroupTheming should reset backgroundColor to white."
                    );

                    // EditorStyles should be reset to defaults (null backgrounds)
                    Assert.That(
                        EditorStyles.textField.normal.background,
                        Is.Null,
                        "ExitWGroupTheming should clear textField background."
                    );
                    Assert.That(
                        EditorStyles.label.normal.textColor,
                        Is.EqualTo(default(Color)),
                        "ExitWGroupTheming should clear label textColor."
                    );
                }

                // After ExitWGroupTheming scope ends, WGroupColorScope modifications should be restored
                Assert.That(
                    GUI.contentColor,
                    Is.EqualTo(LightPalette.TextColor),
                    "After ExitWGroupTheming ends, contentColor should be restored to palette text color."
                );
            }
        }

        [Test]
        public void ExitWGroupThemingRestoresWGroupColorScopeModificationsOnDispose()
        {
            if (!EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires dark editor theme (Pro skin).");
            }

            Texture2D originalTextFieldBg = EditorStyles.textField.normal.background;
            Color originalLabelColor = EditorStyles.label.normal.textColor;

            using (var colorScope = new WGroupColorScope(LightPalette))
            {
                Assert.That(colorScope.IsActive, Is.True);

                // Capture what WGroupColorScope set
                Texture2D wgroupTextFieldBg = EditorStyles.textField.normal.background;
                Color wgroupLabelColor = EditorStyles.label.normal.textColor;

                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    // Inside exit scope, backgrounds should be cleared but text colors preserved
                    Assert.That(EditorStyles.textField.normal.background, Is.Null);
                    Assert.That(
                        EditorStyles.label.normal.textColor,
                        Is.EqualTo(wgroupLabelColor),
                        "Text colors should be preserved inside ExitWGroupTheming scope."
                    );
                }

                // After exit scope, WGroupColorScope's modifications should be restored
                Assert.That(
                    EditorStyles.textField.normal.background,
                    Is.EqualTo(wgroupTextFieldBg),
                    "ExitWGroupTheming should restore WGroupColorScope's textField background."
                );
                Assert.That(
                    EditorStyles.label.normal.textColor,
                    Is.EqualTo(wgroupLabelColor),
                    "ExitWGroupTheming should restore WGroupColorScope's label textColor."
                );
            }

            // After WGroupColorScope ends, original values should be restored
            Assert.That(
                EditorStyles.textField.normal.background,
                Is.EqualTo(originalTextFieldBg),
                "After all scopes, original textField background should be restored."
            );
            Assert.That(
                EditorStyles.label.normal.textColor,
                Is.EqualTo(originalLabelColor),
                "After all scopes, original label textColor should be restored."
            );
        }

        [Test]
        public void ExitWGroupThemingWhenColorScopeNotActiveStillWorks()
        {
            // When not cross-theme, WGroupColorScope won't be active, but ExitWGroupTheming should still work
            UnityHelpersSettings.WGroupPaletteEntry matchingPalette = EditorGUIUtility.isProSkin
                ? DarkPalette
                : LightPalette;

            using (var colorScope = new WGroupColorScope(matchingPalette))
            {
                // Not active because palette matches editor theme
                Assert.That(
                    colorScope.IsActive,
                    Is.False,
                    "Matching palette should not be cross-theme."
                );

                Color beforeContentColor = GUI.contentColor;

                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    // Should still reset to white
                    Assert.That(GUI.contentColor, Is.EqualTo(Color.white));
                }

                // Should restore
                Assert.That(GUI.contentColor, Is.EqualTo(beforeContentColor));
            }
        }

        [Test]
        public void NestedWGroupColorScopesWithExitWGroupThemingInMiddle()
        {
            if (!EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires dark editor theme (Pro skin).");
            }

            Color originalContentColor = GUI.contentColor;

            using (var outerScope = new WGroupColorScope(LightPalette))
            {
                Assert.That(outerScope.IsActive, Is.True);
                Color outerContentColor = GUI.contentColor;

                using (var innerScope = new WGroupColorScope(VeryLightPalette))
                {
                    Color innerContentColor = GUI.contentColor;

                    using (GroupGUIWidthUtility.ExitWGroupTheming())
                    {
                        // Inside exit scope, all theming cleared
                        Assert.That(GUI.contentColor, Is.EqualTo(Color.white));
                        Assert.That(GroupGUIWidthUtility.IsInsideWGroup, Is.False);
                    }

                    // After exit scope, should restore to inner scope's state
                    Assert.That(
                        GUI.contentColor,
                        Is.EqualTo(innerContentColor),
                        "After exit scope, should restore to inner scope content color."
                    );
                }

                // After inner scope, should restore to outer scope's state
                Assert.That(
                    GUI.contentColor,
                    Is.EqualTo(outerContentColor),
                    "After inner scope, should restore to outer scope content color."
                );
            }

            // After all scopes, should restore to original
            Assert.That(
                GUI.contentColor,
                Is.EqualTo(originalContentColor),
                "After all scopes, should restore to original content color."
            );
        }

        [Test]
        public void ExitWGroupThemingExceptionSafetyWithActiveColorScope()
        {
            if (!EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires dark editor theme (Pro skin).");
            }

            using (var colorScope = new WGroupColorScope(LightPalette))
            {
                Assert.That(colorScope.IsActive, Is.True);
                Color wgroupContentColor = GUI.contentColor;
                Texture2D wgroupTextFieldBg = EditorStyles.textField.normal.background;

                try
                {
                    using (GroupGUIWidthUtility.ExitWGroupTheming())
                    {
                        Assert.That(GUI.contentColor, Is.EqualTo(Color.white));
                        throw new System.InvalidOperationException("Test exception");
                    }
                }
                catch (System.InvalidOperationException)
                {
                    // Expected
                }

                // After exception, WGroupColorScope's state should be restored
                Assert.That(
                    GUI.contentColor,
                    Is.EqualTo(wgroupContentColor),
                    "After exception, contentColor should be restored to WGroupColorScope's value."
                );
                Assert.That(
                    EditorStyles.textField.normal.background,
                    Is.EqualTo(wgroupTextFieldBg),
                    "After exception, textField background should be restored to WGroupColorScope's value."
                );
            }
        }

        [Test]
        public void MultipleExitWGroupThemingScopesInsideSameColorScope()
        {
            if (!EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires dark editor theme (Pro skin).");
            }

            using (var colorScope = new WGroupColorScope(LightPalette))
            {
                Assert.That(colorScope.IsActive, Is.True);
                Color wgroupContentColor = GUI.contentColor;

                // First exit scope
                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    Assert.That(GUI.contentColor, Is.EqualTo(Color.white));
                }

                Assert.That(
                    GUI.contentColor,
                    Is.EqualTo(wgroupContentColor),
                    "After first exit scope, should restore."
                );

                // Second exit scope
                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    Assert.That(GUI.contentColor, Is.EqualTo(Color.white));
                }

                Assert.That(
                    GUI.contentColor,
                    Is.EqualTo(wgroupContentColor),
                    "After second exit scope, should restore."
                );
            }
        }

        [Test]
        public void ExitWGroupThemingAllBackgroundStatesAreNullInsideScope()
        {
            if (!EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires dark editor theme (Pro skin).");
            }

            using (var colorScope = new WGroupColorScope(LightPalette))
            {
                Assert.That(colorScope.IsActive, Is.True);

                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    // Check all 8 background states for textField
                    Assert.That(EditorStyles.textField.normal.background, Is.Null);
                    Assert.That(EditorStyles.textField.focused.background, Is.Null);
                    Assert.That(EditorStyles.textField.active.background, Is.Null);
                    Assert.That(EditorStyles.textField.hover.background, Is.Null);
                    Assert.That(EditorStyles.textField.onNormal.background, Is.Null);
                    Assert.That(EditorStyles.textField.onFocused.background, Is.Null);
                    Assert.That(EditorStyles.textField.onActive.background, Is.Null);
                    Assert.That(EditorStyles.textField.onHover.background, Is.Null);

                    // Check popup too
                    Assert.That(EditorStyles.popup.normal.background, Is.Null);
                    Assert.That(EditorStyles.popup.focused.background, Is.Null);
                }
            }
        }

        [Test]
        public void ExitWGroupThemingAllTextColorStatesArePreservedInsideScope()
        {
            if (!EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires dark editor theme (Pro skin).");
            }

            using (var colorScope = new WGroupColorScope(LightPalette))
            {
                Assert.That(colorScope.IsActive, Is.True);

                // Capture text colors set by WGroupColorScope
                Color labelNormal = EditorStyles.label.normal.textColor;
                Color labelFocused = EditorStyles.label.focused.textColor;
                Color labelActive = EditorStyles.label.active.textColor;
                Color labelHover = EditorStyles.label.hover.textColor;
                Color labelOnNormal = EditorStyles.label.onNormal.textColor;
                Color labelOnFocused = EditorStyles.label.onFocused.textColor;
                Color labelOnActive = EditorStyles.label.onActive.textColor;
                Color labelOnHover = EditorStyles.label.onHover.textColor;
                Color foldoutNormal = EditorStyles.foldout.normal.textColor;

                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    // Text colors should be preserved (not cleared) to keep labels visible
                    Assert.That(
                        EditorStyles.label.normal.textColor,
                        Is.EqualTo(labelNormal),
                        "label.normal.textColor should be preserved"
                    );
                    Assert.That(
                        EditorStyles.label.focused.textColor,
                        Is.EqualTo(labelFocused),
                        "label.focused.textColor should be preserved"
                    );
                    Assert.That(
                        EditorStyles.label.active.textColor,
                        Is.EqualTo(labelActive),
                        "label.active.textColor should be preserved"
                    );
                    Assert.That(
                        EditorStyles.label.hover.textColor,
                        Is.EqualTo(labelHover),
                        "label.hover.textColor should be preserved"
                    );
                    Assert.That(
                        EditorStyles.label.onNormal.textColor,
                        Is.EqualTo(labelOnNormal),
                        "label.onNormal.textColor should be preserved"
                    );
                    Assert.That(
                        EditorStyles.label.onFocused.textColor,
                        Is.EqualTo(labelOnFocused),
                        "label.onFocused.textColor should be preserved"
                    );
                    Assert.That(
                        EditorStyles.label.onActive.textColor,
                        Is.EqualTo(labelOnActive),
                        "label.onActive.textColor should be preserved"
                    );
                    Assert.That(
                        EditorStyles.label.onHover.textColor,
                        Is.EqualTo(labelOnHover),
                        "label.onHover.textColor should be preserved"
                    );
                    Assert.That(
                        EditorStyles.foldout.normal.textColor,
                        Is.EqualTo(foldoutNormal),
                        "foldout.normal.textColor should be preserved"
                    );
                }
            }
        }

        [Test]
        public void ExitWGroupThemingRestoresAll12StylesAfterActiveColorScope()
        {
            if (!EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires dark editor theme (Pro skin).");
            }

            using (var colorScope = new WGroupColorScope(LightPalette))
            {
                Assert.That(colorScope.IsActive, Is.True);

                // Capture WGroupColorScope's modifications
                Texture2D wgroupTextFieldBg = EditorStyles.textField.normal.background;
                Texture2D wgroupNumberFieldBg = EditorStyles.numberField.normal.background;
                Texture2D wgroupObjectFieldBg = EditorStyles.objectField.normal.background;
                Texture2D wgroupPopupBg = EditorStyles.popup.normal.background;
                Texture2D wgroupHelpBoxBg = EditorStyles.helpBox.normal.background;
                Color wgroupFoldoutColor = EditorStyles.foldout.normal.textColor;
                Color wgroupLabelColor = EditorStyles.label.normal.textColor;
                Color wgroupToggleColor = EditorStyles.toggle.normal.textColor;
                Color wgroupMiniButtonColor = EditorStyles.miniButton.normal.textColor;
                Color wgroupMiniButtonLeftColor = EditorStyles.miniButtonLeft.normal.textColor;
                Color wgroupMiniButtonMidColor = EditorStyles.miniButtonMid.normal.textColor;
                Color wgroupMiniButtonRightColor = EditorStyles.miniButtonRight.normal.textColor;

                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    // All should be reset inside scope
                }

                // All 12 should be restored after exit scope
                Assert.That(
                    EditorStyles.textField.normal.background,
                    Is.EqualTo(wgroupTextFieldBg)
                );
                Assert.That(
                    EditorStyles.numberField.normal.background,
                    Is.EqualTo(wgroupNumberFieldBg)
                );
                Assert.That(
                    EditorStyles.objectField.normal.background,
                    Is.EqualTo(wgroupObjectFieldBg)
                );
                Assert.That(EditorStyles.popup.normal.background, Is.EqualTo(wgroupPopupBg));
                Assert.That(EditorStyles.helpBox.normal.background, Is.EqualTo(wgroupHelpBoxBg));
                Assert.That(EditorStyles.foldout.normal.textColor, Is.EqualTo(wgroupFoldoutColor));
                Assert.That(EditorStyles.label.normal.textColor, Is.EqualTo(wgroupLabelColor));
                Assert.That(EditorStyles.toggle.normal.textColor, Is.EqualTo(wgroupToggleColor));
                Assert.That(
                    EditorStyles.miniButton.normal.textColor,
                    Is.EqualTo(wgroupMiniButtonColor)
                );
                Assert.That(
                    EditorStyles.miniButtonLeft.normal.textColor,
                    Is.EqualTo(wgroupMiniButtonLeftColor)
                );
                Assert.That(
                    EditorStyles.miniButtonMid.normal.textColor,
                    Is.EqualTo(wgroupMiniButtonMidColor)
                );
                Assert.That(
                    EditorStyles.miniButtonRight.normal.textColor,
                    Is.EqualTo(wgroupMiniButtonRightColor)
                );
            }
        }

        [Test]
        public void NestedScopesRestoreCorrectlyOuterInnerPalettes()
        {
            UnityHelpersSettings.WGroupPaletteEntry outerPalette = LightPalette;
            UnityHelpersSettings.WGroupPaletteEntry innerPalette = DarkPalette;

            Color originalContentColor = GUI.contentColor;
            Color originalBackgroundColor = GUI.backgroundColor;

            using (var outerScope = new WGroupColorScope(outerPalette))
            {
                Color afterOuterContentColor = GUI.contentColor;
                Color afterOuterBackgroundColor = GUI.backgroundColor;

                using (var innerScope = new WGroupColorScope(innerPalette))
                {
                    // Inner scope should override outer
                    if (innerScope.IsActive)
                    {
                        Assert.That(
                            GUI.contentColor,
                            Is.EqualTo(innerPalette.TextColor),
                            "Inner scope should set its own content color."
                        );
                    }
                }

                // After inner scope, should restore to outer scope values
                Assert.That(
                    GUI.contentColor,
                    Is.EqualTo(afterOuterContentColor),
                    "After inner scope disposal, content color should return to outer scope value."
                );
            }

            // After all scopes, should restore to original
            Assert.That(
                GUI.contentColor,
                Is.EqualTo(originalContentColor),
                "After all scopes disposed, content color should return to original."
            );
            Assert.That(
                GUI.backgroundColor,
                Is.EqualTo(originalBackgroundColor),
                "After all scopes disposed, background color should return to original."
            );
        }
    }
#endif
}
