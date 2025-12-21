namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
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
            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();
            SerializableDictionaryPropertyDrawer.ClearMainFoldoutAnimCacheForTests();
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
            Rect controlRect = new(0f, 0f, 400f, 300f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 2;

                // Test ResolveContentRect directly with skipIndentation=false (normal context)
                Rect resolvedRect = SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: false
                );

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
            Rect controlRect = new(0f, 0f, 400f, 300f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 2;

                // Test ResolveContentRect with skipIndentation=true (settings context)
                Rect resolvedRect = SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: true
                );

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
            Rect controlRect = new(0f, 0f, 400f, 300f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                // Test ResolveContentRect directly with skipIndentation=false (normal context)
                Rect resolvedRect = SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: false
                );

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
            Rect controlRect = new(0f, 0f, 400f, 300f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                // Test ResolveContentRect with skipIndentation=true (settings context)
                Rect resolvedRect = SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: true
                );

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
            Rect controlRect = new(0f, 0f, 400f, 300f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 1;
                Rect rectAtLevel1 = SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: false
                );

                EditorGUI.indentLevel = 3;
                Rect rectAtLevel3 = SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: false
                );

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
            Rect controlRect = new(0f, 0f, 400f, 300f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;
                Rect rectAtLevel0 = SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: true
                );

                EditorGUI.indentLevel = 1;
                Rect rectAtLevel1 = SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: true
                );

                EditorGUI.indentLevel = 3;
                Rect rectAtLevel3 = SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: true
                );

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
            Rect controlRect = new(0f, 0f, 500f, 300f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 5;

                Rect resolvedRect = SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: true
                );

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
            Rect controlRect = new(0f, 0f, 500f, 300f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 3;

                Rect resolvedRect = SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: false
                );

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
            Rect controlRect = new(0f, 0f, 400f, 300f);

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
                    Rect resolvedRect =
                        SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: false
                        );

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
            Rect controlRect = new(0f, 0f, 400f, 300f);

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
                    Rect resolvedRect =
                        SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: false
                        );

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
            Rect controlRect = new(0f, 0f, 400f, 300f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                Rect noGroupRect = SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: false
                );

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
                    Rect withGroupRect =
                        SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: false
                        );

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
            Rect controlRect = new(0f, 0f, 400f, 300f);

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
                        Rect resolvedRect =
                            SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                                controlRect,
                                skipIndentation: false
                            );

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
        public void SettingsContextAppliesWGroupPaddingButIgnoresIndentation()
        {
            Rect controlRect = new(10f, 20f, 400f, 300f);

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
                    // Settings context uses skipIndentation=true, but WGroup padding is still applied
                    // because Unity's layout system doesn't automatically apply our WGroup padding
                    Rect resolvedRect =
                        SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: true
                        );

                    // WGroup padding should be applied
                    Assert.AreEqual(
                        controlRect.x + GroupLeftPadding,
                        resolvedRect.x,
                        0.01f,
                        "Settings context should apply WGroup left padding."
                    );

                    Assert.AreEqual(
                        controlRect.width - horizontalPadding,
                        resolvedRect.width,
                        0.01f,
                        "Settings context should apply WGroup horizontal padding to width."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [UnityTest]
        public IEnumerator OnGUISettingsContextAppliesWGroupPaddingButIgnoresIndentation()
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

            // WGroup padding should be applied even in settings context
            Assert.AreEqual(
                controlRect.x + GroupLeftPadding,
                capturedRect.x,
                0.01f,
                "OnGUI in settings context should apply WGroup left padding."
            );

            Assert.AreEqual(
                controlRect.width - horizontalPadding,
                capturedRect.width,
                0.01f,
                "OnGUI in settings context should apply WGroup horizontal padding to width."
            );
        }

        [Test]
        public void SettingsContextPreservesNonZeroRectPositionWithoutWGroup()
        {
            // Test settings context WITHOUT WGroup padding - should preserve position unchanged
            Rect controlRect = new(25f, 50f, 350f, 200f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 1;

                GroupGUIWidthUtility.ResetForTests();
                // No WGroup padding pushed - should preserve rect unchanged
                Rect resolvedRect = SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: true
                );

                Assert.AreEqual(
                    controlRect.x,
                    resolvedRect.x,
                    0.01f,
                    "Settings context without WGroup should preserve original rect x position."
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
        public void SettingsContextWithWGroupAppliesPadding()
        {
            Rect controlRect = new(0f, 0f, 450f, 280f);

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
                    Rect resolvedRect =
                        SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: true
                        );

                    // WGroup padding should be applied
                    Assert.AreEqual(
                        controlRect.x + GroupLeftPadding,
                        resolvedRect.x,
                        0.001f,
                        "Settings context with WGroup should apply left padding."
                    );

                    Assert.AreEqual(
                        0f,
                        resolvedRect.y,
                        0.001f,
                        "Settings context should preserve y=0 exactly."
                    );

                    Assert.AreEqual(
                        controlRect.width - horizontalPadding,
                        resolvedRect.width,
                        0.001f,
                        "Settings context should apply WGroup horizontal padding to width."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void SettingsContextWithNestedPaddingScopesAppliesAccumulatedPadding()
        {
            Rect controlRect = new(0f, 0f, 600f, 400f);

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
                        Rect resolvedRect =
                            SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                                controlRect,
                                skipIndentation: true
                            );

                        float totalLeftPadding = OuterLeftPadding + InnerLeftPadding;
                        float totalRightPadding = OuterRightPadding + InnerRightPadding;
                        float totalHorizontalPadding = totalLeftPadding + totalRightPadding;

                        Assert.AreEqual(
                            controlRect.x + totalLeftPadding,
                            resolvedRect.x,
                            0.01f,
                            "Settings context should apply accumulated WGroup left padding."
                        );

                        Assert.AreEqual(
                            controlRect.width - totalHorizontalPadding,
                            resolvedRect.width,
                            0.01f,
                            "Settings context should apply accumulated WGroup horizontal padding to width."
                        );
                    }
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }


        /// <summary>
        /// Test data for normal context indentation with various indent levels.
        /// </summary>
        private static IEnumerable<TestCaseData> NormalContextIndentationTestCases()
        {
            // Test case: (indentLevel, inputX, inputWidth, shouldIncreaseX, shouldDecreaseWidth)
            yield return new TestCaseData(0, 0f, 400f).SetName("IndentLevel0.ZeroStart");
            yield return new TestCaseData(1, 0f, 400f).SetName("IndentLevel1.ZeroStart");
            yield return new TestCaseData(2, 0f, 400f).SetName("IndentLevel2.ZeroStart");
            yield return new TestCaseData(3, 10f, 500f).SetName("IndentLevel3.NonZeroStart");
            yield return new TestCaseData(0, 50f, 300f).SetName("IndentLevel0.LargeOffset");
        }

        [TestCaseSource(nameof(NormalContextIndentationTestCases))]
        public void ResolveContentRectDataDriven(int indentLevel, float inputX, float inputWidth)
        {
            Rect controlRect = new(inputX, 0f, inputWidth, 300f);
            const float MinimumGroupIndent = 6f;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = indentLevel;
                GroupGUIWidthUtility.ResetForTests();

                Rect resolvedRect = SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: false
                );

                TestContext.WriteLine(
                    $"[DataDriven] indentLevel={indentLevel}, input=({inputX}, {inputWidth}), "
                        + $"resolved=({resolvedRect.x:F3}, {resolvedRect.width:F3})"
                );

                // At indentLevel 0 with no WGroup, MinimumGroupIndent should be applied to x
                if (indentLevel == 0)
                {
                    float expectedX = inputX + MinimumGroupIndent;
                    Assert.AreEqual(
                        expectedX,
                        resolvedRect.x,
                        0.1f,
                        $"At indentLevel 0, x should be input + MinimumGroupIndent ({MinimumGroupIndent})"
                    );

                    // Width should be reduced by MinimumGroupIndent
                    // Note: EditorGUI.IndentedRect at level 0 may also reduce width slightly
                    float maxExpectedWidth = inputWidth - MinimumGroupIndent;
                    Assert.LessOrEqual(
                        resolvedRect.width,
                        maxExpectedWidth + 0.1f,
                        "Width should be reduced by at least MinimumGroupIndent at indent level 0"
                    );
                }
                else
                {
                    // EditorGUI.IndentedRect applies indentation based on indentLevel
                    // The exact value depends on Unity's internal implementation
                    Assert.Greater(
                        resolvedRect.x,
                        inputX,
                        "At indentLevel > 0, x should be greater than input"
                    );

                    // Width should be reduced from original
                    Assert.Less(
                        resolvedRect.width,
                        inputWidth,
                        "Width should be reduced at indentLevel > 0"
                    );
                }

                // Width should always be non-negative
                Assert.GreaterOrEqual(resolvedRect.width, 0f, "Width should never be negative");

                // Width should not exceed original
                Assert.LessOrEqual(
                    resolvedRect.width,
                    inputWidth,
                    "Width should never exceed original width"
                );
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        /// <summary>
        /// Test data for settings context (skipIndentation=true) with WGroup padding.
        /// </summary>
        private static IEnumerable<TestCaseData> SettingsContextWithPaddingTestCases()
        {
            // Test case: (leftPadding, rightPadding, inputX, inputWidth)
            yield return new TestCaseData(10f, 5f, 0f, 400f).SetName("SmallPadding_ZeroStart");
            yield return new TestCaseData(20f, 15f, 5f, 500f).SetName("LargePadding_SmallOffset");
            yield return new TestCaseData(0f, 0f, 10f, 300f).SetName("NoPadding_NonZeroStart");
            yield return new TestCaseData(12f, 8f, 0f, 450f).SetName("MixedPadding_ZeroStart");
        }

        [TestCaseSource(nameof(SettingsContextWithPaddingTestCases))]
        public void ResolveContentRectSettingsContextDataDriven(
            float leftPadding,
            float rightPadding,
            float inputX,
            float inputWidth
        )
        {
            Rect controlRect = new(inputX, 0f, inputWidth, 300f);
            float horizontalPadding = leftPadding + rightPadding;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 2; // Should be ignored in settings context

                GroupGUIWidthUtility.ResetForTests();

                if (leftPadding > 0f || rightPadding > 0f)
                {
                    using (
                        GroupGUIWidthUtility.PushContentPadding(
                            horizontalPadding,
                            leftPadding,
                            rightPadding
                        )
                    )
                    {
                        Rect resolvedRect =
                            SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                                controlRect,
                                skipIndentation: true
                            );

                        TestContext.WriteLine(
                            $"[SettingsDataDriven] padding=({leftPadding}, {rightPadding}), "
                                + $"input=({inputX}, {inputWidth}), "
                                + $"resolved=({resolvedRect.x:F3}, {resolvedRect.width:F3})"
                        );

                        Assert.AreEqual(
                            inputX + leftPadding,
                            resolvedRect.x,
                            0.01f,
                            "Settings context should apply WGroup left padding"
                        );

                        Assert.AreEqual(
                            inputWidth - horizontalPadding,
                            resolvedRect.width,
                            0.01f,
                            "Settings context should apply WGroup horizontal padding to width"
                        );
                    }
                }
                else
                {
                    Rect resolvedRect =
                        SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: true
                        );

                    TestContext.WriteLine(
                        $"[SettingsDataDriven] no padding, input=({inputX}, {inputWidth}), "
                            + $"resolved=({resolvedRect.x:F3}, {resolvedRect.width:F3})"
                    );

                    Assert.AreEqual(
                        inputX,
                        resolvedRect.x,
                        0.01f,
                        "Settings context without WGroup should preserve x"
                    );

                    Assert.AreEqual(
                        inputWidth,
                        resolvedRect.width,
                        0.01f,
                        "Settings context without WGroup should preserve width"
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }



        [Test]
        public void ResolveContentRectWithZeroWidthPreservesMinimumBounds()
        {
            Rect controlRect = new(0f, 0f, 0f, 300f);

            GroupGUIWidthUtility.ResetForTests();
            Rect resolvedRect = SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                controlRect,
                skipIndentation: false
            );

            // Width should not go negative
            Assert.GreaterOrEqual(
                resolvedRect.width,
                0f,
                "Width should never be negative, even with zero input width"
            );
        }

        [Test]
        public void ResolveContentRectWithNegativeInputHandledGracefully()
        {
            Rect controlRect = new(-10f, 0f, 100f, 300f);

            GroupGUIWidthUtility.ResetForTests();
            Rect resolvedRect = SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                controlRect,
                skipIndentation: false
            );

            // Should handle negative x gracefully
            Assert.IsTrue(
                !float.IsNaN(resolvedRect.x) && !float.IsInfinity(resolvedRect.x),
                "X should be a valid number even with negative input"
            );
        }

        [Test]
        public void ResolveContentRectWithLargePaddingClampedToZeroWidth()
        {
            Rect controlRect = new(0f, 0f, 50f, 300f);
            const float LargeLeftPadding = 100f;
            const float LargeRightPadding = 100f;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        LargeLeftPadding + LargeRightPadding,
                        LargeLeftPadding,
                        LargeRightPadding
                    )
                )
                {
                    Rect resolvedRect =
                        SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: true
                        );

                    // When padding exceeds width, width should be clamped to 0
                    Assert.AreEqual(
                        0f,
                        resolvedRect.width,
                        0.001f,
                        "Width should be clamped to 0 when padding exceeds available width"
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void ResolveContentRectWithMaxIndentLevelDoesNotCrash()
        {
            Rect controlRect = new(0f, 0f, 1000f, 300f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 100; // Very high indent level

                GroupGUIWidthUtility.ResetForTests();
                Rect resolvedRect = SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: false
                );

                // Should not crash and return valid values
                Assert.IsTrue(
                    !float.IsNaN(resolvedRect.x) && !float.IsInfinity(resolvedRect.x),
                    "X should be a valid number even with very high indent level"
                );
                Assert.GreaterOrEqual(
                    resolvedRect.width,
                    0f,
                    "Width should be non-negative even with very high indent level"
                );
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

    }
}
