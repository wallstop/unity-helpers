namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Editor.CustomEditors;
    using WallstopStudios.UnityHelpers.Editor.Sprites;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.EditorFramework;
    using IEnumerable = System.Collections.IEnumerable;

    /// <summary>
    /// Tests for SourceFolderEntryDrawer to ensure:
    /// 1. GetPropertyHeight and OnGUI have consistent height calculations
    /// 2. Layout issues (overlapping elements) don't occur
    /// 3. Foldout states correctly affect height calculations
    /// </summary>
    [TestFixture]
    public sealed class SourceFolderEntryDrawerTests : CommonTestBase
    {
        private const string TestRoot = "Assets/Temp/SourceFolderEntryDrawerTests";
        private ScriptableSpriteAtlas _testConfig;
        private SerializedObject _serializedConfig;
        private SerializedProperty _sourceFolderEntriesProperty;
        private SourceFolderEntryDrawer _drawer;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            EnsureFolder(TestRoot);

            // Create a test ScriptableSpriteAtlas with a SourceFolderEntry
            _testConfig = Track(ScriptableObject.CreateInstance<ScriptableSpriteAtlas>());
            _testConfig.name = "TestConfig";
            _testConfig.sourceFolderEntries.Add(
                new SourceFolderEntry
                {
                    folderPath = "Assets/Sprites",
                    selectionMode = SpriteSelectionMode.Regex,
                }
            );

            // Create serialized object
            _serializedConfig = new SerializedObject(_testConfig);
            _sourceFolderEntriesProperty = _serializedConfig.FindProperty(
                nameof(ScriptableSpriteAtlas.sourceFolderEntries)
            );

            // Create drawer instance
            _drawer = new SourceFolderEntryDrawer();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            _serializedConfig?.Dispose();
            CleanupTrackedFoldersAndAssets();
        }

        [Test]
        public void GetPropertyHeightWhenCollapsedReturnsOnlySingleLineHeight()
        {
            // Arrange
            SerializedProperty entryProp = _sourceFolderEntriesProperty.GetArrayElementAtIndex(0);
            entryProp.isExpanded = false;
            _serializedConfig.ApplyModifiedProperties();

            // Act
            float height = _drawer.GetPropertyHeight(entryProp, GUIContent.none);

            // Assert
            Assert.AreEqual(
                EditorGUIUtility.singleLineHeight,
                height,
                0.01f,
                "Collapsed property should have single line height"
            );
        }

        [Test]
        public void GetPropertyHeightWhenExpandedIsPositive()
        {
            // Arrange
            SerializedProperty entryProp = _sourceFolderEntriesProperty.GetArrayElementAtIndex(0);
            entryProp.isExpanded = true;
            _serializedConfig.ApplyModifiedProperties();

            // Act
            float height = _drawer.GetPropertyHeight(entryProp, GUIContent.none);

            // Assert
            Assert.Greater(
                height,
                EditorGUIUtility.singleLineHeight,
                "Expanded property should have more than single line height"
            );
        }

        [Test]
        public void GetPropertyHeightWithRegexModeIncludesAllFoldoutSections()
        {
            // Arrange
            _testConfig.sourceFolderEntries[0].selectionMode = SpriteSelectionMode.Regex;
            _serializedConfig.Update();
            SerializedProperty entryProp = _sourceFolderEntriesProperty.GetArrayElementAtIndex(0);
            entryProp.isExpanded = true;
            _serializedConfig.ApplyModifiedProperties();

            // Act
            float height = _drawer.GetPropertyHeight(entryProp, GUIContent.none);

            // Assert - Height should include space for:
            // - Main foldout, folder path, path field, selection mode
            // - Regexes foldout, Exclude Regexes foldout, Exclude Path Prefixes foldout
            float minExpectedHeight = EditorGUIUtility.singleLineHeight * 6;
            Assert.Greater(
                height,
                minExpectedHeight,
                "Height with Regex mode should include multiple foldout sections"
            );
        }

        [Test]
        public void GetPropertyHeightWithLabelsModeIncludesLabelSections()
        {
            // Arrange
            _testConfig.sourceFolderEntries[0].selectionMode = SpriteSelectionMode.Labels;
            _serializedConfig.Update();
            SerializedProperty entryProp = _sourceFolderEntriesProperty.GetArrayElementAtIndex(0);
            entryProp.isExpanded = true;
            _serializedConfig.ApplyModifiedProperties();

            // Act
            float height = _drawer.GetPropertyHeight(entryProp, GUIContent.none);

            // Assert - Height should include space for labels-related sections
            float minExpectedHeight = EditorGUIUtility.singleLineHeight * 5;
            Assert.Greater(
                height,
                minExpectedHeight,
                "Height with Labels mode should include label-related sections"
            );
        }

        [Test]
        public void GetPropertyHeightWithBothModesIncludesBooleanLogicField()
        {
            // Arrange
            _testConfig.sourceFolderEntries[0].selectionMode =
                SpriteSelectionMode.Regex | SpriteSelectionMode.Labels;
            _serializedConfig.Update();
            SerializedProperty entryProp = _sourceFolderEntriesProperty.GetArrayElementAtIndex(0);
            entryProp.isExpanded = true;
            _serializedConfig.ApplyModifiedProperties();

            // Act
            float heightBoth = _drawer.GetPropertyHeight(entryProp, GUIContent.none);

            // Compare with just Regex mode
            _testConfig.sourceFolderEntries[0].selectionMode = SpriteSelectionMode.Regex;
            _serializedConfig.Update();
            float heightRegexOnly = _drawer.GetPropertyHeight(entryProp, GUIContent.none);

            // Assert - Both modes should be taller due to extra fields
            Assert.Greater(
                heightBoth,
                heightRegexOnly,
                "Combined modes should have greater height than regex-only mode"
            );
        }

        [Test]
        public void GetPropertyHeightWithRegexesArrayIncreasesHeightPerItem()
        {
            // Arrange
            _testConfig.sourceFolderEntries[0].selectionMode = SpriteSelectionMode.Regex;
            _testConfig.sourceFolderEntries[0].regexes.Clear();
            _serializedConfig.Update();
            SerializedProperty entryProp = _sourceFolderEntriesProperty.GetArrayElementAtIndex(0);
            entryProp.isExpanded = true;

            // Expand the Regexes foldout
            SetFoldoutState("RegexesFoldoutState", GetRegexFoldoutKey(entryProp), true);
            _serializedConfig.ApplyModifiedProperties();

            float heightEmpty = _drawer.GetPropertyHeight(entryProp, GUIContent.none);

            // Add regexes
            _testConfig.sourceFolderEntries[0].regexes.Add("pattern1");
            _testConfig.sourceFolderEntries[0].regexes.Add("pattern2");
            _serializedConfig.Update();

            float heightWithItems = _drawer.GetPropertyHeight(entryProp, GUIContent.none);

            // Assert - More items should increase height
            Assert.Greater(
                heightWithItems,
                heightEmpty,
                "Adding regex items should increase height"
            );

            // Height increase should be approximately 2 * singleLineHeight (for 2 items)
            float expectedIncrease =
                2 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
            float actualIncrease = heightWithItems - heightEmpty;
            Assert.AreEqual(
                expectedIncrease,
                actualIncrease,
                1f,
                "Height should increase by approximately singleLineHeight per item"
            );
        }

        [Test]
        public void GetPropertyHeightCollapsedFoldoutsDoNotIncludeContentHeight()
        {
            // Arrange
            _testConfig.sourceFolderEntries[0].selectionMode = SpriteSelectionMode.Regex;
            _testConfig.sourceFolderEntries[0].regexes.Add("pattern1");
            _testConfig.sourceFolderEntries[0].regexes.Add("pattern2");
            _serializedConfig.Update();
            SerializedProperty entryProp = _sourceFolderEntriesProperty.GetArrayElementAtIndex(0);
            entryProp.isExpanded = true;

            // Collapse the Regexes foldout
            SetFoldoutState("RegexesFoldoutState", GetRegexFoldoutKey(entryProp), false);
            _serializedConfig.ApplyModifiedProperties();

            float heightCollapsed = _drawer.GetPropertyHeight(entryProp, GUIContent.none);

            // Expand the Regexes foldout
            SetFoldoutState("RegexesFoldoutState", GetRegexFoldoutKey(entryProp), true);

            float heightExpanded = _drawer.GetPropertyHeight(entryProp, GUIContent.none);

            // Assert - Expanded should be taller
            Assert.Greater(
                heightExpanded,
                heightCollapsed,
                "Expanded foldout should have greater height than collapsed"
            );
        }

        [UnityTest]
        public IEnumerator OnGUIDoesNotThrowWhenRendered()
        {
            // Arrange
            SerializedProperty entryProp = _sourceFolderEntriesProperty.GetArrayElementAtIndex(0);
            entryProp.isExpanded = true;
            _serializedConfig.ApplyModifiedProperties();

            float height = _drawer.GetPropertyHeight(entryProp, GUIContent.none);
            Rect position = new Rect(0, 0, 400, height);
            Exception caughtException = null;

            // Act - Run inside proper IMGUI context
            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    _drawer.OnGUI(position, entryProp, GUIContent.none);
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }
            });

            // Assert
            Assert.IsNull(
                caughtException,
                $"OnGUI should not throw when rendering. Exception: {caughtException}"
            );
        }

        [UnityTest]
        public IEnumerator OnGUIAllModesDoesNotThrow()
        {
            // Test all selection mode combinations
            SpriteSelectionMode[] modes = new[]
            {
                SpriteSelectionMode.Regex,
                SpriteSelectionMode.Labels,
                SpriteSelectionMode.Regex | SpriteSelectionMode.Labels,
            };

            foreach (SpriteSelectionMode mode in modes)
            {
                // Arrange
                _testConfig.sourceFolderEntries[0].selectionMode = mode;
                _serializedConfig.Update();
                SerializedProperty entryProp = _sourceFolderEntriesProperty.GetArrayElementAtIndex(
                    0
                );
                entryProp.isExpanded = true;
                _serializedConfig.ApplyModifiedProperties();

                float height = _drawer.GetPropertyHeight(entryProp, GUIContent.none);
                Rect position = new Rect(0, 0, 400, height);
                Exception caughtException = null;
                SpriteSelectionMode capturedMode = mode;

                // Act - Run inside proper IMGUI context
                yield return TestIMGUIExecutor.Run(() =>
                {
                    try
                    {
                        _drawer.OnGUI(position, entryProp, GUIContent.none);
                    }
                    catch (Exception ex)
                    {
                        caughtException = ex;
                    }
                });

                // Assert
                Assert.IsNull(
                    caughtException,
                    $"OnGUI should not throw for mode {capturedMode}. Exception: {caughtException}"
                );
            }
        }

        [Test]
        public void HeightRemainsConsistentAfterMultipleGetPropertyHeightCalls()
        {
            // Arrange
            SerializedProperty entryProp = _sourceFolderEntriesProperty.GetArrayElementAtIndex(0);
            entryProp.isExpanded = true;
            _serializedConfig.ApplyModifiedProperties();

            // Act - Call multiple times
            float height1 = _drawer.GetPropertyHeight(entryProp, GUIContent.none);
            float height2 = _drawer.GetPropertyHeight(entryProp, GUIContent.none);
            float height3 = _drawer.GetPropertyHeight(entryProp, GUIContent.none);

            // Assert - Should be identical
            Assert.AreEqual(height1, height2, 0.001f, "Height should be consistent across calls");
            Assert.AreEqual(height2, height3, 0.001f, "Height should be consistent across calls");
        }

        [Test]
        public void GetPropertyHeightExcludeRegexesFoldoutAffectsHeight()
        {
            // Arrange
            _testConfig.sourceFolderEntries[0].selectionMode = SpriteSelectionMode.Regex;
            _testConfig.sourceFolderEntries[0].excludeRegexes.Add("excludePattern");
            _serializedConfig.Update();
            SerializedProperty entryProp = _sourceFolderEntriesProperty.GetArrayElementAtIndex(0);
            entryProp.isExpanded = true;

            // Collapse exclude regexes foldout
            SetFoldoutState(
                "ExcludeRegexesFoldoutState",
                GetExcludeRegexFoldoutKey(entryProp),
                false
            );
            float heightCollapsed = _drawer.GetPropertyHeight(entryProp, GUIContent.none);

            // Expand exclude regexes foldout
            SetFoldoutState(
                "ExcludeRegexesFoldoutState",
                GetExcludeRegexFoldoutKey(entryProp),
                true
            );
            float heightExpanded = _drawer.GetPropertyHeight(entryProp, GUIContent.none);

            // Assert
            Assert.Greater(
                heightExpanded,
                heightCollapsed,
                "Expanded Exclude Regexes foldout should increase height"
            );
        }

        [Test]
        public void GetPropertyHeightExcludePathPrefixesFoldoutAffectsHeight()
        {
            // Arrange
            _testConfig.sourceFolderEntries[0].selectionMode = SpriteSelectionMode.Regex;
            _testConfig.sourceFolderEntries[0].excludePathPrefixes.Add("Assets/Exclude/");
            _serializedConfig.Update();
            SerializedProperty entryProp = _sourceFolderEntriesProperty.GetArrayElementAtIndex(0);
            entryProp.isExpanded = true;

            // Collapse exclude path prefixes foldout
            SetFoldoutState(
                "ExcludePathPrefixesFoldoutState",
                GetExcludePathFoldoutKey(entryProp),
                false
            );
            float heightCollapsed = _drawer.GetPropertyHeight(entryProp, GUIContent.none);

            // Expand exclude path prefixes foldout
            SetFoldoutState(
                "ExcludePathPrefixesFoldoutState",
                GetExcludePathFoldoutKey(entryProp),
                true
            );
            float heightExpanded = _drawer.GetPropertyHeight(entryProp, GUIContent.none);

            // Assert
            Assert.Greater(
                heightExpanded,
                heightCollapsed,
                "Expanded Exclude Path Prefixes foldout should increase height"
            );
        }

        [Test]
        public void GetPropertyHeightNullPropertyReturnsSingleLineHeight()
        {
            // Arrange & Act
            float height = _drawer.GetPropertyHeight(null, GUIContent.none);

            // Assert
            Assert.AreEqual(
                EditorGUIUtility.singleLineHeight,
                height,
                0.01f,
                "Null property should return single line height"
            );
        }

        [UnityTest]
        public IEnumerator OnGUINullPropertyDoesNotThrow()
        {
            // Arrange
            Rect position = new Rect(0, 0, 400, EditorGUIUtility.singleLineHeight);
            Exception caughtException = null;

            // Act
            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    _drawer.OnGUI(position, null, GUIContent.none);
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }
            });

            // Assert
            Assert.IsNull(
                caughtException,
                $"OnGUI should handle null property gracefully. Exception: {caughtException}"
            );
        }

        [UnityTest]
        public IEnumerator OnGUICollapsedPropertyDoesNotThrow()
        {
            // Arrange
            SerializedProperty entryProp = _sourceFolderEntriesProperty.GetArrayElementAtIndex(0);
            entryProp.isExpanded = false;
            _serializedConfig.ApplyModifiedProperties();

            float height = _drawer.GetPropertyHeight(entryProp, GUIContent.none);
            Rect position = new Rect(0, 0, 400, height);
            Exception caughtException = null;

            // Act
            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    _drawer.OnGUI(position, entryProp, GUIContent.none);
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }
            });

            // Assert
            Assert.IsNull(
                caughtException,
                $"OnGUI should handle collapsed property. Exception: {caughtException}"
            );
        }

        [UnityTest]
        public IEnumerator OnGUIZeroWidthRectDoesNotThrow()
        {
            // Arrange
            SerializedProperty entryProp = _sourceFolderEntriesProperty.GetArrayElementAtIndex(0);
            entryProp.isExpanded = true;
            _serializedConfig.ApplyModifiedProperties();

            Rect position = new Rect(0, 0, 0, 100);
            Exception caughtException = null;

            // Act
            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    _drawer.OnGUI(position, entryProp, GUIContent.none);
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }
            });

            // Assert
            Assert.IsNull(
                caughtException,
                $"OnGUI should handle zero-width rect. Exception: {caughtException}"
            );
        }

        [UnityTest]
        public IEnumerator OnGUINullLabelDoesNotThrow()
        {
            // Arrange
            SerializedProperty entryProp = _sourceFolderEntriesProperty.GetArrayElementAtIndex(0);
            entryProp.isExpanded = true;
            _serializedConfig.ApplyModifiedProperties();

            float height = _drawer.GetPropertyHeight(entryProp, null);
            Rect position = new Rect(0, 0, 400, height);
            Exception caughtException = null;

            // Act
            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    _drawer.OnGUI(position, entryProp, null);
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }
            });

            // Assert
            Assert.IsNull(
                caughtException,
                $"OnGUI should handle null label. Exception: {caughtException}"
            );
        }

        [UnityTest]
        public IEnumerator OnGUIEmptyRegexesListDoesNotThrow()
        {
            // Arrange
            _testConfig.sourceFolderEntries[0].selectionMode = SpriteSelectionMode.Regex;
            _testConfig.sourceFolderEntries[0].regexes.Clear();
            _serializedConfig.Update();
            SerializedProperty entryProp = _sourceFolderEntriesProperty.GetArrayElementAtIndex(0);
            entryProp.isExpanded = true;
            SetFoldoutState("RegexesFoldoutState", GetRegexFoldoutKey(entryProp), true);
            _serializedConfig.ApplyModifiedProperties();

            float height = _drawer.GetPropertyHeight(entryProp, GUIContent.none);
            Rect position = new Rect(0, 0, 400, height);
            Exception caughtException = null;

            // Act
            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    _drawer.OnGUI(position, entryProp, GUIContent.none);
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }
            });

            // Assert
            Assert.IsNull(
                caughtException,
                $"OnGUI should handle empty regexes list. Exception: {caughtException}"
            );
        }

        private static IEnumerable SelectionModeCases()
        {
            yield return new TestCaseData(SpriteSelectionMode.Regex).SetName("Regex mode");
            yield return new TestCaseData(SpriteSelectionMode.Labels).SetName("Labels mode");
            yield return new TestCaseData(
                SpriteSelectionMode.Regex | SpriteSelectionMode.Labels
            ).SetName("Combined mode");
        }

        [Test]
        [TestCaseSource(nameof(SelectionModeCases))]
        public void GetPropertyHeightExpandedIsPositiveForMode(SpriteSelectionMode mode)
        {
            // Arrange
            _testConfig.sourceFolderEntries[0].selectionMode = mode;
            _serializedConfig.Update();
            SerializedProperty entryProp = _sourceFolderEntriesProperty.GetArrayElementAtIndex(0);
            entryProp.isExpanded = true;
            _serializedConfig.ApplyModifiedProperties();

            // Act
            float height = _drawer.GetPropertyHeight(entryProp, GUIContent.none);

            // Assert
            Assert.Greater(
                height,
                EditorGUIUtility.singleLineHeight,
                $"Expanded property should have more than single line height for mode {mode}"
            );
        }

        [Test]
        [TestCaseSource(nameof(SelectionModeCases))]
        public void GetPropertyHeightCollapsedIsSingleLineForMode(SpriteSelectionMode mode)
        {
            // Arrange
            _testConfig.sourceFolderEntries[0].selectionMode = mode;
            _serializedConfig.Update();
            SerializedProperty entryProp = _sourceFolderEntriesProperty.GetArrayElementAtIndex(0);
            entryProp.isExpanded = false;
            _serializedConfig.ApplyModifiedProperties();

            // Act
            float height = _drawer.GetPropertyHeight(entryProp, GUIContent.none);

            // Assert
            Assert.AreEqual(
                EditorGUIUtility.singleLineHeight,
                height,
                0.01f,
                $"Collapsed property should have single line height for mode {mode}"
            );
        }

        private static IEnumerable RectSizeCases()
        {
            yield return new TestCaseData(400f, 300f).SetName("Normal size");
            yield return new TestCaseData(200f, 150f).SetName("Small size");
            yield return new TestCaseData(800f, 600f).SetName("Large size");
            yield return new TestCaseData(100f, 50f).SetName("Very small size");
        }

        [UnityTest]
        public IEnumerator OnGUIVariousRectSizesDoesNotThrow(
            [ValueSource(nameof(RectSizeCases))] TestCaseData testCase
        )
        {
            // Arrange
            float width = (float)testCase.Arguments[0];
            float baseHeight = (float)testCase.Arguments[1];

            SerializedProperty entryProp = _sourceFolderEntriesProperty.GetArrayElementAtIndex(0);
            entryProp.isExpanded = true;
            _serializedConfig.ApplyModifiedProperties();

            float height = _drawer.GetPropertyHeight(entryProp, GUIContent.none);
            Rect position = new Rect(0, 0, width, Math.Max(height, baseHeight));
            Exception caughtException = null;

            // Act
            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    _drawer.OnGUI(position, entryProp, GUIContent.none);
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }
            });

            // Assert
            Assert.IsNull(
                caughtException,
                $"OnGUI should handle {testCase.TestName}. Exception: {caughtException}"
            );
        }

        private void SetFoldoutState(string fieldName, string key, bool value)
        {
            FieldInfo field = typeof(SourceFolderEntryDrawer).GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Static
            );
            if (field != null)
            {
                Dictionary<string, bool> dict =
                    field.GetValue(null) as System.Collections.Generic.Dictionary<string, bool>;
                if (dict != null)
                {
                    dict[key] = value;
                }
            }
        }

        private string GetRegexFoldoutKey(SerializedProperty property)
        {
            MethodInfo method = typeof(SourceFolderEntryDrawer).GetMethod(
                "GetRegexFoldoutKey",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            return method?.Invoke(null, new object[] { property }) as string ?? string.Empty;
        }

        private string GetExcludeRegexFoldoutKey(SerializedProperty property)
        {
            MethodInfo method = typeof(SourceFolderEntryDrawer).GetMethod(
                "GetExcludeRegexFoldoutKey",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            return method?.Invoke(null, new object[] { property }) as string ?? string.Empty;
        }

        private string GetExcludePathFoldoutKey(SerializedProperty property)
        {
            MethodInfo method = typeof(SourceFolderEntryDrawer).GetMethod(
                "GetExcludePathFoldoutKey",
                BindingFlags.NonPublic | BindingFlags.Static
            );
            return method?.Invoke(null, new object[] { property }) as string ?? string.Empty;
        }
    }
#endif
}
