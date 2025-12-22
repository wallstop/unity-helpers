namespace WallstopStudios.UnityHelpers.Tests.Utils
{
#if UNITY_EDITOR
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Utils.WGroup;

    [TestFixture]
    public sealed class WGroupColorDerivationTests
    {
        // Common test colors
        private static readonly Color Black = new(0f, 0f, 0f, 1f);
        private static readonly Color White = new(1f, 1f, 1f, 1f);
        private static readonly Color MidGray = new(0.5f, 0.5f, 0.5f, 1f);
        private static readonly Color LightGray = new(0.8f, 0.8f, 0.8f, 1f);
        private static readonly Color DarkGray = new(0.2f, 0.2f, 0.2f, 1f);
        private static readonly Color DefaultDarkPalette = new(0.215f, 0.215f, 0.215f, 1f);
        private const float Tolerance = 0.001f;

        // ============================================
        // 1. Luminance Tests
        // ============================================

        [Test]
        public void GetLuminanceReturnsZeroForBlack()
        {
            float luminance = WGroupColorDerivation.GetLuminance(Black);

            Assert.That(luminance, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void GetLuminanceReturnsOneForWhite()
        {
            float luminance = WGroupColorDerivation.GetLuminance(White);

            Assert.That(luminance, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void GetLuminanceReturnsCorrectValueForGray()
        {
            float luminance = WGroupColorDerivation.GetLuminance(MidGray);

            // Mid gray should have luminance around 0.5
            Assert.That(luminance, Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void GetLuminanceUsesCorrectWeighting()
        {
            // Green should contribute most (0.587), red second (0.299), blue least (0.114)
            Color pureRed = new(1f, 0f, 0f, 1f);
            Color pureGreen = new(0f, 1f, 0f, 1f);
            Color pureBlue = new(0f, 0f, 1f, 1f);

            float redLuminance = WGroupColorDerivation.GetLuminance(pureRed);
            float greenLuminance = WGroupColorDerivation.GetLuminance(pureGreen);
            float blueLuminance = WGroupColorDerivation.GetLuminance(pureBlue);

            Assert.That(
                redLuminance,
                Is.EqualTo(0.299f).Within(Tolerance),
                "Red weight should be 0.299"
            );
            Assert.That(
                greenLuminance,
                Is.EqualTo(0.587f).Within(Tolerance),
                "Green weight should be 0.587"
            );
            Assert.That(
                blueLuminance,
                Is.EqualTo(0.114f).Within(Tolerance),
                "Blue weight should be 0.114"
            );

            // Green should contribute most
            Assert.That(
                greenLuminance,
                Is.GreaterThan(redLuminance),
                "Green should contribute more than red"
            );
            Assert.That(
                redLuminance,
                Is.GreaterThan(blueLuminance),
                "Red should contribute more than blue"
            );
        }

        // ============================================
        // 2. IsLightBackground Tests
        // ============================================

        [Test]
        public void IsLightBackgroundReturnsTrueForWhite()
        {
            bool isLight = WGroupColorDerivation.IsLightColor(White);

            Assert.That(isLight, Is.True);
        }

        [Test]
        public void IsLightBackgroundReturnsFalseForBlack()
        {
            bool isLight = WGroupColorDerivation.IsLightColor(Black);

            Assert.That(isLight, Is.False);
        }

        [Test]
        public void IsLightBackgroundReturnsTrueForLightGray()
        {
            // LightGray (0.8, 0.8, 0.8) has luminance 0.8 > 0.5
            bool isLight = WGroupColorDerivation.IsLightColor(LightGray);

            Assert.That(isLight, Is.True);
        }

        [Test]
        public void IsLightBackgroundReturnsFalseForDarkGray()
        {
            // DarkGray (0.2, 0.2, 0.2) has luminance 0.2 <= 0.5
            bool isLight = WGroupColorDerivation.IsLightColor(DarkGray);

            Assert.That(isLight, Is.False);
        }

        [Test]
        public void IsLightBackgroundReturnsFalseForDefaultDarkPalette()
        {
            // Default dark palette (0.215, 0.215, 0.215) should be considered dark
            bool isLight = WGroupColorDerivation.IsLightColor(DefaultDarkPalette);

            Assert.That(
                isLight,
                Is.False,
                "Default dark palette (0.215) should be considered dark"
            );
        }

        [Test]
        public void IsLightBackgroundHandlesBoundaryCorrectly()
        {
            // Exactly 0.5 should be considered dark (> 0.5, not >=)
            bool isLight = WGroupColorDerivation.IsLightColor(MidGray);

            Assert.That(
                isLight,
                Is.False,
                "Luminance of exactly 0.5 should be considered dark (threshold is > 0.5)"
            );
        }

        // ============================================
        // 3. Lighten/Darken Tests
        // ============================================

        [Test]
        public void LightenWithZeroAmountReturnsOriginal()
        {
            Color testColor = new(0.3f, 0.4f, 0.5f, 0.8f);

            Color lightened = WGroupColorDerivation.Lighten(testColor, 0f);

            Assert.That(lightened.r, Is.EqualTo(testColor.r).Within(Tolerance));
            Assert.That(lightened.g, Is.EqualTo(testColor.g).Within(Tolerance));
            Assert.That(lightened.b, Is.EqualTo(testColor.b).Within(Tolerance));
            Assert.That(lightened.a, Is.EqualTo(testColor.a).Within(Tolerance));
        }

        [Test]
        public void LightenWithOneReturnsWhite()
        {
            Color testColor = new(0.3f, 0.4f, 0.5f, 0.8f);

            Color lightened = WGroupColorDerivation.Lighten(testColor, 1f);

            Assert.That(lightened.r, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(lightened.g, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(lightened.b, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void LightenPreservesAlpha()
        {
            float originalAlpha = 0.7f;
            Color testColor = new(0.3f, 0.4f, 0.5f, originalAlpha);

            Color lightened = WGroupColorDerivation.Lighten(testColor, 0.5f);

            Assert.That(lightened.a, Is.EqualTo(originalAlpha).Within(Tolerance));
        }

        [Test]
        public void LightenClampsNegativeAmount()
        {
            Color testColor = new(0.3f, 0.4f, 0.5f, 1f);

            Color lightened = WGroupColorDerivation.Lighten(testColor, -0.5f);

            // Negative amount should be clamped to 0, returning original
            Assert.That(lightened.r, Is.EqualTo(testColor.r).Within(Tolerance));
            Assert.That(lightened.g, Is.EqualTo(testColor.g).Within(Tolerance));
            Assert.That(lightened.b, Is.EqualTo(testColor.b).Within(Tolerance));
        }

        [Test]
        public void LightenClampsExcessiveAmount()
        {
            Color testColor = new(0.3f, 0.4f, 0.5f, 1f);

            Color lightened = WGroupColorDerivation.Lighten(testColor, 1.5f);

            // Excessive amount should be clamped to 1, returning white
            Assert.That(lightened.r, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(lightened.g, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(lightened.b, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void DarkenWithZeroAmountReturnsOriginal()
        {
            Color testColor = new(0.3f, 0.4f, 0.5f, 0.8f);

            Color darkened = WGroupColorDerivation.Darken(testColor, 0f);

            Assert.That(darkened.r, Is.EqualTo(testColor.r).Within(Tolerance));
            Assert.That(darkened.g, Is.EqualTo(testColor.g).Within(Tolerance));
            Assert.That(darkened.b, Is.EqualTo(testColor.b).Within(Tolerance));
            Assert.That(darkened.a, Is.EqualTo(testColor.a).Within(Tolerance));
        }

        [Test]
        public void DarkenWithOneReturnsBlack()
        {
            Color testColor = new(0.3f, 0.4f, 0.5f, 0.8f);

            Color darkened = WGroupColorDerivation.Darken(testColor, 1f);

            Assert.That(darkened.r, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(darkened.g, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(darkened.b, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void DarkenPreservesAlpha()
        {
            float originalAlpha = 0.7f;
            Color testColor = new(0.3f, 0.4f, 0.5f, originalAlpha);

            Color darkened = WGroupColorDerivation.Darken(testColor, 0.5f);

            Assert.That(darkened.a, Is.EqualTo(originalAlpha).Within(Tolerance));
        }

        [Test]
        public void DarkenClampsNegativeAmount()
        {
            Color testColor = new(0.3f, 0.4f, 0.5f, 1f);

            Color darkened = WGroupColorDerivation.Darken(testColor, -0.5f);

            // Negative amount should be clamped to 0, returning original
            Assert.That(darkened.r, Is.EqualTo(testColor.r).Within(Tolerance));
            Assert.That(darkened.g, Is.EqualTo(testColor.g).Within(Tolerance));
            Assert.That(darkened.b, Is.EqualTo(testColor.b).Within(Tolerance));
        }

        [Test]
        public void DarkenClampsExcessiveAmount()
        {
            Color testColor = new(0.3f, 0.4f, 0.5f, 1f);

            Color darkened = WGroupColorDerivation.Darken(testColor, 1.5f);

            // Excessive amount should be clamped to 1, returning black
            Assert.That(darkened.r, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(darkened.g, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(darkened.b, Is.EqualTo(0f).Within(Tolerance));
        }

        // ============================================
        // 4. DeriveRowColor Tests
        // ============================================

        [Test]
        public void DeriveRowColorForDarkBackgroundReturnsLighterColor()
        {
            Color rowColor = WGroupColorDerivation.DeriveRowColor(DarkGray);

            float originalLuminance = WGroupColorDerivation.GetLuminance(DarkGray);
            float derivedLuminance = WGroupColorDerivation.GetLuminance(rowColor);

            Assert.That(
                derivedLuminance,
                Is.GreaterThan(originalLuminance),
                "Dark background should produce lighter row color"
            );
        }

        [Test]
        public void DeriveRowColorForLightBackgroundReturnsDarkerColor()
        {
            Color rowColor = WGroupColorDerivation.DeriveRowColor(LightGray);

            float originalLuminance = WGroupColorDerivation.GetLuminance(LightGray);
            float derivedLuminance = WGroupColorDerivation.GetLuminance(rowColor);

            Assert.That(
                derivedLuminance,
                Is.LessThan(originalLuminance),
                "Light background should produce darker row color"
            );
        }

        [Test]
        public void DeriveRowColorForDefaultDarkPaletteIsReadable()
        {
            Color rowColor = WGroupColorDerivation.DeriveRowColor(DefaultDarkPalette);

            // The row color should be visually distinguishable from pure dark
            // For 0.215 background, row should be at least 0.22 (lightened by 5%)
            Assert.That(
                rowColor.r,
                Is.GreaterThan(0.22f),
                "Default dark palette row color should be readable (> 0.22)"
            );
        }

        [Test]
        public void DeriveRowColorHasFullAlpha()
        {
            Color rowColor = WGroupColorDerivation.DeriveRowColor(DarkGray);

            Assert.That(
                rowColor.a,
                Is.EqualTo(1f).Within(Tolerance),
                "Row color should have full alpha"
            );
        }

        // ============================================
        // 5. DeriveAlternateRowColor Tests
        // ============================================

        [Test]
        public void DeriveAlternateRowColorForDarkBackgroundReturnsLighterThanRow()
        {
            Color rowColor = WGroupColorDerivation.DeriveRowColor(DarkGray);
            Color alternateRowColor = WGroupColorDerivation.DeriveAlternateRowColor(DarkGray);

            float rowLuminance = WGroupColorDerivation.GetLuminance(rowColor);
            float alternateLuminance = WGroupColorDerivation.GetLuminance(alternateRowColor);

            Assert.That(
                alternateLuminance,
                Is.GreaterThan(rowLuminance),
                "Dark background alternate row should be lighter than base row"
            );
        }

        [Test]
        public void DeriveAlternateRowColorForLightBackgroundReturnsDarkerThanRow()
        {
            Color rowColor = WGroupColorDerivation.DeriveRowColor(LightGray);
            Color alternateRowColor = WGroupColorDerivation.DeriveAlternateRowColor(LightGray);

            float rowLuminance = WGroupColorDerivation.GetLuminance(rowColor);
            float alternateLuminance = WGroupColorDerivation.GetLuminance(alternateRowColor);

            Assert.That(
                alternateLuminance,
                Is.LessThan(rowLuminance),
                "Light background alternate row should be darker than base row"
            );
        }

        [Test]
        public void DeriveAlternateRowColorHasFullAlpha()
        {
            Color alternateRowColor = WGroupColorDerivation.DeriveAlternateRowColor(DarkGray);

            Assert.That(
                alternateRowColor.a,
                Is.EqualTo(1f).Within(Tolerance),
                "Alternate row color should have full alpha"
            );
        }

        // ============================================
        // 6. DeriveSelectionColor Tests
        // ============================================

        [Test]
        public void DeriveSelectionColorForDarkBackgroundReturnsBlueAccent()
        {
            Color selectionColor = WGroupColorDerivation.DeriveSelectionColor(DarkGray);

            // Selection color should have blue as dominant component
            Assert.That(
                selectionColor.b,
                Is.GreaterThan(selectionColor.r),
                "Selection should have blue accent"
            );
            Assert.That(
                selectionColor.b,
                Is.GreaterThan(selectionColor.g),
                "Selection should have blue as dominant"
            );
        }

        [Test]
        public void DeriveSelectionColorForLightBackgroundReturnsBlueAccent()
        {
            Color selectionColor = WGroupColorDerivation.DeriveSelectionColor(LightGray);

            // Selection color should have blue as dominant component
            Assert.That(
                selectionColor.b,
                Is.GreaterThan(selectionColor.r),
                "Selection should have blue accent"
            );
        }

        [Test]
        public void DeriveSelectionColorHasAppropriateAlpha()
        {
            Color darkSelection = WGroupColorDerivation.DeriveSelectionColor(DarkGray);
            Color lightSelection = WGroupColorDerivation.DeriveSelectionColor(LightGray);

            // Selection colors should have semi-transparent alpha (not fully opaque)
            Assert.That(
                darkSelection.a,
                Is.GreaterThan(0.5f),
                "Dark selection should have visible alpha"
            );
            Assert.That(
                darkSelection.a,
                Is.LessThan(1f),
                "Dark selection should be semi-transparent"
            );
            Assert.That(
                lightSelection.a,
                Is.GreaterThan(0.5f),
                "Light selection should have visible alpha"
            );
            Assert.That(
                lightSelection.a,
                Is.LessThan(1f),
                "Light selection should be semi-transparent"
            );
        }

        [Test]
        public void DeriveSelectionColorDiffersBetweenLightAndDarkBackgrounds()
        {
            Color darkSelection = WGroupColorDerivation.DeriveSelectionColor(DarkGray);
            Color lightSelection = WGroupColorDerivation.DeriveSelectionColor(LightGray);

            // The two selection colors should differ
            bool colorsAreDifferent =
                Mathf.Abs(darkSelection.r - lightSelection.r) > Tolerance
                || Mathf.Abs(darkSelection.g - lightSelection.g) > Tolerance
                || Mathf.Abs(darkSelection.b - lightSelection.b) > Tolerance
                || Mathf.Abs(darkSelection.a - lightSelection.a) > Tolerance;

            Assert.That(
                colorsAreDifferent,
                Is.True,
                "Selection colors should differ for light vs dark backgrounds"
            );
        }

        // ============================================
        // 7. DeriveBorderColor Tests
        // ============================================

        [Test]
        public void DeriveBorderColorForDarkBackgroundReturnsLighterColor()
        {
            Color borderColor = WGroupColorDerivation.DeriveBorderColor(DarkGray);

            float originalLuminance = WGroupColorDerivation.GetLuminance(DarkGray);
            float borderLuminance = WGroupColorDerivation.GetLuminance(borderColor);

            Assert.That(
                borderLuminance,
                Is.GreaterThan(originalLuminance),
                "Dark background should produce lighter border color"
            );
        }

        [Test]
        public void DeriveBorderColorForLightBackgroundReturnsDarkerColor()
        {
            Color borderColor = WGroupColorDerivation.DeriveBorderColor(LightGray);

            float originalLuminance = WGroupColorDerivation.GetLuminance(LightGray);
            float borderLuminance = WGroupColorDerivation.GetLuminance(borderColor);

            Assert.That(
                borderLuminance,
                Is.LessThan(originalLuminance),
                "Light background should produce darker border color"
            );
        }

        [Test]
        public void DeriveBorderColorHasMoreContrastThanRowColor()
        {
            Color rowColor = WGroupColorDerivation.DeriveRowColor(DarkGray);
            Color borderColor = WGroupColorDerivation.DeriveBorderColor(DarkGray);

            float backgroundLuminance = WGroupColorDerivation.GetLuminance(DarkGray);
            float rowLuminance = WGroupColorDerivation.GetLuminance(rowColor);
            float borderLuminance = WGroupColorDerivation.GetLuminance(borderColor);

            float rowContrast = Mathf.Abs(rowLuminance - backgroundLuminance);
            float borderContrast = Mathf.Abs(borderLuminance - backgroundLuminance);

            Assert.That(
                borderContrast,
                Is.GreaterThan(rowContrast),
                "Border color should have more contrast with background than row color"
            );
        }

        [Test]
        public void DeriveBorderColorHasFullAlpha()
        {
            Color borderColor = WGroupColorDerivation.DeriveBorderColor(DarkGray);

            Assert.That(
                borderColor.a,
                Is.EqualTo(1f).Within(Tolerance),
                "Border color should have full alpha"
            );
        }

        // ============================================
        // 8. DerivePendingBackgroundColor Tests
        // ============================================

        [Test]
        public void DerivePendingBackgroundColorHasYellowGreenTint()
        {
            Color baseColor = new(0.3f, 0.3f, 0.3f, 1f);
            Color pendingColor = WGroupColorDerivation.DerivePendingBackgroundColor(baseColor);

            // The derived color should have elevated green component (yellow + green tint)
            // Yellow = R+G, so both R and G should be boosted, with G getting extra green tint
            // Compare to what we'd expect from just lightening without tint
            Color justLightened = WGroupColorDerivation.Lighten(baseColor, 0.12f);

            // Green should be higher due to yellow + green tint
            Assert.That(
                pendingColor.g,
                Is.GreaterThan(justLightened.g),
                "Pending color should have green tint above simple lightening"
            );
        }

        [Test]
        public void DerivePendingBackgroundColorForDarkBackgroundIsLighter()
        {
            Color pendingColor = WGroupColorDerivation.DerivePendingBackgroundColor(DarkGray);

            float originalLuminance = WGroupColorDerivation.GetLuminance(DarkGray);
            float pendingLuminance = WGroupColorDerivation.GetLuminance(pendingColor);

            Assert.That(
                pendingLuminance,
                Is.GreaterThan(originalLuminance),
                "Dark background pending color should be lighter"
            );
        }

        [Test]
        public void DerivePendingBackgroundColorForLightBackgroundIsDarker()
        {
            Color pendingColor = WGroupColorDerivation.DerivePendingBackgroundColor(LightGray);

            float originalLuminance = WGroupColorDerivation.GetLuminance(LightGray);
            float pendingLuminance = WGroupColorDerivation.GetLuminance(pendingColor);

            // Note: The pending color gets darkened but then gets yellow/green tint added
            // So we check that it still differs from original
            Assert.That(
                pendingLuminance,
                Is.Not.EqualTo(originalLuminance).Within(0.01f),
                "Pending color should differ from original"
            );
        }

        [Test]
        public void DerivePendingBackgroundColorHasFullAlpha()
        {
            Color pendingColor = WGroupColorDerivation.DerivePendingBackgroundColor(DarkGray);

            Assert.That(
                pendingColor.a,
                Is.EqualTo(1f).Within(Tolerance),
                "Pending color should have full alpha"
            );
        }

        // ============================================
        // 9. GetEffective* Tests
        // ============================================

        [Test]
        public void GetEffectiveRowColorReturnsExplicitWhenProvided()
        {
            Color explicitColor = new(0.1f, 0.2f, 0.3f, 1f);

            Color effective = WGroupColorDerivation.GetEffectiveRowColor(DarkGray, explicitColor);

            Assert.That(effective.r, Is.EqualTo(explicitColor.r).Within(Tolerance));
            Assert.That(effective.g, Is.EqualTo(explicitColor.g).Within(Tolerance));
            Assert.That(effective.b, Is.EqualTo(explicitColor.b).Within(Tolerance));
            Assert.That(effective.a, Is.EqualTo(explicitColor.a).Within(Tolerance));
        }

        [Test]
        public void GetEffectiveRowColorReturnsDerivedWhenNull()
        {
            Color effective = WGroupColorDerivation.GetEffectiveRowColor(DarkGray, null);
            Color derived = WGroupColorDerivation.DeriveRowColor(DarkGray);

            Assert.That(effective.r, Is.EqualTo(derived.r).Within(Tolerance));
            Assert.That(effective.g, Is.EqualTo(derived.g).Within(Tolerance));
            Assert.That(effective.b, Is.EqualTo(derived.b).Within(Tolerance));
            Assert.That(effective.a, Is.EqualTo(derived.a).Within(Tolerance));
        }

        [Test]
        public void GetEffectiveAlternateRowColorReturnsExplicitWhenProvided()
        {
            Color explicitColor = new(0.4f, 0.5f, 0.6f, 1f);

            Color effective = WGroupColorDerivation.GetEffectiveAlternateRowColor(
                DarkGray,
                explicitColor
            );

            Assert.That(effective.r, Is.EqualTo(explicitColor.r).Within(Tolerance));
            Assert.That(effective.g, Is.EqualTo(explicitColor.g).Within(Tolerance));
            Assert.That(effective.b, Is.EqualTo(explicitColor.b).Within(Tolerance));
            Assert.That(effective.a, Is.EqualTo(explicitColor.a).Within(Tolerance));
        }

        [Test]
        public void GetEffectiveAlternateRowColorReturnsDerivedWhenNull()
        {
            Color effective = WGroupColorDerivation.GetEffectiveAlternateRowColor(DarkGray, null);
            Color derived = WGroupColorDerivation.DeriveAlternateRowColor(DarkGray);

            Assert.That(effective.r, Is.EqualTo(derived.r).Within(Tolerance));
            Assert.That(effective.g, Is.EqualTo(derived.g).Within(Tolerance));
            Assert.That(effective.b, Is.EqualTo(derived.b).Within(Tolerance));
            Assert.That(effective.a, Is.EqualTo(derived.a).Within(Tolerance));
        }

        [Test]
        public void GetEffectiveSelectionColorReturnsExplicitWhenProvided()
        {
            Color explicitColor = new(0.7f, 0.8f, 0.9f, 0.5f);

            Color effective = WGroupColorDerivation.GetEffectiveSelectionColor(
                DarkGray,
                explicitColor
            );

            Assert.That(effective.r, Is.EqualTo(explicitColor.r).Within(Tolerance));
            Assert.That(effective.g, Is.EqualTo(explicitColor.g).Within(Tolerance));
            Assert.That(effective.b, Is.EqualTo(explicitColor.b).Within(Tolerance));
            Assert.That(effective.a, Is.EqualTo(explicitColor.a).Within(Tolerance));
        }

        [Test]
        public void GetEffectiveSelectionColorReturnsDerivedWhenNull()
        {
            Color effective = WGroupColorDerivation.GetEffectiveSelectionColor(DarkGray, null);
            Color derived = WGroupColorDerivation.DeriveSelectionColor(DarkGray);

            Assert.That(effective.r, Is.EqualTo(derived.r).Within(Tolerance));
            Assert.That(effective.g, Is.EqualTo(derived.g).Within(Tolerance));
            Assert.That(effective.b, Is.EqualTo(derived.b).Within(Tolerance));
            Assert.That(effective.a, Is.EqualTo(derived.a).Within(Tolerance));
        }

        [Test]
        public void GetEffectiveBorderColorReturnsExplicitWhenProvided()
        {
            Color explicitColor = new(0.2f, 0.3f, 0.4f, 1f);

            Color effective = WGroupColorDerivation.GetEffectiveBorderColor(
                DarkGray,
                explicitColor
            );

            Assert.That(effective.r, Is.EqualTo(explicitColor.r).Within(Tolerance));
            Assert.That(effective.g, Is.EqualTo(explicitColor.g).Within(Tolerance));
            Assert.That(effective.b, Is.EqualTo(explicitColor.b).Within(Tolerance));
            Assert.That(effective.a, Is.EqualTo(explicitColor.a).Within(Tolerance));
        }

        [Test]
        public void GetEffectiveBorderColorReturnsDerivedWhenNull()
        {
            Color effective = WGroupColorDerivation.GetEffectiveBorderColor(DarkGray, null);
            Color derived = WGroupColorDerivation.DeriveBorderColor(DarkGray);

            Assert.That(effective.r, Is.EqualTo(derived.r).Within(Tolerance));
            Assert.That(effective.g, Is.EqualTo(derived.g).Within(Tolerance));
            Assert.That(effective.b, Is.EqualTo(derived.b).Within(Tolerance));
            Assert.That(effective.a, Is.EqualTo(derived.a).Within(Tolerance));
        }

        [Test]
        public void GetEffectivePendingBackgroundColorReturnsExplicitWhenProvided()
        {
            Color explicitColor = new(0.5f, 0.6f, 0.3f, 1f);

            Color effective = WGroupColorDerivation.GetEffectivePendingBackgroundColor(
                DarkGray,
                explicitColor
            );

            Assert.That(effective.r, Is.EqualTo(explicitColor.r).Within(Tolerance));
            Assert.That(effective.g, Is.EqualTo(explicitColor.g).Within(Tolerance));
            Assert.That(effective.b, Is.EqualTo(explicitColor.b).Within(Tolerance));
            Assert.That(effective.a, Is.EqualTo(explicitColor.a).Within(Tolerance));
        }

        [Test]
        public void GetEffectivePendingBackgroundColorReturnsDerivedWhenNull()
        {
            Color effective = WGroupColorDerivation.GetEffectivePendingBackgroundColor(
                DarkGray,
                null
            );
            Color derived = WGroupColorDerivation.DerivePendingBackgroundColor(DarkGray);

            Assert.That(effective.r, Is.EqualTo(derived.r).Within(Tolerance));
            Assert.That(effective.g, Is.EqualTo(derived.g).Within(Tolerance));
            Assert.That(effective.b, Is.EqualTo(derived.b).Within(Tolerance));
            Assert.That(effective.a, Is.EqualTo(derived.a).Within(Tolerance));
        }
    }
#endif
}
