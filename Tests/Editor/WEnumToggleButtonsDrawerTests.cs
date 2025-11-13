namespace WallstopStudios.UnityHelpers.Tests.Editor
{
#if UNITY_EDITOR
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Editor.Settings;

    [TestFixture]
    public sealed class WEnumToggleButtonsDrawerTests
    {
        [Test]
        public void DrawPaginationPreservesGuiEnabledState()
        {
            WEnumToggleButtonsPagination.PaginationState state =
                new WEnumToggleButtonsPagination.PaginationState
                {
                    PageSize = 5,
                    TotalItems = 10,
                    PageIndex = 0,
                };

            Rect rect = new Rect(0f, 0f, 200f, EditorGUIUtility.singleLineHeight);

            bool originalEnabled = GUI.enabled;
            GUI.enabled = true;

            WEnumToggleButtonsDrawer.DrawPagination(rect, state);

            try
            {
                Assert.IsTrue(
                    GUI.enabled,
                    "DrawPagination leaked GUI.enabled=false to subsequent controls."
                );
            }
            finally
            {
                GUI.enabled = originalEnabled;
            }
        }

        [Test]
        public void ResolveWEnumToggleButtonsPaletteReturnsExpectedDefaults()
        {
            Color expectedSelected = new Color(0.243f, 0.525f, 0.988f, 1f);
            Color expectedLightInactive = new Color(0.78f, 0.78f, 0.78f, 1f);
            Color expectedDarkInactive = new Color(0.35f, 0.35f, 0.35f, 1f);

            UnityHelpersSettings.WEnumToggleButtonsPaletteEntry lightPalette =
                UnityHelpersSettings.ResolveWEnumToggleButtonsPalette(
                    UnityHelpersSettings.WEnumToggleButtonsLightThemeColorKey
                );
            UnityHelpersSettings.WEnumToggleButtonsPaletteEntry darkPalette =
                UnityHelpersSettings.ResolveWEnumToggleButtonsPalette(
                    UnityHelpersSettings.WEnumToggleButtonsDarkThemeColorKey
                );
            UnityHelpersSettings.WEnumToggleButtonsPaletteEntry defaultPalette =
                UnityHelpersSettings.ResolveWEnumToggleButtonsPalette(
                    UnityHelpersSettings.DefaultWEnumToggleButtonsColorKey
                );

            AssertColorApproximately(expectedSelected, lightPalette.SelectedBackgroundColor);
            AssertColorApproximately(expectedLightInactive, lightPalette.InactiveBackgroundColor);
            AssertColorApproximately(Color.white, lightPalette.SelectedTextColor);
            AssertColorApproximately(Color.black, lightPalette.InactiveTextColor);

            AssertColorApproximately(expectedSelected, darkPalette.SelectedBackgroundColor);
            AssertColorApproximately(expectedDarkInactive, darkPalette.InactiveBackgroundColor);
            AssertColorApproximately(Color.white, darkPalette.SelectedTextColor);
            AssertColorApproximately(Color.white, darkPalette.InactiveTextColor);

            UnityHelpersSettings.WEnumToggleButtonsPaletteEntry expectedDefaultPalette =
                EditorGUIUtility.isProSkin ? darkPalette : lightPalette;

            AssertColorApproximately(
                expectedDefaultPalette.SelectedBackgroundColor,
                defaultPalette.SelectedBackgroundColor
            );
            AssertColorApproximately(
                expectedDefaultPalette.SelectedTextColor,
                defaultPalette.SelectedTextColor
            );
            AssertColorApproximately(
                expectedDefaultPalette.InactiveBackgroundColor,
                defaultPalette.InactiveBackgroundColor
            );
            AssertColorApproximately(
                expectedDefaultPalette.InactiveTextColor,
                defaultPalette.InactiveTextColor
            );
        }

        private static void AssertColorApproximately(Color expected, Color actual)
        {
            const float Tolerance = 0.0001f;
            Assert.AreEqual(expected.r, actual.r, Tolerance, "Unexpected red channel.");
            Assert.AreEqual(expected.g, actual.g, Tolerance, "Unexpected green channel.");
            Assert.AreEqual(expected.b, actual.b, Tolerance, "Unexpected blue channel.");
            Assert.AreEqual(expected.a, actual.a, Tolerance, "Unexpected alpha channel.");
        }
    }
#endif
}
