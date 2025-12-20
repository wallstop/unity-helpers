namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
    using System;
    using System.Collections;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.EditorFramework;

    /// <summary>
    /// Tests for ensuring correct indentation behavior of SerializableDictionary and
    /// SerializableSet property drawers in various contexts (normal inspector vs SettingsProvider).
    /// </summary>
    public sealed class SerializableDictionaryIndentationTests : CommonTestBase
    {
        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            GroupGUIWidthUtility.ResetForTests();
        }

        [Serializable]
        private sealed class TestIntStringDictionary : SerializableDictionary<int, string> { }

        private sealed class TestDictionaryHost : ScriptableObject
        {
            public TestIntStringDictionary dictionary = new();
        }

        [Test]
        public void ResolveContentRectNormalContextAppliesIndentation()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            host.dictionary[1] = "value1";
            host.dictionary[2] = "value2";

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Dictionary");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 2;

                drawer.GetPropertyHeight(dictionaryProperty, label);
                Rect resolvedRect = drawer.LastResolvedPosition;

                Assert.Greater(
                    resolvedRect.x,
                    controlRect.x,
                    "ResolvedPosition.x should be greater than controlRect.x when indentLevel > 0 in normal context."
                );
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void ResolveContentRectSettingsContextSkipsExtraIndentation()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            SerializedObject serializedSettings = TrackDisposable(new SerializedObject(settings));
            serializedSettings.Update();

            SerializedProperty paletteProp = serializedSettings.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );

            Assert.IsTrue(
                paletteProp != null,
                "Settings should have the WButtonCustomColors dictionary property."
            );

            if (paletteProp == null)
            {
                return;
            }

            paletteProp.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Palette");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 2;

                drawer.GetPropertyHeight(paletteProp, label);
                Rect resolvedRect = drawer.LastResolvedPosition;

                Assert.AreEqual(
                    controlRect.x,
                    resolvedRect.x,
                    0.01f,
                    "ResolvedPosition.x should match controlRect.x when targeting settings context (skip extra indentation)."
                );
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void ResolveContentRectNormalContextZeroIndentAppliesMinimumIndent()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            host.dictionary[1] = "value1";

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Dictionary");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                drawer.GetPropertyHeight(dictionaryProperty, label);
                Rect resolvedRect = drawer.LastResolvedPosition;

                float expectedMinimumIndent = 6f;
                Assert.AreEqual(
                    controlRect.x + expectedMinimumIndent,
                    resolvedRect.x,
                    0.01f,
                    "ResolvedPosition.x should have minimum indent applied when indentLevel is 0 in normal context."
                );
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void ResolveContentRectSettingsContextZeroIndentNoMinimumIndent()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            SerializedObject serializedSettings = TrackDisposable(new SerializedObject(settings));
            serializedSettings.Update();

            SerializedProperty paletteProp = serializedSettings.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );

            Assert.IsTrue(
                paletteProp != null,
                "Settings should have the WButtonCustomColors dictionary property."
            );

            if (paletteProp == null)
            {
                return;
            }

            paletteProp.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Palette");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                drawer.GetPropertyHeight(paletteProp, label);
                Rect resolvedRect = drawer.LastResolvedPosition;

                Assert.AreEqual(
                    controlRect.x,
                    resolvedRect.x,
                    0.01f,
                    "ResolvedPosition.x should match controlRect.x when targeting settings context even at indentLevel 0."
                );
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [UnityTest]
        public IEnumerator OnGUINormalContextRespectsIndentLevel()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            host.dictionary[1] = "value1";
            host.dictionary[2] = "value2";

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Dictionary");

            Rect capturedRect = default;
            int previousIndentLevel = EditorGUI.indentLevel;

            yield return TestIMGUIExecutor.Run(() =>
            {
                EditorGUI.indentLevel = 2;
                try
                {
                    serializedObject.UpdateIfRequiredOrScript();
                    drawer.OnGUI(controlRect, dictionaryProperty, label);
                    capturedRect = drawer.LastResolvedPosition;
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            Assert.Greater(
                capturedRect.x,
                controlRect.x,
                "OnGUI should respect indentLevel in normal context and indent the content."
            );
        }

        [UnityTest]
        public IEnumerator OnGUISettingsContextSkipsExtraIndentation()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            SerializedObject serializedSettings = TrackDisposable(new SerializedObject(settings));
            serializedSettings.Update();

            SerializedProperty paletteProp = serializedSettings.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );

            Assert.IsTrue(
                paletteProp != null,
                "Settings should have the WButtonCustomColors dictionary property."
            );

            if (paletteProp == null)
            {
                yield break;
            }

            paletteProp.isExpanded = true;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Palette");

            Rect capturedRect = default;
            int previousIndentLevel = EditorGUI.indentLevel;

            yield return TestIMGUIExecutor.Run(() =>
            {
                EditorGUI.indentLevel = 2;
                try
                {
                    serializedSettings.UpdateIfRequiredOrScript();
                    drawer.OnGUI(controlRect, paletteProp, label);
                    capturedRect = drawer.LastResolvedPosition;
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            Assert.AreEqual(
                controlRect.x,
                capturedRect.x,
                0.01f,
                "OnGUI should skip extra indentation when targeting settings context."
            );
        }

        [Test]
        public void NormalContextWithNestedIndentLevelStaysConsistent()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            host.dictionary[1] = "value1";

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Dictionary");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 1;
                drawer.GetPropertyHeight(dictionaryProperty, label);
                Rect rectAtLevel1 = drawer.LastResolvedPosition;

                EditorGUI.indentLevel = 3;
                drawer.GetPropertyHeight(dictionaryProperty, label);
                Rect rectAtLevel3 = drawer.LastResolvedPosition;

                Assert.Greater(
                    rectAtLevel3.x,
                    rectAtLevel1.x,
                    "Higher indentLevel should result in larger x offset."
                );
                Assert.Less(
                    rectAtLevel3.width,
                    rectAtLevel1.width,
                    "Higher indentLevel should result in smaller width."
                );
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void SettingsContextIgnoresAllIndentLevels()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            SerializedObject serializedSettings = TrackDisposable(new SerializedObject(settings));
            serializedSettings.Update();

            SerializedProperty paletteProp = serializedSettings.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );

            Assert.IsTrue(
                paletteProp != null,
                "Settings should have the WButtonCustomColors dictionary property."
            );

            if (paletteProp == null)
            {
                return;
            }

            paletteProp.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Palette");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;
                drawer.GetPropertyHeight(paletteProp, label);
                Rect rectAtLevel0 = drawer.LastResolvedPosition;

                EditorGUI.indentLevel = 1;
                drawer.GetPropertyHeight(paletteProp, label);
                Rect rectAtLevel1 = drawer.LastResolvedPosition;

                EditorGUI.indentLevel = 3;
                drawer.GetPropertyHeight(paletteProp, label);
                Rect rectAtLevel3 = drawer.LastResolvedPosition;

                Assert.AreEqual(
                    rectAtLevel0.x,
                    rectAtLevel1.x,
                    0.01f,
                    "Settings context should have same x position regardless of indentLevel."
                );
                Assert.AreEqual(
                    rectAtLevel0.x,
                    rectAtLevel3.x,
                    0.01f,
                    "Settings context should have same x position regardless of indentLevel."
                );
                Assert.AreEqual(
                    rectAtLevel0.width,
                    rectAtLevel1.width,
                    0.01f,
                    "Settings context should have same width regardless of indentLevel."
                );
                Assert.AreEqual(
                    rectAtLevel0.width,
                    rectAtLevel3.width,
                    0.01f,
                    "Settings context should have same width regardless of indentLevel."
                );
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void WidthIsPreservedInSettingsContext()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            SerializedObject serializedSettings = TrackDisposable(new SerializedObject(settings));
            serializedSettings.Update();

            SerializedProperty paletteProp = serializedSettings.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );

            Assert.IsTrue(
                paletteProp != null,
                "Settings should have the WButtonCustomColors dictionary property."
            );

            if (paletteProp == null)
            {
                return;
            }

            paletteProp.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 500f, 300f);
            GUIContent label = new("Palette");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 5;

                drawer.GetPropertyHeight(paletteProp, label);
                Rect resolvedRect = drawer.LastResolvedPosition;

                Assert.AreEqual(
                    controlRect.width,
                    resolvedRect.width,
                    0.01f,
                    "Width should be preserved in settings context (no indent reduction)."
                );
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void NormalContextReducesWidthWithIndentation()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            host.dictionary[1] = "value1";

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 500f, 300f);
            GUIContent label = new("Dictionary");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 3;

                drawer.GetPropertyHeight(dictionaryProperty, label);
                Rect resolvedRect = drawer.LastResolvedPosition;

                Assert.Less(
                    resolvedRect.width,
                    controlRect.width,
                    "Width should be reduced in normal context due to indentation."
                );
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void WGroupContextZeroIndentSkipsMinimumIndentWhenPaddingApplied()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            host.dictionary[1] = "value1";

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Dictionary");

            const float GroupLeftPadding = 12f;
            const float GroupRightPadding = 8f;
            float horizontalPadding = GroupLeftPadding + GroupRightPadding;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        GroupLeftPadding,
                        GroupRightPadding
                    )
                )
                {
                    drawer.GetPropertyHeight(dictionaryProperty, label);
                    Rect resolvedRect = drawer.LastResolvedPosition;

                    float expectedX = controlRect.x + GroupLeftPadding;
                    Assert.AreEqual(
                        expectedX,
                        resolvedRect.x,
                        0.01f,
                        "When inside WGroup (padding applied) at indentLevel 0, should NOT add MinimumGroupIndent."
                    );

                    float expectedWidth = controlRect.width - horizontalPadding;
                    Assert.AreEqual(
                        expectedWidth,
                        resolvedRect.width,
                        0.01f,
                        "Width should only account for WGroup padding, not MinimumGroupIndent."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void WGroupContextWithIndentLevelAppliesIndentWithoutDoubleOffset()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            host.dictionary[1] = "value1";

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Dictionary");

            const float GroupLeftPadding = 10f;
            const float GroupRightPadding = 10f;
            float horizontalPadding = GroupLeftPadding + GroupRightPadding;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 2;

                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        GroupLeftPadding,
                        GroupRightPadding
                    )
                )
                {
                    drawer.GetPropertyHeight(dictionaryProperty, label);
                    Rect resolvedRect = drawer.LastResolvedPosition;

                    Assert.Greater(
                        resolvedRect.x,
                        controlRect.x + GroupLeftPadding,
                        "With indentLevel > 0 inside WGroup, position should account for both WGroup padding and indent."
                    );

                    Assert.Less(
                        resolvedRect.width,
                        controlRect.width - horizontalPadding,
                        "Width should be reduced by both WGroup padding and indentation."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void WGroupContextVsNoGroupContextIndentationDifference()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            host.dictionary[1] = "value1";

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Dictionary");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                drawer.GetPropertyHeight(dictionaryProperty, label);
                Rect noGroupRect = drawer.LastResolvedPosition;

                const float GroupLeftPadding = 8f;
                const float GroupRightPadding = 8f;
                float horizontalPadding = GroupLeftPadding + GroupRightPadding;

                GroupGUIWidthUtility.ResetForTests();
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        GroupLeftPadding,
                        GroupRightPadding
                    )
                )
                {
                    drawer.GetPropertyHeight(dictionaryProperty, label);
                    Rect withGroupRect = drawer.LastResolvedPosition;

                    float expectedNoGroupX = controlRect.x + 6f;
                    float expectedWithGroupX = controlRect.x + GroupLeftPadding;

                    Assert.AreEqual(
                        expectedNoGroupX,
                        noGroupRect.x,
                        0.01f,
                        "Without WGroup at indentLevel 0, MinimumGroupIndent (6f) should be applied."
                    );

                    Assert.AreEqual(
                        expectedWithGroupX,
                        withGroupRect.x,
                        0.01f,
                        "With WGroup at indentLevel 0, only WGroup padding should be applied (no MinimumGroupIndent)."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void NestedWGroupContextAccumulatesPaddingCorrectly()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            host.dictionary[1] = "value1";

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Dictionary");

            const float OuterLeftPadding = 8f;
            const float OuterRightPadding = 8f;
            const float InnerLeftPadding = 6f;
            const float InnerRightPadding = 6f;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        OuterLeftPadding + OuterRightPadding,
                        OuterLeftPadding,
                        OuterRightPadding
                    )
                )
                {
                    using (
                        GroupGUIWidthUtility.PushContentPadding(
                            InnerLeftPadding + InnerRightPadding,
                            InnerLeftPadding,
                            InnerRightPadding
                        )
                    )
                    {
                        drawer.GetPropertyHeight(dictionaryProperty, label);
                        Rect resolvedRect = drawer.LastResolvedPosition;

                        float expectedTotalLeftPadding = OuterLeftPadding + InnerLeftPadding;
                        float expectedTotalRightPadding = OuterRightPadding + InnerRightPadding;
                        float expectedX = controlRect.x + expectedTotalLeftPadding;
                        float expectedWidth =
                            controlRect.width
                            - expectedTotalLeftPadding
                            - expectedTotalRightPadding;

                        Assert.AreEqual(
                            expectedX,
                            resolvedRect.x,
                            0.01f,
                            "Nested WGroups should accumulate left padding correctly."
                        );

                        Assert.AreEqual(
                            expectedWidth,
                            resolvedRect.width,
                            0.01f,
                            "Nested WGroups should accumulate total horizontal padding correctly."
                        );
                    }
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [UnityTest]
        public IEnumerator OnGUIInWGroupContextSkipsMinimumIndentAtZeroLevel()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            host.dictionary[1] = "value1";
            host.dictionary[2] = "value2";

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Dictionary");

            const float GroupLeftPadding = 15f;
            const float GroupRightPadding = 10f;
            float horizontalPadding = GroupLeftPadding + GroupRightPadding;

            Rect capturedRect = default;
            int previousIndentLevel = EditorGUI.indentLevel;

            yield return TestIMGUIExecutor.Run(() =>
            {
                EditorGUI.indentLevel = 0;
                try
                {
                    serializedObject.UpdateIfRequiredOrScript();
                    GroupGUIWidthUtility.ResetForTests();
                    using (
                        GroupGUIWidthUtility.PushContentPadding(
                            horizontalPadding,
                            GroupLeftPadding,
                            GroupRightPadding
                        )
                    )
                    {
                        drawer.OnGUI(controlRect, dictionaryProperty, label);
                        capturedRect = drawer.LastResolvedPosition;
                    }
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            float expectedX = controlRect.x + GroupLeftPadding;
            Assert.AreEqual(
                expectedX,
                capturedRect.x,
                0.01f,
                "OnGUI in WGroup context at indentLevel 0 should use WGroup padding without MinimumGroupIndent."
            );
        }

        [Test]
        public void SettingsContextIgnoresGroupPaddingUtilityState()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            SerializedObject serializedSettings = TrackDisposable(new SerializedObject(settings));
            serializedSettings.Update();

            SerializedProperty paletteProp = serializedSettings.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );

            Assert.IsTrue(
                paletteProp != null,
                "Settings should have the WButtonCustomColors dictionary property."
            );

            if (paletteProp == null)
            {
                return;
            }

            paletteProp.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(10f, 20f, 400f, 300f);
            GUIContent label = new("Palette");

            const float GroupLeftPadding = 15f;
            const float GroupRightPadding = 10f;
            float horizontalPadding = GroupLeftPadding + GroupRightPadding;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 2;

                GroupGUIWidthUtility.ResetForTests();
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        GroupLeftPadding,
                        GroupRightPadding
                    )
                )
                {
                    drawer.GetPropertyHeight(paletteProp, label);
                    Rect resolvedRect = drawer.LastResolvedPosition;

                    Assert.AreEqual(
                        controlRect.x,
                        resolvedRect.x,
                        0.01f,
                        "Settings context should return position unchanged regardless of GroupGUIWidthUtility padding state."
                    );

                    Assert.AreEqual(
                        controlRect.width,
                        resolvedRect.width,
                        0.01f,
                        "Settings context should return width unchanged regardless of GroupGUIWidthUtility padding state."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [UnityTest]
        public IEnumerator OnGUISettingsContextIgnoresGroupPaddingUtilityState()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            SerializedObject serializedSettings = TrackDisposable(new SerializedObject(settings));
            serializedSettings.Update();

            SerializedProperty paletteProp = serializedSettings.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );

            Assert.IsTrue(
                paletteProp != null,
                "Settings should have the WButtonCustomColors dictionary property."
            );

            if (paletteProp == null)
            {
                yield break;
            }

            paletteProp.isExpanded = true;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(5f, 10f, 500f, 300f);
            GUIContent label = new("Palette");

            const float GroupLeftPadding = 20f;
            const float GroupRightPadding = 15f;
            float horizontalPadding = GroupLeftPadding + GroupRightPadding;

            Rect capturedRect = default;
            int previousIndentLevel = EditorGUI.indentLevel;

            yield return TestIMGUIExecutor.Run(() =>
            {
                EditorGUI.indentLevel = 3;
                try
                {
                    serializedSettings.UpdateIfRequiredOrScript();
                    GroupGUIWidthUtility.ResetForTests();
                    using (
                        GroupGUIWidthUtility.PushContentPadding(
                            horizontalPadding,
                            GroupLeftPadding,
                            GroupRightPadding
                        )
                    )
                    {
                        drawer.OnGUI(controlRect, paletteProp, label);
                        capturedRect = drawer.LastResolvedPosition;
                    }
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            Assert.AreEqual(
                controlRect.x,
                capturedRect.x,
                0.01f,
                "OnGUI in settings context should return position.x unchanged even with GroupGUIWidthUtility padding."
            );

            Assert.AreEqual(
                controlRect.width,
                capturedRect.width,
                0.01f,
                "OnGUI in settings context should return width unchanged even with GroupGUIWidthUtility padding."
            );
        }

        [Test]
        public void SettingsContextPreservesNonZeroRectPosition()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            SerializedObject serializedSettings = TrackDisposable(new SerializedObject(settings));
            serializedSettings.Update();

            SerializedProperty paletteProp = serializedSettings.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );

            Assert.IsTrue(
                paletteProp != null,
                "Settings should have the WButtonCustomColors dictionary property."
            );

            if (paletteProp == null)
            {
                return;
            }

            paletteProp.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(25f, 50f, 350f, 200f);
            GUIContent label = new("Palette");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 1;

                GroupGUIWidthUtility.ResetForTests();
                drawer.GetPropertyHeight(paletteProp, label);
                Rect resolvedRect = drawer.LastResolvedPosition;

                Assert.AreEqual(
                    controlRect.x,
                    resolvedRect.x,
                    0.01f,
                    "Settings context should preserve original rect x position."
                );

                Assert.AreEqual(
                    controlRect.y,
                    resolvedRect.y,
                    0.01f,
                    "Settings context should preserve original rect y position."
                );

                Assert.AreEqual(
                    controlRect.width,
                    resolvedRect.width,
                    0.01f,
                    "Settings context should preserve original rect width."
                );

                Assert.AreEqual(
                    controlRect.height,
                    resolvedRect.height,
                    0.01f,
                    "Settings context should preserve original rect height."
                );
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void SettingsContextWithZeroPositionPreservesZeroExactly()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            SerializedObject serializedSettings = TrackDisposable(new SerializedObject(settings));
            serializedSettings.Update();

            SerializedProperty paletteProp = serializedSettings.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );

            Assert.IsTrue(
                paletteProp != null,
                "Settings should have the WButtonCustomColors dictionary property."
            );

            if (paletteProp == null)
            {
                return;
            }

            paletteProp.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 450f, 280f);
            GUIContent label = new("Palette");

            const float GroupLeftPadding = 12f;
            const float GroupRightPadding = 8f;
            float horizontalPadding = GroupLeftPadding + GroupRightPadding;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 2;

                GroupGUIWidthUtility.ResetForTests();
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        GroupLeftPadding,
                        GroupRightPadding
                    )
                )
                {
                    drawer.GetPropertyHeight(paletteProp, label);
                    Rect resolvedRect = drawer.LastResolvedPosition;

                    Assert.AreEqual(
                        0f,
                        resolvedRect.x,
                        0.001f,
                        "Settings context should preserve x=0 exactly, not applying any padding."
                    );

                    Assert.AreEqual(
                        0f,
                        resolvedRect.y,
                        0.001f,
                        "Settings context should preserve y=0 exactly."
                    );

                    Assert.AreEqual(
                        controlRect.width,
                        resolvedRect.width,
                        0.001f,
                        "Settings context should preserve original width despite WGroup padding scope."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void SettingsContextWithNestedPaddingScopesStillIgnoresPadding()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            SerializedObject serializedSettings = TrackDisposable(new SerializedObject(settings));
            serializedSettings.Update();

            SerializedProperty paletteProp = serializedSettings.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );

            Assert.IsTrue(
                paletteProp != null,
                "Settings should have the WButtonCustomColors dictionary property."
            );

            if (paletteProp == null)
            {
                return;
            }

            paletteProp.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 600f, 400f);
            GUIContent label = new("Palette");

            const float OuterLeftPadding = 10f;
            const float OuterRightPadding = 10f;
            const float InnerLeftPadding = 8f;
            const float InnerRightPadding = 8f;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        OuterLeftPadding + OuterRightPadding,
                        OuterLeftPadding,
                        OuterRightPadding
                    )
                )
                {
                    using (
                        GroupGUIWidthUtility.PushContentPadding(
                            InnerLeftPadding + InnerRightPadding,
                            InnerLeftPadding,
                            InnerRightPadding
                        )
                    )
                    {
                        drawer.GetPropertyHeight(paletteProp, label);
                        Rect resolvedRect = drawer.LastResolvedPosition;

                        Assert.AreEqual(
                            controlRect.x,
                            resolvedRect.x,
                            0.01f,
                            "Settings context should ignore nested GroupGUIWidthUtility padding scopes."
                        );

                        Assert.AreEqual(
                            controlRect.width,
                            resolvedRect.width,
                            0.01f,
                            "Settings context should preserve width despite nested padding scopes."
                        );
                    }
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }
    }
}
