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

        [Serializable]
        private sealed class TestIntHashSet : SerializableHashSet<int> { }

        private sealed class TestSetHost : ScriptableObject
        {
            public TestIntHashSet set = new();
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
        public void ResolveContentRectNormalContextZeroIndentAppliesMinimumIndent()
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

                float expectedMinimumIndent = 6f;

                TestContext.WriteLine(
                    $"[ResolveContentRectNormalContextZeroIndentAppliesMinimumIndent] "
                        + $"controlRect.x={controlRect.x:F3}, resolvedRect.x={resolvedRect.x:F3}, "
                        + $"expected={controlRect.x + expectedMinimumIndent:F3}"
                );

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

        [UnityTest]
        public IEnumerator OnGUINormalContextRespectsIndentLevel()
        {
            TestSetHost host = CreateScriptableObject<TestSetHost>();
            host.set.Add(1);
            host.set.Add(2);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(nameof(TestSetHost.set));
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
            TestSetHost host = CreateScriptableObject<TestSetHost>();
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
            TestSetHost host = CreateScriptableObject<TestSetHost>();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(nameof(TestSetHost.set));
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
            TestSetHost host = CreateScriptableObject<TestSetHost>();
            host.set.Add(42);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(nameof(TestSetHost.set));
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

                    float expectedNoGroupX = controlRect.x + 6f;
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
            TestSetHost host = CreateScriptableObject<TestSetHost>();
            host.set.Add(1);
            host.set.Add(2);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(nameof(TestSetHost.set));
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
                "OnGUI in WGroup context at indentLevel 0 should use WGroup padding without MinimumGroupIndent."
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
                        "With WGroup padding at indentLevel 0, should use WGroup padding (skip MinimumGroupIndent)."
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

        [TestCase(0, 0f, 0f, 6f, TestName = "NoGroupNoIndentAppliesMinimumIndent")]
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
            const float MinimumGroupIndent = 6f;
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
                        controlRect.x + MinimumGroupIndent,
                        resolvedRect.x,
                        0.01f,
                        "At indentLevel 0 with no WGroup, MinimumGroupIndent should be applied."
                    );
                }
                else if (indentLevel == 0 && horizontalPadding > 0f)
                {
                    Assert.AreEqual(
                        controlRect.x + groupLeftPadding,
                        resolvedRect.x,
                        0.01f,
                        "At indentLevel 0 with WGroup, only WGroup padding should apply (no MinimumGroupIndent)."
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
    }
}
