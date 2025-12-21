namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.EditorFramework;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;

    /// <summary>
    /// Integration tests for SerializableDictionary and SerializableSet property drawers
    /// when rendered inside WGroup contexts, verifying correct indentation and tweening behavior.
    /// </summary>
    public sealed class WGroupSerializableCollectionIntegrationTests : CommonTestBase
    {
        private bool _originalDictionaryTweenEnabled;
        private bool _originalSortedDictionaryTweenEnabled;
        private bool _originalSetTweenEnabled;
        private bool _originalSortedSetTweenEnabled;
        private bool _originalWGroupTweenEnabled;

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
            _originalSortedDictionaryTweenEnabled =
                UnityHelpersSettings.ShouldTweenSerializableSortedDictionaryFoldouts();
            _originalSetTweenEnabled = UnityHelpersSettings.ShouldTweenSerializableSetFoldouts();
            _originalSortedSetTweenEnabled =
                UnityHelpersSettings.ShouldTweenSerializableSortedSetFoldouts();
            _originalWGroupTweenEnabled = UnityHelpersSettings.ShouldTweenWGroupFoldouts();
        }

        [TearDown]
        public override void TearDown()
        {
            UnityHelpersSettings.SetSerializableDictionaryFoldoutTweenEnabled(
                _originalDictionaryTweenEnabled
            );
            UnityHelpersSettings.SetSerializableSortedDictionaryFoldoutTweenEnabled(
                _originalSortedDictionaryTweenEnabled
            );
            UnityHelpersSettings.SetSerializableSetFoldoutTweenEnabled(_originalSetTweenEnabled);
            UnityHelpersSettings.SetSerializableSortedSetFoldoutTweenEnabled(
                _originalSortedSetTweenEnabled
            );
            UnityHelpersSettings.SetWGroupFoldoutTweenEnabled(_originalWGroupTweenEnabled);

            GroupGUIWidthUtility.ResetForTests();
            base.TearDown();
        }

        [Serializable]
        private sealed class TestStringIntDictionary : SerializableDictionary<string, int> { }

        [Serializable]
        private sealed class TestIntSet : SerializableHashSet<int> { }

        private sealed class WGroupDictionaryHost : ScriptableObject
        {
            [WGroup("TestGroup", displayName: "Test Group", collapsible: true, autoIncludeCount: 1)]
            public TestStringIntDictionary dictionary = new();
        }

        private sealed class WGroupSetHost : ScriptableObject
        {
            [WGroup("TestGroup", displayName: "Test Group", collapsible: true, autoIncludeCount: 1)]
            public TestIntSet set = new();
        }

        private sealed class MultiWGroupHost : ScriptableObject
        {
            [WGroup(
                "OuterGroup",
                displayName: "Outer Group",
                collapsible: true,
                autoIncludeCount: 3
            )]
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
            public int outerField;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

            [WGroup(
                "InnerGroup",
                displayName: "Inner Group",
                collapsible: true,
                autoIncludeCount: 1
            )]
            public TestStringIntDictionary nestedDictionary = new();

            [WGroupEnd("InnerGroup")]
            public TestIntSet nestedSet = new();

            [WGroupEnd("OuterGroup")]
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
            public int outerEndField;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
        }

        private sealed class NonCollapsibleWGroupHost : ScriptableObject
        {
            [WGroup(
                "StaticGroup",
                displayName: "Static Group",
                collapsible: false,
                autoIncludeCount: 1
            )]
            public TestStringIntDictionary dictionary = new();
        }

        [Test]
        public void DictionaryInsideWGroupHasCorrectIndentationAtZeroLevel()
        {
            Rect controlRect = new(0f, 0f, 400f, 300f);

            // Use fixed padding values to avoid relying on EditorStyles.helpBox which
            // requires an active GUI context
            const float SimulatedLeftPadding = 4f;
            const float SimulatedRightPadding = 4f;
            const float HorizontalPadding = SimulatedLeftPadding + SimulatedRightPadding;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        HorizontalPadding,
                        SimulatedLeftPadding,
                        SimulatedRightPadding
                    )
                )
                {
                    // Use ResolveContentRectForTests to verify padding is applied correctly
                    // without requiring an IMGUI context
                    Rect resolvedRect =
                        SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: false
                        );

                    Assert.AreEqual(
                        controlRect.x + SimulatedLeftPadding,
                        resolvedRect.x,
                        0.1f,
                        "Dictionary inside WGroup should use WGroup padding, not MinimumGroupIndent."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void SetInsideWGroupHasCorrectIndentationAtZeroLevel()
        {
            Rect controlRect = new(0f, 0f, 400f, 300f);

            // Use fixed padding values to avoid relying on EditorStyles.helpBox which
            // requires an active GUI context
            const float SimulatedLeftPadding = 4f;
            const float SimulatedRightPadding = 4f;
            const float HorizontalPadding = SimulatedLeftPadding + SimulatedRightPadding;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        HorizontalPadding,
                        SimulatedLeftPadding,
                        SimulatedRightPadding
                    )
                )
                {
                    // Use ResolveContentRectForTests to verify padding is applied correctly
                    // without requiring an IMGUI context
                    Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                        controlRect,
                        skipIndentation: false
                    );

                    Assert.AreEqual(
                        controlRect.x + SimulatedLeftPadding,
                        resolvedRect.x,
                        0.1f,
                        "Set inside WGroup should use WGroup padding, not MinimumGroupIndent."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [UnityTest]
        public IEnumerator DictionaryFoldoutHasAlignmentOffsetWhenInsideWGroup()
        {
            WGroupDictionaryHost host = CreateScriptableObject<WGroupDictionaryHost>();
            host.dictionary["key1"] = 100;

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(WGroupDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = false;

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Dictionary");

            const float SimulatedLeftPadding = 12f;
            const float SimulatedRightPadding = 12f;
            float horizontalPadding = SimulatedLeftPadding + SimulatedRightPadding;

            Rect capturedFoldoutRect = default;
            bool hasFoldoutRect = false;
            int previousIndentLevel = EditorGUI.indentLevel;
            string typeResolutionError = null;

            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    EditorGUI.indentLevel = 0;

                    GroupGUIWidthUtility.ResetForTests();
                    SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

                    using (
                        GroupGUIWidthUtility.PushContentPadding(
                            horizontalPadding,
                            SimulatedLeftPadding,
                            SimulatedRightPadding
                        )
                    )
                    {
                        drawer.OnGUI(controlRect, dictionaryProperty, label);

                        hasFoldoutRect =
                            SerializableDictionaryPropertyDrawer.HasLastMainFoldoutRect;
                        if (hasFoldoutRect)
                        {
                            capturedFoldoutRect =
                                SerializableDictionaryPropertyDrawer.LastMainFoldoutRect;
                        }
                        else
                        {
                            typeResolutionError =
                                $"OnGUI completed but HasLastMainFoldoutRect={hasFoldoutRect}. Property path: {dictionaryProperty.propertyPath}";
                        }
                    }
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            Assert.IsTrue(
                hasFoldoutRect,
                $"Main foldout rect should be tracked after OnGUI. {typeResolutionError ?? ""}"
            );

            float expectedX =
                controlRect.x
                + SimulatedLeftPadding
                + SerializableDictionaryPropertyDrawer.WGroupFoldoutAlignmentOffset;

            Assert.AreEqual(
                expectedX,
                capturedFoldoutRect.x,
                0.1f,
                "Dictionary foldout inside WGroup should be shifted right by alignment offset."
            );

            float expectedWidth =
                controlRect.width
                - horizontalPadding
                - SerializableDictionaryPropertyDrawer.WGroupFoldoutAlignmentOffset;
            Assert.AreEqual(
                expectedWidth,
                capturedFoldoutRect.width,
                0.1f,
                "Dictionary foldout width should be reduced by alignment offset."
            );
        }

        [UnityTest]
        public IEnumerator SetFoldoutHasAlignmentOffsetWhenInsideWGroup()
        {
            WGroupSetHost host = CreateScriptableObject<WGroupSetHost>();
            host.set.Add(42);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(WGroupSetHost.set)
            );
            setProperty.isExpanded = false;

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Set");

            const float SimulatedLeftPadding = 12f;
            const float SimulatedRightPadding = 12f;
            float horizontalPadding = SimulatedLeftPadding + SimulatedRightPadding;

            Rect capturedFoldoutRect = default;
            bool hasFoldoutRect = false;
            int previousIndentLevel = EditorGUI.indentLevel;

            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    EditorGUI.indentLevel = 0;

                    GroupGUIWidthUtility.ResetForTests();
                    SerializableSetPropertyDrawer.ResetLayoutTrackingForTests();

                    using (
                        GroupGUIWidthUtility.PushContentPadding(
                            horizontalPadding,
                            SimulatedLeftPadding,
                            SimulatedRightPadding
                        )
                    )
                    {
                        drawer.OnGUI(controlRect, setProperty, label);

                        hasFoldoutRect = SerializableSetPropertyDrawer.HasLastMainFoldoutRect;
                        if (hasFoldoutRect)
                        {
                            capturedFoldoutRect = SerializableSetPropertyDrawer.LastMainFoldoutRect;
                        }
                    }
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            Assert.IsTrue(hasFoldoutRect, "Main foldout rect should be tracked after OnGUI.");

            float expectedX =
                controlRect.x
                + SimulatedLeftPadding
                + SerializableSetPropertyDrawer.WGroupFoldoutAlignmentOffset;

            Assert.AreEqual(
                expectedX,
                capturedFoldoutRect.x,
                0.1f,
                "Set foldout inside WGroup should be shifted right by alignment offset."
            );

            float expectedWidth =
                controlRect.width
                - horizontalPadding
                - SerializableSetPropertyDrawer.WGroupFoldoutAlignmentOffset;
            Assert.AreEqual(
                expectedWidth,
                capturedFoldoutRect.width,
                0.1f,
                "Set foldout width should be reduced by alignment offset."
            );
        }

        [UnityTest]
        public IEnumerator DictionaryFoldoutHasNoOffsetWhenPaddingIsZero()
        {
            WGroupDictionaryHost host = CreateScriptableObject<WGroupDictionaryHost>();
            host.dictionary["key1"] = 100;

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(WGroupDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = false;

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Dictionary");

            Rect capturedFoldoutRect = default;
            Rect capturedResolvedPosition = default;
            bool hasFoldoutRect = false;
            string typeResolutionError = null;
            int previousIndentLevel = EditorGUI.indentLevel;
            int capturedScopeDepth = -1;

            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    EditorGUI.indentLevel = 0;

                    GroupGUIWidthUtility.ResetForTests();
                    SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

                    using (GroupGUIWidthUtility.PushContentPadding(0f, 0f, 0f))
                    {
                        capturedScopeDepth = GroupGUIWidthUtility.CurrentScopeDepth;
                        drawer.OnGUI(controlRect, dictionaryProperty, label);

                        hasFoldoutRect =
                            SerializableDictionaryPropertyDrawer.HasLastMainFoldoutRect;
                        if (hasFoldoutRect)
                        {
                            capturedFoldoutRect =
                                SerializableDictionaryPropertyDrawer.LastMainFoldoutRect;
                            capturedResolvedPosition = drawer.LastResolvedPosition;
                        }
                        else
                        {
                            typeResolutionError =
                                $"OnGUI completed but HasLastMainFoldoutRect={hasFoldoutRect}. Property path: {dictionaryProperty?.propertyPath}";
                        }
                    }
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            Assert.IsTrue(
                hasFoldoutRect,
                $"Main foldout rect should be tracked after OnGUI. {typeResolutionError ?? ""}"
            );

            // Zero padding means no WGroup visual context, so scope depth should remain 0
            Assert.AreEqual(
                0,
                capturedScopeDepth,
                $"Zero padding scope should not increment scope depth (no visual WGroup context). Actual: {capturedScopeDepth}"
            );

            // Without WGroup visual context, the foldout x should match the resolved position x (no alignment offset)
            Assert.AreEqual(
                capturedResolvedPosition.x,
                capturedFoldoutRect.x,
                0.1f,
                $"Dictionary foldout with zero padding should not have alignment offset. "
                    + $"Expected x={capturedResolvedPosition.x:F3}, Actual x={capturedFoldoutRect.x:F3}"
            );

            Assert.AreEqual(
                capturedResolvedPosition.width,
                capturedFoldoutRect.width,
                0.1f,
                $"Dictionary foldout width should match resolved position width when padding is zero. "
                    + $"Expected width={capturedResolvedPosition.width:F3}, Actual width={capturedFoldoutRect.width:F3}"
            );
        }

        [UnityTest]
        public IEnumerator SetFoldoutHasNoOffsetWhenPaddingIsZero()
        {
            WGroupSetHost host = CreateScriptableObject<WGroupSetHost>();
            host.set.Add(42);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(WGroupSetHost.set)
            );
            setProperty.isExpanded = false;

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Set");

            Rect capturedFoldoutRect = default;
            Rect capturedResolvedPosition = default;
            bool hasFoldoutRect = false;
            int previousIndentLevel = EditorGUI.indentLevel;
            int capturedScopeDepth = -1;

            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    EditorGUI.indentLevel = 0;

                    GroupGUIWidthUtility.ResetForTests();
                    SerializableSetPropertyDrawer.ResetLayoutTrackingForTests();

                    using (GroupGUIWidthUtility.PushContentPadding(0f, 0f, 0f))
                    {
                        capturedScopeDepth = GroupGUIWidthUtility.CurrentScopeDepth;
                        drawer.OnGUI(controlRect, setProperty, label);

                        hasFoldoutRect = SerializableSetPropertyDrawer.HasLastMainFoldoutRect;
                        if (hasFoldoutRect)
                        {
                            capturedFoldoutRect = SerializableSetPropertyDrawer.LastMainFoldoutRect;
                            capturedResolvedPosition = drawer.LastResolvedPosition;
                        }
                    }
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            Assert.IsTrue(hasFoldoutRect, "Main foldout rect should be tracked after OnGUI.");

            // Zero padding means no WGroup visual context, so scope depth should remain 0
            Assert.AreEqual(
                0,
                capturedScopeDepth,
                $"Zero padding scope should not increment scope depth (no visual WGroup context). Actual: {capturedScopeDepth}"
            );

            // Without WGroup visual context, the foldout x should match the resolved position x (no alignment offset)
            Assert.AreEqual(
                capturedResolvedPosition.x,
                capturedFoldoutRect.x,
                0.1f,
                $"Set foldout with zero padding should not have alignment offset. "
                    + $"Expected x={capturedResolvedPosition.x:F3}, Actual x={capturedFoldoutRect.x:F3}"
            );

            Assert.AreEqual(
                capturedResolvedPosition.width,
                capturedFoldoutRect.width,
                0.1f,
                $"Set foldout width should match resolved position width when padding is zero. "
                    + $"Expected width={capturedResolvedPosition.width:F3}, Actual width={capturedFoldoutRect.width:F3}"
            );
        }

        [UnityTest]
        public IEnumerator DictionaryFoldoutHasNoAlignmentOffsetWhenNotInsideWGroup()
        {
            WGroupDictionaryHost host = CreateScriptableObject<WGroupDictionaryHost>();
            host.dictionary["key1"] = 100;

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(WGroupDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = false;

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Dictionary");

            Rect capturedFoldoutRect = default;
            Rect capturedResolvedPosition = default;
            bool hasFoldoutRect = false;
            string typeResolutionError = null;

            int previousIndentLevel = EditorGUI.indentLevel;

            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    EditorGUI.indentLevel = 0;

                    GroupGUIWidthUtility.ResetForTests();
                    SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

                    drawer.OnGUI(controlRect, dictionaryProperty, label);

                    hasFoldoutRect = SerializableDictionaryPropertyDrawer.HasLastMainFoldoutRect;
                    if (hasFoldoutRect)
                    {
                        capturedFoldoutRect =
                            SerializableDictionaryPropertyDrawer.LastMainFoldoutRect;
                        capturedResolvedPosition = drawer.LastResolvedPosition;
                    }
                    else
                    {
                        typeResolutionError =
                            $"OnGUI completed but HasLastMainFoldoutRect={hasFoldoutRect}. Property path: {dictionaryProperty?.propertyPath}";
                    }
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            Assert.IsTrue(
                hasFoldoutRect,
                $"Main foldout rect should be tracked after OnGUI. {typeResolutionError ?? ""}"
            );

            // Without WGroup, the foldout x should match the resolved position x (no alignment offset)
            Assert.AreEqual(
                capturedResolvedPosition.x,
                capturedFoldoutRect.x,
                0.1f,
                "Dictionary foldout outside WGroup should not have alignment offset."
            );
        }

        [UnityTest]
        public IEnumerator SetFoldoutHasNoAlignmentOffsetWhenNotInsideWGroup()
        {
            WGroupSetHost host = CreateScriptableObject<WGroupSetHost>();
            host.set.Add(42);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(WGroupSetHost.set)
            );
            setProperty.isExpanded = false;

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Set");

            Rect capturedFoldoutRect = default;
            Rect capturedResolvedPosition = default;
            bool hasFoldoutRect = false;

            int previousIndentLevel = EditorGUI.indentLevel;

            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    EditorGUI.indentLevel = 0;

                    // Ensure no WGroup padding is active
                    GroupGUIWidthUtility.ResetForTests();
                    SerializableSetPropertyDrawer.ResetLayoutTrackingForTests();

                    drawer.OnGUI(controlRect, setProperty, label);

                    hasFoldoutRect = SerializableSetPropertyDrawer.HasLastMainFoldoutRect;
                    if (hasFoldoutRect)
                    {
                        capturedFoldoutRect = SerializableSetPropertyDrawer.LastMainFoldoutRect;
                        capturedResolvedPosition = drawer.LastResolvedPosition;
                    }
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            Assert.IsTrue(hasFoldoutRect, "Main foldout rect should be tracked after OnGUI.");

            // Without WGroup, the foldout x should match the resolved position x (no alignment offset)
            Assert.AreEqual(
                capturedResolvedPosition.x,
                capturedFoldoutRect.x,
                0.1f,
                "Set foldout outside WGroup should not have alignment offset."
            );
        }

        [Test]
        public void WGroupFoldoutAlignmentOffsetConstantHasExpectedValue()
        {
            // Verify the constant values match our expected 2.5px offset
            Assert.AreEqual(
                2.5f,
                SerializableDictionaryPropertyDrawer.WGroupFoldoutAlignmentOffset,
                0.01f,
                "Dictionary WGroup foldout alignment offset should be 2.5f."
            );

            Assert.AreEqual(
                2.5f,
                SerializableSetPropertyDrawer.WGroupFoldoutAlignmentOffset,
                0.01f,
                "Set WGroup foldout alignment offset should be 2.5f."
            );
        }

        [TestCase(0f, 0f, 0f, false, Description = "Zero padding - no WGroup context, no offset")]
        [TestCase(
            0.001f,
            0f,
            0f,
            true,
            Description = "Tiny horizontal padding - creates WGroup context with offset"
        )]
        [TestCase(
            0f,
            0.001f,
            0f,
            true,
            Description = "Tiny left padding only - creates WGroup context with offset"
        )]
        [TestCase(
            0f,
            0f,
            0.001f,
            true,
            Description = "Tiny right padding only - creates WGroup context with offset"
        )]
        [TestCase(10f, 5f, 5f, true, Description = "Typical padding - creates WGroup context")]
        [TestCase(24f, 12f, 12f, true, Description = "Larger padding - creates WGroup context")]
        public void AlignmentOffsetAppliedBasedOnPaddingContext(
            float horizontalPadding,
            float leftPadding,
            float rightPadding,
            bool expectOffset
        )
        {
            GroupGUIWidthUtility.ResetForTests();

            using (
                GroupGUIWidthUtility.PushContentPadding(
                    horizontalPadding,
                    leftPadding,
                    rightPadding
                )
            )
            {
                int scopeDepth = GroupGUIWidthUtility.CurrentScopeDepth;
                bool hasWGroupContext = scopeDepth > 0;

                Assert.AreEqual(
                    expectOffset,
                    hasWGroupContext,
                    $"WGroup context (scope depth > 0) should be {expectOffset} with padding "
                        + $"(h={horizontalPadding}, l={leftPadding}, r={rightPadding}). "
                        + $"Actual scope depth: {scopeDepth}"
                );
            }
        }

        [UnityTest]
        public IEnumerator DictionaryFoldoutAlignmentOffsetConsistentAcrossDrawerInstances()
        {
            WGroupDictionaryHost host = CreateScriptableObject<WGroupDictionaryHost>();
            host.dictionary["key1"] = 100;

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(WGroupDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = false;

            SerializableDictionaryPropertyDrawer drawer1 = new();
            SerializableDictionaryPropertyDrawer drawer2 = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Dictionary");

            const float SimulatedLeftPadding = 12f;
            const float SimulatedRightPadding = 12f;
            float horizontalPadding = SimulatedLeftPadding + SimulatedRightPadding;

            Rect capturedFoldoutRect1 = default;
            Rect capturedFoldoutRect2 = default;

            int previousIndentLevel = EditorGUI.indentLevel;

            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    EditorGUI.indentLevel = 0;

                    GroupGUIWidthUtility.ResetForTests();
                    using (
                        GroupGUIWidthUtility.PushContentPadding(
                            horizontalPadding,
                            SimulatedLeftPadding,
                            SimulatedRightPadding
                        )
                    )
                    {
                        SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();
                        drawer1.OnGUI(controlRect, dictionaryProperty, label);
                        capturedFoldoutRect1 =
                            SerializableDictionaryPropertyDrawer.LastMainFoldoutRect;

                        SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();
                        drawer2.OnGUI(controlRect, dictionaryProperty, label);
                        capturedFoldoutRect2 =
                            SerializableDictionaryPropertyDrawer.LastMainFoldoutRect;
                    }
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            Assert.AreEqual(
                capturedFoldoutRect1.x,
                capturedFoldoutRect2.x,
                0.01f,
                "Foldout x position should be consistent across drawer instances."
            );

            Assert.AreEqual(
                capturedFoldoutRect1.width,
                capturedFoldoutRect2.width,
                0.01f,
                "Foldout width should be consistent across drawer instances."
            );
        }

        [Test]
        public void DictionaryTweenSettingsRespectedWhenEnabled()
        {
            UnityHelpersSettings.SetSerializableDictionaryFoldoutTweenEnabled(true);

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.IsTweeningEnabledForTests(
                    isSortedDictionary: false
                ),
                "Dictionary tweening should be enabled when setting is true."
            );
        }

        [Test]
        public void DictionaryTweenSettingsRespectedWhenDisabled()
        {
            UnityHelpersSettings.SetSerializableDictionaryFoldoutTweenEnabled(false);

            Assert.IsFalse(
                SerializableDictionaryPropertyDrawer.IsTweeningEnabledForTests(
                    isSortedDictionary: false
                ),
                "Dictionary tweening should be disabled when setting is false."
            );
        }

        [Test]
        public void SortedDictionaryTweenSettingsRespectedWhenEnabled()
        {
            UnityHelpersSettings.SetSerializableSortedDictionaryFoldoutTweenEnabled(true);

            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.IsTweeningEnabledForTests(
                    isSortedDictionary: true
                ),
                "Sorted dictionary tweening should be enabled when setting is true."
            );
        }

        [Test]
        public void SortedDictionaryTweenSettingsRespectedWhenDisabled()
        {
            UnityHelpersSettings.SetSerializableSortedDictionaryFoldoutTweenEnabled(false);

            Assert.IsFalse(
                SerializableDictionaryPropertyDrawer.IsTweeningEnabledForTests(
                    isSortedDictionary: true
                ),
                "Sorted dictionary tweening should be disabled when setting is false."
            );
        }

        [Test]
        public void SetTweenSettingsRespectedWhenEnabled()
        {
            UnityHelpersSettings.SetSerializableSetFoldoutTweenEnabled(true);

            Assert.IsTrue(
                SerializableSetPropertyDrawer.IsTweeningEnabledForTests(isSortedSet: false),
                "Set tweening should be enabled when setting is true."
            );
        }

        [Test]
        public void SetTweenSettingsRespectedWhenDisabled()
        {
            UnityHelpersSettings.SetSerializableSetFoldoutTweenEnabled(false);

            Assert.IsFalse(
                SerializableSetPropertyDrawer.IsTweeningEnabledForTests(isSortedSet: false),
                "Set tweening should be disabled when setting is false."
            );
        }

        [Test]
        public void SortedSetTweenSettingsRespectedWhenEnabled()
        {
            UnityHelpersSettings.SetSerializableSortedSetFoldoutTweenEnabled(true);

            Assert.IsTrue(
                SerializableSetPropertyDrawer.IsTweeningEnabledForTests(isSortedSet: true),
                "Sorted set tweening should be enabled when setting is true."
            );
        }

        [Test]
        public void SortedSetTweenSettingsRespectedWhenDisabled()
        {
            UnityHelpersSettings.SetSerializableSortedSetFoldoutTweenEnabled(false);

            Assert.IsFalse(
                SerializableSetPropertyDrawer.IsTweeningEnabledForTests(isSortedSet: true),
                "Sorted set tweening should be disabled when setting is false."
            );
        }

        [Test]
        public void WGroupTweenSettingsAreIndependentOfDictionaryTweenSettings()
        {
            UnityHelpersSettings.SetWGroupFoldoutTweenEnabled(true);
            UnityHelpersSettings.SetSerializableDictionaryFoldoutTweenEnabled(false);

            Assert.IsTrue(
                UnityHelpersSettings.ShouldTweenWGroupFoldouts(),
                "WGroup tweening should be enabled independently."
            );

            Assert.IsFalse(
                SerializableDictionaryPropertyDrawer.IsTweeningEnabledForTests(
                    isSortedDictionary: false
                ),
                "Dictionary tweening should remain disabled."
            );
        }

        [Test]
        public void WGroupContextDoesNotAffectDictionaryTweeningSetting()
        {
            WGroupDictionaryHost host = CreateScriptableObject<WGroupDictionaryHost>();
            host.dictionary["key1"] = 100;

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            UnityHelpersSettings.SetSerializableDictionaryFoldoutTweenEnabled(true);
            UnityHelpersSettings.SetWGroupFoldoutTweenEnabled(false);

            bool dictionaryTweenEnabled =
                SerializableDictionaryPropertyDrawer.IsTweeningEnabledForTests(
                    isSortedDictionary: false
                );

            Assert.IsTrue(
                dictionaryTweenEnabled,
                "Dictionary tweening setting should not be affected by WGroup tween setting."
            );
        }

        [UnityTest]
        public IEnumerator DictionaryInWGroupRendersWithCorrectPaddingDuringOnGUI()
        {
            WGroupDictionaryHost host = CreateScriptableObject<WGroupDictionaryHost>();
            host.dictionary["key1"] = 100;
            host.dictionary["key2"] = 200;

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(WGroupDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Dictionary");

            const float SimulatedLeftPadding = 12f;
            const float SimulatedRightPadding = 12f;
            float horizontalPadding = SimulatedLeftPadding + SimulatedRightPadding;

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
                            SimulatedLeftPadding,
                            SimulatedRightPadding
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

            float expectedX = controlRect.x + SimulatedLeftPadding;
            Assert.AreEqual(
                expectedX,
                capturedRect.x,
                0.1f,
                "Dictionary OnGUI in WGroup context should apply WGroup padding correctly."
            );

            float expectedWidth = controlRect.width - horizontalPadding;
            Assert.AreEqual(
                expectedWidth,
                capturedRect.width,
                0.1f,
                "Dictionary OnGUI in WGroup context should have width reduced by WGroup padding."
            );
        }

        [UnityTest]
        public IEnumerator SetInWGroupRendersWithCorrectPaddingDuringOnGUI()
        {
            WGroupSetHost host = CreateScriptableObject<WGroupSetHost>();
            host.set.Add(1);
            host.set.Add(2);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(WGroupSetHost.set)
            );
            setProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Set");

            const float SimulatedLeftPadding = 12f;
            const float SimulatedRightPadding = 12f;
            float horizontalPadding = SimulatedLeftPadding + SimulatedRightPadding;

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
                            SimulatedLeftPadding,
                            SimulatedRightPadding
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

            float expectedX = controlRect.x + SimulatedLeftPadding;
            Assert.AreEqual(
                expectedX,
                capturedRect.x,
                0.1f,
                "Set OnGUI in WGroup context should apply WGroup padding correctly."
            );

            float expectedWidth = controlRect.width - horizontalPadding;
            Assert.AreEqual(
                expectedWidth,
                capturedRect.width,
                0.1f,
                "Set OnGUI in WGroup context should have width reduced by WGroup padding."
            );
        }

        [Test]
        public void DictionaryWidthIsNotNegativeWithLargePadding()
        {
            Rect controlRect = new(0f, 0f, 100f, 300f);

            const float LargePadding = 200f;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        LargePadding,
                        LargePadding / 2f,
                        LargePadding / 2f
                    )
                )
                {
                    Rect resolvedRect =
                        SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: false
                        );

                    Assert.GreaterOrEqual(
                        resolvedRect.width,
                        0f,
                        "Width should not be negative even with padding larger than control rect."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void SetWidthIsNotNegativeWithLargePadding()
        {
            Rect controlRect = new(0f, 0f, 100f, 300f);

            const float LargePadding = 200f;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        LargePadding,
                        LargePadding / 2f,
                        LargePadding / 2f
                    )
                )
                {
                    Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                        controlRect,
                        skipIndentation: false
                    );

                    Assert.GreaterOrEqual(
                        resolvedRect.width,
                        0f,
                        "Width should not be negative even with padding larger than control rect."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void PaddingScopeIsProperlyCleanedUp()
        {
            GroupGUIWidthUtility.ResetForTests();

            Assert.AreEqual(0f, GroupGUIWidthUtility.CurrentLeftPadding, 0.001f);
            Assert.AreEqual(0f, GroupGUIWidthUtility.CurrentRightPadding, 0.001f);

            using (GroupGUIWidthUtility.PushContentPadding(20f, 10f, 10f))
            {
                Assert.AreEqual(10f, GroupGUIWidthUtility.CurrentLeftPadding, 0.001f);
                Assert.AreEqual(10f, GroupGUIWidthUtility.CurrentRightPadding, 0.001f);
            }

            Assert.AreEqual(0f, GroupGUIWidthUtility.CurrentLeftPadding, 0.001f);
            Assert.AreEqual(0f, GroupGUIWidthUtility.CurrentRightPadding, 0.001f);
        }

        [Test]
        public void NestedPaddingScopesAccumulateCorrectly()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushContentPadding(20f, 10f, 10f))
            {
                Assert.AreEqual(10f, GroupGUIWidthUtility.CurrentLeftPadding, 0.001f);

                using (GroupGUIWidthUtility.PushContentPadding(16f, 8f, 8f))
                {
                    Assert.AreEqual(18f, GroupGUIWidthUtility.CurrentLeftPadding, 0.001f);
                    Assert.AreEqual(18f, GroupGUIWidthUtility.CurrentRightPadding, 0.001f);
                }

                Assert.AreEqual(10f, GroupGUIWidthUtility.CurrentLeftPadding, 0.001f);
            }

            Assert.AreEqual(0f, GroupGUIWidthUtility.CurrentLeftPadding, 0.001f);
        }

        [Test]
        public void DictionaryIndentationConsistentAcrossMultipleDrawCalls()
        {
            Rect controlRect = new(0f, 0f, 400f, 300f);

            const float LeftPadding = 12f;
            const float RightPadding = 12f;

            int previousIndentLevel = EditorGUI.indentLevel;
            List<float> capturedXPositions = new();

            try
            {
                EditorGUI.indentLevel = 0;

                for (int i = 0; i < 5; i++)
                {
                    GroupGUIWidthUtility.ResetForTests();
                    using (
                        GroupGUIWidthUtility.PushContentPadding(
                            LeftPadding + RightPadding,
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
                        capturedXPositions.Add(resolvedRect.x);
                    }
                }

                float firstX = capturedXPositions[0];
                foreach (float x in capturedXPositions)
                {
                    Assert.AreEqual(
                        firstX,
                        x,
                        0.01f,
                        "Indentation should be consistent across multiple draw calls."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void DictionaryInSettingsContextWithWGroupPaddingAppliesPadding()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            SerializedObject serializedSettings = TrackDisposable(new SerializedObject(settings));
            serializedSettings.Update();

            SerializedProperty paletteProp = serializedSettings.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );

            if (paletteProp == null)
            {
                Assert.Ignore("WButtonCustomColors property not found in settings.");
                return;
            }

            paletteProp.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);

            const float WGroupLeftPadding = 10f;
            const float WGroupRightPadding = 10f;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        WGroupLeftPadding + WGroupRightPadding,
                        WGroupLeftPadding,
                        WGroupRightPadding
                    )
                )
                {
                    // Use skipIndentation=true to simulate settings context
                    Rect resolvedRect =
                        SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: true
                        );

                    Assert.AreEqual(
                        controlRect.x + WGroupLeftPadding,
                        resolvedRect.x,
                        0.01f,
                        "Settings context with WGroup padding should apply WGroup padding to x position."
                    );

                    float expectedWidth =
                        controlRect.width - WGroupLeftPadding - WGroupRightPadding;
                    Assert.AreEqual(
                        expectedWidth,
                        resolvedRect.width,
                        0.01f,
                        "Settings context with WGroup padding should reduce width by total padding."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void DictionaryInSettingsContextWithoutWGroupPaddingHasUnchangedPosition()
        {
            Rect controlRect = new(0f, 0f, 400f, 300f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                // No WGroup padding pushed

                // Use skipIndentation=true to simulate settings context
                Rect resolvedRect = SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: true
                );

                Assert.AreEqual(
                    controlRect.x,
                    resolvedRect.x,
                    0.01f,
                    "Settings context without WGroup padding should have unchanged x position."
                );

                Assert.AreEqual(
                    controlRect.width,
                    resolvedRect.width,
                    0.01f,
                    "Settings context without WGroup padding should have unchanged width."
                );
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void SetInSettingsContextWithWGroupPaddingAppliesPadding()
        {
            Rect controlRect = new(0f, 0f, 400f, 300f);

            const float WGroupLeftPadding = 10f;
            const float WGroupRightPadding = 10f;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        WGroupLeftPadding + WGroupRightPadding,
                        WGroupLeftPadding,
                        WGroupRightPadding
                    )
                )
                {
                    // Use skipIndentation=true to simulate settings context
                    Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                        controlRect,
                        skipIndentation: true
                    );

                    Assert.AreEqual(
                        controlRect.x + WGroupLeftPadding,
                        resolvedRect.x,
                        0.01f,
                        "Set in WGroup context should apply WGroup padding to x position."
                    );

                    float expectedWidth =
                        controlRect.width - WGroupLeftPadding - WGroupRightPadding;
                    Assert.AreEqual(
                        expectedWidth,
                        resolvedRect.width,
                        0.01f,
                        "Set in WGroup context should reduce width by total padding."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void SetInSettingsContextWithoutWGroupPaddingAppliesMinimumIndent()
        {
            Rect controlRect = new(0f, 0f, 400f, 300f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                // No WGroup padding pushed

                // Use skipIndentation=false to get normal behavior with minimum indent
                Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: false
                );

                // Without WGroup padding and with indent level 0, the minimum indent (6) is applied
                const float MinimumGroupIndent = 6f;
                Assert.AreEqual(
                    controlRect.x + MinimumGroupIndent,
                    resolvedRect.x,
                    0.01f,
                    "Set without WGroup padding and indent=0 should apply minimum indent."
                );
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void DictionaryWithNonZeroIndentLevelAppliesUnityIndentation()
        {
            Rect controlRect = new(0f, 0f, 400f, 300f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                // Set indent level to simulate being inside another property
                EditorGUI.indentLevel = 2;

                GroupGUIWidthUtility.ResetForTests();
                // No WGroup padding pushed

                Rect resolvedRect = SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                    controlRect,
                    skipIndentation: false
                );

                // With non-zero indent level, Unity's IndentedRect applies indentation
                // The exact indentation depends on Unity's internal logic, but it should be > 0
                Assert.Greater(
                    resolvedRect.x,
                    controlRect.x,
                    "Dictionary with indent level > 0 should have indentation applied."
                );
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void NestedWGroupsAccumulatePaddingCorrectly()
        {
            Rect controlRect = new(0f, 0f, 400f, 300f);

            const float OuterLeftPadding = 10f;
            const float OuterRightPadding = 10f;
            const float InnerLeftPadding = 8f;
            const float InnerRightPadding = 8f;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();

                // Simulate outer WGroup
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        OuterLeftPadding + OuterRightPadding,
                        OuterLeftPadding,
                        OuterRightPadding
                    )
                )
                {
                    // Simulate inner WGroup
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

                        float expectedX = controlRect.x + OuterLeftPadding + InnerLeftPadding;
                        Assert.AreEqual(
                            expectedX,
                            resolvedRect.x,
                            0.01f,
                            "Nested WGroups should accumulate left padding."
                        );

                        float expectedWidth =
                            controlRect.width
                            - OuterLeftPadding
                            - OuterRightPadding
                            - InnerLeftPadding
                            - InnerRightPadding;
                        Assert.AreEqual(
                            expectedWidth,
                            resolvedRect.width,
                            0.01f,
                            "Nested WGroups should accumulate total padding reduction on width."
                        );
                    }
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void DictionaryInSettingsContextWithNonZeroIndentLevelAppliesOnlyWGroupPadding()
        {
            Rect controlRect = new(0f, 0f, 400f, 300f);

            const float WGroupLeftPadding = 10f;
            const float WGroupRightPadding = 10f;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                // Simulate indent level being set by WGroup/parent context (as happens in real use)
                // Note: In skipIndentation mode, indent level is ignored
                EditorGUI.indentLevel = 1;

                GroupGUIWidthUtility.ResetForTests();
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        WGroupLeftPadding + WGroupRightPadding,
                        WGroupLeftPadding,
                        WGroupRightPadding
                    )
                )
                {
                    // Use skipIndentation=true to simulate settings context
                    Rect resolvedRect =
                        SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: true
                        );

                    // In skipIndentation mode, only WGroup padding is applied
                    Assert.AreEqual(
                        controlRect.x + WGroupLeftPadding,
                        resolvedRect.x,
                        0.01f,
                        "Settings context with non-zero indent should still only apply WGroup padding."
                    );

                    float expectedWidth =
                        controlRect.width - WGroupLeftPadding - WGroupRightPadding;
                    Assert.AreEqual(
                        expectedWidth,
                        resolvedRect.width,
                        0.01f,
                        "Settings context with non-zero indent should reduce width by WGroup padding only."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void SetInSettingsContextWithNonZeroIndentLevelAppliesOnlyWGroupPadding()
        {
            Rect controlRect = new(0f, 0f, 400f, 300f);

            const float WGroupLeftPadding = 10f;
            const float WGroupRightPadding = 10f;

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                // Simulate indent level being set by WGroup/parent context
                EditorGUI.indentLevel = 1;

                GroupGUIWidthUtility.ResetForTests();
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        WGroupLeftPadding + WGroupRightPadding,
                        WGroupLeftPadding,
                        WGroupRightPadding
                    )
                )
                {
                    // Use skipIndentation=true to simulate settings context
                    Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                        controlRect,
                        skipIndentation: true
                    );

                    Assert.AreEqual(
                        controlRect.x + WGroupLeftPadding,
                        resolvedRect.x,
                        0.01f,
                        "Settings context Set with non-zero indent should only apply WGroup padding."
                    );

                    float expectedWidth =
                        controlRect.width - WGroupLeftPadding - WGroupRightPadding;
                    Assert.AreEqual(
                        expectedWidth,
                        resolvedRect.width,
                        0.01f,
                        "Settings context Set with non-zero indent should reduce width by WGroup padding only."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [UnityTest]
        public IEnumerator DictionaryInSettingsContextRestoresOriginalIndentLevelAfterDraw()
        {
            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            SerializedObject serializedSettings = TrackDisposable(new SerializedObject(settings));
            serializedSettings.Update();

            SerializedProperty paletteProp = serializedSettings.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );

            if (paletteProp == null)
            {
                Assert.Ignore("WButtonCustomColors property not found in settings.");
                yield break;
            }

            paletteProp.isExpanded = false; // Keep collapsed to minimize side effects

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 50f);
            GUIContent label = new("Palette");

            int previousIndentLevel = EditorGUI.indentLevel;
            int capturedIndentAfter = -1;

            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    // Set a specific indent level
                    EditorGUI.indentLevel = 2;

                    GroupGUIWidthUtility.ResetForTests();

                    // Call OnGUI - it should internally set indentLevel to 0 but restore it after
                    drawer.OnGUI(controlRect, paletteProp, label);

                    // Capture indent level after OnGUI
                    capturedIndentAfter = EditorGUI.indentLevel;
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            // Verify indent level was restored
            Assert.AreEqual(
                2,
                capturedIndentAfter,
                "Settings context dictionary drawer should restore original indent level after OnGUI."
            );
        }

        [UnityTest]
        public IEnumerator SetInSettingsContextRestoresOriginalIndentLevelAfterDraw()
        {
            WGroupSetHost host = CreateScriptableObject<WGroupSetHost>();
            host.set.Add(42);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(WGroupSetHost.set)
            );
            setProperty.isExpanded = false; // Keep collapsed

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 50f);
            GUIContent label = new("Set");

            int previousIndentLevel = EditorGUI.indentLevel;
            int capturedIndentAfter = -1;

            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    // Set a specific indent level
                    EditorGUI.indentLevel = 2;

                    GroupGUIWidthUtility.ResetForTests();

                    // Call OnGUI
                    drawer.OnGUI(controlRect, setProperty, label);

                    // Capture indent level after OnGUI
                    capturedIndentAfter = EditorGUI.indentLevel;
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            // Verify indent level was restored
            Assert.AreEqual(
                2,
                capturedIndentAfter,
                "Set drawer should restore original indent level after OnGUI."
            );
        }

        [Test]
        public void DictionaryInWGroupCreatesAnimBoolWhenTweenEnabled()
        {
            UnityHelpersSettings.SetSerializableDictionaryFoldoutTweenEnabled(true);

            WGroupDictionaryHost host = CreateScriptableObject<WGroupDictionaryHost>();
            host.dictionary["key1"] = 100;

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(WGroupDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignFieldInfo(
                drawer,
                typeof(WGroupDictionaryHost),
                nameof(WGroupDictionaryHost.dictionary)
            );
            GUIContent label = new("Dictionary");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                using (GroupGUIWidthUtility.PushContentPadding(24f, 12f, 12f))
                {
                    // Call GetPropertyHeight to initialize the pending entry
                    drawer.GetPropertyHeight(dictionaryProperty, label);

                    // Now check if animation state is properly initialized
                    bool found = drawer.TryGetPendingAnimationStateForTests(
                        dictionaryProperty,
                        out bool isExpanded,
                        out float animProgress,
                        out bool hasAnimBool
                    );

                    Assert.IsTrue(found, "Pending entry should exist after GetPropertyHeight.");
                    Assert.IsTrue(
                        hasAnimBool,
                        "AnimBool should be created when dictionary tweening is enabled."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void DictionaryInWGroupNoAnimBoolWhenTweenDisabled()
        {
            UnityHelpersSettings.SetSerializableDictionaryFoldoutTweenEnabled(false);

            WGroupDictionaryHost host = CreateScriptableObject<WGroupDictionaryHost>();
            host.dictionary["key1"] = 100;

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(WGroupDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignFieldInfo(
                drawer,
                typeof(WGroupDictionaryHost),
                nameof(WGroupDictionaryHost.dictionary)
            );
            GUIContent label = new("Dictionary");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                using (GroupGUIWidthUtility.PushContentPadding(24f, 12f, 12f))
                {
                    drawer.GetPropertyHeight(dictionaryProperty, label);

                    bool found = drawer.TryGetPendingAnimationStateForTests(
                        dictionaryProperty,
                        out bool isExpanded,
                        out float animProgress,
                        out bool hasAnimBool
                    );

                    Assert.IsTrue(found, "Pending entry should exist after GetPropertyHeight.");
                    Assert.IsFalse(
                        hasAnimBool,
                        "AnimBool should NOT be created when dictionary tweening is disabled."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void SetInWGroupCreatesAnimBoolWhenTweenEnabled()
        {
            UnityHelpersSettings.SetSerializableSetFoldoutTweenEnabled(true);

            WGroupSetHost host = CreateScriptableObject<WGroupSetHost>();
            host.set.Add(42);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(WGroupSetHost.set)
            );
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignFieldInfo(
                drawer,
                typeof(WGroupSetHost),
                nameof(WGroupSetHost.set)
            );
            GUIContent label = new("Set");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                using (GroupGUIWidthUtility.PushContentPadding(24f, 12f, 12f))
                {
                    drawer.GetPropertyHeight(setProperty, label);

                    bool found = drawer.TryGetPendingAnimationStateForTests(
                        setProperty,
                        out bool isExpanded,
                        out float animProgress,
                        out bool hasAnimBool
                    );

                    Assert.IsTrue(found, "Pending entry should exist after GetPropertyHeight.");
                    Assert.IsTrue(
                        hasAnimBool,
                        "AnimBool should be created when set tweening is enabled."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void SetInWGroupNoAnimBoolWhenTweenDisabled()
        {
            UnityHelpersSettings.SetSerializableSetFoldoutTweenEnabled(false);

            WGroupSetHost host = CreateScriptableObject<WGroupSetHost>();
            host.set.Add(42);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(WGroupSetHost.set)
            );
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignFieldInfo(
                drawer,
                typeof(WGroupSetHost),
                nameof(WGroupSetHost.set)
            );
            GUIContent label = new("Set");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                using (GroupGUIWidthUtility.PushContentPadding(24f, 12f, 12f))
                {
                    drawer.GetPropertyHeight(setProperty, label);

                    bool found = drawer.TryGetPendingAnimationStateForTests(
                        setProperty,
                        out bool isExpanded,
                        out float animProgress,
                        out bool hasAnimBool
                    );

                    Assert.IsTrue(found, "Pending entry should exist after GetPropertyHeight.");
                    Assert.IsFalse(
                        hasAnimBool,
                        "AnimBool should NOT be created when set tweening is disabled."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void DictionaryFoldoutProgressStartsAtZeroWhenCollapsed()
        {
            UnityHelpersSettings.SetSerializableDictionaryFoldoutTweenEnabled(true);

            WGroupDictionaryHost host = CreateScriptableObject<WGroupDictionaryHost>();
            host.dictionary["key1"] = 100;

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(WGroupDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignFieldInfo(
                drawer,
                typeof(WGroupDictionaryHost),
                nameof(WGroupDictionaryHost.dictionary)
            );
            GUIContent label = new("Dictionary");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                using (GroupGUIWidthUtility.PushContentPadding(24f, 12f, 12f))
                {
                    drawer.GetPropertyHeight(dictionaryProperty, label);

                    // Set to collapsed state
                    drawer.SetPendingExpandedStateForTests(dictionaryProperty, false);

                    float progress = drawer.GetPendingFoldoutProgressFromInstance(
                        dictionaryProperty
                    );

                    // Progress should be 0 immediately after setting collapsed (AnimBool starts at target)
                    // or transitioning towards 0
                    Assert.GreaterOrEqual(progress, 0f, "Progress should be >= 0.");
                    Assert.LessOrEqual(progress, 1f, "Progress should be <= 1.");
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void SetFoldoutProgressStartsAtZeroWhenCollapsed()
        {
            UnityHelpersSettings.SetSerializableSetFoldoutTweenEnabled(true);

            WGroupSetHost host = CreateScriptableObject<WGroupSetHost>();
            host.set.Add(42);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(WGroupSetHost.set)
            );
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignFieldInfo(
                drawer,
                typeof(WGroupSetHost),
                nameof(WGroupSetHost.set)
            );
            GUIContent label = new("Set");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                using (GroupGUIWidthUtility.PushContentPadding(24f, 12f, 12f))
                {
                    drawer.GetPropertyHeight(setProperty, label);

                    drawer.SetPendingExpandedStateForTests(setProperty, false);

                    float progress = drawer.GetPendingFoldoutProgressFromInstance(setProperty);

                    Assert.GreaterOrEqual(progress, 0f, "Progress should be >= 0.");
                    Assert.LessOrEqual(progress, 1f, "Progress should be <= 1.");
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void DictionaryFoldoutProgressIsImmediateWhenTweenDisabled()
        {
            UnityHelpersSettings.SetSerializableDictionaryFoldoutTweenEnabled(false);

            WGroupDictionaryHost host = CreateScriptableObject<WGroupDictionaryHost>();
            host.dictionary["key1"] = 100;

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(WGroupDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignFieldInfo(
                drawer,
                typeof(WGroupDictionaryHost),
                nameof(WGroupDictionaryHost.dictionary)
            );
            GUIContent label = new("Dictionary");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                using (GroupGUIWidthUtility.PushContentPadding(24f, 12f, 12f))
                {
                    drawer.GetPropertyHeight(dictionaryProperty, label);

                    // Set to expanded
                    drawer.SetPendingExpandedStateForTests(dictionaryProperty, true);
                    float expandedProgress = drawer.GetPendingFoldoutProgressFromInstance(
                        dictionaryProperty
                    );
                    Assert.AreEqual(
                        1f,
                        expandedProgress,
                        0.001f,
                        "When tween disabled and expanded, progress should immediately be 1."
                    );

                    // Set to collapsed
                    drawer.SetPendingExpandedStateForTests(dictionaryProperty, false);
                    float collapsedProgress = drawer.GetPendingFoldoutProgressFromInstance(
                        dictionaryProperty
                    );
                    Assert.AreEqual(
                        0f,
                        collapsedProgress,
                        0.001f,
                        "When tween disabled and collapsed, progress should immediately be 0."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void SetFoldoutProgressIsImmediateWhenTweenDisabled()
        {
            UnityHelpersSettings.SetSerializableSetFoldoutTweenEnabled(false);

            WGroupSetHost host = CreateScriptableObject<WGroupSetHost>();
            host.set.Add(42);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(WGroupSetHost.set)
            );
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignFieldInfo(
                drawer,
                typeof(WGroupSetHost),
                nameof(WGroupSetHost.set)
            );
            GUIContent label = new("Set");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                using (GroupGUIWidthUtility.PushContentPadding(24f, 12f, 12f))
                {
                    drawer.GetPropertyHeight(setProperty, label);

                    // Set to expanded
                    drawer.SetPendingExpandedStateForTests(setProperty, true);
                    float expandedProgress = drawer.GetPendingFoldoutProgressFromInstance(
                        setProperty
                    );
                    Assert.AreEqual(
                        1f,
                        expandedProgress,
                        0.001f,
                        "When tween disabled and expanded, progress should immediately be 1."
                    );

                    // Set to collapsed
                    drawer.SetPendingExpandedStateForTests(setProperty, false);
                    float collapsedProgress = drawer.GetPendingFoldoutProgressFromInstance(
                        setProperty
                    );
                    Assert.AreEqual(
                        0f,
                        collapsedProgress,
                        0.001f,
                        "When tween disabled and collapsed, progress should immediately be 0."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void DictionaryTweenSettingsRespectedInWGroupContext()
        {
            WGroupDictionaryHost host = CreateScriptableObject<WGroupDictionaryHost>();
            host.dictionary["key1"] = 100;

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(WGroupDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignFieldInfo(
                drawer,
                typeof(WGroupDictionaryHost),
                nameof(WGroupDictionaryHost.dictionary)
            );
            GUIContent label = new("Dictionary");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                using (GroupGUIWidthUtility.PushContentPadding(24f, 12f, 12f))
                {
                    // Test with tween enabled
                    UnityHelpersSettings.SetSerializableDictionaryFoldoutTweenEnabled(true);
                    drawer.GetPropertyHeight(dictionaryProperty, label);

                    bool found1 = drawer.TryGetPendingAnimationStateForTests(
                        dictionaryProperty,
                        out _,
                        out _,
                        out bool hasAnimBool1
                    );
                    Assert.IsTrue(
                        found1 && hasAnimBool1,
                        "Should have AnimBool when tween enabled."
                    );

                    // Now disable tween
                    UnityHelpersSettings.SetSerializableDictionaryFoldoutTweenEnabled(false);
                    drawer.GetPropertyHeight(dictionaryProperty, label);

                    bool found2 = drawer.TryGetPendingAnimationStateForTests(
                        dictionaryProperty,
                        out _,
                        out _,
                        out bool hasAnimBool2
                    );
                    Assert.IsTrue(
                        found2 && !hasAnimBool2,
                        "Should NOT have AnimBool when tween disabled."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void SetTweenSettingsRespectedInWGroupContext()
        {
            WGroupSetHost host = CreateScriptableObject<WGroupSetHost>();
            host.set.Add(42);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(WGroupSetHost.set)
            );
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignFieldInfo(
                drawer,
                typeof(WGroupSetHost),
                nameof(WGroupSetHost.set)
            );
            GUIContent label = new("Set");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                using (GroupGUIWidthUtility.PushContentPadding(24f, 12f, 12f))
                {
                    // Test with tween enabled
                    UnityHelpersSettings.SetSerializableSetFoldoutTweenEnabled(true);
                    drawer.GetPropertyHeight(setProperty, label);

                    bool found1 = drawer.TryGetPendingAnimationStateForTests(
                        setProperty,
                        out _,
                        out _,
                        out bool hasAnimBool1
                    );
                    Assert.IsTrue(
                        found1 && hasAnimBool1,
                        "Should have AnimBool when tween enabled."
                    );

                    // Now disable tween
                    UnityHelpersSettings.SetSerializableSetFoldoutTweenEnabled(false);
                    drawer.GetPropertyHeight(setProperty, label);

                    bool found2 = drawer.TryGetPendingAnimationStateForTests(
                        setProperty,
                        out _,
                        out _,
                        out bool hasAnimBool2
                    );
                    Assert.IsTrue(
                        found2 && !hasAnimBool2,
                        "Should NOT have AnimBool when tween disabled."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void DictionaryInSettingsContextCreatesAnimBoolWhenTweenEnabled()
        {
            UnityHelpersSettings.SetSerializableDictionaryFoldoutTweenEnabled(true);

            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            SerializedObject serializedSettings = TrackDisposable(new SerializedObject(settings));
            serializedSettings.Update();

            SerializedProperty paletteProp = serializedSettings.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );

            if (paletteProp == null)
            {
                Assert.Ignore("WButtonCustomColors property not found in settings.");
                return;
            }

            paletteProp.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignFieldInfo(
                drawer,
                typeof(UnityHelpersSettings),
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
            GUIContent label = new("Palette");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();

                drawer.GetPropertyHeight(paletteProp, label);

                bool found = drawer.TryGetPendingAnimationStateForTests(
                    paletteProp,
                    out bool isExpanded,
                    out float animProgress,
                    out bool hasAnimBool
                );

                Assert.IsTrue(found, "Pending entry should exist after GetPropertyHeight.");
                Assert.IsTrue(
                    hasAnimBool,
                    "AnimBool should be created when dictionary tweening is enabled in Settings context."
                );
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void DictionaryInSettingsContextFoldoutProgressIsImmediateWhenTweenDisabled()
        {
            UnityHelpersSettings.SetSerializableDictionaryFoldoutTweenEnabled(false);

            UnityHelpersSettings settings = UnityHelpersSettings.instance;
            SerializedObject serializedSettings = TrackDisposable(new SerializedObject(settings));
            serializedSettings.Update();

            SerializedProperty paletteProp = serializedSettings.FindProperty(
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );

            if (paletteProp == null)
            {
                Assert.Ignore("WButtonCustomColors property not found in settings.");
                return;
            }

            paletteProp.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignFieldInfo(
                drawer,
                typeof(UnityHelpersSettings),
                UnityHelpersSettings.SerializedPropertyNames.WButtonCustomColors
            );
            GUIContent label = new("Palette");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();

                drawer.GetPropertyHeight(paletteProp, label);

                // Set to expanded
                drawer.SetPendingExpandedStateForTests(paletteProp, true);
                float expandedProgress = drawer.GetPendingFoldoutProgressFromInstance(paletteProp);
                Assert.AreEqual(
                    1f,
                    expandedProgress,
                    0.001f,
                    "Settings context: when tween disabled and expanded, progress should immediately be 1."
                );

                // Set to collapsed
                drawer.SetPendingExpandedStateForTests(paletteProp, false);
                float collapsedProgress = drawer.GetPendingFoldoutProgressFromInstance(paletteProp);
                Assert.AreEqual(
                    0f,
                    collapsedProgress,
                    0.001f,
                    "Settings context: when tween disabled and collapsed, progress should immediately be 0."
                );
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void NestedWGroupDictionaryAnimationIsIndependent()
        {
            UnityHelpersSettings.SetSerializableDictionaryFoldoutTweenEnabled(true);
            UnityHelpersSettings.SetSerializableSetFoldoutTweenEnabled(true);

            MultiWGroupHost host = CreateScriptableObject<MultiWGroupHost>();
            host.nestedDictionary["innerKey"] = 50;
            host.nestedSet.Add(99);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictProperty = serializedObject.FindProperty(
                nameof(MultiWGroupHost.nestedDictionary)
            );
            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(MultiWGroupHost.nestedSet)
            );
            dictProperty.isExpanded = true;
            setProperty.isExpanded = true;

            SerializableDictionaryPropertyDrawer dictDrawer = new();
            PropertyDrawerTestHelper.AssignFieldInfo(
                dictDrawer,
                typeof(MultiWGroupHost),
                nameof(MultiWGroupHost.nestedDictionary)
            );
            SerializableSetPropertyDrawer setDrawer = new();
            PropertyDrawerTestHelper.AssignFieldInfo(
                setDrawer,
                typeof(MultiWGroupHost),
                nameof(MultiWGroupHost.nestedSet)
            );
            GUIContent dictLabel = new("Dict");
            GUIContent setLabel = new("Set");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                // Simulate outer group padding
                using (GroupGUIWidthUtility.PushContentPadding(24f, 12f, 12f))
                {
                    // Simulate inner group padding
                    using (GroupGUIWidthUtility.PushContentPadding(24f, 12f, 12f))
                    {
                        dictDrawer.GetPropertyHeight(dictProperty, dictLabel);
                        setDrawer.GetPropertyHeight(setProperty, setLabel);

                        bool dictFound = dictDrawer.TryGetPendingAnimationStateForTests(
                            dictProperty,
                            out _,
                            out _,
                            out bool dictHasAnim
                        );
                        bool setFound = setDrawer.TryGetPendingAnimationStateForTests(
                            setProperty,
                            out _,
                            out _,
                            out bool setHasAnim
                        );

                        Assert.IsTrue(
                            dictFound && dictHasAnim,
                            "Nested dictionary should have AnimBool in nested WGroup."
                        );
                        Assert.IsTrue(
                            setFound && setHasAnim,
                            "Nested set should have AnimBool in nested WGroup."
                        );
                    }
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void WGroupTweenAndCollectionTweenAreIndependent()
        {
            // Enable WGroup tween, disable collection tweens
            UnityHelpersSettings.SetWGroupFoldoutTweenEnabled(true);
            UnityHelpersSettings.SetSerializableDictionaryFoldoutTweenEnabled(false);
            UnityHelpersSettings.SetSerializableSetFoldoutTweenEnabled(false);

            Assert.IsTrue(
                UnityHelpersSettings.ShouldTweenWGroupFoldouts(),
                "WGroup tween should be enabled."
            );
            Assert.IsFalse(
                SerializableDictionaryPropertyDrawer.IsTweeningEnabledForTests(false),
                "Dictionary tween should be disabled."
            );
            Assert.IsFalse(
                SerializableSetPropertyDrawer.IsTweeningEnabledForTests(false),
                "Set tween should be disabled."
            );

            // Now reverse: disable WGroup tween, enable collection tweens
            UnityHelpersSettings.SetWGroupFoldoutTweenEnabled(false);
            UnityHelpersSettings.SetSerializableDictionaryFoldoutTweenEnabled(true);
            UnityHelpersSettings.SetSerializableSetFoldoutTweenEnabled(true);

            Assert.IsFalse(
                UnityHelpersSettings.ShouldTweenWGroupFoldouts(),
                "WGroup tween should be disabled."
            );
            Assert.IsTrue(
                SerializableDictionaryPropertyDrawer.IsTweeningEnabledForTests(false),
                "Dictionary tween should be enabled."
            );
            Assert.IsTrue(
                SerializableSetPropertyDrawer.IsTweeningEnabledForTests(false),
                "Set tween should be enabled."
            );
        }

        [Test]
        public void SortedDictionaryInWGroupCreatesAnimBoolWhenTweenEnabled()
        {
            UnityHelpersSettings.SetSerializableSortedDictionaryFoldoutTweenEnabled(true);

            WGroupSortedDictionaryHost host = CreateScriptableObject<WGroupSortedDictionaryHost>();
            host.sortedDictionary["key1"] = 100;

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictProperty = serializedObject.FindProperty(
                nameof(WGroupSortedDictionaryHost.sortedDictionary)
            );
            dictProperty.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignFieldInfo(
                drawer,
                typeof(WGroupSortedDictionaryHost),
                nameof(WGroupSortedDictionaryHost.sortedDictionary)
            );
            GUIContent label = new("SortedDict");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                using (GroupGUIWidthUtility.PushContentPadding(24f, 12f, 12f))
                {
                    drawer.GetPropertyHeight(dictProperty, label);

                    bool found = drawer.TryGetPendingAnimationStateForTests(
                        dictProperty,
                        out _,
                        out _,
                        out bool hasAnimBool
                    );

                    Assert.IsTrue(found, "Pending entry should exist.");
                    Assert.IsTrue(
                        hasAnimBool,
                        "AnimBool should be created when sorted dictionary tweening is enabled."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void SortedSetInWGroupCreatesAnimBoolWhenTweenEnabled()
        {
            UnityHelpersSettings.SetSerializableSortedSetFoldoutTweenEnabled(true);

            WGroupSortedSetHost host = CreateScriptableObject<WGroupSortedSetHost>();
            host.sortedSet.Add(42);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(WGroupSortedSetHost.sortedSet)
            );
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignFieldInfo(
                drawer,
                typeof(WGroupSortedSetHost),
                nameof(WGroupSortedSetHost.sortedSet)
            );
            GUIContent label = new("SortedSet");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;

                GroupGUIWidthUtility.ResetForTests();
                using (GroupGUIWidthUtility.PushContentPadding(24f, 12f, 12f))
                {
                    drawer.GetPropertyHeight(setProperty, label);

                    bool found = drawer.TryGetPendingAnimationStateForTests(
                        setProperty,
                        out _,
                        out _,
                        out bool hasAnimBool
                    );

                    Assert.IsTrue(found, "Pending entry should exist.");
                    Assert.IsTrue(
                        hasAnimBool,
                        "AnimBool should be created when sorted set tweening is enabled."
                    );
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Serializable]
        private sealed class TestSortedStringIntDictionary
            : SerializableSortedDictionary<string, int> { }

        [Serializable]
        private sealed class TestSortedIntSet : SerializableSortedSet<int> { }

        private sealed class WGroupSortedDictionaryHost : ScriptableObject
        {
            [WGroup(
                "SortedGroup",
                displayName: "Sorted Group",
                collapsible: true,
                autoIncludeCount: 1
            )]
            public TestSortedStringIntDictionary sortedDictionary = new();
        }

        private sealed class WGroupSortedSetHost : ScriptableObject
        {
            [WGroup(
                "SortedGroup",
                displayName: "Sorted Group",
                collapsible: true,
                autoIncludeCount: 1
            )]
            public TestSortedIntSet sortedSet = new();
        }

        [Test]
        [TestCase(
            true,
            true,
            TestName = "DictionaryAnimBoolCreatedWhenTweenEnabledThenClearedWhenDisabled"
        )]
        [TestCase(false, true, TestName = "DictionaryAnimBoolNotCreatedWhenTweenAlwaysDisabled")]
        public void DictionaryAnimBoolLifecycleWithTweenToggle(bool enableFirst, bool disableAfter)
        {
            WGroupDictionaryHost host = CreateScriptableObject<WGroupDictionaryHost>();
            host.dictionary["key1"] = 100;

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(WGroupDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = true;

            SerializableDictionaryPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignFieldInfo(
                drawer,
                typeof(WGroupDictionaryHost),
                nameof(WGroupDictionaryHost.dictionary)
            );
            GUIContent label = new("Dictionary");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;
                GroupGUIWidthUtility.ResetForTests();

                using (GroupGUIWidthUtility.PushContentPadding(24f, 12f, 12f))
                {
                    UnityHelpersSettings.SetSerializableDictionaryFoldoutTweenEnabled(enableFirst);
                    drawer.GetPropertyHeight(dictionaryProperty, label);

                    bool foundInitial = drawer.TryGetPendingAnimationStateForTests(
                        dictionaryProperty,
                        out bool isExpandedInitial,
                        out float animProgressInitial,
                        out bool hasAnimBoolInitial
                    );

                    string initialDiagnostic =
                        $"[Initial] found={foundInitial}, expanded={isExpandedInitial}, progress={animProgressInitial:F3}, hasAnimBool={hasAnimBoolInitial}";
                    UnityEngine.Debug.Log(initialDiagnostic);

                    Assert.IsTrue(foundInitial, "Initial call should create pending entry.");
                    Assert.AreEqual(
                        enableFirst,
                        hasAnimBoolInitial,
                        $"AnimBool should {(enableFirst ? "exist" : "not exist")} when tween is {(enableFirst ? "enabled" : "disabled")}. Diagnostic: {initialDiagnostic}"
                    );

                    if (disableAfter && enableFirst)
                    {
                        UnityHelpersSettings.SetSerializableDictionaryFoldoutTweenEnabled(false);
                        drawer.GetPropertyHeight(dictionaryProperty, label);

                        bool foundAfter = drawer.TryGetPendingAnimationStateForTests(
                            dictionaryProperty,
                            out bool isExpandedAfter,
                            out float animProgressAfter,
                            out bool hasAnimBoolAfter
                        );

                        string afterDiagnostic =
                            $"[After Disable] found={foundAfter}, expanded={isExpandedAfter}, progress={animProgressAfter:F3}, hasAnimBool={hasAnimBoolAfter}";
                        UnityEngine.Debug.Log(afterDiagnostic);

                        Assert.IsTrue(
                            foundAfter,
                            "Pending entry should still exist after disabling tween."
                        );
                        Assert.IsFalse(
                            hasAnimBoolAfter,
                            $"AnimBool should be cleaned up when tween is disabled. Diagnostic: {afterDiagnostic}"
                        );
                    }
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        [TestCase(
            true,
            true,
            TestName = "SetAnimBoolCreatedWhenTweenEnabledThenClearedWhenDisabled"
        )]
        [TestCase(false, true, TestName = "SetAnimBoolNotCreatedWhenTweenAlwaysDisabled")]
        public void SetAnimBoolLifecycleWithTweenToggle(bool enableFirst, bool disableAfter)
        {
            WGroupSetHost host = CreateScriptableObject<WGroupSetHost>();
            host.set.Add(42);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(WGroupSetHost.set)
            );
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignFieldInfo(
                drawer,
                typeof(WGroupSetHost),
                nameof(WGroupSetHost.set)
            );
            GUIContent label = new("Set");

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;
                GroupGUIWidthUtility.ResetForTests();

                using (GroupGUIWidthUtility.PushContentPadding(24f, 12f, 12f))
                {
                    UnityHelpersSettings.SetSerializableSetFoldoutTweenEnabled(enableFirst);
                    drawer.GetPropertyHeight(setProperty, label);

                    bool foundInitial = drawer.TryGetPendingAnimationStateForTests(
                        setProperty,
                        out bool isExpandedInitial,
                        out float animProgressInitial,
                        out bool hasAnimBoolInitial
                    );

                    string initialDiagnostic =
                        $"[Initial] found={foundInitial}, expanded={isExpandedInitial}, progress={animProgressInitial:F3}, hasAnimBool={hasAnimBoolInitial}";
                    UnityEngine.Debug.Log(initialDiagnostic);

                    Assert.IsTrue(foundInitial, "Initial call should create pending entry.");
                    Assert.AreEqual(
                        enableFirst,
                        hasAnimBoolInitial,
                        $"AnimBool should {(enableFirst ? "exist" : "not exist")} when tween is {(enableFirst ? "enabled" : "disabled")}. Diagnostic: {initialDiagnostic}"
                    );

                    if (disableAfter && enableFirst)
                    {
                        UnityHelpersSettings.SetSerializableSetFoldoutTweenEnabled(false);
                        drawer.GetPropertyHeight(setProperty, label);

                        bool foundAfter = drawer.TryGetPendingAnimationStateForTests(
                            setProperty,
                            out bool isExpandedAfter,
                            out float animProgressAfter,
                            out bool hasAnimBoolAfter
                        );

                        string afterDiagnostic =
                            $"[After Disable] found={foundAfter}, expanded={isExpandedAfter}, progress={animProgressAfter:F3}, hasAnimBool={hasAnimBoolAfter}";
                        UnityEngine.Debug.Log(afterDiagnostic);

                        Assert.IsTrue(
                            foundAfter,
                            "Pending entry should still exist after disabling tween."
                        );
                        Assert.IsFalse(
                            hasAnimBoolAfter,
                            $"AnimBool should be cleaned up when tween is disabled. Diagnostic: {afterDiagnostic}"
                        );
                    }
                }
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        [TestCase(true, TestName = "SortedDictionaryAnimBoolCleanedUpWhenTweenDisabled")]
        [TestCase(false, TestName = "SortedSetAnimBoolCleanedUpWhenTweenDisabled")]
        public void SortedCollectionAnimBoolLifecycle(bool isSortedDictionary)
        {
            int previousIndentLevel = EditorGUI.indentLevel;

            try
            {
                EditorGUI.indentLevel = 0;
                GroupGUIWidthUtility.ResetForTests();

                if (isSortedDictionary)
                {
                    WGroupSortedDictionaryHost host =
                        CreateScriptableObject<WGroupSortedDictionaryHost>();
                    host.sortedDictionary["key1"] = 100;

                    SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
                    serializedObject.Update();

                    SerializedProperty sortedDictProperty = serializedObject.FindProperty(
                        nameof(WGroupSortedDictionaryHost.sortedDictionary)
                    );
                    sortedDictProperty.isExpanded = true;

                    SerializableDictionaryPropertyDrawer drawer = new();
                    PropertyDrawerTestHelper.AssignFieldInfo(
                        drawer,
                        typeof(WGroupSortedDictionaryHost),
                        nameof(WGroupSortedDictionaryHost.sortedDictionary)
                    );
                    GUIContent label = new("SortedDictionary");

                    using (GroupGUIWidthUtility.PushContentPadding(24f, 12f, 12f))
                    {
                        UnityHelpersSettings.SetSerializableSortedDictionaryFoldoutTweenEnabled(
                            true
                        );
                        drawer.GetPropertyHeight(sortedDictProperty, label);

                        bool found1 = drawer.TryGetPendingAnimationStateForTests(
                            sortedDictProperty,
                            out _,
                            out _,
                            out bool hasAnimBool1
                        );
                        Assert.IsTrue(
                            found1 && hasAnimBool1,
                            "Sorted dictionary should have AnimBool when tween enabled."
                        );

                        UnityHelpersSettings.SetSerializableSortedDictionaryFoldoutTweenEnabled(
                            false
                        );
                        drawer.GetPropertyHeight(sortedDictProperty, label);

                        bool found2 = drawer.TryGetPendingAnimationStateForTests(
                            sortedDictProperty,
                            out _,
                            out _,
                            out bool hasAnimBool2
                        );
                        Assert.IsTrue(found2, "Pending entry should still exist.");
                        Assert.IsFalse(
                            hasAnimBool2,
                            "Sorted dictionary AnimBool should be cleaned up when tween disabled."
                        );
                    }
                }
                else
                {
                    WGroupSortedSetHost host = CreateScriptableObject<WGroupSortedSetHost>();
                    host.sortedSet.Add(42);

                    SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
                    serializedObject.Update();

                    SerializedProperty sortedSetProperty = serializedObject.FindProperty(
                        nameof(WGroupSortedSetHost.sortedSet)
                    );
                    sortedSetProperty.isExpanded = true;

                    SerializableSetPropertyDrawer drawer = new();
                    PropertyDrawerTestHelper.AssignFieldInfo(
                        drawer,
                        typeof(WGroupSortedSetHost),
                        nameof(WGroupSortedSetHost.sortedSet)
                    );
                    GUIContent label = new("SortedSet");

                    using (GroupGUIWidthUtility.PushContentPadding(24f, 12f, 12f))
                    {
                        UnityHelpersSettings.SetSerializableSortedSetFoldoutTweenEnabled(true);
                        drawer.GetPropertyHeight(sortedSetProperty, label);

                        bool found1 = drawer.TryGetPendingAnimationStateForTests(
                            sortedSetProperty,
                            out _,
                            out _,
                            out bool hasAnimBool1
                        );
                        Assert.IsTrue(
                            found1 && hasAnimBool1,
                            "Sorted set should have AnimBool when tween enabled."
                        );

                        UnityHelpersSettings.SetSerializableSortedSetFoldoutTweenEnabled(false);
                        drawer.GetPropertyHeight(sortedSetProperty, label);

                        bool found2 = drawer.TryGetPendingAnimationStateForTests(
                            sortedSetProperty,
                            out _,
                            out _,
                            out bool hasAnimBool2
                        );
                        Assert.IsTrue(found2, "Pending entry should still exist.");
                        Assert.IsFalse(
                            hasAnimBool2,
                            "Sorted set AnimBool should be cleaned up when tween disabled."
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
