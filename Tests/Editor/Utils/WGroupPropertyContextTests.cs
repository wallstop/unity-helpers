namespace WallstopStudios.UnityHelpers.Tests.Utils
{
#if UNITY_EDITOR
    using System;
    using System.Collections;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes;
    using WallstopStudios.UnityHelpers.Tests.EditorFramework;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes;

    /// <summary>
    /// Tests for the WGroup property context functionality in GroupGUIWidthUtility.
    /// Verifies that IsInsideWGroupPropertyDraw properly tracks when properties are
    /// being drawn inside a WGroup context, and that SerializableDictionary/SerializableSet
    /// correctly apply the WGroupFoldoutAlignmentOffset based on this context.
    /// </summary>
    [TestFixture]
    public sealed class WGroupPropertyContextTests : CommonTestBase
    {
        private int _originalIndentLevel;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            _originalIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            GroupGUIWidthUtility.ResetForTests();
            SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();
            SerializableDictionaryPropertyDrawer.ClearMainFoldoutAnimCacheForTests();
            SerializableSetPropertyDrawer.ResetLayoutTrackingForTests();
        }

        [TearDown]
        public override void TearDown()
        {
            EditorGUI.indentLevel = _originalIndentLevel;
            GroupGUIWidthUtility.ResetForTests();
            base.TearDown();
        }

        [Test]
        public void IsInsideWGroupPropertyDrawDefaultValueIsFalse()
        {
            GroupGUIWidthUtility.ResetForTests();

            bool isInside = GroupGUIWidthUtility.IsInsideWGroupPropertyDraw;

            Assert.That(
                isInside,
                Is.False,
                "IsInsideWGroupPropertyDraw should default to false after reset."
            );
        }

        [Test]
        public void IsInsideWGroupPropertyDrawReturnsTrueAfterPushWGroupPropertyContext()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPropertyContext())
            {
                bool isInside = GroupGUIWidthUtility.IsInsideWGroupPropertyDraw;

                Assert.That(
                    isInside,
                    Is.True,
                    "IsInsideWGroupPropertyDraw should return true after PushWGroupPropertyContext()."
                );
            }
        }

        [Test]
        public void IsInsideWGroupPropertyDrawReturnsToPreviousValueAfterScopeDisposed()
        {
            GroupGUIWidthUtility.ResetForTests();

            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                Is.False,
                "IsInsideWGroupPropertyDraw should be false before scope."
            );

            using (GroupGUIWidthUtility.PushWGroupPropertyContext())
            {
                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.True,
                    "IsInsideWGroupPropertyDraw should be true inside scope."
                );
            }

            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                Is.False,
                "IsInsideWGroupPropertyDraw should return to false after scope is disposed."
            );
        }

        [Test]
        public void NestedWGroupPropertyContextScopesWorkCorrectly()
        {
            GroupGUIWidthUtility.ResetForTests();

            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                Is.False,
                "Should be false initially."
            );

            using (GroupGUIWidthUtility.PushWGroupPropertyContext())
            {
                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.True,
                    "Should be true in first scope."
                );

                using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                {
                    Assert.That(
                        GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                        Is.True,
                        "Should be true in nested scope."
                    );
                }

                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.True,
                    "Should remain true after nested scope disposes (previous value was true)."
                );
            }

            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                Is.False,
                "Should return to false after all scopes disposed."
            );
        }

        [Test]
        public void ResetForTestsResetsIsInsideWGroupPropertyDrawFlag()
        {
            using (GroupGUIWidthUtility.PushWGroupPropertyContext())
            {
                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.True,
                    "Flag should be true inside scope."
                );

                GroupGUIWidthUtility.ResetForTests();

                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.False,
                    "ResetForTests should reset IsInsideWGroupPropertyDraw to false."
                );
            }

            // After disposal, the previous value was captured before reset,
            // so it would restore to false anyway in this case
            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                Is.False,
                "Should remain false after scope disposal post-reset."
            );
        }

        [Test]
        public void WGroupPropertyContextScopeRestoresPreviousValueOnException()
        {
            GroupGUIWidthUtility.ResetForTests();

            try
            {
                using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                {
                    Assert.That(
                        GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                        Is.True,
                        "Should be true inside scope before exception."
                    );
                    throw new InvalidOperationException("Test exception");
                }
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                Is.False,
                "Should restore to false after scope disposed due to exception."
            );
        }

        [Test]
        public void WGroupPropertyContextScopeIsIdempotentOnMultipleDispose()
        {
            GroupGUIWidthUtility.ResetForTests();

            IDisposable scope = GroupGUIWidthUtility.PushWGroupPropertyContext();

            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                Is.True,
                "Should be true after push."
            );

            scope.Dispose();

            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                Is.False,
                "Should be false after first dispose."
            );

            // Dispose again - should not change anything
            scope.Dispose();

            Assert.That(
                GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                Is.False,
                "Should remain false after second dispose (idempotent)."
            );
        }

        [Test]
        public void ExitWGroupThemingClearsPaletteAndContextFlags()
        {
            GroupGUIWidthUtility.ResetForTests();

            // Set up a WGroup context with palette and property context
            using (GroupGUIWidthUtility.PushWGroupPropertyContext())
            {
                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.True,
                    "Should be inside WGroup property context before exit."
                );

                // Exit the theming
                using (GroupGUIWidthUtility.ExitWGroupTheming())
                {
                    Assert.That(
                        GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                        Is.False,
                        "Should NOT be inside WGroup property context while in ExitWGroupTheming scope."
                    );

                    Assert.That(
                        GroupGUIWidthUtility.IsInsideWGroup,
                        Is.False,
                        "Should NOT be inside WGroup while in ExitWGroupTheming scope."
                    );
                }

                // After exit scope ends, context should be restored
                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.True,
                    "Should be restored to inside WGroup property context after ExitWGroupTheming scope ends."
                );
            }
        }

        [Test]
        public void ExitWGroupThemingRestoresContextOnException()
        {
            GroupGUIWidthUtility.ResetForTests();

            using (GroupGUIWidthUtility.PushWGroupPropertyContext())
            {
                try
                {
                    using (GroupGUIWidthUtility.ExitWGroupTheming())
                    {
                        Assert.That(
                            GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                            Is.False,
                            "Should NOT be inside WGroup property context in exit scope."
                        );
                        throw new InvalidOperationException("Test exception");
                    }
                }
                catch (InvalidOperationException)
                {
                    // Expected
                }

                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.True,
                    "Should restore to inside WGroup property context after exception."
                );
            }
        }

        [Test]
        public void DictionaryInsideWGroupPropertyContextDoesNotApplyPadding()
        {
            // When inside WGroup property context, WGroup uses EditorGUILayout.PropertyField
            // which means Unity's layout system already constrains the rect. The drawer should
            // NOT apply padding again - only EditorGUI.IndentedRect.
            Rect controlRect = new(0f, 0f, 400f, 300f);

            const float GroupLeftPadding = 12f;
            const float GroupRightPadding = 8f;
            float horizontalPadding = GroupLeftPadding + GroupRightPadding;

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
                using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                {
                    Rect resolvedRect =
                        SerializableDictionaryPropertyDrawer.ResolveContentRectForTests(
                            controlRect,
                            skipIndentation: false
                        );

                    // Padding should NOT be applied - Unity's layout handles it
                    Assert.AreEqual(
                        controlRect.x,
                        resolvedRect.x,
                        0.01f,
                        "Dictionary inside WGroup context should NOT apply padding (Unity layout handles it)."
                    );

                    Assert.AreEqual(
                        controlRect.width,
                        resolvedRect.width,
                        0.01f,
                        "Dictionary inside WGroup context should NOT reduce width (Unity layout handles it)."
                    );
                }
            }
        }

        [Test]
        public void NestedWGroupsWithDictionaryDoNotApplyCumulativePadding()
        {
            // When inside WGroup property context, the drawer does not apply padding
            // because Unity's layout system already handles it.
            Rect controlRect = new(0f, 0f, 400f, 300f);

            const float OuterLeftPadding = 10f;
            const float OuterRightPadding = 10f;
            const float InnerLeftPadding = 8f;
            const float InnerRightPadding = 8f;

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
                using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                {
                    using (
                        GroupGUIWidthUtility.PushContentPadding(
                            InnerLeftPadding + InnerRightPadding,
                            InnerLeftPadding,
                            InnerRightPadding
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

                            // Padding should NOT be applied - Unity's layout handles it
                            Assert.AreEqual(
                                controlRect.x,
                                resolvedRect.x,
                                0.01f,
                                "Nested WGroups should NOT apply padding for dictionary."
                            );

                            Assert.AreEqual(
                                controlRect.width,
                                resolvedRect.width,
                                0.01f,
                                "Nested WGroups should NOT reduce width for dictionary."
                            );
                        }
                    }
                }
            }
        }

        [Test]
        public void WGroupFoldoutAlignmentOffsetOnlyAppliedInsideWGroupContextForDictionary()
        {
            float alignmentOffset =
                SerializableDictionaryPropertyDrawer.WGroupFoldoutAlignmentOffset;

            Assert.That(
                alignmentOffset,
                Is.GreaterThan(0f),
                "WGroupFoldoutAlignmentOffset should be a positive value."
            );
            Assert.That(
                alignmentOffset,
                Is.EqualTo(2.5f).Within(0.001f),
                "WGroupFoldoutAlignmentOffset should be 2.5f for consistent visual alignment."
            );
        }

        [UnityTest]
        public IEnumerator DictionaryFoldoutGetsAlignmentOffsetWhenInsideWGroupPropertyContext()
        {
            IntegrationTestWGroupDictionaryHost host =
                CreateScriptableObject<IntegrationTestWGroupDictionaryHost>();
            host.dictionary["key1"] = 100;

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(IntegrationTestWGroupDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Dictionary");

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
                    SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

                    using (
                        GroupGUIWidthUtility.PushContentPadding(
                            horizontalPadding,
                            SimulatedLeftPadding,
                            SimulatedRightPadding
                        )
                    )
                    {
                        using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                        {
                            serializedObject.UpdateIfRequiredOrScript();
                            drawer.OnGUI(controlRect, dictionaryProperty, label);

                            hasFoldoutRect =
                                SerializableDictionaryPropertyDrawer.HasLastMainFoldoutRect;
                            if (hasFoldoutRect)
                            {
                                capturedFoldoutRect =
                                    SerializableDictionaryPropertyDrawer.LastMainFoldoutRect;
                            }
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
                + SerializableDictionaryPropertyDrawer.WGroupFoldoutAlignmentOffset;

            Assert.AreEqual(
                expectedX,
                capturedFoldoutRect.x,
                0.1f,
                "Dictionary foldout inside WGroup property context should be shifted right by alignment offset."
            );
        }

        [UnityTest]
        public IEnumerator DictionaryFoldoutDoesNotGetAlignmentOffsetOutsideWGroupPropertyContext()
        {
            TestDictionaryHost host = CreateScriptableObject<TestDictionaryHost>();
            host.dictionary[1] = "value1";

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty dictionaryProperty = serializedObject.FindProperty(
                nameof(TestDictionaryHost.dictionary)
            );
            dictionaryProperty.isExpanded = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableDictionaryPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Dictionary");

            Rect capturedFoldoutRect = default;
            Rect capturedResolvedPosition = default;
            bool hasFoldoutRect = false;
            int previousIndentLevel = EditorGUI.indentLevel;

            yield return TestIMGUIExecutor.Run(() =>
            {
                try
                {
                    EditorGUI.indentLevel = 0;

                    GroupGUIWidthUtility.ResetForTests();
                    SerializableDictionaryPropertyDrawer.ResetLayoutTrackingForTests();

                    // No WGroup context - should not apply alignment offset
                    serializedObject.UpdateIfRequiredOrScript();
                    drawer.OnGUI(controlRect, dictionaryProperty, label);

                    hasFoldoutRect = SerializableDictionaryPropertyDrawer.HasLastMainFoldoutRect;
                    if (hasFoldoutRect)
                    {
                        capturedFoldoutRect =
                            SerializableDictionaryPropertyDrawer.LastMainFoldoutRect;
                        capturedResolvedPosition = drawer.LastResolvedPosition;
                    }
                }
                finally
                {
                    EditorGUI.indentLevel = previousIndentLevel;
                }
            });

            Assert.IsTrue(hasFoldoutRect, "Main foldout rect should be tracked after OnGUI.");

            // Outside WGroup context, the foldout x should match the resolved position x
            // (no alignment offset applied)
            Assert.AreEqual(
                capturedResolvedPosition.x,
                capturedFoldoutRect.x,
                0.1f,
                "Dictionary foldout outside WGroup property context should NOT have alignment offset applied."
            );
        }

        [Test]
        public void SetInsideWGroupPropertyContextDoesNotApplyPadding()
        {
            // When inside WGroup property context, WGroup uses EditorGUILayout.PropertyField
            // which means Unity's layout system already constrains the rect. The drawer should
            // NOT apply padding again - only EditorGUI.IndentedRect.
            Rect controlRect = new(0f, 0f, 400f, 300f);

            const float GroupLeftPadding = 12f;
            const float GroupRightPadding = 8f;
            float horizontalPadding = GroupLeftPadding + GroupRightPadding;

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
                using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                {
                    Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                        controlRect,
                        skipIndentation: false
                    );

                    // Padding should NOT be applied - Unity's layout handles it
                    Assert.AreEqual(
                        controlRect.x,
                        resolvedRect.x,
                        0.01f,
                        "Set inside WGroup context should NOT apply padding (Unity layout handles it)."
                    );

                    Assert.AreEqual(
                        controlRect.width,
                        resolvedRect.width,
                        0.01f,
                        "Set inside WGroup context should NOT reduce width (Unity layout handles it)."
                    );
                }
            }
        }

        [Test]
        public void NestedWGroupsWithSetDoNotApplyCumulativePadding()
        {
            // When inside WGroup property context, the drawer does not apply padding
            // because Unity's layout system already handles it.
            Rect controlRect = new(0f, 0f, 400f, 300f);

            const float OuterLeftPadding = 10f;
            const float OuterRightPadding = 10f;
            const float InnerLeftPadding = 8f;
            const float InnerRightPadding = 8f;

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
                using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                {
                    using (
                        GroupGUIWidthUtility.PushContentPadding(
                            InnerLeftPadding + InnerRightPadding,
                            InnerLeftPadding,
                            InnerRightPadding
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

                            // Padding should NOT be applied - Unity's layout handles it
                            Assert.AreEqual(
                                controlRect.x,
                                resolvedRect.x,
                                0.01f,
                                "Nested WGroups should NOT apply padding for set."
                            );

                            Assert.AreEqual(
                                controlRect.width,
                                resolvedRect.width,
                                0.01f,
                                "Nested WGroups should NOT reduce width for set."
                            );
                        }
                    }
                }
            }
        }

        [Test]
        public void WGroupFoldoutAlignmentOffsetOnlyAppliedInsideWGroupContextForSet()
        {
            float alignmentOffset = SerializableSetPropertyDrawer.WGroupFoldoutAlignmentOffset;

            Assert.That(
                alignmentOffset,
                Is.GreaterThan(0f),
                "WGroupFoldoutAlignmentOffset should be a positive value."
            );
            Assert.That(
                alignmentOffset,
                Is.EqualTo(2.5f).Within(0.001f),
                "WGroupFoldoutAlignmentOffset should be 2.5f for consistent visual alignment."
            );
        }

        [UnityTest]
        public IEnumerator SetFoldoutGetsAlignmentOffsetWhenInsideWGroupPropertyContext()
        {
            IntegrationTestWGroupSetHost host =
                CreateScriptableObject<IntegrationTestWGroupSetHost>();
            host.set.Add(42);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(IntegrationTestWGroupSetHost.set)
            );
            setProperty.isExpanded = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

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
                        using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                        {
                            serializedObject.UpdateIfRequiredOrScript();
                            drawer.OnGUI(controlRect, setProperty, label);

                            hasFoldoutRect = SerializableSetPropertyDrawer.HasLastMainFoldoutRect;
                            if (hasFoldoutRect)
                            {
                                capturedFoldoutRect =
                                    SerializableSetPropertyDrawer.LastMainFoldoutRect;
                            }
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
                "Set foldout inside WGroup property context should be shifted right by alignment offset."
            );
        }

        [UnityTest]
        public IEnumerator SetFoldoutDoesNotGetAlignmentOffsetOutsideWGroupPropertyContext()
        {
            StringSetHost host = CreateScriptableObject<StringSetHost>();
            host.set.Add("test_value");

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(StringSetHost.set)
            );
            setProperty.isExpanded = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

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

                    GroupGUIWidthUtility.ResetForTests();
                    SerializableSetPropertyDrawer.ResetLayoutTrackingForTests();

                    // No WGroup context - should not apply alignment offset
                    serializedObject.UpdateIfRequiredOrScript();
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

            // Outside WGroup context, the foldout x should match the resolved position x
            // (no alignment offset applied)
            Assert.AreEqual(
                capturedResolvedPosition.x,
                capturedFoldoutRect.x,
                0.1f,
                "Set foldout outside WGroup property context should NOT have alignment offset applied."
            );
        }

        [Test]
        public void WGroupPropertyContextIsIndependentOfPaddingScope()
        {
            GroupGUIWidthUtility.ResetForTests();

            // Push padding without property context
            using (GroupGUIWidthUtility.PushContentPadding(10f, 5f, 5f))
            {
                Assert.That(
                    GroupGUIWidthUtility.CurrentScopeDepth,
                    Is.EqualTo(1),
                    "Scope depth should be 1 after padding push."
                );
                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.False,
                    "Property context should still be false without explicit push."
                );

                // Now push property context
                using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                {
                    Assert.That(
                        GroupGUIWidthUtility.CurrentScopeDepth,
                        Is.EqualTo(1),
                        "Scope depth should remain 1 - property context doesn't affect depth."
                    );
                    Assert.That(
                        GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                        Is.True,
                        "Property context should be true after push."
                    );
                }

                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.False,
                    "Property context should return to false."
                );
                Assert.That(
                    GroupGUIWidthUtility.CurrentScopeDepth,
                    Is.EqualTo(1),
                    "Scope depth should still be 1."
                );
            }

            Assert.That(
                GroupGUIWidthUtility.CurrentScopeDepth,
                Is.EqualTo(0),
                "Scope depth should be 0 after all scopes disposed."
            );
        }

        [Test]
        public void WGroupPropertyContextWithoutPaddingStillTracksCorrectly()
        {
            GroupGUIWidthUtility.ResetForTests();

            // Push property context without any padding
            using (GroupGUIWidthUtility.PushWGroupPropertyContext())
            {
                Assert.That(
                    GroupGUIWidthUtility.IsInsideWGroupPropertyDraw,
                    Is.True,
                    "Property context should be true."
                );
                Assert.That(
                    GroupGUIWidthUtility.CurrentScopeDepth,
                    Is.EqualTo(0),
                    "Scope depth should remain 0 - no padding was pushed."
                );
                Assert.That(
                    GroupGUIWidthUtility.CurrentHorizontalPadding,
                    Is.EqualTo(0f).Within(0.001f),
                    "Horizontal padding should be 0."
                );
            }
        }

        [Test]
        public void DictionaryAndSetHaveMatchingWGroupFoldoutAlignmentOffsets()
        {
            float dictionaryOffset =
                SerializableDictionaryPropertyDrawer.WGroupFoldoutAlignmentOffset;
            float setOffset = SerializableSetPropertyDrawer.WGroupFoldoutAlignmentOffset;

            Assert.AreEqual(
                dictionaryOffset,
                setOffset,
                0.001f,
                "Dictionary and Set should have matching WGroupFoldoutAlignmentOffset values for visual consistency."
            );
        }
    }
#endif
}
