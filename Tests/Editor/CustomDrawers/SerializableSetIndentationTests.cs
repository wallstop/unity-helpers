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
            TestSetHost host = CreateScriptableObject<TestSetHost>();
            host.set.Add(1);
            host.set.Add(2);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(nameof(TestSetHost.set));
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Set");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 2;

                drawer.GetPropertyHeight(setProperty, label);
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
        public void ResolveContentRectNormalContextZeroIndentAppliesMinimumIndent()
        {
            TestSetHost host = CreateScriptableObject<TestSetHost>();
            host.set.Add(1);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(nameof(TestSetHost.set));
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Set");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                drawer.GetPropertyHeight(setProperty, label);
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
            TestSetHost host = CreateScriptableObject<TestSetHost>();
            host.set.Add(1);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(nameof(TestSetHost.set));
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Set");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 1;
                drawer.GetPropertyHeight(setProperty, label);
                Rect rectAtLevel1 = drawer.LastResolvedPosition;

                EditorGUI.indentLevel = 3;
                drawer.GetPropertyHeight(setProperty, label);
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
        public void NormalContextReducesWidthWithIndentation()
        {
            TestSetHost host = CreateScriptableObject<TestSetHost>();
            host.set.Add(1);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(nameof(TestSetHost.set));
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 500f, 300f);
            GUIContent label = new("Set");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 3;

                drawer.GetPropertyHeight(setProperty, label);
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

            Assert.Greater(
                drawer.LastResolvedPosition.width,
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

                Assert.DoesNotThrow(
                    () => drawer.GetPropertyHeight(setProperty, label),
                    "GetPropertyHeight should not throw for very large indentLevel."
                );

                Rect resolvedRect = drawer.LastResolvedPosition;
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
            TestSetHost host = CreateScriptableObject<TestSetHost>();
            host.set.Add(1);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(nameof(TestSetHost.set));
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Set");

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
                    drawer.GetPropertyHeight(setProperty, label);
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
            TestSetHost host = CreateScriptableObject<TestSetHost>();
            host.set.Add(1);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(nameof(TestSetHost.set));
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Set");

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
                    drawer.GetPropertyHeight(setProperty, label);
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
            TestSetHost host = CreateScriptableObject<TestSetHost>();
            host.set.Add(1);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(nameof(TestSetHost.set));
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Set");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                drawer.GetPropertyHeight(setProperty, label);
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
                    drawer.GetPropertyHeight(setProperty, label);
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
            TestSetHost host = CreateScriptableObject<TestSetHost>();
            host.set.Add(1);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(nameof(TestSetHost.set));
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Set");

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
                        drawer.GetPropertyHeight(setProperty, label);
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
            TestSetHost host = CreateScriptableObject<TestSetHost>();
            host.set.Add(1);
            host.set.Add(2);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(nameof(TestSetHost.set));
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(15f, 25f, 450f, 280f);
            GUIContent label = new("Set");

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
                    drawer.GetPropertyHeight(setProperty, label);
                    Rect resolvedRect = drawer.LastResolvedPosition;

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
            TestSetHost host = CreateScriptableObject<TestSetHost>();
            host.set.Add(42);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(nameof(TestSetHost.set));
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Set");

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
                    drawer.GetPropertyHeight(setProperty, label);
                    Rect resolvedRect = drawer.LastResolvedPosition;

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
            TestSetHost host = CreateScriptableObject<TestSetHost>();
            host.set.Add(100);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(nameof(TestSetHost.set));
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 350f, 250f);
            GUIContent label = new("Set");

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
                    drawer.GetPropertyHeight(setProperty, label);
                    Rect resolvedRect = drawer.LastResolvedPosition;

                    float expectedX = controlRect.x + GroupLeftPadding;
                    Assert.AreEqual(
                        expectedX,
                        resolvedRect.x,
                        0.01f,
                        "With WGroup padding at indentLevel 0, should use WGroup padding (skip MinimumGroupIndent)."
                    );

                    float expectedWidth = controlRect.width - horizontalPadding;
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
    }
}
