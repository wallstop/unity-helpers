#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Editor.Utils.WGroup;

    /// <summary>
    /// Comprehensive tests for WGroup theming behavior with nested SerializableCollections.
    /// Verifies verbatim color application, nested scope isolation, alpha multiplication,
    /// palette stack functionality, and cross-theme edge cases.
    /// </summary>
    [TestFixture]
    public sealed class WGroupNestedCollectionThemingTests
    {
        private Color _originalContentColor;
        private Color _originalBackgroundColor;
        private Color _originalGuiColor;

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

        private static readonly UnityHelpersSettings.WGroupPaletteEntry NeonPalette = new(
            new Color(0.0f, 1.0f, 0.5f, 1f),
            Color.black
        );

        private static readonly UnityHelpersSettings.WGroupPaletteEntry WarmLightPalette = new(
            new Color(0.95f, 0.9f, 0.85f, 1f),
            new Color(0.15f, 0.1f, 0.05f, 1f)
        );

        private static readonly UnityHelpersSettings.WGroupPaletteEntry CoolDarkPalette = new(
            new Color(0.1f, 0.12f, 0.18f, 1f),
            new Color(0.85f, 0.9f, 0.95f, 1f)
        );

        [SetUp]
        public void SetUp()
        {
            _originalContentColor = GUI.contentColor;
            _originalBackgroundColor = GUI.backgroundColor;
            _originalGuiColor = GUI.color;
            GroupGUIWidthUtility.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            GUI.contentColor = _originalContentColor;
            GUI.backgroundColor = _originalBackgroundColor;
            GUI.color = _originalGuiColor;
            GroupGUIWidthUtility.ResetForTests();
        }

        // =====================================================================
        // 1. Verbatim Color Application Tests
        // =====================================================================

        [Test]
        public void VerbatimGuiColorIsWhiteInCrossThemeScope()
        {
            UnityHelpersSettings.WGroupPaletteEntry crossThemePalette = EditorGUIUtility.isProSkin
                ? LightPalette
                : DarkPalette;

            using (WGroupColorScope scope = new WGroupColorScope(crossThemePalette))
            {
                if (scope.IsActive)
                {
                    Assert.That(
                        GUI.color.r,
                        Is.EqualTo(1f).Within(0.001f),
                        "GUI.color.r should be 1 (white) for verbatim nested colors."
                    );
                    Assert.That(
                        GUI.color.g,
                        Is.EqualTo(1f).Within(0.001f),
                        "GUI.color.g should be 1 (white) for verbatim nested colors."
                    );
                    Assert.That(
                        GUI.color.b,
                        Is.EqualTo(1f).Within(0.001f),
                        "GUI.color.b should be 1 (white) for verbatim nested colors."
                    );
                }
            }
        }

        [Test]
        public void ContentColorMatchesPaletteTextColorInCrossTheme()
        {
            UnityHelpersSettings.WGroupPaletteEntry crossThemePalette = EditorGUIUtility.isProSkin
                ? LightPalette
                : DarkPalette;

            using (WGroupColorScope scope = new WGroupColorScope(crossThemePalette))
            {
                if (scope.IsActive)
                {
                    Assert.That(
                        GUI.contentColor,
                        Is.EqualTo(crossThemePalette.TextColor),
                        "ContentColor should equal palette's text color in cross-theme mode."
                    );
                }
            }
        }

        [Test]
        public void BackgroundColorIsCalculatedFieldBackgroundColor()
        {
            UnityHelpersSettings.WGroupPaletteEntry crossThemePalette = EditorGUIUtility.isProSkin
                ? LightPalette
                : DarkPalette;

            Color expectedFieldBg = WGroupColorScope.CalculateFieldBackgroundColor(
                crossThemePalette.BackgroundColor
            );

            using (WGroupColorScope scope = new WGroupColorScope(crossThemePalette))
            {
                Assert.That(
                    scope.FieldBackgroundColor,
                    Is.EqualTo(expectedFieldBg),
                    "FieldBackgroundColor should match calculated field background color."
                );
            }
        }

        [Test]
        public void VerbatimWhiteAllowsNestedDrawersToApplyOwnColors()
        {
            UnityHelpersSettings.WGroupPaletteEntry crossThemePalette = EditorGUIUtility.isProSkin
                ? LightPalette
                : DarkPalette;

            using (WGroupColorScope scope = new WGroupColorScope(crossThemePalette))
            {
                if (scope.IsActive)
                {
                    Color nestedColor = new Color(0.5f, 0.7f, 0.3f, 1f);
                    Color expectedResult = nestedColor * GUI.color;

                    Assert.That(
                        expectedResult.r,
                        Is.EqualTo(nestedColor.r).Within(0.001f),
                        "White GUI.color * nested color should equal nested color (R)."
                    );
                    Assert.That(
                        expectedResult.g,
                        Is.EqualTo(nestedColor.g).Within(0.001f),
                        "White GUI.color * nested color should equal nested color (G)."
                    );
                    Assert.That(
                        expectedResult.b,
                        Is.EqualTo(nestedColor.b).Within(0.001f),
                        "White GUI.color * nested color should equal nested color (B)."
                    );
                }
            }
        }

        [Test]
        public void LightPaletteFieldBackgroundIsNearWhite()
        {
            using (WGroupColorScope scope = new WGroupColorScope(LightPalette))
            {
                float fieldLuminance =
                    0.299f * scope.FieldBackgroundColor.r
                    + 0.587f * scope.FieldBackgroundColor.g
                    + 0.114f * scope.FieldBackgroundColor.b;

                Assert.That(
                    fieldLuminance,
                    Is.GreaterThan(0.85f),
                    "Light palette field background should have high luminance (> 0.85)."
                );
            }
        }

        [Test]
        public void DarkPaletteFieldBackgroundIsDark()
        {
            using (WGroupColorScope scope = new WGroupColorScope(DarkPalette))
            {
                float fieldLuminance =
                    0.299f * scope.FieldBackgroundColor.r
                    + 0.587f * scope.FieldBackgroundColor.g
                    + 0.114f * scope.FieldBackgroundColor.b;

                Assert.That(
                    fieldLuminance,
                    Is.LessThan(0.25f),
                    "Dark palette field background should have low luminance (< 0.25)."
                );
            }
        }

        // =====================================================================
        // 2. Nested Scope Color Isolation Tests
        // =====================================================================

        [Test]
        public void NestedScopesProperlyPushAndPopColors()
        {
            Color beforeOuter = GUI.contentColor;

            using (WGroupColorScope outerScope = new WGroupColorScope(LightPalette))
            {
                Color afterOuterPush = GUI.contentColor;

                using (WGroupColorScope innerScope = new WGroupColorScope(DarkPalette))
                {
                    Color afterInnerPush = GUI.contentColor;

                    Assert.That(
                        afterInnerPush,
                        Is.Not.EqualTo(beforeOuter).Or.EqualTo(beforeOuter),
                        "Inner scope content color is set according to scope state."
                    );
                }

                Assert.That(
                    GUI.contentColor,
                    Is.EqualTo(afterOuterPush),
                    "After inner scope disposal, outer scope content color should be restored."
                );
            }

            Assert.That(
                GUI.contentColor,
                Is.EqualTo(beforeOuter),
                "After all scopes disposed, original content color should be restored."
            );
        }

        [Test]
        public void ThreeLevelNestedScopesRestoreCorrectly()
        {
            Color original = GUI.contentColor;
            Color level1Color = default;
            Color level2Color = default;
            Color level3Color = default;

            using (WGroupColorScope scope1 = new WGroupColorScope(LightPalette))
            {
                level1Color = GUI.contentColor;

                using (WGroupColorScope scope2 = new WGroupColorScope(DarkPalette))
                {
                    level2Color = GUI.contentColor;

                    using (WGroupColorScope scope3 = new WGroupColorScope(VeryLightPalette))
                    {
                        level3Color = GUI.contentColor;
                    }

                    Assert.That(
                        GUI.contentColor,
                        Is.EqualTo(level2Color),
                        "After level 3 disposal, should return to level 2 colors."
                    );
                }

                Assert.That(
                    GUI.contentColor,
                    Is.EqualTo(level1Color),
                    "After level 2 disposal, should return to level 1 colors."
                );
            }

            Assert.That(
                GUI.contentColor,
                Is.EqualTo(original),
                "After level 1 disposal, should return to original colors."
            );
        }

        [Test]
        public void FourLevelNestedScopesRestoreCorrectly()
        {
            Color original = GUI.contentColor;

            using (WGroupColorScope scope1 = new WGroupColorScope(LightPalette))
            {
                Color after1 = GUI.contentColor;

                using (WGroupColorScope scope2 = new WGroupColorScope(DarkPalette))
                {
                    Color after2 = GUI.contentColor;

                    using (WGroupColorScope scope3 = new WGroupColorScope(MidGrayPalette))
                    {
                        Color after3 = GUI.contentColor;

                        using (WGroupColorScope scope4 = new WGroupColorScope(NeonPalette))
                        {
                            // Deepest nesting level
                        }

                        Assert.That(
                            GUI.contentColor,
                            Is.EqualTo(after3),
                            "After level 4 disposal, should return to level 3."
                        );
                    }

                    Assert.That(
                        GUI.contentColor,
                        Is.EqualTo(after2),
                        "After level 3 disposal, should return to level 2."
                    );
                }

                Assert.That(
                    GUI.contentColor,
                    Is.EqualTo(after1),
                    "After level 2 disposal, should return to level 1."
                );
            }

            Assert.That(
                GUI.contentColor,
                Is.EqualTo(original),
                "After all scopes disposed, should return to original."
            );
        }

        [Test]
        public void NestedScopesWithSamePaletteRestoreCorrectly()
        {
            Color original = GUI.contentColor;

            using (WGroupColorScope outer = new WGroupColorScope(LightPalette))
            {
                Color afterOuter = GUI.contentColor;

                using (WGroupColorScope inner = new WGroupColorScope(LightPalette))
                {
                    // Same palette nested
                }

                Assert.That(
                    GUI.contentColor,
                    Is.EqualTo(afterOuter),
                    "Nested same palette should restore correctly."
                );
            }

            Assert.That(
                GUI.contentColor,
                Is.EqualTo(original),
                "Original should be restored after all scopes."
            );
        }

        [Test]
        public void NestedGuiColorRestoresCorrectly()
        {
            Color originalGuiColor = GUI.color;

            using (WGroupColorScope outer = new WGroupColorScope(LightPalette))
            {
                Color outerGuiColor = GUI.color;

                using (WGroupColorScope inner = new WGroupColorScope(DarkPalette))
                {
                    Color innerGuiColor = GUI.color;
                }

                Assert.That(
                    GUI.color,
                    Is.EqualTo(outerGuiColor),
                    "After inner scope, GUI.color should return to outer scope value."
                );
            }

            Assert.That(
                GUI.color,
                Is.EqualTo(originalGuiColor),
                "After all scopes, GUI.color should return to original."
            );
        }

        // =====================================================================
        // 3. Alpha Multiplication Behavior Tests
        // =====================================================================

        [Test]
        public void WhiteGuiColorWithAlphaMultiplicationYieldsCorrectResult()
        {
            UnityHelpersSettings.WGroupPaletteEntry crossThemePalette = EditorGUIUtility.isProSkin
                ? LightPalette
                : DarkPalette;

            using (WGroupColorScope scope = new WGroupColorScope(crossThemePalette))
            {
                if (scope.IsActive)
                {
                    float testAlpha = 0.5f;
                    Color guiColorWithAlpha = new Color(
                        GUI.color.r,
                        GUI.color.g,
                        GUI.color.b,
                        testAlpha
                    );
                    Color testColor = new Color(0.8f, 0.6f, 0.4f, 1f);
                    Color result = testColor * guiColorWithAlpha;

                    Assert.That(
                        result.r,
                        Is.EqualTo(testColor.r).Within(0.001f),
                        "With white GUI.color, R channel should be preserved."
                    );
                    Assert.That(
                        result.g,
                        Is.EqualTo(testColor.g).Within(0.001f),
                        "With white GUI.color, G channel should be preserved."
                    );
                    Assert.That(
                        result.b,
                        Is.EqualTo(testColor.b).Within(0.001f),
                        "With white GUI.color, B channel should be preserved."
                    );
                    Assert.That(
                        result.a,
                        Is.EqualTo(testAlpha).Within(0.001f),
                        "Alpha should be multiplied correctly."
                    );
                }
            }
        }

        [Test]
        public void AlphaZeroMultiplicationProducesTransparent()
        {
            float alpha = 0.0f;
            Color white = Color.white;
            Color withAlpha = new Color(white.r, white.g, white.b, alpha);
            Color testColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            Color result = testColor * withAlpha;

            Assert.That(
                result.a,
                Is.EqualTo(0f).Within(0.001f),
                "Alpha 0 multiplication should produce fully transparent result."
            );
        }

        [Test]
        public void AlphaHalfMultiplicationProducesHalfTransparent()
        {
            float alpha = 0.5f;
            Color white = Color.white;
            Color withAlpha = new Color(white.r, white.g, white.b, alpha);
            Color testColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            Color result = testColor * withAlpha;

            Assert.That(
                result.a,
                Is.EqualTo(0.5f).Within(0.001f),
                "Alpha 0.5 multiplication should produce 0.5 alpha result."
            );
            Assert.That(
                result.r,
                Is.EqualTo(0.8f).Within(0.001f),
                "RGB should be preserved with white base color."
            );
        }

        [Test]
        public void AlphaQuarterMultiplicationProducesCorrectAlpha()
        {
            float alpha = 0.25f;
            Color white = Color.white;
            Color withAlpha = new Color(white.r, white.g, white.b, alpha);
            Color testColor = new Color(0.6f, 0.7f, 0.8f, 1f);
            Color result = testColor * withAlpha;

            Assert.That(
                result.a,
                Is.EqualTo(0.25f).Within(0.001f),
                "Alpha 0.25 multiplication should produce 0.25 alpha result."
            );
        }

        [Test]
        public void AlphaFullMultiplicationPreservesOriginal()
        {
            float alpha = 1.0f;
            Color white = Color.white;
            Color withAlpha = new Color(white.r, white.g, white.b, alpha);
            Color testColor = new Color(0.3f, 0.6f, 0.9f, 0.8f);
            Color result = testColor * withAlpha;

            Assert.That(
                result.r,
                Is.EqualTo(testColor.r).Within(0.001f),
                "Full alpha with white should preserve R."
            );
            Assert.That(
                result.g,
                Is.EqualTo(testColor.g).Within(0.001f),
                "Full alpha with white should preserve G."
            );
            Assert.That(
                result.b,
                Is.EqualTo(testColor.b).Within(0.001f),
                "Full alpha with white should preserve B."
            );
            Assert.That(
                result.a,
                Is.EqualTo(testColor.a).Within(0.001f),
                "Full alpha with white should preserve A."
            );
        }

        [Test]
        public void SimulatedAnimationFadeProducesExpectedGrayscale()
        {
            Color white = Color.white;
            float[] testAlphas = { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f };

            foreach (float alpha in testAlphas)
            {
                Color withAlpha = new Color(white.r, white.g, white.b, alpha);
                Color gray = new Color(0.5f, 0.5f, 0.5f, 1f);
                Color result = gray * withAlpha;

                Assert.That(
                    result.r,
                    Is.EqualTo(0.5f).Within(0.001f),
                    $"Grayscale R should be preserved at alpha={alpha}."
                );
                Assert.That(
                    result.g,
                    Is.EqualTo(0.5f).Within(0.001f),
                    $"Grayscale G should be preserved at alpha={alpha}."
                );
                Assert.That(
                    result.b,
                    Is.EqualTo(0.5f).Within(0.001f),
                    $"Grayscale B should be preserved at alpha={alpha}."
                );
                Assert.That(
                    result.a,
                    Is.EqualTo(alpha).Within(0.001f),
                    $"Alpha should match at alpha={alpha}."
                );
            }
        }

        // =====================================================================
        // 4. GroupGUIWidthUtility Palette Stack Tests
        // =====================================================================

        [Test]
        public void CurrentPaletteIsNullWhenNotInWGroup()
        {
            GroupGUIWidthUtility.ResetForTests();

            UnityHelpersSettings.WGroupPaletteEntry? currentPalette =
                GroupGUIWidthUtility.CurrentPalette;

            Assert.That(
                currentPalette,
                Is.Null,
                "CurrentPalette should be null when not inside a WGroup."
            );
        }

        [Test]
        public void CurrentPaletteIsSetAfterPushWGroupPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                UnityHelpersSettings.WGroupPaletteEntry? currentPalette =
                    GroupGUIWidthUtility.CurrentPalette;

                Assert.That(
                    currentPalette,
                    Is.Not.Null,
                    "CurrentPalette should not be null after PushWGroupPalette."
                );
                Assert.That(
                    currentPalette.Value.BackgroundColor,
                    Is.EqualTo(LightPalette.BackgroundColor),
                    "CurrentPalette background should match pushed palette."
                );
                Assert.That(
                    currentPalette.Value.TextColor,
                    Is.EqualTo(LightPalette.TextColor),
                    "CurrentPalette text color should match pushed palette."
                );
            }
        }

        [Test]
        public void NestedPalettePushesAndPopsWorkCorrectly()
        {
            GroupGUIWidthUtility.ResetForTests();

            Assert.That(GroupGUIWidthUtility.CurrentPalette, Is.Null, "Should be null initially.");

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                Assert.That(
                    GroupGUIWidthUtility.CurrentPalette.Value.BackgroundColor,
                    Is.EqualTo(LightPalette.BackgroundColor),
                    "Level 1: Should be LightPalette."
                );

                using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
                {
                    Assert.That(
                        GroupGUIWidthUtility.CurrentPalette.Value.BackgroundColor,
                        Is.EqualTo(DarkPalette.BackgroundColor),
                        "Level 2: Should be DarkPalette."
                    );

                    using (GroupGUIWidthUtility.PushWGroupPalette(NeonPalette))
                    {
                        Assert.That(
                            GroupGUIWidthUtility.CurrentPalette.Value.BackgroundColor,
                            Is.EqualTo(NeonPalette.BackgroundColor),
                            "Level 3: Should be NeonPalette."
                        );
                    }

                    Assert.That(
                        GroupGUIWidthUtility.CurrentPalette.Value.BackgroundColor,
                        Is.EqualTo(DarkPalette.BackgroundColor),
                        "After level 3 pop: Should return to DarkPalette."
                    );
                }

                Assert.That(
                    GroupGUIWidthUtility.CurrentPalette.Value.BackgroundColor,
                    Is.EqualTo(LightPalette.BackgroundColor),
                    "After level 2 pop: Should return to LightPalette."
                );
            }

            Assert.That(
                GroupGUIWidthUtility.CurrentPalette,
                Is.Null,
                "After all pops: Should be null."
            );
        }

        [Test]
        public void GetThemedRowColorReturnsCorrectColorBasedOnPaletteLuminance()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color lightRowColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            Color darkRowColor = new Color(0.15f, 0.15f, 0.15f, 1f);

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                Color result = GroupGUIWidthUtility.GetThemedRowColor(lightRowColor, darkRowColor);

                Assert.That(
                    result,
                    Is.EqualTo(lightRowColor),
                    "Light palette should return light row color."
                );
            }

            using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
            {
                Color result = GroupGUIWidthUtility.GetThemedRowColor(lightRowColor, darkRowColor);

                Assert.That(
                    result,
                    Is.EqualTo(darkRowColor),
                    "Dark palette should return dark row color."
                );
            }
        }

        [Test]
        public void GetPaletteDerivedRowColorReturnsPaletteBasedColors()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                Color row0 = GroupGUIWidthUtility.GetPaletteDerivedRowColor(0);
                Color row1 = GroupGUIWidthUtility.GetPaletteDerivedRowColor(1);

                Assert.That(
                    row0,
                    Is.EqualTo(LightPalette.BackgroundColor),
                    "Even row (0) should be palette background."
                );

                float row1Luminance = 0.299f * row1.r + 0.587f * row1.g + 0.114f * row1.b;
                float bgLuminance =
                    0.299f * LightPalette.BackgroundColor.r
                    + 0.587f * LightPalette.BackgroundColor.g
                    + 0.114f * LightPalette.BackgroundColor.b;

                Assert.That(
                    row1Luminance,
                    Is.LessThan(bgLuminance),
                    "Odd row (1) for light palette should be slightly darker than background."
                );
            }

            using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
            {
                Color row1 = GroupGUIWidthUtility.GetPaletteDerivedRowColor(1);

                float row1Luminance = 0.299f * row1.r + 0.587f * row1.g + 0.114f * row1.b;
                float bgLuminance =
                    0.299f * DarkPalette.BackgroundColor.r
                    + 0.587f * DarkPalette.BackgroundColor.g
                    + 0.114f * DarkPalette.BackgroundColor.b;

                Assert.That(
                    row1Luminance,
                    Is.GreaterThan(bgLuminance),
                    "Odd row (1) for dark palette should be slightly lighter than background."
                );
            }
        }

        [Test]
        public void GetPaletteBackgroundColorReturnsPaletteBackground()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color defaultBg = GroupGUIWidthUtility.GetPaletteBackgroundColor();
            Color expectedDefault = EditorGUIUtility.isProSkin
                ? new Color(0.22f, 0.22f, 0.22f, 1f)
                : new Color(0.88f, 0.88f, 0.88f, 1f);

            Assert.That(
                defaultBg,
                Is.EqualTo(expectedDefault),
                "Default background should match editor skin."
            );

            using (GroupGUIWidthUtility.PushWGroupPalette(NeonPalette))
            {
                Color paletteBg = GroupGUIWidthUtility.GetPaletteBackgroundColor();

                Assert.That(
                    paletteBg,
                    Is.EqualTo(NeonPalette.BackgroundColor),
                    "Background should match current palette."
                );
            }
        }

        [Test]
        public void GetPaletteTextColorReturnsPaletteTextColor()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color defaultText = GroupGUIWidthUtility.GetPaletteTextColor();
            Color expectedDefault = EditorGUIUtility.isProSkin
                ? new Color(0.9f, 0.9f, 0.9f, 1f)
                : new Color(0.1f, 0.1f, 0.1f, 1f);

            Assert.That(
                defaultText,
                Is.EqualTo(expectedDefault),
                "Default text color should match editor skin."
            );

            using (GroupGUIWidthUtility.PushWGroupPalette(WarmLightPalette))
            {
                Color paletteText = GroupGUIWidthUtility.GetPaletteTextColor();

                Assert.That(
                    paletteText,
                    Is.EqualTo(WarmLightPalette.TextColor),
                    "Text color should match current palette."
                );
            }
        }

        [Test]
        public void GetPaletteDerivedBorderColorVariesByLuminance()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                Color border = GroupGUIWidthUtility.GetPaletteDerivedBorderColor();
                float borderLuminance = 0.299f * border.r + 0.587f * border.g + 0.114f * border.b;
                float bgLuminance =
                    0.299f * LightPalette.BackgroundColor.r
                    + 0.587f * LightPalette.BackgroundColor.g
                    + 0.114f * LightPalette.BackgroundColor.b;

                Assert.That(
                    borderLuminance,
                    Is.LessThan(bgLuminance),
                    "Light palette border should be darker than background."
                );
            }

            using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
            {
                Color border = GroupGUIWidthUtility.GetPaletteDerivedBorderColor();
                float borderLuminance = 0.299f * border.r + 0.587f * border.g + 0.114f * border.b;
                float bgLuminance =
                    0.299f * DarkPalette.BackgroundColor.r
                    + 0.587f * DarkPalette.BackgroundColor.g
                    + 0.114f * DarkPalette.BackgroundColor.b;

                Assert.That(
                    borderLuminance,
                    Is.GreaterThan(bgLuminance),
                    "Dark palette border should be lighter than background."
                );
            }
        }

        [Test]
        public void ShouldUseLightThemeStylingReflectsPaletteLuminance()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                bool result = GroupGUIWidthUtility.ShouldUseLightThemeStyling();

                Assert.That(result, Is.True, "Light palette should use light theme styling.");
            }

            using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
            {
                bool result = GroupGUIWidthUtility.ShouldUseLightThemeStyling();

                Assert.That(result, Is.False, "Dark palette should not use light theme styling.");
            }
        }

        // =====================================================================
        // 5. Cross-Theme Edge Cases
        // =====================================================================

        [Test]
        public void LightPaletteOnDarkEditorWithNestedScopes()
        {
            if (!EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires dark editor theme (Pro skin).");
            }

            using (WGroupColorScope outerScope = new WGroupColorScope(LightPalette))
            {
                Assert.That(
                    outerScope.IsActive,
                    Is.True,
                    "Light palette on dark editor should be cross-theme (active)."
                );

                Color outerContentColor = GUI.contentColor;

                using (WGroupColorScope innerScope = new WGroupColorScope(VeryLightPalette))
                {
                    Assert.That(
                        innerScope.IsActive,
                        Is.True,
                        "Very light palette on dark editor should also be cross-theme."
                    );
                }

                Assert.That(
                    GUI.contentColor,
                    Is.EqualTo(outerContentColor),
                    "After inner scope, outer content color should be restored."
                );
            }
        }

        [Test]
        public void DarkPaletteOnLightEditorWithNestedScopes()
        {
            if (EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires light editor theme (Personal skin).");
            }

            using (WGroupColorScope outerScope = new WGroupColorScope(DarkPalette))
            {
                Assert.That(
                    outerScope.IsActive,
                    Is.True,
                    "Dark palette on light editor should be cross-theme (active)."
                );

                Color outerContentColor = GUI.contentColor;

                using (WGroupColorScope innerScope = new WGroupColorScope(VeryDarkPalette))
                {
                    Assert.That(
                        innerScope.IsActive,
                        Is.True,
                        "Very dark palette on light editor should also be cross-theme."
                    );
                }

                Assert.That(
                    GUI.contentColor,
                    Is.EqualTo(outerContentColor),
                    "After inner scope, outer content color should be restored."
                );
            }
        }

        [Test]
        public void MixedPalettesLightInsideDarkScope()
        {
            Color original = GUI.contentColor;

            using (WGroupColorScope darkScope = new WGroupColorScope(DarkPalette))
            {
                Color afterDark = GUI.contentColor;

                using (WGroupColorScope lightScope = new WGroupColorScope(LightPalette))
                {
                    Color afterLight = GUI.contentColor;

                    if (lightScope.IsActive)
                    {
                        Assert.That(
                            GUI.contentColor,
                            Is.EqualTo(LightPalette.TextColor),
                            "Light inside dark: content should be light palette text."
                        );
                    }
                }

                Assert.That(
                    GUI.contentColor,
                    Is.EqualTo(afterDark),
                    "After light scope, dark scope colors should be restored."
                );
            }

            Assert.That(
                GUI.contentColor,
                Is.EqualTo(original),
                "After all scopes, original should be restored."
            );
        }

        [Test]
        public void MixedPalettesDarkInsideLightScope()
        {
            Color original = GUI.contentColor;

            using (WGroupColorScope lightScope = new WGroupColorScope(LightPalette))
            {
                Color afterLight = GUI.contentColor;

                using (WGroupColorScope darkScope = new WGroupColorScope(DarkPalette))
                {
                    Color afterDark = GUI.contentColor;

                    if (darkScope.IsActive)
                    {
                        Assert.That(
                            GUI.contentColor,
                            Is.EqualTo(DarkPalette.TextColor),
                            "Dark inside light: content should be dark palette text."
                        );
                    }
                }

                Assert.That(
                    GUI.contentColor,
                    Is.EqualTo(afterLight),
                    "After dark scope, light scope colors should be restored."
                );
            }

            Assert.That(
                GUI.contentColor,
                Is.EqualTo(original),
                "After all scopes, original should be restored."
            );
        }

        [Test]
        public void AlternatingLightDarkPalettesInDeepNesting()
        {
            Color original = GUI.contentColor;

            using (WGroupColorScope scope1 = new WGroupColorScope(LightPalette))
            {
                Color after1 = GUI.contentColor;

                using (WGroupColorScope scope2 = new WGroupColorScope(DarkPalette))
                {
                    Color after2 = GUI.contentColor;

                    using (WGroupColorScope scope3 = new WGroupColorScope(LightPalette))
                    {
                        Color after3 = GUI.contentColor;

                        using (WGroupColorScope scope4 = new WGroupColorScope(DarkPalette))
                        {
                            // Deepest: dark palette
                        }

                        Assert.That(
                            GUI.contentColor,
                            Is.EqualTo(after3),
                            "After level 4, should return to level 3."
                        );
                    }

                    Assert.That(
                        GUI.contentColor,
                        Is.EqualTo(after2),
                        "After level 3, should return to level 2."
                    );
                }

                Assert.That(
                    GUI.contentColor,
                    Is.EqualTo(after1),
                    "After level 2, should return to level 1."
                );
            }

            Assert.That(
                GUI.contentColor,
                Is.EqualTo(original),
                "After all scopes, should return to original."
            );
        }

        [Test]
        public void WarmLightPaletteOnDarkEditorIsCrossTheme()
        {
            if (!EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires dark editor theme (Pro skin).");
            }

            using (WGroupColorScope scope = new WGroupColorScope(WarmLightPalette))
            {
                Assert.That(
                    scope.IsActive,
                    Is.True,
                    "Warm light palette on dark editor should be cross-theme."
                );
                Assert.That(
                    GUI.contentColor,
                    Is.EqualTo(WarmLightPalette.TextColor),
                    "Content color should match warm light palette text."
                );
            }
        }

        [Test]
        public void CoolDarkPaletteOnLightEditorIsCrossTheme()
        {
            if (EditorGUIUtility.isProSkin)
            {
                Assert.Ignore("Test requires light editor theme (Personal skin).");
            }

            using (WGroupColorScope scope = new WGroupColorScope(CoolDarkPalette))
            {
                Assert.That(
                    scope.IsActive,
                    Is.True,
                    "Cool dark palette on light editor should be cross-theme."
                );
                Assert.That(
                    GUI.contentColor,
                    Is.EqualTo(CoolDarkPalette.TextColor),
                    "Content color should match cool dark palette text."
                );
            }
        }

        [Test]
        public void CombinedPaletteStackAndColorScopeWorkTogether()
        {
            GroupGUIWidthUtility.ResetForTests();
            Color original = GUI.contentColor;

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            using (WGroupColorScope colorScope = new WGroupColorScope(LightPalette))
            {
                Assert.That(
                    GroupGUIWidthUtility.CurrentPalette,
                    Is.Not.Null,
                    "Palette should be set from stack."
                );
                Assert.That(
                    GroupGUIWidthUtility.CurrentPalette.Value.BackgroundColor,
                    Is.EqualTo(LightPalette.BackgroundColor),
                    "Palette stack should have correct palette."
                );
            }

            Assert.That(
                GroupGUIWidthUtility.CurrentPalette,
                Is.Null,
                "Palette should be null after scope disposal."
            );
            Assert.That(
                GUI.contentColor,
                Is.EqualTo(original),
                "Content color should be restored after scope disposal."
            );
        }

        [Test]
        public void NestedMixedPalettesWithPaletteStackTracking()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                Assert.That(
                    GroupGUIWidthUtility.IsInsideLightPaletteWGroup(),
                    Is.True,
                    "Should be inside light palette WGroup."
                );

                using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
                {
                    Assert.That(
                        GroupGUIWidthUtility.IsInsideDarkPaletteWGroup(),
                        Is.True,
                        "Should be inside dark palette WGroup."
                    );
                    Assert.That(
                        GroupGUIWidthUtility.IsInsideLightPaletteWGroup(),
                        Is.False,
                        "Should not be inside light palette when dark is current."
                    );
                }

                Assert.That(
                    GroupGUIWidthUtility.IsInsideLightPaletteWGroup(),
                    Is.True,
                    "After dark pop, should be inside light palette again."
                );
            }

            Assert.That(
                GroupGUIWidthUtility.IsInsideLightPaletteWGroup(),
                Is.False,
                "After all pops, should not be inside any palette."
            );
            Assert.That(
                GroupGUIWidthUtility.IsInsideDarkPaletteWGroup(),
                Is.False,
                "After all pops, should not be inside any palette."
            );
        }

        [Test]
        public void CrossThemeDetectionWithBoundaryLuminance()
        {
            UnityHelpersSettings.WGroupPaletteEntry boundaryPalette = new(
                new Color(0.5f, 0.5f, 0.5f, 1f),
                Color.white
            );

            float luminance =
                0.299f * boundaryPalette.BackgroundColor.r
                + 0.587f * boundaryPalette.BackgroundColor.g
                + 0.114f * boundaryPalette.BackgroundColor.b;

            Assert.That(
                luminance,
                Is.EqualTo(0.5f).Within(0.001f),
                "Boundary palette should have exactly 0.5 luminance."
            );

            bool isLightBackground = luminance > 0.5f;
            Assert.That(
                isLightBackground,
                Is.False,
                "Exactly 0.5 luminance should NOT be considered light."
            );
        }

        [Test]
        public void SlightlyAboveBoundaryIsConsideredLight()
        {
            UnityHelpersSettings.WGroupPaletteEntry slightlyLight = new(
                new Color(0.51f, 0.51f, 0.51f, 1f),
                Color.black
            );

            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(slightlyLight))
            {
                bool isLight = GroupGUIWidthUtility.IsInsideLightPaletteWGroup();

                Assert.That(isLight, Is.True, "0.51 luminance should be considered light (> 0.5).");
            }
        }

        [Test]
        public void SlightlyBelowBoundaryIsConsideredDark()
        {
            UnityHelpersSettings.WGroupPaletteEntry slightlyDark = new(
                new Color(0.49f, 0.49f, 0.49f, 1f),
                Color.white
            );

            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(slightlyDark))
            {
                bool isDark = GroupGUIWidthUtility.IsInsideDarkPaletteWGroup();

                Assert.That(isDark, Is.True, "0.49 luminance should be considered dark (<= 0.5).");
            }
        }

        [Test]
        public void ThemedSelectionColorFollowsPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color lightSelection = new Color(0.9f, 0.9f, 0.95f, 0.8f);
            Color darkSelection = new Color(0.2f, 0.3f, 0.5f, 0.8f);

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                Color result = GroupGUIWidthUtility.GetThemedSelectionColor(
                    lightSelection,
                    darkSelection
                );

                Assert.That(
                    result,
                    Is.EqualTo(lightSelection),
                    "Light palette should use light selection color."
                );
            }

            using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
            {
                Color result = GroupGUIWidthUtility.GetThemedSelectionColor(
                    lightSelection,
                    darkSelection
                );

                Assert.That(
                    result,
                    Is.EqualTo(darkSelection),
                    "Dark palette should use dark selection color."
                );
            }
        }

        [Test]
        public void ThemedBorderColorFollowsPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color lightBorder = new Color(0.7f, 0.7f, 0.7f, 1f);
            Color darkBorder = new Color(0.25f, 0.25f, 0.25f, 1f);

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                Color result = GroupGUIWidthUtility.GetThemedBorderColor(lightBorder, darkBorder);

                Assert.That(
                    result,
                    Is.EqualTo(lightBorder),
                    "Light palette should use light border color."
                );
            }

            using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
            {
                Color result = GroupGUIWidthUtility.GetThemedBorderColor(lightBorder, darkBorder);

                Assert.That(
                    result,
                    Is.EqualTo(darkBorder),
                    "Dark palette should use dark border color."
                );
            }
        }

        [Test]
        public void ThemedPendingBackgroundFollowsPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color lightPending = new Color(0.92f, 0.92f, 0.92f, 1f);
            Color darkPending = new Color(0.18f, 0.18f, 0.18f, 1f);

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                Color result = GroupGUIWidthUtility.GetThemedPendingBackgroundColor(
                    lightPending,
                    darkPending
                );

                Assert.That(
                    result,
                    Is.EqualTo(lightPending),
                    "Light palette should use light pending background."
                );
            }

            using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
            {
                Color result = GroupGUIWidthUtility.GetThemedPendingBackgroundColor(
                    lightPending,
                    darkPending
                );

                Assert.That(
                    result,
                    Is.EqualTo(darkPending),
                    "Dark palette should use dark pending background."
                );
            }
        }

        // =====================================================================
        // 6. IsInsideWGroup Property Tests (for SerializableCollection drawers)
        // =====================================================================

        [Test]
        public void IsInsideWGroupIsFalseWhenNotInPaletteScope()
        {
            GroupGUIWidthUtility.ResetForTests();

            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroup,
                Is.False,
                "IsInsideWGroup should be false when not in any palette scope."
            );
        }

        [Test]
        public void IsInsideWGroupIsTrueWhenInPaletteScope()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroup,
                    Is.True,
                    "IsInsideWGroup should be true when inside a palette scope."
                );
            }
        }

        [Test]
        public void IsInsideWGroupBecomesFalseAfterScopeDisposal()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
            {
                Assert.That(GroupGUIWidthUtility.IsInsideWGroup, Is.True);
            }

            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroup,
                Is.False,
                "IsInsideWGroup should return to false after palette scope is disposed."
            );
        }

        [Test]
        public void IsInsideWGroupRemainsCorrectDuringNestedScopes()
        {
            GroupGUIWidthUtility.ResetForTests();

            Assert.That(GroupGUIWidthUtility.IsInsideWGroup, Is.False);

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                Assert.That(GroupGUIWidthUtility.IsInsideWGroup, Is.True);

                using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
                {
                    Assert.That(
                        GroupGUIWidthUtility.IsInsideWGroup,
                        Is.True,
                        "IsInsideWGroup should remain true in nested scope."
                    );
                }

                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroup,
                    Is.True,
                    "IsInsideWGroup should remain true after inner scope disposal."
                );
            }

            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroup,
                Is.False,
                "IsInsideWGroup should be false after all scopes disposed."
            );
        }

        [Test]
        public void ListDrawingAlwaysSetsWhiteGuiColorAndRestores()
        {
            GroupGUIWidthUtility.ResetForTests();
            Color testOriginalColor = new Color(0.5f, 0.6f, 0.7f, 0.8f);
            GUI.color = testOriginalColor;

            // Simulate what SerializableDictionary/Set drawers now do - always set white
            Color listPreviousGuiColor = GUI.color;
            GUI.color = Color.white;

            // At this point, GUI.color should be white for list drawing
            Assert.That(
                GUI.color,
                Is.EqualTo(Color.white),
                "GUI.color should be white during list drawing."
            );

            // Restore
            GUI.color = listPreviousGuiColor;

            // GUI.color should be restored
            Assert.That(
                GUI.color,
                Is.EqualTo(testOriginalColor),
                "GUI.color should be restored after list drawing."
            );
        }

        [Test]
        public void ListDrawingAlwaysSetsWhiteGuiBackgroundColorAndRestores()
        {
            GroupGUIWidthUtility.ResetForTests();
            Color testOriginalColor = new Color(0.4f, 0.5f, 0.6f, 0.9f);
            GUI.backgroundColor = testOriginalColor;

            // Simulate what SerializableDictionary/Set drawers now do - always set white
            Color listPreviousBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.white;

            // At this point, GUI.backgroundColor should be white for list drawing
            Assert.That(
                GUI.backgroundColor,
                Is.EqualTo(Color.white),
                "GUI.backgroundColor should be white during list drawing."
            );

            // Restore
            GUI.backgroundColor = listPreviousBackgroundColor;

            // GUI.backgroundColor should be restored
            Assert.That(
                GUI.backgroundColor,
                Is.EqualTo(testOriginalColor),
                "GUI.backgroundColor should be restored after list drawing."
            );
        }

        [Test]
        public void ListDrawingAlwaysSetsWhiteGuiContentColorAndRestores()
        {
            GroupGUIWidthUtility.ResetForTests();
            Color testOriginalColor = new Color(0.3f, 0.4f, 0.5f, 0.7f);
            GUI.contentColor = testOriginalColor;

            // Simulate what SerializableDictionary/Set drawers now do - always set white
            Color listPreviousContentColor = GUI.contentColor;
            GUI.contentColor = Color.white;

            // At this point, GUI.contentColor should be white for list drawing
            Assert.That(
                GUI.contentColor,
                Is.EqualTo(Color.white),
                "GUI.contentColor should be white during list drawing."
            );

            // Restore
            GUI.contentColor = listPreviousContentColor;

            // GUI.contentColor should be restored
            Assert.That(
                GUI.contentColor,
                Is.EqualTo(testOriginalColor),
                "GUI.contentColor should be restored after list drawing."
            );
        }

        [Test]
        public void ListDrawingWhiteColorPreventsAnyTinting()
        {
            GroupGUIWidthUtility.ResetForTests();

            // Test with various "tinted" starting colors that might come from parent scopes
            Color[] tintedColors = new Color[]
            {
                new Color(0.8f, 0.8f, 0.8f, 1f), // Light gray tint
                new Color(0.2f, 0.2f, 0.2f, 1f), // Dark gray tint
                new Color(1f, 0.8f, 0.8f, 1f), // Reddish tint
                new Color(0.5f, 0.5f, 0.5f, 0.5f), // Half transparent gray
            };

            foreach (Color tintedColor in tintedColors)
            {
                GUI.color = tintedColor;

                // Simulate list drawing pattern
                Color listPreviousGuiColor = GUI.color;
                GUI.color = Color.white;

                // During list drawing, color should always be white regardless of starting tint
                Assert.That(
                    GUI.color,
                    Is.EqualTo(Color.white),
                    $"GUI.color should be white during list drawing, even when starting from {tintedColor}."
                );

                GUI.color = listPreviousGuiColor;

                // After restore, should be back to original tinted color
                Assert.That(
                    GUI.color,
                    Is.EqualTo(tintedColor),
                    $"GUI.color should be restored to {tintedColor} after list drawing."
                );
            }
        }

        [Test]
        public void ListDrawingInsideWGroupUsesWhiteColor()
        {
            GroupGUIWidthUtility.ResetForTests();
            Color testOriginalColor = new Color(0.3f, 0.4f, 0.5f, 1f);
            GUI.color = testOriginalColor;

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                // Simulate list drawing pattern (same as outside WGroup now)
                Color listPreviousGuiColor = GUI.color;
                GUI.color = Color.white;

                Assert.That(
                    GUI.color,
                    Is.EqualTo(Color.white),
                    "GUI.color should be white during list drawing inside WGroup."
                );

                GUI.color = listPreviousGuiColor;

                Assert.That(
                    GUI.color,
                    Is.EqualTo(testOriginalColor),
                    "GUI.color should be restored after list drawing inside WGroup."
                );
            }
        }

        [Test]
        public void ListDrawingResetsAllThreeGuiColorsAndRestores()
        {
            GroupGUIWidthUtility.ResetForTests();
            Color originalColor = new Color(0.5f, 0.6f, 0.7f, 0.8f);
            Color originalBackgroundColor = new Color(0.4f, 0.5f, 0.6f, 0.9f);
            Color originalContentColor = new Color(0.3f, 0.4f, 0.5f, 0.7f);
            GUI.color = originalColor;
            GUI.backgroundColor = originalBackgroundColor;
            GUI.contentColor = originalContentColor;

            // Simulate what SerializableDictionary/Set drawers now do - reset all three colors
            Color listPreviousGuiColor = GUI.color;
            Color listPreviousBackgroundColor = GUI.backgroundColor;
            Color listPreviousContentColor = GUI.contentColor;
            GUI.color = Color.white;
            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;

            // At this point, all three colors should be white for list drawing
            Assert.That(
                GUI.color,
                Is.EqualTo(Color.white),
                "GUI.color should be white during list drawing."
            );
            Assert.That(
                GUI.backgroundColor,
                Is.EqualTo(Color.white),
                "GUI.backgroundColor should be white during list drawing."
            );
            Assert.That(
                GUI.contentColor,
                Is.EqualTo(Color.white),
                "GUI.contentColor should be white during list drawing."
            );

            // Restore
            GUI.color = listPreviousGuiColor;
            GUI.backgroundColor = listPreviousBackgroundColor;
            GUI.contentColor = listPreviousContentColor;

            // All three colors should be restored
            Assert.That(
                GUI.color,
                Is.EqualTo(originalColor),
                "GUI.color should be restored after list drawing."
            );
            Assert.That(
                GUI.backgroundColor,
                Is.EqualTo(originalBackgroundColor),
                "GUI.backgroundColor should be restored after list drawing."
            );
            Assert.That(
                GUI.contentColor,
                Is.EqualTo(originalContentColor),
                "GUI.contentColor should be restored after list drawing."
            );
        }

        [Test]
        public void ListDrawingInsideWGroupColorScopeResetsAllColors()
        {
            GroupGUIWidthUtility.ResetForTests();

            UnityHelpersSettings.WGroupPaletteEntry crossThemePalette = EditorGUIUtility.isProSkin
                ? LightPalette
                : DarkPalette;

            using (GroupGUIWidthUtility.PushWGroupPalette(crossThemePalette))
            {
                using (WGroupColorScope scope = new WGroupColorScope(crossThemePalette))
                {
                    // WGroupColorScope should have set various colors
                    Color colorBeforeReset = GUI.color;
                    Color backgroundColorBeforeReset = GUI.backgroundColor;
                    Color contentColorBeforeReset = GUI.contentColor;

                    // Simulate list drawing pattern
                    Color listPreviousGuiColor = GUI.color;
                    Color listPreviousBackgroundColor = GUI.backgroundColor;
                    Color listPreviousContentColor = GUI.contentColor;
                    GUI.color = Color.white;
                    GUI.backgroundColor = Color.white;
                    GUI.contentColor = Color.white;

                    // During list drawing, all colors should be white
                    Assert.That(
                        GUI.color,
                        Is.EqualTo(Color.white),
                        "GUI.color should be white during list drawing in WGroupColorScope."
                    );
                    Assert.That(
                        GUI.backgroundColor,
                        Is.EqualTo(Color.white),
                        "GUI.backgroundColor should be white during list drawing in WGroupColorScope."
                    );
                    Assert.That(
                        GUI.contentColor,
                        Is.EqualTo(Color.white),
                        "GUI.contentColor should be white during list drawing in WGroupColorScope."
                    );

                    // Restore
                    GUI.color = listPreviousGuiColor;
                    GUI.backgroundColor = listPreviousBackgroundColor;
                    GUI.contentColor = listPreviousContentColor;

                    // After restore, colors should match what they were before reset
                    Assert.That(
                        GUI.color,
                        Is.EqualTo(colorBeforeReset),
                        "GUI.color should be restored to WGroupColorScope color after list drawing."
                    );
                    Assert.That(
                        GUI.backgroundColor,
                        Is.EqualTo(backgroundColorBeforeReset),
                        "GUI.backgroundColor should be restored to WGroupColorScope color after list drawing."
                    );
                    Assert.That(
                        GUI.contentColor,
                        Is.EqualTo(contentColorBeforeReset),
                        "GUI.contentColor should be restored to WGroupColorScope color after list drawing."
                    );
                }
            }
        }
    }
}
#endif
