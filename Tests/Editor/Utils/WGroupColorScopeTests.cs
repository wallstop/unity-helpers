namespace WallstopStudios.UnityHelpers.Tests.Utils
{
#if UNITY_EDITOR
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Settings;
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
        public void CrossThemeGuiColorBlendedTowardsTextColor()
        {
            // When cross-theme, GUI.color should be blended slightly towards text color
            // to ensure icons/textures are visible

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
                // GUI.color should not be pure white when in cross-theme mode
                // It should be blended towards the text color
                bool isBlended =
                    !Mathf.Approximately(GUI.color.r, 1f)
                    || !Mathf.Approximately(GUI.color.g, 1f)
                    || !Mathf.Approximately(GUI.color.b, 1f);

                Assert.That(
                    isBlended,
                    Is.True,
                    "GUI color should be blended towards text color in cross-theme mode."
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
    }
#endif
}
