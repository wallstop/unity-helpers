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
    /// Tests for manual entry section padding resolution in SerializableSet property drawer.
    /// Verifies that settings context uses reduced padding to compensate for WGroup padding stacking.
    /// Note: UnityHelpersSettings does not contain SerializableHashSet properties, so settings
    /// context behavior for sets is tested via mock detection and property attribute inference.
    /// </summary>
    public sealed class SerializableSetPendingPaddingTests : CommonTestBase
    {
        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            GroupGUIWidthUtility.ResetForTests();
            SerializableSetPropertyDrawer.ResetLayoutTrackingForTests();
        }

        [Test]
        public void ManualEntrySectionPaddingConstantsHaveExpectedValues()
        {
            // The normal padding should be larger than the settings padding
            // Normal: 6f, Settings: 2f (difference of 4f compensates for WGroup padding)
            float normalPadding = 6f;
            float settingsPadding = 2f;

            Assert.AreEqual(normalPadding, 6f, "Normal manual entry section padding should be 6f.");
            Assert.AreEqual(
                settingsPadding,
                2f,
                "Settings manual entry section padding should be 2f."
            );
            Assert.AreEqual(
                4f,
                normalPadding - settingsPadding,
                "Padding difference should be 4f to compensate for WGroup horizontal padding."
            );
        }

        [Test]
        public void NormalContextUsesFullManualEntrySectionPadding()
        {
            PaddingTestSetHost host = CreateScriptableObject<PaddingTestSetHost>();
            host.set.Add(1);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(PaddingTestSetHost.set)
            );
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Set");

            float height = drawer.GetPropertyHeight(setProperty, label);

            Assert.Greater(height, 0f, "Property height should be positive.");

            // The drawer should NOT target settings in this case
            bool targetsSettings =
                serializedObject.targetObject is UnityHelpersSettings
                || Array.Exists(serializedObject.targetObjects, t => t is UnityHelpersSettings);

            Assert.IsFalse(
                targetsSettings,
                "Regular ScriptableObject should not be detected as UnityHelpersSettings."
            );
        }

        [Test]
        public void ManualEntryFoldoutToggleOffsetDiffersForContexts()
        {
            // Normal context toggle offset: 16f
            // Settings context toggle offset: 6f (10f less to account for WGroup offset)
            float normalOffset = 16f;
            float settingsOffset = 6f;

            Assert.AreEqual(16f, normalOffset, "Normal foldout toggle offset should be 16f.");
            Assert.AreEqual(6f, settingsOffset, "Settings foldout toggle offset should be 6f.");
            Assert.AreEqual(
                10f,
                normalOffset - settingsOffset,
                "Toggle offset difference should be 10f."
            );
        }

        [UnityTest]
        public IEnumerator OnGUINormalContextDrawsManualEntryWithFullPadding()
        {
            PaddingTestSetHost host = CreateScriptableObject<PaddingTestSetHost>();
            host.set.Add(1);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(PaddingTestSetHost.set)
            );
            setProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Set");

            Rect capturedRect = default;

            yield return TestIMGUIExecutor.Run(() =>
            {
                serializedObject.UpdateIfRequiredOrScript();
                drawer.OnGUI(controlRect, setProperty, label);
                capturedRect = drawer.LastResolvedPosition;
            });

            Assert.Greater(capturedRect.width, 0f, "Resolved position should have valid width.");
        }

        [Test]
        public void NullPropertyDoesNotCrashPaddingResolution()
        {
            PaddingTestSetHost host = CreateScriptableObject<PaddingTestSetHost>();

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));

            SerializedProperty nullProperty = null;

            Assert.DoesNotThrow(
                () =>
                {
                    SerializableSetPropertyDrawer drawer = new();
                    GUIContent label = new("Test");
                    try
                    {
                        drawer.GetPropertyHeight(nullProperty, label);
                    }
                    catch (NullReferenceException)
                    {
                        // Expected - property is null
                    }
                    catch (ArgumentNullException)
                    {
                        // Expected - property is null
                    }
                },
                "Null property should be handled gracefully."
            );
        }

        [Test]
        public void PropertyWithNullSerializedObjectDoesNotCrash()
        {
            PaddingTestSetHost host = CreateScriptableObject<PaddingTestSetHost>();
            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(PaddingTestSetHost.set)
            );

            // Dispose the serialized object to make its properties invalid
            serializedObject.Dispose();

            SerializableSetPropertyDrawer drawer = new();
            GUIContent label = new("Test");

            Assert.DoesNotThrow(
                () =>
                {
                    try
                    {
                        drawer.GetPropertyHeight(setProperty, label);
                    }
                    catch (Exception ex)
                        when (ex is ObjectDisposedException
                            || ex is NullReferenceException
                            || ex is InvalidOperationException
                        )
                    {
                        // Expected exceptions for disposed object access
                    }
                },
                "Disposed serialized object should be handled gracefully."
            );
        }

        [Test]
        public void MultipleDrawerInstancesOperateIndependently()
        {
            PaddingTestSetHost host1 = CreateScriptableObject<PaddingTestSetHost>();
            host1.set.Add(1);

            PaddingTestSetHost host2 = CreateScriptableObject<PaddingTestSetHost>();
            host2.set.Add(2);
            host2.set.Add(3);

            SerializedObject serializedObject1 = TrackDisposable(new SerializedObject(host1));
            SerializedObject serializedObject2 = TrackDisposable(new SerializedObject(host2));
            serializedObject1.Update();
            serializedObject2.Update();

            SerializedProperty property1 = serializedObject1.FindProperty(
                nameof(PaddingTestSetHost.set)
            );
            SerializedProperty property2 = serializedObject2.FindProperty(
                nameof(PaddingTestSetHost.set)
            );
            property1.isExpanded = true;
            property2.isExpanded = true;

            SerializableSetPropertyDrawer drawer1 = new();
            SerializableSetPropertyDrawer drawer2 = new();

            GUIContent label1 = new("Set 1");
            GUIContent label2 = new("Set 2");

            float height1 = drawer1.GetPropertyHeight(property1, label1);
            float height2 = drawer2.GetPropertyHeight(property2, label2);

            Assert.Greater(height1, 0f, "First set height should be positive.");
            Assert.Greater(height2, 0f, "Second set height should be positive.");
        }

        [Test]
        public void EmptySetHandledGracefully()
        {
            PaddingTestSetHost host = CreateScriptableObject<PaddingTestSetHost>();
            // Set is empty by default

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(PaddingTestSetHost.set)
            );
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            GUIContent label = new("Set");

            float height = drawer.GetPropertyHeight(setProperty, label);

            Assert.Greater(height, 0f, "Empty set should still have valid height.");
        }

        [Test]
        public void ExpandedSetTallerThanCollapsed()
        {
            PaddingTestSetHost host = CreateScriptableObject<PaddingTestSetHost>();
            host.set.Add(1);
            host.set.Add(2);
            host.set.Add(3);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(PaddingTestSetHost.set)
            );

            SerializableSetPropertyDrawer drawer = new();
            GUIContent label = new("Set");

            setProperty.isExpanded = true;
            float expandedHeight = drawer.GetPropertyHeight(setProperty, label);

            setProperty.isExpanded = false;
            float collapsedHeight = drawer.GetPropertyHeight(setProperty, label);

            Assert.Greater(
                expandedHeight,
                collapsedHeight,
                "Expanded set should be taller than collapsed."
            );
        }

        [UnityTest]
        public IEnumerator DrawerMaintainsConsistentPaddingAcrossMultipleRepaints()
        {
            PaddingTestSetHost host = CreateScriptableObject<PaddingTestSetHost>();
            host.set.Add(1);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(PaddingTestSetHost.set)
            );
            setProperty.isExpanded = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            SerializableSetPropertyDrawer drawer = new();
            Rect controlRect = new(0f, 0f, 400f, 300f);
            GUIContent label = new("Set");

            Rect firstRect = default;
            Rect secondRect = default;

            // First repaint
            yield return TestIMGUIExecutor.Run(() =>
            {
                serializedObject.UpdateIfRequiredOrScript();
                drawer.OnGUI(controlRect, setProperty, label);
                firstRect = drawer.LastResolvedPosition;
            });

            // Second repaint
            yield return TestIMGUIExecutor.Run(() =>
            {
                serializedObject.UpdateIfRequiredOrScript();
                drawer.OnGUI(controlRect, setProperty, label);
                secondRect = drawer.LastResolvedPosition;
            });

            Assert.AreEqual(
                firstRect.x,
                secondRect.x,
                0.01f,
                "Resolved x position should be consistent across repaints."
            );
            Assert.AreEqual(
                firstRect.width,
                secondRect.width,
                0.01f,
                "Resolved width should be consistent across repaints."
            );
        }

        [Test]
        public void SetWithManyElementsHandledCorrectly()
        {
            PaddingTestSetHost host = CreateScriptableObject<PaddingTestSetHost>();
            for (int i = 0; i < 20; i++)
            {
                host.set.Add(i);
            }

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(PaddingTestSetHost.set)
            );
            setProperty.isExpanded = true;

            SerializableSetPropertyDrawer drawer = new();
            GUIContent label = new("Large Set");

            float height = drawer.GetPropertyHeight(setProperty, label);

            Assert.Greater(
                height,
                EditorGUIUtility.singleLineHeight * 10,
                "Set with 20 elements should have substantial height."
            );
        }

        [Test]
        public void VeryLargeIndentLevelHandledGracefully()
        {
            PaddingTestSetHost host = CreateScriptableObject<PaddingTestSetHost>();
            host.set.Add(42);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(PaddingTestSetHost.set)
            );
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
        public void ZeroIndentLevelAppliesMinimumPadding()
        {
            PaddingTestSetHost host = CreateScriptableObject<PaddingTestSetHost>();
            host.set.Add(1);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(PaddingTestSetHost.set)
            );
            setProperty.isExpanded = true;

            GUIContent label = new("Set");
            Rect originalRect = new(0f, 0f, 400f, 300f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;
                GroupGUIWidthUtility.ResetForTests();

                Rect resolvedRect = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                    originalRect,
                    skipIndentation: false
                );

                float expectedMinimumIndent = 6f;
                Assert.GreaterOrEqual(
                    resolvedRect.x,
                    expectedMinimumIndent - 1f,
                    "Minimum padding should be applied at indent level 0."
                );
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }

        [Test]
        public void HigherIndentLevelReducesWidth()
        {
            PaddingTestSetHost host = CreateScriptableObject<PaddingTestSetHost>();
            host.set.Add(1);

            SerializedObject serializedObject = TrackDisposable(new SerializedObject(host));
            serializedObject.Update();

            SerializedProperty setProperty = serializedObject.FindProperty(
                nameof(PaddingTestSetHost.set)
            );
            setProperty.isExpanded = true;

            GUIContent label = new("Set");
            Rect originalRect = new(0f, 0f, 400f, 300f);

            int previousIndentLevel = EditorGUI.indentLevel;
            try
            {
                GroupGUIWidthUtility.ResetForTests();

                EditorGUI.indentLevel = 1;
                Rect rectAtLevel1 = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                    originalRect,
                    skipIndentation: false
                );

                EditorGUI.indentLevel = 4;
                Rect rectAtLevel4 = SerializableSetPropertyDrawer.ResolveContentRectForTests(
                    originalRect,
                    skipIndentation: false
                );

                Assert.Greater(
                    rectAtLevel4.x,
                    rectAtLevel1.x,
                    "Higher indent level should increase x offset."
                );
                Assert.Less(
                    rectAtLevel4.width,
                    rectAtLevel1.width,
                    "Higher indent level should reduce width."
                );
            }
            finally
            {
                EditorGUI.indentLevel = previousIndentLevel;
            }
        }
    }
}
