namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
    using System.Collections;
    using System.Collections.Generic;
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
                        "When inside WGroup (padding applied) at indentLevel 0, should use WGroup left padding."
                    );

                    float expectedWidth = controlRect.width - horizontalPadding;
                    Assert.AreEqual(
                        expectedWidth,
                        resolvedRect.width,
                        0.01f,
                        "Width should account for WGroup padding."
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

                    float expectedNoGroupX = controlRect.x;
                    float expectedWithGroupX = controlRect.x + GroupLeftPadding;

                    Assert.AreEqual(
                        expectedNoGroupX,
                        noGroupRect.x,
                        0.01f,
                        "Without WGroup at indentLevel 0, should align with Unity's default list rendering (no offset)."
                    );

                    Assert.AreEqual(
                        expectedWithGroupX,
                        withGroupRect.x,
                        0.01f,
                        "With WGroup at indentLevel 0, WGroup padding should be applied."
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
                "OnGUI in WGroup context at indentLevel 0 should use WGroup padding."
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

                // At indentLevel 0 with no WGroup, aligns with Unity's default list rendering (no offset)
                if (indentLevel == 0)
                {
                    float expectedX = inputX;
                    Assert.AreEqual(
                        expectedX,
                        resolvedRect.x,
                        0.1f,
                        "At indentLevel 0, x should align with Unity's default list rendering (no offset)"
                    );

                    // Width should be unchanged at indentLevel 0 without WGroup
                    Assert.AreEqual(
                        inputWidth,
                        resolvedRect.width,
                        0.1f,
                        "Width should be unchanged at indent level 0 without WGroup"
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

        /// <summary>
        /// Tests that when IsInsideWGroupPropertyDraw is true, ResolveContentRect returns
        /// the position unchanged - Unity's layout has already positioned the rect.
        /// </summary>
        [Test]
        public void WGroupPropertyContextReturnsPositionUnchanged()
        {
            Rect controlRect = new(25f, 50f, 400f, 300f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 2;
                GroupGUIWidthUtility.ResetForTests();

                using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                {
                    Rect resolvedRect =
                        SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: false
                        );

                    TestContext.WriteLine(
                        $"[WGroupPropertyContextReturnsPositionUnchanged] "
                            + $"controlRect=({controlRect.x:F3}, {controlRect.y:F3}, {controlRect.width:F3}, {controlRect.height:F3}), "
                            + $"resolvedRect=({resolvedRect.x:F3}, {resolvedRect.y:F3}, {resolvedRect.width:F3}, {resolvedRect.height:F3})"
                    );

                    Assert.AreEqual(
                        controlRect.x,
                        resolvedRect.x,
                        0.001f,
                        "WGroupPropertyContext should return position.x unchanged."
                    );
                    Assert.AreEqual(
                        controlRect.y,
                        resolvedRect.y,
                        0.001f,
                        "WGroupPropertyContext should return position.y unchanged."
                    );
                    Assert.AreEqual(
                        controlRect.width,
                        resolvedRect.width,
                        0.001f,
                        "WGroupPropertyContext should return position.width unchanged."
                    );
                    Assert.AreEqual(
                        controlRect.height,
                        resolvedRect.height,
                        0.001f,
                        "WGroupPropertyContext should return position.height unchanged."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        /// <summary>
        /// Tests WGroupPropertyContext with various indent levels - rect should always be unchanged.
        /// </summary>
        [TestCase(0, TestName = "WGroupPropertyContextWithIndentLevel0")]
        [TestCase(1, TestName = "WGroupPropertyContextWithIndentLevel1")]
        [TestCase(2, TestName = "WGroupPropertyContextWithIndentLevel2")]
        [TestCase(5, TestName = "WGroupPropertyContextWithIndentLevel5")]
        [TestCase(10, TestName = "WGroupPropertyContextWithIndentLevel10")]
        public void WGroupPropertyContextIgnoresIndentLevel(int indentLevel)
        {
            Rect controlRect = new(15f, 30f, 450f, 250f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = indentLevel;
                GroupGUIWidthUtility.ResetForTests();

                using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                {
                    Rect resolvedRect =
                        SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: false
                        );

                    TestContext.WriteLine(
                        $"[WGroupPropertyContextIgnoresIndentLevel] "
                            + $"indentLevel={indentLevel}, "
                            + $"controlRect.x={controlRect.x:F3}, resolvedRect.x={resolvedRect.x:F3}"
                    );

                    Assert.AreEqual(
                        controlRect.x,
                        resolvedRect.x,
                        0.001f,
                        $"WGroupPropertyContext should ignore indentLevel {indentLevel} and return x unchanged."
                    );
                    Assert.AreEqual(
                        controlRect.width,
                        resolvedRect.width,
                        0.001f,
                        $"WGroupPropertyContext should ignore indentLevel {indentLevel} and return width unchanged."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        /// <summary>
        /// Tests that even with padding values set, the rect is unchanged in WGroup property context.
        /// This is the key distinction: PushWGroupPropertyContext means Unity's layout already applied padding.
        /// </summary>
        [Test]
        public void WGroupPropertyContextIgnoresPaddingValues()
        {
            Rect controlRect = new(20f, 40f, 500f, 300f);

            const float GroupLeftPadding = 15f;
            const float GroupRightPadding = 10f;
            float horizontalPadding = GroupLeftPadding + GroupRightPadding;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 1;
                GroupGUIWidthUtility.ResetForTests();

                // Push padding, then push WGroupPropertyContext
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        GroupLeftPadding,
                        GroupRightPadding
                    )
                )
                {
                    using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                    {
                        Rect resolvedRect =
                            SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                                controlRect,
                                skipIndentation: false
                            );

                        TestContext.WriteLine(
                            $"[WGroupPropertyContextIgnoresPaddingValues] "
                                + $"controlRect=({controlRect.x:F3}, {controlRect.width:F3}), "
                                + $"resolvedRect=({resolvedRect.x:F3}, {resolvedRect.width:F3}), "
                                + $"padding=({GroupLeftPadding:F3}, {GroupRightPadding:F3})"
                        );

                        // Despite padding being set, rect should be unchanged because
                        // Unity's layout system already applied it
                        Assert.AreEqual(
                            controlRect.x,
                            resolvedRect.x,
                            0.001f,
                            "WGroupPropertyContext should return x unchanged even with padding set."
                        );
                        Assert.AreEqual(
                            controlRect.width,
                            resolvedRect.width,
                            0.001f,
                            "WGroupPropertyContext should return width unchanged even with padding set."
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
        /// Tests zero padding and zero indent in WGroup property context.
        /// </summary>
        [Test]
        public void WGroupPropertyContextZeroPaddingZeroIndent()
        {
            Rect controlRect = new(0f, 0f, 400f, 300f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;
                GroupGUIWidthUtility.ResetForTests();

                using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                {
                    Rect resolvedRect =
                        SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: false
                        );

                    TestContext.WriteLine(
                        $"[WGroupPropertyContextZeroPaddingZeroIndent] "
                            + $"controlRect=({controlRect.x:F3}, {controlRect.width:F3}), "
                            + $"resolvedRect=({resolvedRect.x:F3}, {resolvedRect.width:F3})"
                    );

                    Assert.AreEqual(
                        controlRect.x,
                        resolvedRect.x,
                        0.001f,
                        "WGroupPropertyContext with zero padding and indent should return x unchanged."
                    );
                    Assert.AreEqual(
                        controlRect.width,
                        resolvedRect.width,
                        0.001f,
                        "WGroupPropertyContext with zero padding and indent should return width unchanged."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        /// <summary>
        /// Tests WGroupPropertyContext with maximum padding values.
        /// </summary>
        [Test]
        public void WGroupPropertyContextWithMaxPaddingValues()
        {
            Rect controlRect = new(100f, 50f, 600f, 400f);

            const float MaxLeftPadding = 100f;
            const float MaxRightPadding = 100f;
            float horizontalPadding = MaxLeftPadding + MaxRightPadding;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 5;
                GroupGUIWidthUtility.ResetForTests();

                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        MaxLeftPadding,
                        MaxRightPadding
                    )
                )
                {
                    using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                    {
                        Rect resolvedRect =
                            SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                                controlRect,
                                skipIndentation: false
                            );

                        TestContext.WriteLine(
                            $"[WGroupPropertyContextWithMaxPaddingValues] "
                                + $"controlRect=({controlRect.x:F3}, {controlRect.width:F3}), "
                                + $"resolvedRect=({resolvedRect.x:F3}, {resolvedRect.width:F3}), "
                                + $"maxPadding=({MaxLeftPadding:F3}, {MaxRightPadding:F3})"
                        );

                        Assert.AreEqual(
                            controlRect.x,
                            resolvedRect.x,
                            0.001f,
                            "WGroupPropertyContext should return x unchanged even with max padding."
                        );
                        Assert.AreEqual(
                            controlRect.width,
                            resolvedRect.width,
                            0.001f,
                            "WGroupPropertyContext should return width unchanged even with max padding."
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
        /// Tests WGroupPropertyContext with very high indent level.
        /// </summary>
        [Test]
        public void WGroupPropertyContextWithVeryHighIndentLevel()
        {
            Rect controlRect = new(50f, 25f, 800f, 500f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 50;
                GroupGUIWidthUtility.ResetForTests();

                using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                {
                    Rect resolvedRect =
                        SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: false
                        );

                    TestContext.WriteLine(
                        $"[WGroupPropertyContextWithVeryHighIndentLevel] "
                            + $"indentLevel=50, "
                            + $"controlRect=({controlRect.x:F3}, {controlRect.width:F3}), "
                            + $"resolvedRect=({resolvedRect.x:F3}, {resolvedRect.width:F3})"
                    );

                    Assert.AreEqual(
                        controlRect.x,
                        resolvedRect.x,
                        0.001f,
                        "WGroupPropertyContext should return x unchanged even with very high indent."
                    );
                    Assert.AreEqual(
                        controlRect.width,
                        resolvedRect.width,
                        0.001f,
                        "WGroupPropertyContext should return width unchanged even with very high indent."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        /// <summary>
        /// Tests nested WGroupPropertyContext scopes.
        /// </summary>
        [Test]
        public void WGroupPropertyContextNestedScopesReturnUnchanged()
        {
            Rect controlRect = new(30f, 60f, 350f, 200f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 3;
                GroupGUIWidthUtility.ResetForTests();

                using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                {
                    using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                    {
                        Rect resolvedRect =
                            SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                                controlRect,
                                skipIndentation: false
                            );

                        TestContext.WriteLine(
                            $"[WGroupPropertyContextNestedScopesReturnUnchanged] "
                                + $"controlRect=({controlRect.x:F3}, {controlRect.width:F3}), "
                                + $"resolvedRect=({resolvedRect.x:F3}, {resolvedRect.width:F3})"
                        );

                        Assert.AreEqual(
                            controlRect.x,
                            resolvedRect.x,
                            0.001f,
                            "Nested WGroupPropertyContext should return x unchanged."
                        );
                        Assert.AreEqual(
                            controlRect.width,
                            resolvedRect.width,
                            0.001f,
                            "Nested WGroupPropertyContext should return width unchanged."
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
        /// Tests that WGroupPropertyContext correctly restores state after disposal.
        /// </summary>
        [Test]
        public void WGroupPropertyContextRestoredAfterDisposal()
        {
            Rect controlRect = new(0f, 0f, 400f, 300f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 2;
                GroupGUIWidthUtility.ResetForTests();

                // First, resolve without WGroupPropertyContext
                Rect rectWithoutContext =
                    SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                        controlRect,
                        skipIndentation: false
                    );

                // Then with WGroupPropertyContext
                using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                {
                    Rect rectWithContext =
                        SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: false
                        );

                    Assert.AreEqual(
                        controlRect.x,
                        rectWithContext.x,
                        0.001f,
                        "Inside WGroupPropertyContext, x should be unchanged."
                    );
                }

                // After disposal, should be back to normal behavior
                Rect rectAfterContext =
                    SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                        controlRect,
                        skipIndentation: false
                    );

                TestContext.WriteLine(
                    $"[WGroupPropertyContextRestoredAfterDisposal] "
                        + $"rectWithoutContext.x={rectWithoutContext.x:F3}, "
                        + $"rectAfterContext.x={rectAfterContext.x:F3}"
                );

                Assert.AreEqual(
                    rectWithoutContext.x,
                    rectAfterContext.x,
                    0.001f,
                    "After WGroupPropertyContext disposal, behavior should be restored."
                );
                Assert.AreEqual(
                    rectWithoutContext.width,
                    rectAfterContext.width,
                    0.001f,
                    "After WGroupPropertyContext disposal, width behavior should be restored."
                );
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        /// <summary>
        /// Tests WGroupPropertyContext with skipIndentation=true parameter.
        /// Even in settings context, WGroupPropertyContext should return unchanged.
        /// </summary>
        [Test]
        public void WGroupPropertyContextWithSkipIndentationTrue()
        {
            Rect controlRect = new(10f, 20f, 400f, 300f);

            const float LeftPadding = 15f;
            const float RightPadding = 10f;
            float horizontalPadding = LeftPadding + RightPadding;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 2;
                GroupGUIWidthUtility.ResetForTests();

                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        LeftPadding,
                        RightPadding
                    )
                )
                {
                    using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                    {
                        Rect resolvedRect =
                            SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                                controlRect,
                                skipIndentation: true
                            );

                        TestContext.WriteLine(
                            $"[WGroupPropertyContextWithSkipIndentationTrue] "
                                + $"controlRect=({controlRect.x:F3}, {controlRect.width:F3}), "
                                + $"resolvedRect=({resolvedRect.x:F3}, {resolvedRect.width:F3})"
                        );

                        Assert.AreEqual(
                            controlRect.x,
                            resolvedRect.x,
                            0.001f,
                            "WGroupPropertyContext with skipIndentation=true should return x unchanged."
                        );
                        Assert.AreEqual(
                            controlRect.width,
                            resolvedRect.width,
                            0.001f,
                            "WGroupPropertyContext with skipIndentation=true should return width unchanged."
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
        /// Tests that padding is correctly applied when not in WGroup property context.
        /// This is the case where CurrentScopeDepth > 0 but IsInsideWGroupPropertyDraw = false.
        /// </summary>
        [Test]
        public void PushContentPaddingOnlyAppliesPadding()
        {
            Rect controlRect = new(0f, 0f, 400f, 300f);

            const float LeftPadding = 12f;
            const float RightPadding = 8f;
            float horizontalPadding = LeftPadding + RightPadding;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;
                GroupGUIWidthUtility.ResetForTests();

                // Push padding but NOT WGroupPropertyContext
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        LeftPadding,
                        RightPadding
                    )
                )
                {
                    Rect resolvedRect =
                        SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: false
                        );

                    TestContext.WriteLine(
                        $"[PushContentPaddingOnlyAppliesPadding] "
                            + $"controlRect=({controlRect.x:F3}, {controlRect.width:F3}), "
                            + $"resolvedRect=({resolvedRect.x:F3}, {resolvedRect.width:F3}), "
                            + $"expectedX={controlRect.x + LeftPadding:F3}"
                    );

                    // Without WGroupPropertyContext, padding should be manually applied
                    Assert.AreEqual(
                        controlRect.x + LeftPadding,
                        resolvedRect.x,
                        0.01f,
                        "Without WGroupPropertyContext, left padding should be manually applied."
                    );
                    Assert.AreEqual(
                        controlRect.width - horizontalPadding,
                        resolvedRect.width,
                        0.01f,
                        "Without WGroupPropertyContext, horizontal padding should reduce width."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        /// <summary>
        /// Tests nested padding accumulation without WGroupPropertyContext.
        /// </summary>
        [Test]
        public void PushContentPaddingNestedAccumulation()
        {
            Rect controlRect = new(0f, 0f, 500f, 300f);

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
                                skipIndentation: false
                            );

                        float totalLeftPadding = OuterLeftPadding + InnerLeftPadding;
                        float totalRightPadding = OuterRightPadding + InnerRightPadding;
                        float totalHorizontalPadding = totalLeftPadding + totalRightPadding;

                        TestContext.WriteLine(
                            $"[PushContentPaddingNestedAccumulation] "
                                + $"resolvedRect.x={resolvedRect.x:F3} (expected={totalLeftPadding:F3}), "
                                + $"resolvedRect.width={resolvedRect.width:F3} (expected={controlRect.width - totalHorizontalPadding:F3})"
                        );

                        Assert.AreEqual(
                            controlRect.x + totalLeftPadding,
                            resolvedRect.x,
                            0.01f,
                            "Nested padding should accumulate left padding correctly."
                        );
                        Assert.AreEqual(
                            controlRect.width - totalHorizontalPadding,
                            resolvedRect.width,
                            0.01f,
                            "Nested padding should accumulate total horizontal padding correctly."
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
        /// Tests combined padding and indent levels without WGroupPropertyContext.
        /// </summary>
        [Test]
        public void PushContentPaddingCombinedWithIndentLevel()
        {
            Rect controlRect = new(0f, 0f, 400f, 300f);

            const float LeftPadding = 10f;
            const float RightPadding = 10f;
            float horizontalPadding = LeftPadding + RightPadding;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 2;
                GroupGUIWidthUtility.ResetForTests();

                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        LeftPadding,
                        RightPadding
                    )
                )
                {
                    Rect resolvedRect =
                        SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: false
                        );

                    TestContext.WriteLine(
                        $"[PushContentPaddingCombinedWithIndentLevel] "
                            + $"resolvedRect.x={resolvedRect.x:F3}, "
                            + $"resolvedRect.width={resolvedRect.width:F3}, "
                            + $"minExpectedX={controlRect.x + LeftPadding:F3}"
                    );

                    // Should apply both padding AND indent
                    Assert.Greater(
                        resolvedRect.x,
                        controlRect.x + LeftPadding,
                        "Should apply both padding and indent offset."
                    );
                    Assert.Less(
                        resolvedRect.width,
                        controlRect.width - horizontalPadding,
                        "Should reduce width by both padding and indent."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        /// <summary>
        /// Tests that zero padding does not increase scope depth.
        /// </summary>
        [Test]
        public void PushContentPaddingZeroPaddingDoesNotIncreaseScopeDepth()
        {
            Rect controlRect = new(0f, 0f, 400f, 300f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;
                GroupGUIWidthUtility.ResetForTests();

                int initialScopeDepth = GroupGUIWidthUtility.CurrentScopeDepth;

                // Push zero padding
                using (GroupGUIWidthUtility.PushContentPadding(0f, 0f, 0f))
                {
                    int scopeDepthWithZeroPadding = GroupGUIWidthUtility.CurrentScopeDepth;

                    TestContext.WriteLine(
                        $"[PushContentPaddingZeroPaddingDoesNotIncreaseScopeDepth] "
                            + $"initialScopeDepth={initialScopeDepth}, "
                            + $"scopeDepthWithZeroPadding={scopeDepthWithZeroPadding}"
                    );

                    Assert.AreEqual(
                        initialScopeDepth,
                        scopeDepthWithZeroPadding,
                        "Zero padding should not increase scope depth."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        /// <summary>
        /// Tests contrast between WGroupPropertyContext (returns unchanged) vs
        /// PushContentPadding only (applies padding manually).
        /// </summary>
        [Test]
        public void ContrastWGroupPropertyContextVsPushContentPaddingOnly()
        {
            Rect controlRect = new(10f, 20f, 400f, 300f);

            const float LeftPadding = 15f;
            const float RightPadding = 10f;
            float horizontalPadding = LeftPadding + RightPadding;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 1;
                GroupGUIWidthUtility.ResetForTests();

                // Case 1: PushContentPadding only (no WGroupPropertyContext)
                Rect rectWithPaddingOnly;
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        LeftPadding,
                        RightPadding
                    )
                )
                {
                    rectWithPaddingOnly =
                        SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: false
                        );
                }

                GroupGUIWidthUtility.ResetForTests();

                // Case 2: PushContentPadding with WGroupPropertyContext
                Rect rectWithWGroupContext;
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        LeftPadding,
                        RightPadding
                    )
                )
                {
                    using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                    {
                        rectWithWGroupContext =
                            SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                                controlRect,
                                skipIndentation: false
                            );
                    }
                }

                TestContext.WriteLine(
                    $"[ContrastWGroupPropertyContextVsPushContentPaddingOnly] "
                        + $"paddingOnly=({rectWithPaddingOnly.x:F3}, {rectWithPaddingOnly.width:F3}), "
                        + $"withWGroupContext=({rectWithWGroupContext.x:F3}, {rectWithWGroupContext.width:F3})"
                );

                // PushContentPadding only should apply padding
                Assert.Greater(
                    rectWithPaddingOnly.x,
                    controlRect.x,
                    "PushContentPadding only should shift x position."
                );
                Assert.Less(
                    rectWithPaddingOnly.width,
                    controlRect.width,
                    "PushContentPadding only should reduce width."
                );

                // WGroupPropertyContext should return unchanged
                Assert.AreEqual(
                    controlRect.x,
                    rectWithWGroupContext.x,
                    0.001f,
                    "WGroupPropertyContext should return x unchanged."
                );
                Assert.AreEqual(
                    controlRect.width,
                    rectWithWGroupContext.width,
                    0.001f,
                    "WGroupPropertyContext should return width unchanged."
                );
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        /// <summary>
        /// Tests PushContentPadding with skipIndentation=true (settings context).
        /// Without WGroupPropertyContext, padding should still be applied.
        /// </summary>
        [Test]
        public void PushContentPaddingWithSkipIndentationTrue()
        {
            Rect controlRect = new(5f, 10f, 450f, 300f);

            const float LeftPadding = 12f;
            const float RightPadding = 8f;
            float horizontalPadding = LeftPadding + RightPadding;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 3; // Should be ignored with skipIndentation=true
                GroupGUIWidthUtility.ResetForTests();

                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        LeftPadding,
                        RightPadding
                    )
                )
                {
                    Rect resolvedRect =
                        SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: true
                        );

                    TestContext.WriteLine(
                        $"[PushContentPaddingWithSkipIndentationTrue] "
                            + $"controlRect=({controlRect.x:F3}, {controlRect.width:F3}), "
                            + $"resolvedRect=({resolvedRect.x:F3}, {resolvedRect.width:F3})"
                    );

                    // With skipIndentation=true but no WGroupPropertyContext, padding is still applied
                    Assert.AreEqual(
                        controlRect.x + LeftPadding,
                        resolvedRect.x,
                        0.01f,
                        "skipIndentation=true without WGroupPropertyContext should still apply left padding."
                    );
                    Assert.AreEqual(
                        controlRect.width - horizontalPadding,
                        resolvedRect.width,
                        0.01f,
                        "skipIndentation=true without WGroupPropertyContext should still apply horizontal padding."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        /// <summary>
        /// Tests WGroupPropertyContext with a rect at origin (0,0).
        /// </summary>
        [Test]
        public void WGroupPropertyContextWithRectAtOrigin()
        {
            Rect controlRect = new(0f, 0f, 300f, 200f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;
                GroupGUIWidthUtility.ResetForTests();

                using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                {
                    Rect resolvedRect =
                        SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: false
                        );

                    TestContext.WriteLine(
                        $"[WGroupPropertyContextWithRectAtOrigin] "
                            + $"controlRect=({controlRect.x:F3}, {controlRect.y:F3}), "
                            + $"resolvedRect=({resolvedRect.x:F3}, {resolvedRect.y:F3})"
                    );

                    Assert.AreEqual(
                        0f,
                        resolvedRect.x,
                        0.001f,
                        "WGroupPropertyContext should preserve x=0."
                    );
                    Assert.AreEqual(
                        0f,
                        resolvedRect.y,
                        0.001f,
                        "WGroupPropertyContext should preserve y=0."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        /// <summary>
        /// Tests WGroupPropertyContext with a very narrow rect.
        /// </summary>
        [Test]
        public void WGroupPropertyContextWithNarrowRect()
        {
            Rect controlRect = new(10f, 10f, 50f, 300f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 3;
                GroupGUIWidthUtility.ResetForTests();

                using (GroupGUIWidthUtility.PushContentPadding(40f, 20f, 20f))
                {
                    using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                    {
                        Rect resolvedRect =
                            SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                                controlRect,
                                skipIndentation: false
                            );

                        TestContext.WriteLine(
                            $"[WGroupPropertyContextWithNarrowRect] "
                                + $"controlRect.width={controlRect.width:F3}, "
                                + $"resolvedRect.width={resolvedRect.width:F3}"
                        );

                        Assert.AreEqual(
                            controlRect.width,
                            resolvedRect.width,
                            0.001f,
                            "WGroupPropertyContext should preserve width even for narrow rect."
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
        /// Tests triple-nested padding scopes without WGroupPropertyContext.
        /// </summary>
        [Test]
        public void TripleNestedPaddingScopesWithoutWGroupContext()
        {
            Rect controlRect = new(0f, 0f, 600f, 300f);

            const float Padding1Left = 10f;
            const float Padding1Right = 10f;
            const float Padding2Left = 8f;
            const float Padding2Right = 8f;
            const float Padding3Left = 6f;
            const float Padding3Right = 6f;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;
                GroupGUIWidthUtility.ResetForTests();

                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        Padding1Left + Padding1Right,
                        Padding1Left,
                        Padding1Right
                    )
                )
                {
                    using (
                        GroupGUIWidthUtility.PushContentPadding(
                            Padding2Left + Padding2Right,
                            Padding2Left,
                            Padding2Right
                        )
                    )
                    {
                        using (
                            GroupGUIWidthUtility.PushContentPadding(
                                Padding3Left + Padding3Right,
                                Padding3Left,
                                Padding3Right
                            )
                        )
                        {
                            Rect resolvedRect =
                                SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                                    controlRect,
                                    skipIndentation: false
                                );

                            float totalLeft = Padding1Left + Padding2Left + Padding3Left;
                            float totalRight = Padding1Right + Padding2Right + Padding3Right;
                            float totalHorizontal = totalLeft + totalRight;

                            TestContext.WriteLine(
                                $"[TripleNestedPaddingScopesWithoutWGroupContext] "
                                    + $"totalLeft={totalLeft:F3}, totalRight={totalRight:F3}, "
                                    + $"resolvedRect.x={resolvedRect.x:F3}, resolvedRect.width={resolvedRect.width:F3}"
                            );

                            Assert.AreEqual(
                                controlRect.x + totalLeft,
                                resolvedRect.x,
                                0.01f,
                                "Triple-nested padding should accumulate left padding."
                            );
                            Assert.AreEqual(
                                controlRect.width - totalHorizontal,
                                resolvedRect.width,
                                0.01f,
                                "Triple-nested padding should accumulate horizontal padding."
                            );
                        }
                    }
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        /// <summary>
        /// Tests asymmetric padding (different left and right values) without WGroupPropertyContext.
        /// </summary>
        [Test]
        public void AsymmetricPaddingWithoutWGroupContext()
        {
            Rect controlRect = new(0f, 0f, 400f, 300f);

            const float LeftPadding = 25f;
            const float RightPadding = 5f;
            float horizontalPadding = LeftPadding + RightPadding;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;
                GroupGUIWidthUtility.ResetForTests();

                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        LeftPadding,
                        RightPadding
                    )
                )
                {
                    Rect resolvedRect =
                        SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: false
                        );

                    TestContext.WriteLine(
                        $"[AsymmetricPaddingWithoutWGroupContext] "
                            + $"leftPadding={LeftPadding:F3}, rightPadding={RightPadding:F3}, "
                            + $"resolvedRect.x={resolvedRect.x:F3}, resolvedRect.width={resolvedRect.width:F3}"
                    );

                    Assert.AreEqual(
                        controlRect.x + LeftPadding,
                        resolvedRect.x,
                        0.01f,
                        "Asymmetric padding should apply left padding correctly."
                    );
                    Assert.AreEqual(
                        controlRect.width - horizontalPadding,
                        resolvedRect.width,
                        0.01f,
                        "Asymmetric padding should apply total horizontal padding to width."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        /// <summary>
        /// Tests WGroupPropertyContext combined with Settings context (UnityHelpersSettings).
        /// </summary>
        [UnityTest]
        public IEnumerator WGroupPropertyContextInSettingsContext()
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
            Rect controlRect = new(15f, 25f, 500f, 300f);
            GUIContent label = new("Palette");

            const float GroupLeftPadding = 20f;
            const float GroupRightPadding = 15f;
            float horizontalPadding = GroupLeftPadding + GroupRightPadding;

            Rect capturedRect = default;
            int previousIndentLevel = EditorGUI.indentLevel;

            yield return TestIMGUIExecutor.Run(() =>
            {
                EditorGUI.indentLevel = 2;
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
                        using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                        {
                            drawer.OnGUI(controlRect, paletteProp, label);
                            capturedRect = drawer.LastResolvedPosition;
                        }
                    }
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            TestContext.WriteLine(
                $"[WGroupPropertyContextInSettingsContext] "
                    + $"controlRect=({controlRect.x:F3}, {controlRect.width:F3}), "
                    + $"capturedRect=({capturedRect.x:F3}, {capturedRect.width:F3})"
            );

            // With WGroupPropertyContext, even in settings, the rect should be returned unchanged
            Assert.AreEqual(
                controlRect.x,
                capturedRect.x,
                0.01f,
                "WGroupPropertyContext in settings context should return x unchanged."
            );
            Assert.AreEqual(
                controlRect.width,
                capturedRect.width,
                0.01f,
                "WGroupPropertyContext in settings context should return width unchanged."
            );
        }
    }
}
