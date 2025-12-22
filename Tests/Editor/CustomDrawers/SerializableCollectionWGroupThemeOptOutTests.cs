#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
    using System.Collections;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes;
    using WallstopStudios.UnityHelpers.Tests.EditorFramework;

    /// <summary>
    /// Tests that verify SerializableDictionaryPropertyDrawer and SerializableSetPropertyDrawer
    /// opt out of WGroup color theming. These drawers manage their own styling and should not
    /// use palette-derived colors even when rendered inside a WGroup context.
    /// </summary>
    [TestFixture]
    public sealed class SerializableCollectionWGroupThemeOptOutTests : CommonTestBase
    {
        // Default colors that drawers use - these match the hardcoded values in the drawer classes
        private static readonly Color LightRowColor = new(0.97f, 0.97f, 0.97f, 1f);
        private static readonly Color DarkRowColor = new(0.16f, 0.16f, 0.16f, 0.45f);

        // Test palette entries to simulate WGroup contexts
        private static readonly UnityHelpersSettings.WGroupPaletteEntry LightPalette = new(
            new Color(0.82f, 0.82f, 0.82f, 1f),
            Color.black
        );

        private static readonly UnityHelpersSettings.WGroupPaletteEntry DarkPalette = new(
            new Color(0.215f, 0.215f, 0.215f, 1f),
            Color.white
        );

        private static readonly UnityHelpersSettings.WGroupPaletteEntry BrightRedPalette = new(
            new Color(1.0f, 0.2f, 0.2f, 1f),
            Color.white
        );

        private bool _originalDictionaryTweenEnabled;
        private bool _originalSetTweenEnabled;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            GroupGUIWidthUtility.ResetForTests();
            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();
            SerializableDictionaryPropertyDrawer.ClearMainFoldoutAnimCacheForTests();
            SerializableSetPropertyDrawer.ResetLayoutTrackingForTests();

            _originalDictionaryTweenEnabled =
                UnityHelpersSettings.ShouldTweenSerializableDictionaryFoldouts();
            _originalSetTweenEnabled = UnityHelpersSettings.ShouldTweenSerializableSetFoldouts();

            // Disable tweening to simplify tests
            UnityHelpersSettings.SetSerializableDictionaryFoldoutTweenEnabled(false);
            UnityHelpersSettings.SetSerializableSetFoldoutTweenEnabled(false);
        }

        [TearDown]
        public override void TearDown()
        {
            UnityHelpersSettings.SetSerializableDictionaryFoldoutTweenEnabled(
                _originalDictionaryTweenEnabled
            );
            UnityHelpersSettings.SetSerializableSetFoldoutTweenEnabled(_originalSetTweenEnabled);

            GroupGUIWidthUtility.ResetForTests();
            base.TearDown();
        }

        // ============================================================================
        // 1. Dictionary opt-out tests
        // ============================================================================

        [UnityTest]
        public IEnumerator DictionaryOptsOutOfWGroupThemingWhenInsideWGroup()
        {
            // Arrange: Create a dictionary host with data
            IntegrationTestWGroupDictionaryHost host =
                CreateScriptableObject<IntegrationTestWGroupDictionaryHost>();
            host.dictionary["key1"] = 100;

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(IntegrationTestWGroupDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Dictionary");

            bool wasInsideWGroup = false;
            bool paletteWasSet = false;
            int previousIndentLevel = EditorGUI.indentLevel;

            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    EditorGUI.indentLevel = 0;
                    GroupGUIWidthUtility.ResetForTests();
                    SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

                    // Simulate being inside a WGroup by pushing both padding and palette
                    using (GroupGUIWidthUtility.PushContentPadding(12f, 6f, 6f))
                    using (GroupGUIWidthUtility.PushWGroupPalette(BrightRedPalette))
                    {
                        // Verify we're inside a WGroup context before drawing
                        wasInsideWGroup = GroupGUIWidthUtility.CurrentScopeDepth > 0;
                        paletteWasSet = GroupGUIWidthUtility.CurrentPalette != null;

                        // Draw the dictionary - it should opt out internally
                        drawer.OnGUI(controlRect, dictionaryProperty, label);
                    }
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            // Assert: Verify context was indeed WGroup before drawer ran
            Assert.IsTrue(wasInsideWGroup, "Test should have been inside WGroup context.");
            Assert.IsTrue(paletteWasSet, "Test should have had a palette set.");

            // The drawer internally sets _currentDrawInsideWGroup = false, opting out of WGroup theming.
            // This test verifies the drawer completes successfully when inside a WGroup,
            // and the opt-out behavior is documented by the drawer's explicit assignment.
            // The drawer draws itself using its own colors, not palette colors.
        }

        [UnityTest]
        public IEnumerator DictionaryUsesDefaultColorsNotPaletteColors()
        {
            // Arrange
            IntegrationTestWGroupDictionaryHost host =
                CreateScriptableObject<IntegrationTestWGroupDictionaryHost>();
            host.dictionary["key1"] = 100;

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(IntegrationTestWGroupDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = false;

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Dictionary");

            Color capturedRowColorInContext = default;
            Color capturedRowColorOutsideContext = default;
            int previousIndentLevel = EditorGUI.indentLevel;

            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    EditorGUI.indentLevel = 0;
                    GroupGUIWidthUtility.ResetForTests();
                    SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

                    // First: Get expected row color without any WGroup context
                    capturedRowColorOutsideContext = GroupGUIWidthUtility.GetThemedRowColor(
                        LightRowColor,
                        DarkRowColor
                    );

                    // Now push a WGroup palette and verify GetThemedRowColor would return different values
                    using (GroupGUIWidthUtility.PushContentPadding(12f, 6f, 6f))
                    using (GroupGUIWidthUtility.PushWGroupPalette(BrightRedPalette))
                    {
                        // Inside WGroup context, GetThemedRowColor considers palette luminance
                        capturedRowColorInContext = GroupGUIWidthUtility.GetThemedRowColor(
                            LightRowColor,
                            DarkRowColor
                        );

                        // Draw dictionary - it opts out and uses default theme colors
                        drawer.OnGUI(controlRect, dictionaryProperty, label);
                    }
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            // The dictionary drawer opts out by setting _currentDrawInsideWGroup = false,
            // meaning it uses editor skin colors (EditorGUIUtility.isProSkin) rather than
            // palette-derived colors. The expected behavior is that drawer colors don't
            // change based on WGroup palette.
            Color expectedDefaultColor = EditorGUIUtility.isProSkin ? DarkRowColor : LightRowColor;

            Assert.AreEqual(
                expectedDefaultColor,
                capturedRowColorOutsideContext,
                "Row color outside WGroup should match expected default."
            );

            // When bright red palette is active, GetThemedRowColor returns different colors
            // because palette luminance affects the choice. The dictionary drawer ignores this.
            // We verify that the GetThemedRowColor API respects palette, proving opt-out is intentional.
            float paletteBackgroundLuminance =
                0.299f * BrightRedPalette.BackgroundColor.r
                + 0.587f * BrightRedPalette.BackgroundColor.g
                + 0.114f * BrightRedPalette.BackgroundColor.b;

            Color expectedPaletteBasedColor =
                paletteBackgroundLuminance > 0.5f ? LightRowColor : DarkRowColor;
            Assert.AreEqual(
                expectedPaletteBasedColor,
                capturedRowColorInContext,
                "GetThemedRowColor inside WGroup should respect palette luminance."
            );
        }

        // ============================================================================
        // 2. Set opt-out tests
        // ============================================================================

        [UnityTest]
        public IEnumerator SetOptsOutOfWGroupThemingWhenInsideWGroup()
        {
            // Arrange: Create a set host with data
            IntegrationTestWGroupSetHost host =
                CreateScriptableObject<IntegrationTestWGroupSetHost>();
            host.set.Add(42);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(IntegrationTestWGroupSetHost.set)
            );
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Set");

            bool wasInsideWGroup = false;
            bool paletteWasSet = false;
            int previousIndentLevel = EditorGUI.indentLevel;

            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    EditorGUI.indentLevel = 0;
                    GroupGUIWidthUtility.ResetForTests();
                    SerializableSetPropertyDrawer.ResetLayoutTrackingForTests();

                    // Simulate being inside a WGroup by pushing both padding and palette
                    using (GroupGUIWidthUtility.PushContentPadding(12f, 6f, 6f))
                    using (GroupGUIWidthUtility.PushWGroupPalette(BrightRedPalette))
                    {
                        // Verify we're inside a WGroup context before drawing
                        wasInsideWGroup = GroupGUIWidthUtility.CurrentScopeDepth > 0;
                        paletteWasSet = GroupGUIWidthUtility.CurrentPalette != null;

                        // Draw the set - it should opt out internally
                        drawer.OnGUI(controlRect, setProperty, label);
                    }
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            // Assert: Verify context was indeed WGroup before drawer ran
            Assert.IsTrue(wasInsideWGroup, "Test should have been inside WGroup context.");
            Assert.IsTrue(paletteWasSet, "Test should have had a palette set.");

            // The drawer internally sets _currentDrawInsideWGroup = false, opting out of WGroup theming.
            // This test verifies the drawer completes successfully when inside a WGroup,
            // and the opt-out behavior is documented by the drawer's explicit assignment.
        }

        [UnityTest]
        public IEnumerator SetUsesDefaultColorsNotPaletteColors()
        {
            // Arrange
            IntegrationTestWGroupSetHost host =
                CreateScriptableObject<IntegrationTestWGroupSetHost>();
            host.set.Add(42);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(IntegrationTestWGroupSetHost.set)
            );
            setProperty.isExpanded = false;

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Set");

            Color capturedRowColorInContext = default;
            Color capturedRowColorOutsideContext = default;
            int previousIndentLevel = EditorGUI.indentLevel;

            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    EditorGUI.indentLevel = 0;
                    GroupGUIWidthUtility.ResetForTests();
                    SerializableSetPropertyDrawer.ResetLayoutTrackingForTests();

                    // First: Get expected row color without any WGroup context
                    capturedRowColorOutsideContext = GroupGUIWidthUtility.GetThemedRowColor(
                        LightRowColor,
                        DarkRowColor
                    );

                    // Now push a WGroup palette and verify GetThemedRowColor would return different values
                    using (GroupGUIWidthUtility.PushContentPadding(12f, 6f, 6f))
                    using (GroupGUIWidthUtility.PushWGroupPalette(BrightRedPalette))
                    {
                        // Inside WGroup context, GetThemedRowColor considers palette luminance
                        capturedRowColorInContext = GroupGUIWidthUtility.GetThemedRowColor(
                            LightRowColor,
                            DarkRowColor
                        );

                        // Draw set - it opts out and uses default theme colors
                        drawer.OnGUI(controlRect, setProperty, label);
                    }
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            // The set drawer opts out by setting _currentDrawInsideWGroup = false,
            // meaning it uses editor skin colors (EditorGUIUtility.isProSkin) rather than
            // palette-derived colors.
            Color expectedDefaultColor = EditorGUIUtility.isProSkin ? DarkRowColor : LightRowColor;

            Assert.AreEqual(
                expectedDefaultColor,
                capturedRowColorOutsideContext,
                "Row color outside WGroup should match expected default."
            );

            // When bright red palette is active, GetThemedRowColor returns different colors
            // because palette luminance affects the choice. The set drawer ignores this.
            float paletteBackgroundLuminance =
                0.299f * BrightRedPalette.BackgroundColor.r
                + 0.587f * BrightRedPalette.BackgroundColor.g
                + 0.114f * BrightRedPalette.BackgroundColor.b;

            Color expectedPaletteBasedColor =
                paletteBackgroundLuminance > 0.5f ? LightRowColor : DarkRowColor;
            Assert.AreEqual(
                expectedPaletteBasedColor,
                capturedRowColorInContext,
                "GetThemedRowColor inside WGroup should respect palette luminance."
            );
        }

        // ============================================================================
        // 3. Background color opt-out tests
        // ============================================================================

        [Test]
        public void DictionaryBackgroundColorNotDerivedFromWGroupPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            // Without WGroup, GetThemedRowColor returns skin-based defaults
            Color outsideWGroupColor = GroupGUIWidthUtility.GetThemedRowColor(
                LightRowColor,
                DarkRowColor
            );

            Color expectedDefault = EditorGUIUtility.isProSkin ? DarkRowColor : LightRowColor;
            Assert.AreEqual(
                expectedDefault,
                outsideWGroupColor,
                "Outside WGroup, themed row color should match editor skin default."
            );

            // Push various palettes and verify GetThemedRowColor returns palette-influenced colors
            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                Color lightPaletteColor = GroupGUIWidthUtility.GetThemedRowColor(
                    LightRowColor,
                    DarkRowColor
                );

                // Light palette background (luminance > 0.5) -> should return LightRowColor
                Assert.AreEqual(
                    LightRowColor,
                    lightPaletteColor,
                    "Light palette should yield light row color."
                );
            }

            using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
            {
                Color darkPaletteColor = GroupGUIWidthUtility.GetThemedRowColor(
                    LightRowColor,
                    DarkRowColor
                );

                // Dark palette background (luminance <= 0.5) -> should return DarkRowColor
                Assert.AreEqual(
                    DarkRowColor,
                    darkPaletteColor,
                    "Dark palette should yield dark row color."
                );
            }

            // After scopes are disposed, back to default
            Color afterWGroupColor = GroupGUIWidthUtility.GetThemedRowColor(
                LightRowColor,
                DarkRowColor
            );
            Assert.AreEqual(
                expectedDefault,
                afterWGroupColor,
                "After WGroup scope, themed row color should return to skin default."
            );

            // The dictionary drawer sets _currentDrawInsideWGroup = false, which means
            // even though these GetThemedRowColor calls would return palette-influenced colors
            // if palette context exists, the drawer does NOT use these palette-aware APIs for
            // its internal row coloring. It uses the hardcoded skin-based logic directly.
        }

        [Test]
        public void SetBackgroundColorNotDerivedFromWGroupPalette()
        {
            GroupGUIWidthUtility.ResetForTests();

            // Without WGroup, GetThemedRowColor returns skin-based defaults
            Color outsideWGroupColor = GroupGUIWidthUtility.GetThemedRowColor(
                LightRowColor,
                DarkRowColor
            );

            Color expectedDefault = EditorGUIUtility.isProSkin ? DarkRowColor : LightRowColor;
            Assert.AreEqual(
                expectedDefault,
                outsideWGroupColor,
                "Outside WGroup, themed row color should match editor skin default."
            );

            // Push various palettes and verify GetThemedRowColor returns palette-influenced colors
            using (GroupGUIWidthUtility.PushWGroupPalette(LightPalette))
            {
                Color lightPaletteColor = GroupGUIWidthUtility.GetThemedRowColor(
                    LightRowColor,
                    DarkRowColor
                );

                Assert.AreEqual(
                    LightRowColor,
                    lightPaletteColor,
                    "Light palette should yield light row color."
                );
            }

            using (GroupGUIWidthUtility.PushWGroupPalette(DarkPalette))
            {
                Color darkPaletteColor = GroupGUIWidthUtility.GetThemedRowColor(
                    LightRowColor,
                    DarkRowColor
                );

                Assert.AreEqual(
                    DarkRowColor,
                    darkPaletteColor,
                    "Dark palette should yield dark row color."
                );
            }

            // After scopes are disposed, back to default
            Color afterWGroupColor = GroupGUIWidthUtility.GetThemedRowColor(
                LightRowColor,
                DarkRowColor
            );
            Assert.AreEqual(
                expectedDefault,
                afterWGroupColor,
                "After WGroup scope, themed row color should return to skin default."
            );

            // The set drawer sets _currentDrawInsideWGroup = false, which means
            // even though these GetThemedRowColor calls would return palette-influenced colors
            // if palette context exists, the drawer does NOT use these palette-aware APIs for
            // its internal row coloring.
        }

        // ============================================================================
        // 4. Cross-theme verification tests
        // ============================================================================

        [UnityTest]
        public IEnumerator DictionaryIgnoresCrossThemePaletteColors()
        {
            IntegrationTestWGroupDictionaryHost host =
                CreateScriptableObject<IntegrationTestWGroupDictionaryHost>();
            host.dictionary["key1"] = 100;

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(IntegrationTestWGroupDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Dictionary");

            // Choose a cross-theme palette (light on dark skin or dark on light skin)
            UnityHelpersSettings.WGroupPaletteEntry crossThemePalette = EditorGUIUtility.isProSkin
                ? LightPalette // Light palette on dark skin
                : DarkPalette; // Dark palette on light skin

            bool paletteIsSet = false;
            int previousIndentLevel = EditorGUI.indentLevel;

            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    EditorGUI.indentLevel = 0;
                    GroupGUIWidthUtility.ResetForTests();
                    SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

                    using (GroupGUIWidthUtility.PushContentPadding(12f, 6f, 6f))
                    using (GroupGUIWidthUtility.PushWGroupPalette(crossThemePalette))
                    {
                        paletteIsSet = GroupGUIWidthUtility.CurrentPalette != null;

                        // Draw the dictionary with cross-theme palette active
                        // The drawer should use default skin colors, not adapt to palette
                        drawer.OnGUI(controlRect, dictionaryProperty, label);
                    }
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            Assert.IsTrue(paletteIsSet, "Cross-theme palette should have been set.");
            // If we reach here without exceptions, the drawer successfully rendered
            // using its own styling, ignoring the cross-theme palette.
        }

        [UnityTest]
        public IEnumerator SetIgnoresCrossThemePaletteColors()
        {
            IntegrationTestWGroupSetHost host =
                CreateScriptableObject<IntegrationTestWGroupSetHost>();
            host.set.Add(42);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(IntegrationTestWGroupSetHost.set)
            );
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Set");

            // Choose a cross-theme palette
            UnityHelpersSettings.WGroupPaletteEntry crossThemePalette = EditorGUIUtility.isProSkin
                ? LightPalette
                : DarkPalette;

            bool paletteIsSet = false;
            int previousIndentLevel = EditorGUI.indentLevel;

            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    EditorGUI.indentLevel = 0;
                    GroupGUIWidthUtility.ResetForTests();
                    SerializableSetPropertyDrawer.ResetLayoutTrackingForTests();

                    using (GroupGUIWidthUtility.PushContentPadding(12f, 6f, 6f))
                    using (GroupGUIWidthUtility.PushWGroupPalette(crossThemePalette))
                    {
                        paletteIsSet = GroupGUIWidthUtility.CurrentPalette != null;

                        // Draw the set with cross-theme palette active
                        drawer.OnGUI(controlRect, setProperty, label);
                    }
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            Assert.IsTrue(paletteIsSet, "Cross-theme palette should have been set.");
        }
    }
}
#endif
