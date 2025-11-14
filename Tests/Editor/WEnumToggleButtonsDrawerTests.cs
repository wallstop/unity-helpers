namespace WallstopStudios.UnityHelpers.Tests.Editor
{
#if UNITY_EDITOR
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Editor.Settings;

    [TestFixture]
    public sealed class WEnumToggleButtonsDrawerTests
    {
        [SetUp]
        public void SetUp()
        {
            WEnumToggleButtonsPagination.Reset();
        }

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

        [Test]
        public void GetPropertyHeightUsesCachedLayoutWhenAvailable()
        {
            ToggleDropdownAsset asset = ScriptableObject.CreateInstance<ToggleDropdownAsset>();
            try
            {
                SerializedObject serializedObject = new SerializedObject(asset);
                serializedObject.Update();

                SerializedProperty property = serializedObject.FindProperty(
                    nameof(ToggleDropdownAsset.mode)
                );
                Assert.IsNotNull(property, "Failed to locate serialized property for test asset.");

                FieldInfo fieldInfo = typeof(ToggleDropdownAsset).GetField(
                    nameof(ToggleDropdownAsset.mode),
                    BindingFlags.Instance | BindingFlags.Public
                );
                Assert.IsNotNull(fieldInfo, "Failed to locate field info for test asset.");

                WEnumToggleButtonsAttribute toggleAttribute =
                    fieldInfo.GetCustomAttribute<WEnumToggleButtonsAttribute>();
                Assert.IsNotNull(
                    toggleAttribute,
                    "Expected WEnumToggleButtonsAttribute on test field."
                );

                WEnumToggleButtonsDrawer drawer = new WEnumToggleButtonsDrawer();
                ConfigureDrawer(drawer, fieldInfo, toggleAttribute);

                GUIContent label = new GUIContent("Mode");
                float baselineHeight = drawer.GetPropertyHeight(property, label);
                Assert.Greater(baselineHeight, 0f, "Baseline height should be positive.");

                ToggleSet toggleSet = WEnumToggleButtonsUtility.CreateToggleSet(
                    property,
                    fieldInfo
                );
                Assert.IsFalse(
                    toggleSet.IsEmpty,
                    "Toggle set should contain options for the test."
                );

                bool usePagination = WEnumToggleButtonsUtility.ShouldPaginate(
                    toggleAttribute,
                    toggleSet.Options.Count,
                    out int pageSize
                );
                int visibleCount = usePagination
                    ? WEnumToggleButtonsPagination
                        .GetState(property, toggleSet.Options.Count, pageSize)
                        .VisibleCount
                    : toggleSet.Options.Count;

                LayoutSignature signature = WEnumToggleButtonsLayoutCache.CreateSignature(
                    toggleSet.Options.Count,
                    visibleCount,
                    toggleAttribute.ButtonsPerRow,
                    toggleSet.SupportsMultipleSelection,
                    toggleAttribute.ShowSelectAll,
                    toggleAttribute.ShowSelectNone,
                    usePagination,
                    hasSummary: false,
                    widthHint: 160f
                );

                const float CachedHeight = 128f;
                WEnumToggleButtonsLayoutCache.Store(property, signature, 160f, CachedHeight);

                float cachedHeight = drawer.GetPropertyHeight(property, label);
                Assert.AreEqual(
                    CachedHeight,
                    cachedHeight,
                    "Drawer should use cached layout height when signature matches."
                );
            }
            finally
            {
                ScriptableObject.DestroyImmediate(asset);
            }
        }

        [Test]
        public void CreateToggleSetHandlesMissingFieldInfo()
        {
            ToggleDropdownAsset asset = ScriptableObject.CreateInstance<ToggleDropdownAsset>();
            try
            {
                SerializedObject serializedObject = new SerializedObject(asset);
                serializedObject.Update();

                SerializedProperty property = serializedObject.FindProperty(
                    nameof(ToggleDropdownAsset.mode)
                );
                Assert.IsNotNull(property, "Failed to locate serialized property for test asset.");

                ToggleSet toggleSet = WEnumToggleButtonsUtility.CreateToggleSet(
                    property,
                    fieldInfo: null
                );
                Assert.IsFalse(
                    toggleSet.IsEmpty,
                    "Toggle set should be created even when FieldInfo is unavailable."
                );
            }
            finally
            {
                ScriptableObject.DestroyImmediate(asset);
            }
        }

        private static void AssertColorApproximately(Color expected, Color actual)
        {
            const float Tolerance = 0.0001f;
            Assert.AreEqual(expected.r, actual.r, Tolerance, "Unexpected red channel.");
            Assert.AreEqual(expected.g, actual.g, Tolerance, "Unexpected green channel.");
            Assert.AreEqual(expected.b, actual.b, Tolerance, "Unexpected blue channel.");
            Assert.AreEqual(expected.a, actual.a, Tolerance, "Unexpected alpha channel.");
        }

        private static void ConfigureDrawer(
            WEnumToggleButtonsDrawer drawer,
            FieldInfo fieldInfo,
            WEnumToggleButtonsAttribute toggleAttribute
        )
        {
            FieldInfo attributeField = typeof(PropertyDrawer).GetField(
                "m_Attribute",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            FieldInfo fieldInfoField = typeof(PropertyDrawer).GetField(
                "m_FieldInfo",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            attributeField?.SetValue(drawer, toggleAttribute);
            fieldInfoField?.SetValue(drawer, fieldInfo);
        }

        private sealed class ToggleDropdownAsset : ScriptableObject
        {
            [WEnumToggleButtons]
            [ValueDropdown(typeof(DropdownProvider), nameof(DropdownProvider.GetModes))]
            public string mode;
        }

        private static class DropdownProvider
        {
            internal static string[] GetModes()
            {
                return new[] { "Alpha", "Beta", "Gamma" };
            }
        }
    }
#endif
}
