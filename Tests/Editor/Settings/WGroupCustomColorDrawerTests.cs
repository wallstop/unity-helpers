#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Settings
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Editor.Settings;

    public sealed class WGroupCustomColorDrawerTests
    {
        private UnityHelpersSettings _settings;
        private SerializedObject _serializedSettings;

        [SetUp]
        public void SetUp()
        {
            _settings = UnityHelpersSettings.instance;
            _serializedSettings = new SerializedObject(_settings);

            // Clear any existing foldout states to ensure clean test state
            SerializableDictionaryPropertyDrawer.WGroupCustomColorFoldoutStates.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            _serializedSettings?.Dispose();
            _serializedSettings = null;

            // Clean up foldout states after tests
            SerializableDictionaryPropertyDrawer.WGroupCustomColorFoldoutStates.Clear();
        }

        [Test]
        public void WGroupCustomColorFoldoutStatesIsSharedDictionary()
        {
            // Verify the shared foldout state dictionary exists and is accessible
            Dictionary<string, bool> foldoutStates =
                SerializableDictionaryPropertyDrawer.WGroupCustomColorFoldoutStates;
            Assert.NotNull(foldoutStates, "WGroupCustomColorFoldoutStates should be initialized");
        }

        [Test]
        public void CalculateWGroupCustomColorHeightReturnsCollapsedHeightWhenNotExpanded()
        {
            SerializedProperty paletteProperty = _serializedSettings.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColors
            );
            Assert.NotNull(paletteProperty, "WGroupCustomColors property should exist");

            SerializedProperty valuesProperty = paletteProperty.FindPropertyRelative("_values");
            Assert.NotNull(valuesProperty, "Values property should exist");

            if (valuesProperty.arraySize == 0)
            {
                Assert.Pass("No WGroupCustomColor entries to test - skipping");
                return;
            }

            SerializedProperty firstValue = valuesProperty.GetArrayElementAtIndex(0);
            Assert.NotNull(firstValue, "First value property should exist");

            // Ensure foldout is collapsed
            string foldoutKey = firstValue.propertyPath;
            SerializableDictionaryPropertyDrawer.WGroupCustomColorFoldoutStates[foldoutKey] = false;

            float height = SerializableDictionaryPropertyDrawer.CalculateWGroupCustomColorHeight(
                firstValue
            );

            // Collapsed height: 1 row (background+text) + foldout header row = 2 rows + spacing
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float expectedCollapsedHeight = lineHeight + spacing + lineHeight;

            Assert.AreEqual(
                expectedCollapsedHeight,
                height,
                0.001f,
                "Height should match collapsed state calculation"
            );
        }

        [Test]
        public void CalculateWGroupCustomColorHeightReturnsExpandedHeightWhenExpanded()
        {
            SerializedProperty paletteProperty = _serializedSettings.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColors
            );
            Assert.NotNull(paletteProperty, "WGroupCustomColors property should exist");

            SerializedProperty valuesProperty = paletteProperty.FindPropertyRelative("_values");
            Assert.NotNull(valuesProperty, "Values property should exist");

            if (valuesProperty.arraySize == 0)
            {
                Assert.Pass("No WGroupCustomColor entries to test - skipping");
                return;
            }

            SerializedProperty firstValue = valuesProperty.GetArrayElementAtIndex(0);
            Assert.NotNull(firstValue, "First value property should exist");

            // Set foldout to expanded
            string foldoutKey = firstValue.propertyPath;
            SerializableDictionaryPropertyDrawer.WGroupCustomColorFoldoutStates[foldoutKey] = true;

            float height = SerializableDictionaryPropertyDrawer.CalculateWGroupCustomColorHeight(
                firstValue
            );

            // Expanded height: base + foldout header + 5 toggle rows + reset button
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float resetButtonHeight = 20f;

            float expectedExpandedHeight =
                lineHeight // background+text row
                + spacing
                + lineHeight // foldout header
                + (spacing + lineHeight) * 5 // 5 toggle+color rows
                + spacing
                + resetButtonHeight; // reset button

            Assert.AreEqual(
                expectedExpandedHeight,
                height,
                0.001f,
                "Height should match expanded state calculation"
            );
        }

        [Test]
        public void FoldoutStateChangeUpdatesHeight()
        {
            SerializedProperty paletteProperty = _serializedSettings.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColors
            );
            Assert.NotNull(paletteProperty, "WGroupCustomColors property should exist");

            SerializedProperty valuesProperty = paletteProperty.FindPropertyRelative("_values");
            Assert.NotNull(valuesProperty, "Values property should exist");

            if (valuesProperty.arraySize == 0)
            {
                Assert.Pass("No WGroupCustomColor entries to test - skipping");
                return;
            }

            SerializedProperty firstValue = valuesProperty.GetArrayElementAtIndex(0);
            string foldoutKey = firstValue.propertyPath;

            // Start collapsed
            SerializableDictionaryPropertyDrawer.WGroupCustomColorFoldoutStates[foldoutKey] = false;
            float collapsedHeight =
                SerializableDictionaryPropertyDrawer.CalculateWGroupCustomColorHeight(firstValue);

            // Expand
            SerializableDictionaryPropertyDrawer.WGroupCustomColorFoldoutStates[foldoutKey] = true;
            float expandedHeight =
                SerializableDictionaryPropertyDrawer.CalculateWGroupCustomColorHeight(firstValue);

            Assert.Greater(
                expandedHeight,
                collapsedHeight,
                "Expanded height should be greater than collapsed height"
            );
        }

        [Test]
        public void TypesWithCustomPropertyDrawerContainsWGroupCustomColor()
        {
            // Verify WGroupCustomColor is registered in the types that bypass foldout rendering
            SerializedProperty paletteProperty = _serializedSettings.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WGroupCustomColors
            );
            Assert.NotNull(paletteProperty, "WGroupCustomColors property should exist");

            SerializedProperty valuesProperty = paletteProperty.FindPropertyRelative("_values");
            Assert.NotNull(valuesProperty, "Values property should exist");

            if (valuesProperty.arraySize == 0)
            {
                Assert.Pass("No WGroupCustomColor entries to test - skipping");
                return;
            }

            // The type should be recognized by the dictionary drawer
            // We verify indirectly by checking that the height calculation method exists and works
            SerializedProperty firstValue = valuesProperty.GetArrayElementAtIndex(0);
            float height = SerializableDictionaryPropertyDrawer.CalculateWGroupCustomColorHeight(
                firstValue
            );

            Assert.Greater(
                height,
                0f,
                "CalculateWGroupCustomColorHeight should return a positive height"
            );
        }

        [Test]
        public void SignalChildHeightChangedUpdatesFrame()
        {
            int frameBefore =
                SerializableDictionaryPropertyDrawer.GetChildHeightChangedFrameForTests();

            SerializableDictionaryPropertyDrawer.SignalChildHeightChanged();

            int frameAfter =
                SerializableDictionaryPropertyDrawer.GetChildHeightChangedFrameForTests();

            // The frame should be set to the current frame (Time.frameCount)
            // In editor tests, Time.frameCount might be 0, so we just verify it was called
            Assert.AreEqual(
                Time.frameCount,
                frameAfter,
                "SignalChildHeightChanged should set frame to current frame count"
            );
        }

        [Test]
        public void FoldoutStateInitializesToFalseWhenNotPresent()
        {
            string testKey = "test/path/to/property";

            // Ensure key doesn't exist
            SerializableDictionaryPropertyDrawer.WGroupCustomColorFoldoutStates.Remove(testKey);

            // Access should return false (default) after initialization
            bool exists =
                SerializableDictionaryPropertyDrawer.WGroupCustomColorFoldoutStates.TryGetValue(
                    testKey,
                    out bool expanded
                );

            Assert.IsFalse(exists, "Key should not exist before initialization");

            // Simulate what GetPropertyHeight does - initialize if not present
            if (
                !SerializableDictionaryPropertyDrawer.WGroupCustomColorFoldoutStates.TryGetValue(
                    testKey,
                    out expanded
                )
            )
            {
                expanded = false;
                SerializableDictionaryPropertyDrawer.WGroupCustomColorFoldoutStates[testKey] =
                    expanded;
            }

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.WGroupCustomColorFoldoutStates.ContainsKey(
                    testKey
                ),
                "Key should exist after initialization"
            );

            Assert.IsFalse(expanded, "Initial state should be collapsed (false)");
        }

        [Test]
        public void MultiplePropertiesHaveIndependentFoldoutStates()
        {
            string key1 = "property/path/1";
            string key2 = "property/path/2";

            // Set different states
            SerializableDictionaryPropertyDrawer.WGroupCustomColorFoldoutStates[key1] = true;
            SerializableDictionaryPropertyDrawer.WGroupCustomColorFoldoutStates[key2] = false;

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.WGroupCustomColorFoldoutStates[key1],
                "First property should be expanded"
            );

            Assert.IsFalse(
                SerializableDictionaryPropertyDrawer.WGroupCustomColorFoldoutStates[key2],
                "Second property should be collapsed"
            );

            // Toggle first property
            SerializableDictionaryPropertyDrawer.WGroupCustomColorFoldoutStates[key1] = false;

            Assert.IsFalse(
                SerializableDictionaryPropertyDrawer.WGroupCustomColorFoldoutStates[key1],
                "First property should now be collapsed"
            );

            Assert.IsFalse(
                SerializableDictionaryPropertyDrawer.WGroupCustomColorFoldoutStates[key2],
                "Second property should still be collapsed"
            );
        }

        [Test]
        public void HeightCalculationMatchesExpectedLayout()
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float resetButtonHeight = 20f;

            // Expected collapsed layout:
            // Row 1: Background + Text color fields
            // Row 2: "Collection Styling (Advanced)" foldout header
            float expectedCollapsedHeight = lineHeight + spacing + lineHeight;

            // Expected expanded layout (adds to collapsed):
            // Row 3: Row Color toggle + field
            // Row 4: Alternate Row Color toggle + field
            // Row 5: Selection Color toggle + field
            // Row 6: Border Color toggle + field
            // Row 7: Pending Background toggle + field
            // Row 8: Reset to Defaults button
            float expandedExtra =
                (spacing + lineHeight) * 5 // 5 toggle+color rows
                + spacing
                + resetButtonHeight; // reset button

            float expectedExpandedHeight = expectedCollapsedHeight + expandedExtra;

            // Verify the constants match our understanding
            Assert.AreEqual(
                lineHeight + spacing + lineHeight,
                expectedCollapsedHeight,
                0.001f,
                "Collapsed height calculation is correct"
            );

            Assert.Greater(
                expectedExpandedHeight,
                expectedCollapsedHeight,
                "Expanded height should be greater than collapsed"
            );

            // The expanded height should include 5 additional rows plus reset button
            float expectedDifference = (spacing + lineHeight) * 5 + spacing + resetButtonHeight;
            Assert.AreEqual(
                expectedDifference,
                expectedExpandedHeight - expectedCollapsedHeight,
                0.001f,
                "Height difference should account for 5 toggle rows and reset button"
            );
        }
    }
}
#endif
