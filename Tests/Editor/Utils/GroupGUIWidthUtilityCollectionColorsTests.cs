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
    /// Integration tests for palette color flow through GroupGUIWidthUtility.
    /// Tests the integration between WGroupPaletteEntry, GroupGUIWidthUtility, and the color derivation system.
    /// </summary>
    [TestFixture]
    public sealed class GroupGUIWidthUtilityCollectionColorsTests
    {
        private const float Tolerance = 0.001f;

        // Standard fallback colors for testing
        private static readonly Color FallbackLightRow = new(0.88f, 0.88f, 0.88f, 1f);
        private static readonly Color FallbackDarkRow = new(0.22f, 0.22f, 0.22f, 1f);
        private static readonly Color FallbackLightAlternate = new(0.85f, 0.85f, 0.85f, 1f);
        private static readonly Color FallbackDarkAlternate = new(0.25f, 0.25f, 0.25f, 1f);
        private static readonly Color FallbackLightSelection = new(0.33f, 0.62f, 0.95f, 0.65f);
        private static readonly Color FallbackDarkSelection = new(0.2f, 0.45f, 0.85f, 0.7f);
        private static readonly Color FallbackLightBorder = new(0.7f, 0.7f, 0.7f, 1f);
        private static readonly Color FallbackDarkBorder = new(0.25f, 0.25f, 0.25f, 1f);
        private static readonly Color FallbackLightPending = new(0.92f, 0.92f, 0.92f, 1f);
        private static readonly Color FallbackDarkPending = new(0.18f, 0.18f, 0.18f, 1f);

        // Standard test palettes
        private static readonly UnityHelpersSettings.WGroupPaletteEntry LightPalette = new(
            new Color(0.82f, 0.82f, 0.82f, 1f),
            Color.black
        );

        private static readonly UnityHelpersSettings.WGroupPaletteEntry DarkPalette = new(
            new Color(0.215f, 0.215f, 0.215f, 1f),
            Color.white
        );

        private static readonly UnityHelpersSettings.WGroupPaletteEntry DefaultDarkPalette = new(
            new Color(0.215f, 0.215f, 0.215f, 1f),
            Color.white
        );

        // Explicit colors for testing
        private static readonly Color ExplicitRowColor = new(0.9f, 0.3f, 0.3f, 1f);
        private static readonly Color ExplicitAlternateRowColor = new(0.3f, 0.9f, 0.3f, 1f);
        private static readonly Color ExplicitSelectionColor = new(0.3f, 0.3f, 0.9f, 0.8f);
        private static readonly Color ExplicitBorderColor = new(0.5f, 0.5f, 0.5f, 1f);
        private static readonly Color ExplicitPendingColor = new(0.8f, 0.8f, 0.4f, 1f);

        [SetUp]
        public void SetUp()
        {
            GroupGUIWidthUtility.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            GroupGUIWidthUtility.ResetForTests();
        }

        // =====================================================================
        // 1. WGroupPaletteEntry Construction Tests
        // =====================================================================

        [Test]
        public void PaletteEntryWithNullCollectionColorsHasNullProperties()
        {
            UnityHelpersSettings.WGroupPaletteEntry palette = new(
                new Color(0.5f, 0.5f, 0.5f, 1f),
                Color.white
            );

            Assert.That(palette.RowColor, Is.Null, "RowColor should be null when not provided.");
            Assert.That(
                palette.AlternateRowColor,
                Is.Null,
                "AlternateRowColor should be null when not provided."
            );
            Assert.That(
                palette.SelectionColor,
                Is.Null,
                "SelectionColor should be null when not provided."
            );
            Assert.That(
                palette.BorderColor,
                Is.Null,
                "BorderColor should be null when not provided."
            );
            Assert.That(
                palette.PendingBackgroundColor,
                Is.Null,
                "PendingBackgroundColor should be null when not provided."
            );
        }

        [Test]
        public void PaletteEntryWithExplicitColorsReturnsThoseColors()
        {
            UnityHelpersSettings.WGroupPaletteEntry palette = new(
                new Color(0.5f, 0.5f, 0.5f, 1f),
                Color.white,
                rowColor: ExplicitRowColor,
                alternateRowColor: ExplicitAlternateRowColor,
                selectionColor: ExplicitSelectionColor,
                borderColor: ExplicitBorderColor,
                pendingBackgroundColor: ExplicitPendingColor
            );

            Assert.That(
                palette.RowColor.HasValue,
                Is.True,
                "RowColor should have a value when provided."
            );
            Assert.That(
                palette.RowColor.Value,
                Is.EqualTo(ExplicitRowColor),
                "RowColor should equal the provided value."
            );

            Assert.That(
                palette.AlternateRowColor.HasValue,
                Is.True,
                "AlternateRowColor should have a value when provided."
            );
            Assert.That(
                palette.AlternateRowColor.Value,
                Is.EqualTo(ExplicitAlternateRowColor),
                "AlternateRowColor should equal the provided value."
            );

            Assert.That(
                palette.SelectionColor.HasValue,
                Is.True,
                "SelectionColor should have a value when provided."
            );
            Assert.That(
                palette.SelectionColor.Value,
                Is.EqualTo(ExplicitSelectionColor),
                "SelectionColor should equal the provided value."
            );

            Assert.That(
                palette.BorderColor.HasValue,
                Is.True,
                "BorderColor should have a value when provided."
            );
            Assert.That(
                palette.BorderColor.Value,
                Is.EqualTo(ExplicitBorderColor),
                "BorderColor should equal the provided value."
            );

            Assert.That(
                palette.PendingBackgroundColor.HasValue,
                Is.True,
                "PendingBackgroundColor should have a value when provided."
            );
            Assert.That(
                palette.PendingBackgroundColor.Value,
                Is.EqualTo(ExplicitPendingColor),
                "PendingBackgroundColor should equal the provided value."
            );
        }

        [Test]
        public void PaletteEntryPartialColorsReturnsMixOfExplicitAndNull()
        {
            UnityHelpersSettings.WGroupPaletteEntry palette = new(
                new Color(0.5f, 0.5f, 0.5f, 1f),
                Color.white,
                rowColor: ExplicitRowColor,
                alternateRowColor: null,
                selectionColor: ExplicitSelectionColor,
                borderColor: null,
                pendingBackgroundColor: null
            );

            Assert.That(
                palette.RowColor.HasValue,
                Is.True,
                "RowColor should have value when explicitly provided."
            );
            Assert.That(
                palette.RowColor.Value,
                Is.EqualTo(ExplicitRowColor),
                "RowColor should match explicit value."
            );

            Assert.That(
                palette.AlternateRowColor,
                Is.Null,
                "AlternateRowColor should be null when not provided."
            );

            Assert.That(
                palette.SelectionColor.HasValue,
                Is.True,
                "SelectionColor should have value when explicitly provided."
            );
            Assert.That(
                palette.SelectionColor.Value,
                Is.EqualTo(ExplicitSelectionColor),
                "SelectionColor should match explicit value."
            );

            Assert.That(
                palette.BorderColor,
                Is.Null,
                "BorderColor should be null when not provided."
            );

            Assert.That(
                palette.PendingBackgroundColor,
                Is.Null,
                "PendingBackgroundColor should be null when not provided."
            );
        }

        // =====================================================================
        // 2. GroupGUIWidthUtility Fallback Tests (no palette context)
        // =====================================================================

        [Test]
        public void GetPaletteRowColorReturnsFallbackWhenNoPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color result = GroupGUIWidthUtility.GetPaletteRowColor(
                FallbackLightRow,
                FallbackDarkRow
            );

            Color expectedFallback = EditorGUIUtility.isProSkin
                ? FallbackDarkRow
                : FallbackLightRow;

            Assert.That(
                result.r,
                Is.EqualTo(expectedFallback.r).Within(Tolerance),
                "Row color R should match editor skin fallback when no palette."
            );
            Assert.That(
                result.g,
                Is.EqualTo(expectedFallback.g).Within(Tolerance),
                "Row color G should match editor skin fallback when no palette."
            );
            Assert.That(
                result.b,
                Is.EqualTo(expectedFallback.b).Within(Tolerance),
                "Row color B should match editor skin fallback when no palette."
            );
        }

        [Test]
        public void GetPaletteAlternateRowColorReturnsFallbackWhenNoPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color result = GroupGUIWidthUtility.GetPaletteAlternateRowColor(
                FallbackLightAlternate,
                FallbackDarkAlternate
            );

            Color expectedFallback = EditorGUIUtility.isProSkin
                ? FallbackDarkAlternate
                : FallbackLightAlternate;

            Assert.That(
                result.r,
                Is.EqualTo(expectedFallback.r).Within(Tolerance),
                "Alternate row color R should match editor skin fallback when no palette."
            );
            Assert.That(
                result.g,
                Is.EqualTo(expectedFallback.g).Within(Tolerance),
                "Alternate row color G should match editor skin fallback when no palette."
            );
            Assert.That(
                result.b,
                Is.EqualTo(expectedFallback.b).Within(Tolerance),
                "Alternate row color B should match editor skin fallback when no palette."
            );
        }

        [Test]
        public void GetPaletteSelectionColorReturnsFallbackWhenNoPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color result = GroupGUIWidthUtility.GetPaletteSelectionColor(
                FallbackLightSelection,
                FallbackDarkSelection
            );

            Color expectedFallback = EditorGUIUtility.isProSkin
                ? FallbackDarkSelection
                : FallbackLightSelection;

            Assert.That(
                result.r,
                Is.EqualTo(expectedFallback.r).Within(Tolerance),
                "Selection color R should match editor skin fallback when no palette."
            );
            Assert.That(
                result.g,
                Is.EqualTo(expectedFallback.g).Within(Tolerance),
                "Selection color G should match editor skin fallback when no palette."
            );
            Assert.That(
                result.b,
                Is.EqualTo(expectedFallback.b).Within(Tolerance),
                "Selection color B should match editor skin fallback when no palette."
            );
        }

        [Test]
        public void GetPaletteBorderColorReturnsFallbackWhenNoPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color result = GroupGUIWidthUtility.GetPaletteBorderColor(
                FallbackLightBorder,
                FallbackDarkBorder
            );

            Color expectedFallback = EditorGUIUtility.isProSkin
                ? FallbackDarkBorder
                : FallbackLightBorder;

            Assert.That(
                result.r,
                Is.EqualTo(expectedFallback.r).Within(Tolerance),
                "Border color R should match editor skin fallback when no palette."
            );
            Assert.That(
                result.g,
                Is.EqualTo(expectedFallback.g).Within(Tolerance),
                "Border color G should match editor skin fallback when no palette."
            );
            Assert.That(
                result.b,
                Is.EqualTo(expectedFallback.b).Within(Tolerance),
                "Border color B should match editor skin fallback when no palette."
            );
        }

        [Test]
        public void GetPalettePendingBackgroundColorReturnsFallbackWhenNoPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color result = GroupGUIWidthUtility.GetPalettePendingBackgroundColor(
                FallbackLightPending,
                FallbackDarkPending
            );

            Color expectedFallback = EditorGUIUtility.isProSkin
                ? FallbackDarkPending
                : FallbackLightPending;

            Assert.That(
                result.r,
                Is.EqualTo(expectedFallback.r).Within(Tolerance),
                "Pending background color R should match editor skin fallback when no palette."
            );
            Assert.That(
                result.g,
                Is.EqualTo(expectedFallback.g).Within(Tolerance),
                "Pending background color G should match editor skin fallback when no palette."
            );
            Assert.That(
                result.b,
                Is.EqualTo(expectedFallback.b).Within(Tolerance),
                "Pending background color B should match editor skin fallback when no palette."
            );
        }

        // =====================================================================
        // 3. GroupGUIWidthUtility Palette-Aware Tests (with palette context)
        // =====================================================================

        [Test]
        public void GetPaletteRowColorReturnsExplicitWhenPaletteHasRowColor()
        {
            UnityHelpersSettings.WGroupPaletteEntry palette = new(
                new Color(0.5f, 0.5f, 0.5f, 1f),
                Color.white,
                rowColor: ExplicitRowColor
            );

            using (GroupGUIWidthUtility.PushWGroupPalette(palette))
            {
                Color result = GroupGUIWidthUtility.GetPaletteRowColor(
                    FallbackLightRow,
                    FallbackDarkRow
                );

                Assert.That(
                    result.r,
                    Is.EqualTo(ExplicitRowColor.r).Within(Tolerance),
                    "Row color R should match explicit palette value."
                );
                Assert.That(
                    result.g,
                    Is.EqualTo(ExplicitRowColor.g).Within(Tolerance),
                    "Row color G should match explicit palette value."
                );
                Assert.That(
                    result.b,
                    Is.EqualTo(ExplicitRowColor.b).Within(Tolerance),
                    "Row color B should match explicit palette value."
                );
            }
        }

        [Test]
        public void GetPaletteRowColorReturnsDerivedWhenPaletteHasNullRowColor()
        {
            using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
            {
                Color result = GroupGUIWidthUtility.GetPaletteRowColor(
                    FallbackLightRow,
                    FallbackDarkRow
                );

                // Should get derived color, not fallback
                Color derived = WGroupColorDerivation.DeriveRowColor(DarkPalette.BackgroundColor);

                Assert.That(
                    result.r,
                    Is.EqualTo(derived.r).Within(Tolerance),
                    "Row color R should match derived color when palette has null RowColor."
                );
                Assert.That(
                    result.g,
                    Is.EqualTo(derived.g).Within(Tolerance),
                    "Row color G should match derived color when palette has null RowColor."
                );
                Assert.That(
                    result.b,
                    Is.EqualTo(derived.b).Within(Tolerance),
                    "Row color B should match derived color when palette has null RowColor."
                );
            }
        }

        [Test]
        public void GetPaletteAlternateRowColorReturnsExplicitWhenPaletteHasColor()
        {
            UnityHelpersSettings.WGroupPaletteEntry palette = new(
                new Color(0.5f, 0.5f, 0.5f, 1f),
                Color.white,
                alternateRowColor: ExplicitAlternateRowColor
            );

            using (GroupGUIWidthUtility.PushWGroupPalette(palette))
            {
                Color result = GroupGUIWidthUtility.GetPaletteAlternateRowColor(
                    FallbackLightAlternate,
                    FallbackDarkAlternate
                );

                Assert.That(
                    result.r,
                    Is.EqualTo(ExplicitAlternateRowColor.r).Within(Tolerance),
                    "Alternate row color R should match explicit palette value."
                );
                Assert.That(
                    result.g,
                    Is.EqualTo(ExplicitAlternateRowColor.g).Within(Tolerance),
                    "Alternate row color G should match explicit palette value."
                );
                Assert.That(
                    result.b,
                    Is.EqualTo(ExplicitAlternateRowColor.b).Within(Tolerance),
                    "Alternate row color B should match explicit palette value."
                );
            }
        }

        [Test]
        public void GetPaletteAlternateRowColorReturnsDerivedWhenPaletteHasNull()
        {
            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                Color result = GroupGUIWidthUtility.GetPaletteAlternateRowColor(
                    FallbackLightAlternate,
                    FallbackDarkAlternate
                );

                Color derived = WGroupColorDerivation.DeriveAlternateRowColor(
                    LightPalette.BackgroundColor
                );

                Assert.That(
                    result.r,
                    Is.EqualTo(derived.r).Within(Tolerance),
                    "Alternate row color R should match derived color."
                );
                Assert.That(
                    result.g,
                    Is.EqualTo(derived.g).Within(Tolerance),
                    "Alternate row color G should match derived color."
                );
                Assert.That(
                    result.b,
                    Is.EqualTo(derived.b).Within(Tolerance),
                    "Alternate row color B should match derived color."
                );
            }
        }

        [Test]
        public void GetPaletteSelectionColorReturnsExplicitWhenPaletteHasColor()
        {
            UnityHelpersSettings.WGroupPaletteEntry palette = new(
                new Color(0.5f, 0.5f, 0.5f, 1f),
                Color.white,
                selectionColor: ExplicitSelectionColor
            );

            using (GroupGUIWidthUtility.PushWGroupPalette(palette))
            {
                Color result = GroupGUIWidthUtility.GetPaletteSelectionColor(
                    FallbackLightSelection,
                    FallbackDarkSelection
                );

                Assert.That(
                    result.r,
                    Is.EqualTo(ExplicitSelectionColor.r).Within(Tolerance),
                    "Selection color R should match explicit palette value."
                );
                Assert.That(
                    result.g,
                    Is.EqualTo(ExplicitSelectionColor.g).Within(Tolerance),
                    "Selection color G should match explicit palette value."
                );
                Assert.That(
                    result.b,
                    Is.EqualTo(ExplicitSelectionColor.b).Within(Tolerance),
                    "Selection color B should match explicit palette value."
                );
                Assert.That(
                    result.a,
                    Is.EqualTo(ExplicitSelectionColor.a).Within(Tolerance),
                    "Selection color A should match explicit palette value."
                );
            }
        }

        [Test]
        public void GetPaletteSelectionColorReturnsDerivedWhenPaletteHasNull()
        {
            using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
            {
                Color result = GroupGUIWidthUtility.GetPaletteSelectionColor(
                    FallbackLightSelection,
                    FallbackDarkSelection
                );

                Color derived = WGroupColorDerivation.DeriveSelectionColor(
                    DarkPalette.BackgroundColor
                );

                Assert.That(
                    result.r,
                    Is.EqualTo(derived.r).Within(Tolerance),
                    "Selection color R should match derived color."
                );
                Assert.That(
                    result.g,
                    Is.EqualTo(derived.g).Within(Tolerance),
                    "Selection color G should match derived color."
                );
                Assert.That(
                    result.b,
                    Is.EqualTo(derived.b).Within(Tolerance),
                    "Selection color B should match derived color."
                );
            }
        }

        [Test]
        public void GetPaletteBorderColorReturnsExplicitWhenPaletteHasColor()
        {
            UnityHelpersSettings.WGroupPaletteEntry palette = new(
                new Color(0.5f, 0.5f, 0.5f, 1f),
                Color.white,
                borderColor: ExplicitBorderColor
            );

            using (GroupGUIWidthUtility.PushWGroupPalette(palette))
            {
                Color result = GroupGUIWidthUtility.GetPaletteBorderColor(
                    FallbackLightBorder,
                    FallbackDarkBorder
                );

                Assert.That(
                    result.r,
                    Is.EqualTo(ExplicitBorderColor.r).Within(Tolerance),
                    "Border color R should match explicit palette value."
                );
                Assert.That(
                    result.g,
                    Is.EqualTo(ExplicitBorderColor.g).Within(Tolerance),
                    "Border color G should match explicit palette value."
                );
                Assert.That(
                    result.b,
                    Is.EqualTo(ExplicitBorderColor.b).Within(Tolerance),
                    "Border color B should match explicit palette value."
                );
            }
        }

        [Test]
        public void GetPaletteBorderColorReturnsDerivedWhenPaletteHasNull()
        {
            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                Color result = GroupGUIWidthUtility.GetPaletteBorderColor(
                    FallbackLightBorder,
                    FallbackDarkBorder
                );

                Color derived = WGroupColorDerivation.DeriveBorderColor(
                    LightPalette.BackgroundColor
                );

                Assert.That(
                    result.r,
                    Is.EqualTo(derived.r).Within(Tolerance),
                    "Border color R should match derived color."
                );
                Assert.That(
                    result.g,
                    Is.EqualTo(derived.g).Within(Tolerance),
                    "Border color G should match derived color."
                );
                Assert.That(
                    result.b,
                    Is.EqualTo(derived.b).Within(Tolerance),
                    "Border color B should match derived color."
                );
            }
        }

        [Test]
        public void GetPalettePendingBackgroundColorReturnsExplicitWhenPaletteHasColor()
        {
            UnityHelpersSettings.WGroupPaletteEntry palette = new(
                new Color(0.5f, 0.5f, 0.5f, 1f),
                Color.white,
                pendingBackgroundColor: ExplicitPendingColor
            );

            using (GroupGUIWidthUtility.PushWGroupPalette(palette))
            {
                Color result = GroupGUIWidthUtility.GetPalettePendingBackgroundColor(
                    FallbackLightPending,
                    FallbackDarkPending
                );

                Assert.That(
                    result.r,
                    Is.EqualTo(ExplicitPendingColor.r).Within(Tolerance),
                    "Pending background color R should match explicit palette value."
                );
                Assert.That(
                    result.g,
                    Is.EqualTo(ExplicitPendingColor.g).Within(Tolerance),
                    "Pending background color G should match explicit palette value."
                );
                Assert.That(
                    result.b,
                    Is.EqualTo(ExplicitPendingColor.b).Within(Tolerance),
                    "Pending background color B should match explicit palette value."
                );
            }
        }

        [Test]
        public void GetPalettePendingBackgroundColorReturnsDerivedWhenPaletteHasNull()
        {
            using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
            {
                Color result = GroupGUIWidthUtility.GetPalettePendingBackgroundColor(
                    FallbackLightPending,
                    FallbackDarkPending
                );

                Color derived = WGroupColorDerivation.DerivePendingBackgroundColor(
                    DarkPalette.BackgroundColor
                );

                Assert.That(
                    result.r,
                    Is.EqualTo(derived.r).Within(Tolerance),
                    "Pending background color R should match derived color."
                );
                Assert.That(
                    result.g,
                    Is.EqualTo(derived.g).Within(Tolerance),
                    "Pending background color G should match derived color."
                );
                Assert.That(
                    result.b,
                    Is.EqualTo(derived.b).Within(Tolerance),
                    "Pending background color B should match derived color."
                );
            }
        }

        // =====================================================================
        // 4. Default-Dark Theme Regression Tests
        // =====================================================================

        [Test]
        public void DefaultDarkPaletteDerivesReadableRowColor()
        {
            // This is the critical fix verification: dark palette should derive
            // a lightened row color, not a dark one that would be unreadable

            using (GroupGUIWidthUtility.PushWGroupPalette(DefaultDarkPalette))
            {
                Color derivedRow = GroupGUIWidthUtility.GetPaletteRowColor(
                    FallbackLightRow,
                    FallbackDarkRow
                );

                // For dark backgrounds, row should be LIGHTENED, not darkened
                float backgroundLuminance = WGroupColorDerivation.GetLuminance(
                    DefaultDarkPalette.BackgroundColor
                );
                float rowLuminance = WGroupColorDerivation.GetLuminance(derivedRow);

                Assert.That(
                    backgroundLuminance,
                    Is.LessThanOrEqualTo(0.5f),
                    "Default dark palette should have dark background (luminance <= 0.5)."
                );

                Assert.That(
                    rowLuminance,
                    Is.GreaterThan(backgroundLuminance),
                    "Derived row color should be lighter than background for dark palettes."
                );
            }
        }

        [Test]
        public void DefaultDarkPaletteDerivedRowColorIsLighterThanHardCodedDarkRowColor()
        {
            // The old hard-coded dark row color was too dark (0.16, 0.16, 0.16, 0.45)
            // The derived color should be lighter for better contrast

            Color oldHardCodedDarkRow = new(0.16f, 0.16f, 0.16f, 0.45f);

            using (GroupGUIWidthUtility.PushWGroupPalette(DefaultDarkPalette))
            {
                Color derivedRow = GroupGUIWidthUtility.GetPaletteRowColor(
                    FallbackLightRow,
                    oldHardCodedDarkRow
                );

                float oldRowLuminance =
                    (0.299f * oldHardCodedDarkRow.r)
                    + (0.587f * oldHardCodedDarkRow.g)
                    + (0.114f * oldHardCodedDarkRow.b);

                float derivedRowLuminance = WGroupColorDerivation.GetLuminance(derivedRow);

                Assert.That(
                    derivedRowLuminance,
                    Is.GreaterThan(oldRowLuminance),
                    "Derived row color should be lighter than old hard-coded dark row color (0.16)."
                );
            }
        }

        [Test]
        public void DefaultDarkPaletteDerivedColorsHaveGoodContrast()
        {
            using (GroupGUIWidthUtility.PushWGroupPalette(DefaultDarkPalette))
            {
                Color rowColor = GroupGUIWidthUtility.GetPaletteRowColor(
                    FallbackLightRow,
                    FallbackDarkRow
                );
                Color alternateRowColor = GroupGUIWidthUtility.GetPaletteAlternateRowColor(
                    FallbackLightAlternate,
                    FallbackDarkAlternate
                );

                float rowLuminance = WGroupColorDerivation.GetLuminance(rowColor);
                float alternateLuminance = WGroupColorDerivation.GetLuminance(alternateRowColor);

                // Alternate row should be different from row for visual distinction
                float contrastDifference = Mathf.Abs(alternateLuminance - rowLuminance);

                Assert.That(
                    contrastDifference,
                    Is.GreaterThan(0.01f),
                    "Row and alternate row should have visible contrast difference."
                );

                // Alternate should be lighter than row for dark backgrounds
                Assert.That(
                    alternateLuminance,
                    Is.GreaterThan(rowLuminance),
                    "Alternate row should be lighter than row for dark palettes."
                );
            }
        }

        // =====================================================================
        // 5. Edge Case Tests
        // =====================================================================

        [Test]
        public void NullPaletteDoesNotThrow()
        {
            GroupGUIWidthUtility.ResetForTests();

            Assert.That(GroupGUIWidthUtility.CurrentPalette, Is.Null, "Palette should be null.");

            Assert.DoesNotThrow(
                () =>
                {
                    Color row = GroupGUIWidthUtility.GetPaletteRowColor(
                        FallbackLightRow,
                        FallbackDarkRow
                    );
                    Color alternate = GroupGUIWidthUtility.GetPaletteAlternateRowColor(
                        FallbackLightAlternate,
                        FallbackDarkAlternate
                    );
                    Color selection = GroupGUIWidthUtility.GetPaletteSelectionColor(
                        FallbackLightSelection,
                        FallbackDarkSelection
                    );
                    Color border = GroupGUIWidthUtility.GetPaletteBorderColor(
                        FallbackLightBorder,
                        FallbackDarkBorder
                    );
                    Color pending = GroupGUIWidthUtility.GetPalettePendingBackgroundColor(
                        FallbackLightPending,
                        FallbackDarkPending
                    );
                },
                "Getting palette colors with null palette should not throw."
            );
        }

        [Test]
        public void EmptyPaletteWithOnlyBackgroundColorWorks()
        {
            // Palette with only background and text color, no collection colors
            UnityHelpersSettings.WGroupPaletteEntry minimalPalette = new(
                new Color(0.4f, 0.4f, 0.4f, 1f),
                Color.white
            );

            using (GroupGUIWidthUtility.PushWGroupPalette(minimalPalette))
            {
                Assert.DoesNotThrow(
                    () =>
                    {
                        Color row = GroupGUIWidthUtility.GetPaletteRowColor(
                            FallbackLightRow,
                            FallbackDarkRow
                        );
                        Color alternate = GroupGUIWidthUtility.GetPaletteAlternateRowColor(
                            FallbackLightAlternate,
                            FallbackDarkAlternate
                        );
                        Color selection = GroupGUIWidthUtility.GetPaletteSelectionColor(
                            FallbackLightSelection,
                            FallbackDarkSelection
                        );
                        Color border = GroupGUIWidthUtility.GetPaletteBorderColor(
                            FallbackLightBorder,
                            FallbackDarkBorder
                        );
                        Color pending = GroupGUIWidthUtility.GetPalettePendingBackgroundColor(
                            FallbackLightPending,
                            FallbackDarkPending
                        );
                    },
                    "Getting palette colors with minimal palette should not throw."
                );

                // All should derive from background
                Color derivedRow = WGroupColorDerivation.DeriveRowColor(
                    minimalPalette.BackgroundColor
                );
                Color actualRow = GroupGUIWidthUtility.GetPaletteRowColor(
                    FallbackLightRow,
                    FallbackDarkRow
                );

                Assert.That(
                    actualRow.r,
                    Is.EqualTo(derivedRow.r).Within(Tolerance),
                    "Minimal palette should derive row color from background."
                );
            }
        }

        [Test]
        public void PaletteWithOnlySomeCustomColorsWorksMixed()
        {
            // Palette with some explicit colors, some null (derived)
            UnityHelpersSettings.WGroupPaletteEntry mixedPalette = new(
                new Color(0.6f, 0.6f, 0.6f, 1f),
                Color.black,
                rowColor: ExplicitRowColor, // Explicit
                alternateRowColor: null, // Will derive
                selectionColor: ExplicitSelectionColor, // Explicit
                borderColor: null, // Will derive
                pendingBackgroundColor: null // Will derive
            );

            using (GroupGUIWidthUtility.PushWGroupPalette(mixedPalette))
            {
                // Explicit colors should match
                Color rowColor = GroupGUIWidthUtility.GetPaletteRowColor(
                    FallbackLightRow,
                    FallbackDarkRow
                );
                Assert.That(
                    rowColor.r,
                    Is.EqualTo(ExplicitRowColor.r).Within(Tolerance),
                    "Explicit row color should be used."
                );

                Color selectionColor = GroupGUIWidthUtility.GetPaletteSelectionColor(
                    FallbackLightSelection,
                    FallbackDarkSelection
                );
                Assert.That(
                    selectionColor.r,
                    Is.EqualTo(ExplicitSelectionColor.r).Within(Tolerance),
                    "Explicit selection color should be used."
                );

                // Null colors should derive
                Color alternateRowColor = GroupGUIWidthUtility.GetPaletteAlternateRowColor(
                    FallbackLightAlternate,
                    FallbackDarkAlternate
                );
                Color derivedAlternate = WGroupColorDerivation.DeriveAlternateRowColor(
                    mixedPalette.BackgroundColor
                );
                Assert.That(
                    alternateRowColor.r,
                    Is.EqualTo(derivedAlternate.r).Within(Tolerance),
                    "Null alternate row color should derive from background."
                );

                Color borderColor = GroupGUIWidthUtility.GetPaletteBorderColor(
                    FallbackLightBorder,
                    FallbackDarkBorder
                );
                Color derivedBorder = WGroupColorDerivation.DeriveBorderColor(
                    mixedPalette.BackgroundColor
                );
                Assert.That(
                    borderColor.r,
                    Is.EqualTo(derivedBorder.r).Within(Tolerance),
                    "Null border color should derive from background."
                );

                Color pendingColor = GroupGUIWidthUtility.GetPalettePendingBackgroundColor(
                    FallbackLightPending,
                    FallbackDarkPending
                );
                Color derivedPending = WGroupColorDerivation.DerivePendingBackgroundColor(
                    mixedPalette.BackgroundColor
                );
                Assert.That(
                    pendingColor.r,
                    Is.EqualTo(derivedPending.r).Within(Tolerance),
                    "Null pending color should derive from background."
                );
            }
        }

        // =====================================================================
        // 6. Integration Context Tests
        // =====================================================================

        [Test]
        public void NestedPaletteContextUsesInnermostPalette()
        {
            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                Color outerRow = GroupGUIWidthUtility.GetPaletteRowColor(
                    FallbackLightRow,
                    FallbackDarkRow
                );
                Color expectedOuterRow = WGroupColorDerivation.DeriveRowColor(
                    LightPalette.BackgroundColor
                );

                Assert.That(
                    outerRow.r,
                    Is.EqualTo(expectedOuterRow.r).Within(Tolerance),
                    "Outer scope should use light palette."
                );

                using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
                {
                    Color innerRow = GroupGUIWidthUtility.GetPaletteRowColor(
                        FallbackLightRow,
                        FallbackDarkRow
                    );
                    Color expectedInnerRow = WGroupColorDerivation.DeriveRowColor(
                        DarkPalette.BackgroundColor
                    );

                    Assert.That(
                        innerRow.r,
                        Is.EqualTo(expectedInnerRow.r).Within(Tolerance),
                        "Inner scope should use dark palette."
                    );
                }

                Color afterInnerRow = GroupGUIWidthUtility.GetPaletteRowColor(
                    FallbackLightRow,
                    FallbackDarkRow
                );
                Assert.That(
                    afterInnerRow.r,
                    Is.EqualTo(expectedOuterRow.r).Within(Tolerance),
                    "After inner scope, should restore to light palette."
                );
            }
        }

        [Test]
        public void PaletteContextRestoresAfterDispose()
        {
            Color beforeRow = GroupGUIWidthUtility.GetPaletteRowColor(
                FallbackLightRow,
                FallbackDarkRow
            );

            using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
            {
                Color duringRow = GroupGUIWidthUtility.GetPaletteRowColor(
                    FallbackLightRow,
                    FallbackDarkRow
                );

                // During scope, should get derived color
                Color expectedDerived = WGroupColorDerivation.DeriveRowColor(
                    DarkPalette.BackgroundColor
                );
                Assert.That(
                    duringRow.r,
                    Is.EqualTo(expectedDerived.r).Within(Tolerance),
                    "During scope should use palette-derived color."
                );
            }

            Color afterRow = GroupGUIWidthUtility.GetPaletteRowColor(
                FallbackLightRow,
                FallbackDarkRow
            );

            Assert.That(
                afterRow.r,
                Is.EqualTo(beforeRow.r).Within(Tolerance),
                "After scope dispose, should return to previous state."
            );
        }

        [Test]
        public void IsInsideWGroupReturnsTrueWhenPaletteActive()
        {
            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroup,
                Is.False,
                "Should not be inside WGroup before push."
            );

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroup,
                    Is.True,
                    "Should be inside WGroup after push."
                );
            }

            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroup,
                Is.False,
                "Should not be inside WGroup after dispose."
            );
        }

        [Test]
        public void CurrentPaletteReturnsCorrectPaletteInScope()
        {
            Assert.That(
                GroupGUIWidthUtility.CurrentPalette,
                Is.Null,
                "CurrentPalette should be null before push."
            );

            using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
            {
                Assert.That(
                    GroupGUIWidthUtility.CurrentPalette,
                    Is.Not.Null,
                    "CurrentPalette should not be null inside scope."
                );

                Assert.That(
                    GroupGUIWidthUtility.CurrentPalette.Value.BackgroundColor,
                    Is.EqualTo(DarkPalette.BackgroundColor),
                    "CurrentPalette should return the pushed palette's background color."
                );

                Assert.That(
                    GroupGUIWidthUtility.CurrentPalette.Value.TextColor,
                    Is.EqualTo(DarkPalette.TextColor),
                    "CurrentPalette should return the pushed palette's text color."
                );
            }

            Assert.That(
                GroupGUIWidthUtility.CurrentPalette,
                Is.Null,
                "CurrentPalette should be null after scope dispose."
            );
        }
    }
}
#endif
