namespace WallstopStudios.UnityHelpers.Tests.Utils
{
#if UNITY_EDITOR
    using System;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Tests.Core;

    /// <summary>
    /// Tests for the WGroup palette stack functionality in GroupGUIWidthUtility.
    /// Verifies that palettes are properly tracked on a stack so child drawers
    /// (like SerializableDictionary/Set) can query the current WGroup's palette
    /// for consistent theming.
    /// </summary>
    [TestFixture]
    public sealed class WGroupPaletteStackTests : CommonTestBase
    {
        private static readonly UnityHelpersSettings.WGroupPaletteEntry LightPalette = new(
            new Color(0.82f, 0.82f, 0.82f, 1f),
            Color.black
        );

        private static readonly UnityHelpersSettings.WGroupPaletteEntry DarkPalette = new(
            new Color(0.215f, 0.215f, 0.215f, 1f),
            Color.white
        );

        private static readonly UnityHelpersSettings.WGroupPaletteEntry NeonPalette = new(
            new Color(0.0f, 1.0f, 0.5f, 1f),
            Color.black
        );

        private static readonly UnityHelpersSettings.WGroupPaletteEntry VeryDarkPalette = new(
            new Color(0.1f, 0.1f, 0.1f, 1f),
            Color.white
        );

        private static readonly Color LightRowColor = new(0.97f, 0.97f, 0.97f, 1f);
        private static readonly Color DarkRowColor = new(0.16f, 0.16f, 0.16f, 0.45f);
        private static readonly Color LightSelectionColor = new(0.33f, 0.62f, 0.95f, 0.65f);
        private static readonly Color DarkSelectionColor = new(0.2f, 0.45f, 0.85f, 0.7f);
        private static readonly Color LightBorderColor = new(0.7f, 0.7f, 0.7f, 1f);
        private static readonly Color DarkBorderColor = new(0.25f, 0.25f, 0.25f, 1f);
        private static readonly Color LightPendingBackground = new(0.92f, 0.92f, 0.92f, 1f);
        private static readonly Color DarkPendingBackground = new(0.18f, 0.18f, 0.18f, 1f);

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            GroupGUIWidthUtility.ResetForTests();
        }

        [TearDown]
        public override void TearDown()
        {
            GroupGUIWidthUtility.ResetForTests();
            base.TearDown();
        }

        [Test]
        public void CurrentPaletteDefaultValueIsNull()
        {
            GroupGUIWidthUtility.ResetForTests();

            UnityHelpersSettings.WGroupPaletteEntry? currentPalette =
                GroupGUIWidthUtility.CurrentPalette;

            Assert.That(
                currentPalette,
                Is.Null,
                "CurrentPalette should default to null after reset."
            );
        }

        [Test]
        public void CurrentPaletteReturnsPaletteAfterPush()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                UnityHelpersSettings.WGroupPaletteEntry? currentPalette =
                    GroupGUIWidthUtility.CurrentPalette;

                Assert.That(
                    currentPalette,
                    Is.Not.Null,
                    "CurrentPalette should not be null after push."
                );
                Assert.That(
                    currentPalette.Value.BackgroundColor,
                    Is.EqualTo(LightPalette.BackgroundColor),
                    "CurrentPalette should have the pushed palette's background color."
                );
                Assert.That(
                    currentPalette.Value.TextColor,
                    Is.EqualTo(LightPalette.TextColor),
                    "CurrentPalette should have the pushed palette's text color."
                );
            }
        }

        [Test]
        public void CurrentPaletteReturnsToNullAfterScopeDisposed()
        {
            GroupGUIWidthUtility.ResetForTests();

            Assert.That(
                GroupGUIWidthUtility.CurrentPalette,
                Is.Null,
                "CurrentPalette should be null before scope."
            );

            using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
            {
                Assert.That(
                    GroupGUIWidthUtility.CurrentPalette,
                    Is.Not.Null,
                    "CurrentPalette should not be null inside scope."
                );
            }

            Assert.That(
                GroupGUIWidthUtility.CurrentPalette,
                Is.Null,
                "CurrentPalette should return to null after scope is disposed."
            );
        }

        [Test]
        public void NestedPalettesScopesProperlySaveAndRestore()
        {
            GroupGUIWidthUtility.ResetForTests();

            Assert.That(GroupGUIWidthUtility.CurrentPalette, Is.Null, "Should be null initially.");

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                Assert.That(
                    GroupGUIWidthUtility.CurrentPalette.Value.BackgroundColor,
                    Is.EqualTo(LightPalette.BackgroundColor),
                    "Should be LightPalette in first scope."
                );

                using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
                {
                    Assert.That(
                        GroupGUIWidthUtility.CurrentPalette.Value.BackgroundColor,
                        Is.EqualTo(DarkPalette.BackgroundColor),
                        "Should be DarkPalette in nested scope."
                    );

                    using (GroupGUIWidthUtility.PushWGroupPalette(NeonPalette))
                    {
                        Assert.That(
                            GroupGUIWidthUtility.CurrentPalette.Value.BackgroundColor,
                            Is.EqualTo(NeonPalette.BackgroundColor),
                            "Should be NeonPalette in deeply nested scope."
                        );
                    }

                    Assert.That(
                        GroupGUIWidthUtility.CurrentPalette.Value.BackgroundColor,
                        Is.EqualTo(DarkPalette.BackgroundColor),
                        "Should return to DarkPalette after innermost scope disposes."
                    );
                }

                Assert.That(
                    GroupGUIWidthUtility.CurrentPalette.Value.BackgroundColor,
                    Is.EqualTo(LightPalette.BackgroundColor),
                    "Should return to LightPalette after middle scope disposes."
                );
            }

            Assert.That(
                GroupGUIWidthUtility.CurrentPalette,
                Is.Null,
                "Should return to null after all scopes disposed."
            );
        }

        [Test]
        public void ResetForTestsResetsPaletteStack()
        {
            using (GroupGUIWidthUtility.PushWGroupPalette(NeonPalette))
            {
                Assert.That(
                    GroupGUIWidthUtility.CurrentPalette,
                    Is.Not.Null,
                    "Palette should be set inside scope."
                );

                GroupGUIWidthUtility.ResetForTests();

                Assert.That(
                    GroupGUIWidthUtility.CurrentPalette,
                    Is.Null,
                    "ResetForTests should reset CurrentPalette to null."
                );
            }
        }

        [Test]
        public void PaletteScopeIsIdempotentOnMultipleDispose()
        {
            GroupGUIWidthUtility.ResetForTests();

            IDisposable scope = GroupGUIWidthUtility.PushWGroupPalette(LightPalette);

            Assert.That(
                GroupGUIWidthUtility.CurrentPalette,
                Is.Not.Null,
                "Should have palette after push."
            );

            scope.Dispose();

            Assert.That(
                GroupGUIWidthUtility.CurrentPalette,
                Is.Null,
                "Should be null after first dispose."
            );

            scope.Dispose();

            Assert.That(
                GroupGUIWidthUtility.CurrentPalette,
                Is.Null,
                "Should remain null after second dispose (idempotent)."
            );
        }

        [Test]
        public void PaletteScopeRestoresPreviousValueOnException()
        {
            GroupGUIWidthUtility.ResetForTests();

            try
            {
                using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
                {
                    Assert.That(
                        GroupGUIWidthUtility.CurrentPalette,
                        Is.Not.Null,
                        "Should have palette before exception."
                    );
                    throw new InvalidOperationException("Test exception");
                }
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            Assert.That(
                GroupGUIWidthUtility.CurrentPalette,
                Is.Null,
                "Should restore to null after scope disposed due to exception."
            );
        }

        [Test]
        public void IsInsideLightPaletteWGroupReturnsFalseWhenNoPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            bool result = GroupGUIWidthUtility.IsInsideLightPaletteWGroup();

            Assert.That(
                result,
                Is.False,
                "IsInsideLightPaletteWGroup should return false when no palette is set."
            );
        }

        [Test]
        public void IsInsideLightPaletteWGroupReturnsTrueForLightPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                bool result = GroupGUIWidthUtility.IsInsideLightPaletteWGroup();

                Assert.That(
                    result,
                    Is.True,
                    "IsInsideLightPaletteWGroup should return true for light palette (luminance > 0.5)."
                );
            }
        }

        [Test]
        public void IsInsideLightPaletteWGroupReturnsFalseForDarkPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
            {
                bool result = GroupGUIWidthUtility.IsInsideLightPaletteWGroup();

                Assert.That(
                    result,
                    Is.False,
                    "IsInsideLightPaletteWGroup should return false for dark palette (luminance <= 0.5)."
                );
            }
        }

        [Test]
        public void IsInsideDarkPaletteWGroupReturnsFalseWhenNoPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            bool result = GroupGUIWidthUtility.IsInsideDarkPaletteWGroup();

            Assert.That(
                result,
                Is.False,
                "IsInsideDarkPaletteWGroup should return false when no palette is set."
            );
        }

        [Test]
        public void IsInsideDarkPaletteWGroupReturnsTrueForDarkPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(VeryDarkPalette))
            {
                bool result = GroupGUIWidthUtility.IsInsideDarkPaletteWGroup();

                Assert.That(
                    result,
                    Is.True,
                    "IsInsideDarkPaletteWGroup should return true for dark palette (luminance <= 0.5)."
                );
            }
        }

        [Test]
        public void IsInsideDarkPaletteWGroupReturnsFalseForLightPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                bool result = GroupGUIWidthUtility.IsInsideDarkPaletteWGroup();

                Assert.That(
                    result,
                    Is.False,
                    "IsInsideDarkPaletteWGroup should return false for light palette (luminance > 0.5)."
                );
            }
        }

        [Test]
        public void GetThemedRowColorReturnsEditorThemeColorWhenNoPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            Color result = GroupGUIWidthUtility.GetThemedRowColor(LightRowColor, DarkRowColor);

            Color expected = EditorGUIUtility.isProSkin ? DarkRowColor : LightRowColor;
            Assert.That(
                result,
                Is.EqualTo(expected),
                "GetThemedRowColor should use editor skin when no palette is set."
            );
        }

        [Test]
        public void GetThemedRowColorReturnsLightColorForLightPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                Color result = GroupGUIWidthUtility.GetThemedRowColor(LightRowColor, DarkRowColor);

                Assert.That(
                    result,
                    Is.EqualTo(LightRowColor),
                    "GetThemedRowColor should return light row color for light palette."
                );
            }
        }

        [Test]
        public void GetThemedRowColorReturnsDarkColorForDarkPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
            {
                Color result = GroupGUIWidthUtility.GetThemedRowColor(LightRowColor, DarkRowColor);

                Assert.That(
                    result,
                    Is.EqualTo(DarkRowColor),
                    "GetThemedRowColor should return dark row color for dark palette."
                );
            }
        }

        [Test]
        public void GetThemedSelectionColorReturnsLightColorForLightPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                Color result = GroupGUIWidthUtility.GetThemedSelectionColor(
                    LightSelectionColor,
                    DarkSelectionColor
                );

                Assert.That(
                    result,
                    Is.EqualTo(LightSelectionColor),
                    "GetThemedSelectionColor should return light selection color for light palette."
                );
            }
        }

        [Test]
        public void GetThemedSelectionColorReturnsDarkColorForDarkPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
            {
                Color result = GroupGUIWidthUtility.GetThemedSelectionColor(
                    LightSelectionColor,
                    DarkSelectionColor
                );

                Assert.That(
                    result,
                    Is.EqualTo(DarkSelectionColor),
                    "GetThemedSelectionColor should return dark selection color for dark palette."
                );
            }
        }

        [Test]
        public void GetThemedBorderColorReturnsLightColorForLightPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                Color result = GroupGUIWidthUtility.GetThemedBorderColor(
                    LightBorderColor,
                    DarkBorderColor
                );

                Assert.That(
                    result,
                    Is.EqualTo(LightBorderColor),
                    "GetThemedBorderColor should return light border color for light palette."
                );
            }
        }

        [Test]
        public void GetThemedBorderColorReturnsDarkColorForDarkPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(VeryDarkPalette))
            {
                Color result = GroupGUIWidthUtility.GetThemedBorderColor(
                    LightBorderColor,
                    DarkBorderColor
                );

                Assert.That(
                    result,
                    Is.EqualTo(DarkBorderColor),
                    "GetThemedBorderColor should return dark border color for dark palette."
                );
            }
        }

        [Test]
        public void GetThemedPendingBackgroundColorReturnsLightColorForLightPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                Color result = GroupGUIWidthUtility.GetThemedPendingBackgroundColor(
                    LightPendingBackground,
                    DarkPendingBackground
                );

                Assert.That(
                    result,
                    Is.EqualTo(LightPendingBackground),
                    "GetThemedPendingBackgroundColor should return light background for light palette."
                );
            }
        }

        [Test]
        public void GetThemedPendingBackgroundColorReturnsDarkColorForDarkPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
            {
                Color result = GroupGUIWidthUtility.GetThemedPendingBackgroundColor(
                    LightPendingBackground,
                    DarkPendingBackground
                );

                Assert.That(
                    result,
                    Is.EqualTo(DarkPendingBackground),
                    "GetThemedPendingBackgroundColor should return dark background for dark palette."
                );
            }
        }

        [Test]
        public void ShouldUseLightThemeStylingReturnsTrueForLightPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                bool result = GroupGUIWidthUtility.ShouldUseLightThemeStyling();

                Assert.That(
                    result,
                    Is.True,
                    "ShouldUseLightThemeStyling should return true for light palette."
                );
            }
        }

        [Test]
        public void ShouldUseLightThemeStylingReturnsFalseForDarkPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
            {
                bool result = GroupGUIWidthUtility.ShouldUseLightThemeStyling();

                Assert.That(
                    result,
                    Is.False,
                    "ShouldUseLightThemeStyling should return false for dark palette."
                );
            }
        }

        [Test]
        public void ShouldUseLightThemeStylingFollowsEditorThemeWhenNoPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            bool result = GroupGUIWidthUtility.ShouldUseLightThemeStyling();

            bool expected = !EditorGUIUtility.isProSkin;
            Assert.That(
                result,
                Is.EqualTo(expected),
                "ShouldUseLightThemeStyling should follow editor theme when no palette is set."
            );
        }

        [Test]
        public void NeonPaletteIsConsideredLightPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPalette(NeonPalette))
            {
                bool isLight = GroupGUIWidthUtility.IsInsideLightPaletteWGroup();

                Assert.That(
                    isLight,
                    Is.True,
                    "Neon palette (green: 0, 1, 0.5) should be considered light (luminance > 0.5)."
                );

                Color rowColor = GroupGUIWidthUtility.GetThemedRowColor(
                    LightRowColor,
                    DarkRowColor
                );
                Assert.That(
                    rowColor,
                    Is.EqualTo(LightRowColor),
                    "Neon palette should use light row color."
                );
            }
        }

        [Test]
        public void PaletteStackWorksCombinedWithPropertyContextScope()
        {
            GroupGUIWidthUtility.ResetForTests();

            Assert.That(
                GroupGUIWidthUtility.CurrentPalette,
                Is.Null,
                "Palette should be null initially."
            );
            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                Is.False,
                "Property context should be false initially."
            );

            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            using (GroupGUIWidthUtility.PushWGroupPropertyContext())
            {
                Assert.That(
                    GroupGUIWidthUtility.CurrentPalette,
                    Is.Not.Null,
                    "Palette should be set inside combined scopes."
                );
                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.True,
                    "Property context should be true inside combined scopes."
                );
            }

            Assert.That(
                GroupGUIWidthUtility.CurrentPalette,
                Is.Null,
                "Palette should be null after combined scopes disposed."
            );
            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                Is.False,
                "Property context should be false after combined scopes disposed."
            );
        }

        [Test]
        public void LuminanceBoundaryAt50Percent()
        {
            GroupGUIWidthUtility.ResetForTests();

            UnityHelpersSettings.WGroupPaletteEntry exactlyHalf = new(
                new Color(0.5f, 0.5f, 0.5f, 1f),
                Color.white
            );

            using (GroupGUIWidthUtility.PushWGroupPalette(exactlyHalf))
            {
                bool isLight = GroupGUIWidthUtility.IsInsideLightPaletteWGroup();
                bool isDark = GroupGUIWidthUtility.IsInsideDarkPaletteWGroup();

                Assert.That(
                    isLight,
                    Is.False,
                    "Exactly 0.5 luminance should NOT be considered light (> 0.5 required)."
                );
                Assert.That(
                    isDark,
                    Is.True,
                    "Exactly 0.5 luminance should be considered dark (<= 0.5)."
                );
            }
        }

        [Test]
        public void SlightlyAboveHalfLuminanceIsLight()
        {
            GroupGUIWidthUtility.ResetForTests();

            UnityHelpersSettings.WGroupPaletteEntry slightlyAboveHalf = new(
                new Color(0.51f, 0.51f, 0.51f, 1f),
                Color.black
            );

            using (GroupGUIWidthUtility.PushWGroupPalette(slightlyAboveHalf))
            {
                bool isLight = GroupGUIWidthUtility.IsInsideLightPaletteWGroup();

                Assert.That(
                    isLight,
                    Is.True,
                    "Luminance slightly above 0.5 should be considered light."
                );
            }
        }
    }
#endif
}
