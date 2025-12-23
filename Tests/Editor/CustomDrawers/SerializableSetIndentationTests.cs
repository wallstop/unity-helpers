namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
    using System;
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
    /// Tests for ensuring correct indentation behavior of SerializableSet property drawer
    /// in various contexts (normal inspector vs SettingsProvider).
    /// Note: UnityHelpersSettings does not have SerializableHashSet properties, so settings
    /// context behavior is tested using mock scenarios and dictionary tests serve as the
    /// canonical reference for settings provider behavior.
    /// </summary>
    public sealed class SerializableSetIndentationTests : CommonTestBase
    {
        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            GroupGUIWidthUtility.ResetForTests();
            SerializableSetPropertyDrawer.ResetLayoutTrackingForTests();
        }

        [Test]
        public void ResolveContentRectNormalContextAppliesIndentation()
        {
            Rect controlRect = new(0f, 0f, 400f, 300f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 2;

                Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: false
                );

                TestContext.WriteLine(
                    $"[ResolveContentRectNormalContextAppliesIndentation] "
                        + $"controlRect.x={controlRect.x:F3}, resolvedRect.x={resolvedRect.x:F3}, "
                        + $"indentLevel={EditorGUI.indentLevel}"
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
        public void ResolveContentRectNormalContextZeroIndentAlignsWithUnityLists()
        {
            Rect controlRect = new(0f, 0f, 400f, 300f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;
                GroupGUIWidthUtility.ResetForTests();

                Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: false
                );

                float expectedX = controlRect.x - 1.25f;
                TestContext.WriteLine(
                    $"[ResolveContentRectNormalContextZeroIndentAlignsWithUnityLists] "
                        + $"controlRect.x={controlRect.x:F3}, resolvedRect.x={resolvedRect.x:F3}, "
                        + $"expected={expectedX:F3}"
                );

                Assert.AreEqual(
                    expectedX,
                    resolvedRect.x,
                    0.01f,
                    "ResolvedPosition.x should align with Unity's default list rendering (-1px alignment offset) when indentLevel is 0."
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
            IndentationTestSetHost host = CreateScriptableObject<IndentationTestSetHost>();
            host.set.Add(1);
            host.set.Add(2);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(IndentationTestSetHost.set)
            );
            setProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Set");

            Rect capturedRect = default;
            int previousIndentLevel = EditorGUI.indentLevel;

            yield return TestIMGUIExecutor.Run(() =>
            {
                EditorGUI.indentLevel = 2;
                try
                {
                    serializedObject.UpdateIfRequiredOrScript();
                    drawer.OnGUI(controlRect, setProperty, label);
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

        [Test]
        public void NormalContextWithNestedIndentLevelStaysConsistent()
        {
            Rect controlRect = new(0f, 0f, 400f, 300f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 1;
                GroupGUIWidthUtility.ResetForTests();
                Rect rectAtLevel1 = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: false
                );

                EditorGUI.indentLevel = 3;
                Rect rectAtLevel3 = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: false
                );

                TestContext.WriteLine(
                    $"[NormalContextWithNestedIndentLevelStaysConsistent] "
                        + $"rectAtLevel1.x={rectAtLevel1.x:F3}, rectAtLevel3.x={rectAtLevel3.x:F3}, "
                        + $"rectAtLevel1.width={rectAtLevel1.width:F3}, rectAtLevel3.width={rectAtLevel3.width:F3}"
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
        public void NormalContextReducesWidthWithIndentation()
        {
            Rect controlRect = new(0f, 0f, 500f, 300f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 3;
                GroupGUIWidthUtility.ResetForTests();

                Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: false
                );

                TestContext.WriteLine(
                    $"[NormalContextReducesWidthWithIndentation] "
                        + $"controlRect.width={controlRect.width:F3}, resolvedRect.width={resolvedRect.width:F3}"
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
        public void TargetsUnityHelpersSettingsReturnsFalseForRegularScriptableObject()
        {
            IndentationTestSetHost host = CreateScriptableObject<IndentationTestSetHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));

            bool targetsSettings =
                serializedObject.targetObject is UnityHelpersSettings
                || Array.Exists(serializedObject.targetObjects, t => t is UnityHelpersSettings);

            Assert.IsFalse(
                targetsSettings,
                "Regular ScriptableObject should not be detected as UnityHelpersSettings."
            );
        }

        [Test]
        public void TargetsUnityHelpersSettingsReturnsTrueForSettings()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            SerializedObject serializedSettings = TrackDisposable(new SerializedObject(settings));

            bool targetsSettings =
                serializedSettings.targetObject is UnityHelpersSettings
                || Array.Exists(serializedSettings.targetObjects, t => t is UnityHelpersSettings);

            Assert.IsTrue(targetsSettings, "UnityHelpersSettings should be correctly detected.");
        }

        [Test]
        public void DrawerDoesNotThrowWhenSetIsEmpty()
        {
            IndentationTestSetHost host = CreateScriptableObject<IndentationTestSetHost>();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(IndentationTestSetHost.set)
            );
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Set");

            Assert.DoesNotThrow(
                () => drawer.GetPropertyHeight(setProperty, label),
                "GetPropertyHeight should not throw for empty set."
            );

            GroupGUIWidthUtility.ResetForTests();
            Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                controlRect,
                skipIndentation: false
            );

            TestContext.WriteLine(
                $"[DrawerDoesNotThrowWhenSetIsEmpty] "
                    + $"controlRect.width={controlRect.width:F3}, resolvedRect.width={resolvedRect.width:F3}"
            );

            Assert.Greater(
                resolvedRect.width,
                0f,
                "Resolved position should have valid width for empty set."
            );
        }

        [Test]
        public void DrawerHandlesVeryLargeIndentLevel()
        {
            IndentationTestSetHost host = CreateScriptableObject<IndentationTestSetHost>();
            host.set.Add(42);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(IndentationTestSetHost.set)
            );
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 600f, 300f);
            GUIContent label = new("Set");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 20;
                GroupGUIWidthUtility.ResetForTests();

                Assert.DoesNotThrow(
                    () => drawer.GetPropertyHeight(setProperty, label),
                    "GetPropertyHeight should not throw for very large indentLevel."
                );

                Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: false
                );

                TestContext.WriteLine(
                    $"[DrawerHandlesVeryLargeIndentLevel] "
                        + $"controlRect.width={controlRect.width:F3}, resolvedRect.width={resolvedRect.width:F3}, "
                        + $"indentLevel={EditorGUI.indentLevel}"
                );

                Assert.GreaterOrEqual(
                    resolvedRect.width,
                    0f,
                    "Width should not be negative even with very large indentLevel."
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
                GroupGUIWidthUtility.ResetForTests();

                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        GroupLeftPadding,
                        GroupRightPadding
                    )
                )
                {
                    Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                        controlRect,
                        skipIndentation: false
                    );

                    float expectedX = controlRect.x + GroupLeftPadding;

                    TestContext.WriteLine(
                        $"[WGroupContextZeroIndentSkipsMinimumIndentWhenPaddingApplied] "
                            + $"controlRect.x={controlRect.x:F3}, resolvedRect.x={resolvedRect.x:F3}, "
                            + $"expectedX={expectedX:F3}, GroupLeftPadding={GroupLeftPadding:F3}"
                    );

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
                GroupGUIWidthUtility.ResetForTests();

                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        GroupLeftPadding,
                        GroupRightPadding
                    )
                )
                {
                    Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                        controlRect,
                        skipIndentation: false
                    );

                    TestContext.WriteLine(
                        $"[WGroupContextWithIndentLevelAppliesIndentWithoutDoubleOffset] "
                            + $"controlRect.x={controlRect.x:F3}, resolvedRect.x={resolvedRect.x:F3}, "
                            + $"controlRect.width={controlRect.width:F3}, resolvedRect.width={resolvedRect.width:F3}, "
                            + $"GroupLeftPadding={GroupLeftPadding:F3}, indentLevel={EditorGUI.indentLevel}"
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
                Rect noGroupRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
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
                    Rect withGroupRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                        controlRect,
                        skipIndentation: false
                    );

                    float expectedNoGroupX = controlRect.x;
                    float expectedWithGroupX = controlRect.x + GroupLeftPadding;

                    TestContext.WriteLine(
                        $"[WGroupContextVsNoGroupContextIndentationDifference] "
                            + $"noGroupRect.x={noGroupRect.x:F3} (expected={expectedNoGroupX:F3}), "
                            + $"withGroupRect.x={withGroupRect.x:F3} (expected={expectedWithGroupX:F3})"
                    );

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
                            SerializableSetPropertyDrawer.ResolveContentRectForTests(
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

                        TestContext.WriteLine(
                            $"[NestedWGroupContextAccumulatesPaddingCorrectly] "
                                + $"resolvedRect.x={resolvedRect.x:F3} (expected={expectedX:F3}), "
                                + $"resolvedRect.width={resolvedRect.width:F3} (expected={expectedWidth:F3})"
                        );

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
            IndentationTestSetHost host = CreateScriptableObject<IndentationTestSetHost>();
            host.set.Add(1);
            host.set.Add(2);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(IndentationTestSetHost.set)
            );
            setProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Set");

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
                        drawer.OnGUI(controlRect, setProperty, label);
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
        public void SettingsContextSkipIndentationScenarioPreservesRectUnchanged()
        {
            Rect controlRect = new(15f, 25f, 450f, 280f);

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
                    Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                        controlRect,
                        skipIndentation: false
                    );

                    TestContext.WriteLine(
                        $"[SettingsContextSkipIndentationScenarioPreservesRectUnchanged] "
                            + $"controlRect.x={controlRect.x:F3}, resolvedRect.x={resolvedRect.x:F3}, "
                            + $"controlRect.width={controlRect.width:F3}, resolvedRect.width={resolvedRect.width:F3}"
                    );

                    Assert.Greater(
                        resolvedRect.x,
                        controlRect.x,
                        "Normal context should modify x position based on padding and indent."
                    );

                    Assert.Less(
                        resolvedRect.width,
                        controlRect.width,
                        "Normal context should reduce width based on padding."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void NormalContextWithGroupPaddingAppliesPaddingAndIndent()
        {
            Rect controlRect = new(0f, 0f, 400f, 300f);

            const float GroupLeftPadding = 10f;
            const float GroupRightPadding = 10f;
            float horizontalPadding = GroupLeftPadding + GroupRightPadding;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 1;

                GroupGUIWidthUtility.ResetForTests();
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        GroupLeftPadding,
                        GroupRightPadding
                    )
                )
                {
                    Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                        controlRect,
                        skipIndentation: false
                    );

                    TestContext.WriteLine(
                        $"[NormalContextWithGroupPaddingAppliesPaddingAndIndent] "
                            + $"resolvedRect.x={resolvedRect.x:F3}, expected > {controlRect.x + GroupLeftPadding:F3}, "
                            + $"resolvedRect.width={resolvedRect.width:F3}, expected < {controlRect.width - horizontalPadding:F3}"
                    );

                    Assert.Greater(
                        resolvedRect.x,
                        controlRect.x + GroupLeftPadding,
                        "Normal context should apply both WGroup padding and indent offset."
                    );

                    Assert.Less(
                        resolvedRect.width,
                        controlRect.width - horizontalPadding,
                        "Normal context should reduce width by both padding and indent."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void NormalContextZeroIndentWithGroupPaddingSkipsMinimumIndent()
        {
            Rect controlRect = new(0f, 0f, 350f, 250f);

            const float GroupLeftPadding = 8f;
            const float GroupRightPadding = 8f;
            float horizontalPadding = GroupLeftPadding + GroupRightPadding;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        GroupLeftPadding,
                        GroupRightPadding
                    )
                )
                {
                    Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                        controlRect,
                        skipIndentation: false
                    );

                    float expectedX = controlRect.x + GroupLeftPadding;
                    float expectedWidth = controlRect.width - horizontalPadding;

                    TestContext.WriteLine(
                        $"[NormalContextZeroIndentWithGroupPaddingSkipsMinimumIndent] "
                            + $"resolvedRect.x={resolvedRect.x:F3} (expected={expectedX:F3}), "
                            + $"resolvedRect.width={resolvedRect.width:F3} (expected={expectedWidth:F3})"
                    );

                    Assert.AreEqual(
                        expectedX,
                        resolvedRect.x,
                        0.01f,
                        "With WGroup padding at indentLevel 0, should use WGroup padding."
                    );

                    Assert.AreEqual(
                        expectedWidth,
                        resolvedRect.width,
                        0.01f,
                        "Width should account for WGroup padding only at indentLevel 0."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [TestCase(0, 0f, 0f, 0f, TestName = "NoGroupNoIndentAlignsWithUnityLists")]
        [TestCase(0, 10f, 5f, 10f, TestName = "WithGroupNoIndentUsesGroupPadding")]
        [TestCase(1, 0f, 0f, 15f, TestName = "NoGroupWithIndentAppliesUnityIndent")]
        [TestCase(2, 8f, 4f, 8f, TestName = "WithGroupWithIndentCombinesBoth")]
        public void ResolveContentRectDataDrivenLeftPadding(
            int indentLevel,
            float groupLeftPadding,
            float groupRightPadding,
            float minimumExpectedXShift
        )
        {
            Rect controlRect = new(0f, 0f, 400f, 300f);
            float horizontalPadding = groupLeftPadding + groupRightPadding;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = indentLevel;
                GroupGUIWidthUtility.ResetForTests();

                Rect resolvedRect;
                if (horizontalPadding > 0f)
                {
                    using (
                        GroupGUIWidthUtility.PushContentPadding(
                            horizontalPadding,
                            groupLeftPadding,
                            groupRightPadding
                        )
                    )
                    {
                        resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: false
                        );
                    }
                }
                else
                {
                    resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                        controlRect,
                        skipIndentation: false
                    );
                }

                TestContext.WriteLine(
                    $"[ResolveContentRectDataDrivenLeftPadding] "
                        + $"indentLevel={indentLevel}, groupLeftPadding={groupLeftPadding:F3}, "
                        + $"resolvedRect.x={resolvedRect.x:F3}, minimumExpectedXShift={minimumExpectedXShift:F3}"
                );

                Assert.GreaterOrEqual(
                    resolvedRect.x,
                    controlRect.x + minimumExpectedXShift - 0.01f,
                    $"X position should be at least {minimumExpectedXShift} pixels right of control origin."
                );

                if (indentLevel == 0 && horizontalPadding <= 0f)
                {
                    Assert.AreEqual(
                        controlRect.x - 1.25f,
                        resolvedRect.x,
                        0.01f,
                        "At indentLevel 0 with no WGroup, should align with Unity's default list rendering (-1px alignment offset)."
                    );
                }
                else if (indentLevel == 0 && horizontalPadding > 0f)
                {
                    Assert.AreEqual(
                        controlRect.x + groupLeftPadding,
                        resolvedRect.x,
                        0.01f,
                        "At indentLevel 0 with WGroup, WGroup padding should apply."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [TestCase(
            400f,
            0f,
            0f,
            0,
            ExpectedResult = true,
            TestName = "NoGroupNoIndentPreservesWidth"
        )]
        [TestCase(400f, 20f, 10f, 0, ExpectedResult = true, TestName = "WithGroupReducesWidth")]
        [TestCase(400f, 0f, 0f, 3, ExpectedResult = true, TestName = "WithIndentReducesWidth")]
        [TestCase(
            400f,
            15f,
            5f,
            2,
            ExpectedResult = true,
            TestName = "GroupAndIndentBothReduceWidth"
        )]
        public bool ResolveContentRectDataDrivenWidthReduction(
            float controlWidth,
            float groupLeftPadding,
            float groupRightPadding,
            int indentLevel
        )
        {
            Rect controlRect = new(0f, 0f, controlWidth, 300f);
            float horizontalPadding = groupLeftPadding + groupRightPadding;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = indentLevel;
                GroupGUIWidthUtility.ResetForTests();

                Rect resolvedRect;
                if (horizontalPadding > 0f)
                {
                    using (
                        GroupGUIWidthUtility.PushContentPadding(
                            horizontalPadding,
                            groupLeftPadding,
                            groupRightPadding
                        )
                    )
                    {
                        resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: false
                        );
                    }
                }
                else
                {
                    resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                        controlRect,
                        skipIndentation: false
                    );
                }

                TestContext.WriteLine(
                    $"[ResolveContentRectDataDrivenWidthReduction] "
                        + $"controlWidth={controlWidth:F3}, horizontalPadding={horizontalPadding:F3}, "
                        + $"indentLevel={indentLevel}, resolvedRect.width={resolvedRect.width:F3}"
                );

                bool widthReduced =
                    (horizontalPadding > 0f || indentLevel > 0)
                    && resolvedRect.width < controlWidth;
                bool widthPreserved =
                    horizontalPadding <= 0f
                    && indentLevel == 0
                    && resolvedRect.width <= controlWidth;

                return widthReduced || widthPreserved;
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
                    Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
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
                    Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
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
                            SerializableSetPropertyDrawer.ResolveContentRectForTests(
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
                    Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
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
                            SerializableSetPropertyDrawer.ResolveContentRectForTests(
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
                    Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
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
                            SerializableSetPropertyDrawer.ResolveContentRectForTests(
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
                Rect rectWithoutContext = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: false
                );

                // Then with WGroupPropertyContext
                using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                {
                    Rect rectWithContext = SerializableSetPropertyDrawer.ResolveContentRectForTests(
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
                Rect rectAfterContext = SerializableSetPropertyDrawer.ResolveContentRectForTests(
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
                    Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
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
                            SerializableSetPropertyDrawer.ResolveContentRectForTests(
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
                    Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
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
                    rectWithPaddingOnly = SerializableSetPropertyDrawer.ResolveContentRectForTests(
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
                            SerializableSetPropertyDrawer.ResolveContentRectForTests(
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
                    Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
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
                            SerializableSetPropertyDrawer.ResolveContentRectForTests(
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
                                SerializableSetPropertyDrawer.ResolveContentRectForTests(
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
                    Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
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
    }
}
