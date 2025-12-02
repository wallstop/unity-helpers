#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Editor.Internal;

    public sealed class WInLineEditorDrawerTests
    {
        private const string CollapsedTargetPropertyName = nameof(InlineEditorHost.collapsedTarget);
        private const float InlinePaddingContribution = 4f;

        [SetUp]
        public void SetUp()
        {
            WInLineEditorDrawer.ClearCachedStateForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            WInLineEditorDrawer.ClearCachedStateForTesting();
        }

        [Test]
        public void HeaderFoldoutControlsInlineHeight()
        {
            float collapsedHeight = MeasurePropertyHeight<InlineEditorHost>(
                propertyExpanded: false,
                setInlineExpanded: false
            );
            float expandedHeight = MeasurePropertyHeight<InlineEditorHost>(
                propertyExpanded: false,
                setInlineExpanded: true
            );
            Assert.That(expandedHeight, Is.GreaterThan(collapsedHeight));

            float collapsedAgainHeight = MeasurePropertyHeight<InlineEditorHost>(
                propertyExpanded: false,
                setInlineExpanded: false
            );
            Assert.That(collapsedAgainHeight, Is.EqualTo(collapsedHeight).Within(0.001f));
        }

        [Test]
        public void BuiltInInlineInspectorRemainsSuppressed()
        {
            float collapsedHeight = MeasurePropertyHeight<InlineEditorHost>(
                propertyExpanded: false
            );
            float expandedHeight = MeasurePropertyHeight<InlineEditorHost>(propertyExpanded: true);
            Assert.That(expandedHeight, Is.EqualTo(collapsedHeight));
        }

        [Test]
        public void StandaloneHeaderOnlyDrawnWhenObjectFieldHidden()
        {
            float heightWithObjectField = MeasurePropertyHeight<InlineEditorHost>(
                propertyExpanded: false,
                setInlineExpanded: true
            );
            float heightWithStandaloneHeader = MeasurePropertyHeight<HeaderOnlyInlineEditorHost>(
                propertyExpanded: false,
                setInlineExpanded: true
            );
            const float ExpectedHeaderContribution = 22f; // HeaderHeight + Spacing
            float difference = heightWithStandaloneHeader - heightWithObjectField;
            Assert.That(difference, Is.EqualTo(ExpectedHeaderContribution).Within(0.001f));
        }

        [Test]
        public void InlineInspectorOmitsScriptField()
        {
            float collapsedHeight = MeasurePropertyHeight<NoScrollInlineEditorHost>(
                propertyExpanded: false,
                setInlineExpanded: false
            );
            float expandedHeight = MeasurePropertyHeight<NoScrollInlineEditorHost>(
                propertyExpanded: false,
                setInlineExpanded: true
            );
            float inlineHeight = expandedHeight - collapsedHeight;
            float expectedHeight = EditorGUIUtility.singleLineHeight + InlinePaddingContribution;
            Assert.That(inlineHeight, Is.EqualTo(expectedHeight).Within(0.01f));
        }

        [Test]
        public void SimpleTargetsDoNotTriggerHorizontalScrollbars()
        {
            SimpleInlineEditorTarget target = CreateHiddenInstance<SimpleInlineEditorTarget>();
            try
            {
                WInLineEditorAttribute inlineAttribute = new WInLineEditorAttribute();
                bool usesScrollbar = WInLineEditorDrawer.UsesHorizontalScrollbarForTesting(
                    target,
                    inlineAttribute,
                    availableWidth: 360f
                );
                Assert.That(usesScrollbar, Is.False);
            }
            finally
            {
                ScriptableObject.DestroyImmediate(target);
            }
        }

        [Test]
        public void ComplexTargetsStillTriggerHorizontalScrollbars()
        {
            ArrayInlineEditorTarget target = CreateHiddenInstance<ArrayInlineEditorTarget>();
            try
            {
                WInLineEditorAttribute inlineAttribute = new WInLineEditorAttribute();
                bool usesScrollbar = WInLineEditorDrawer.UsesHorizontalScrollbarForTesting(
                    target,
                    inlineAttribute,
                    availableWidth: 360f
                );
                Assert.That(usesScrollbar, Is.True);
            }
            finally
            {
                ScriptableObject.DestroyImmediate(target);
            }
        }

        [Test]
        public void ExplicitMinWidthOverridesSimpleTargetHeuristic()
        {
            SimpleInlineEditorTarget target = CreateHiddenInstance<SimpleInlineEditorTarget>();
            try
            {
                WInLineEditorAttribute inlineAttribute = new WInLineEditorAttribute(
                    minInspectorWidth: 720f
                );
                bool usesScrollbar = WInLineEditorDrawer.UsesHorizontalScrollbarForTesting(
                    target,
                    inlineAttribute,
                    availableWidth: 360f
                );
                Assert.That(usesScrollbar, Is.True);
            }
            finally
            {
                ScriptableObject.DestroyImmediate(target);
            }
        }

        [Test]
        public void CustomEditorsRespectMeasuredInlineHeight()
        {
            float collapsedHeight = MeasurePropertyHeight<CustomEditorInlineHost>(
                propertyExpanded: false,
                setInlineExpanded: false
            );
            float expandedHeight = MeasurePropertyHeight<CustomEditorInlineHost>(
                propertyExpanded: false,
                setInlineExpanded: true
            );
            float inlineHeight = expandedHeight - collapsedHeight;
            Assert.That(inlineHeight, Is.GreaterThan(40f));
            Assert.That(inlineHeight, Is.LessThan(140f));
        }

        [Test]
        public void InlineInspectorContentRectAppliesPadding()
        {
            Rect outer = new Rect(10f, 20f, 200f, 100f);
            Rect content = WInLineEditorDrawer.GetInlineContentRectForTesting(outer);
            Assert.That(content.x, Is.EqualTo(outer.x + 2f));
            Assert.That(content.y, Is.EqualTo(outer.y + 2f));
            Assert.That(content.width, Is.EqualTo(outer.width - 4f));
            Assert.That(content.height, Is.EqualTo(outer.height - 4f));
        }

        [Test]
        public void InlineInspectorContentRectClampsHeightToZero()
        {
            Rect outer = new Rect(0f, 0f, 4f, 3f);
            Rect content = WInLineEditorDrawer.GetInlineContentRectForTesting(outer);
            Assert.That(content.height, Is.EqualTo(0f));
        }

        private static float MeasurePropertyHeight<THost>(
            bool propertyExpanded,
            bool? setInlineExpanded = null
        )
            where THost : ScriptableObject
        {
            WInLineEditorDrawer.ClearCachedStateForTesting();
            THost host = ScriptableObject.CreateInstance<THost>();
            InlineEditorTarget target = ScriptableObject.CreateInstance<InlineEditorTarget>();
            host.hideFlags = HideFlags.HideAndDontSave;
            target.hideFlags = HideFlags.HideAndDontSave;

            try
            {
                using SerializedObject serializedHost = new SerializedObject(host);
                serializedHost.Update();
                SerializedProperty property = serializedHost.FindProperty(
                    CollapsedTargetPropertyName
                );
                Assert.That(property, Is.Not.Null);
                property.objectReferenceValue = target;
                serializedHost.ApplyModifiedPropertiesWithoutUndo();
                serializedHost.Update();
                property = serializedHost.FindProperty(CollapsedTargetPropertyName);
                Assert.That(property, Is.Not.Null);
                property.isExpanded = propertyExpanded;
                if (setInlineExpanded.HasValue)
                {
                    WInLineEditorDrawer.SetInlineFoldoutStateForTesting(
                        property,
                        setInlineExpanded.Value
                    );
                }

                GUIContent label = new GUIContent("Target");
                WInLineEditorDrawer drawer = new WInLineEditorDrawer();
                return drawer.GetPropertyHeight(property, label);
            }
            finally
            {
                ScriptableObject.DestroyImmediate(host);
                ScriptableObject.DestroyImmediate(target);
            }
        }

        private static T CreateHiddenInstance<T>()
            where T : ScriptableObject
        {
            T instance = ScriptableObject.CreateInstance<T>();
            instance.hideFlags = HideFlags.HideAndDontSave;
            return instance;
        }

        private sealed class InlineEditorHost : ScriptableObject
        {
            [WInLineEditor(WInLineEditorMode.FoldoutCollapsed)]
            public InlineEditorTarget collapsedTarget;
        }

        private sealed class HeaderOnlyInlineEditorHost : ScriptableObject
        {
            [WInLineEditor(mode: WInLineEditorMode.FoldoutCollapsed, drawObjectField: false)]
            public InlineEditorTarget collapsedTarget;
        }

        private sealed class NoScrollInlineEditorHost : ScriptableObject
        {
            [WInLineEditor(WInLineEditorMode.FoldoutCollapsed, 400f, false, 64f, true, true, false)]
            public InlineEditorTarget collapsedTarget;
        }

        private sealed class SimpleInlineEditorTarget : ScriptableObject
        {
            public int number;
            public string description;
        }

        private sealed class ArrayInlineEditorTarget : ScriptableObject
        {
            public int[] values = new int[2];
        }

        private sealed class CustomEditorInlineHost : ScriptableObject
        {
            [WInLineEditor(WInLineEditorMode.FoldoutCollapsed)]
            public SimpleCustomEditorTarget customTarget;
        }

        private sealed class SimpleCustomEditorTarget : ScriptableObject
        {
            public bool toggle;
            public int number;
        }

        [CustomEditor(typeof(SimpleCustomEditorTarget))]
        private sealed class SimpleCustomEditorTargetEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                serializedObject.Update();
                SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
                if (scriptProperty != null && !InlineInspectorContext.IsActive)
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(scriptProperty, true);
                    }
                    EditorGUILayout.Space();
                }

                SerializedProperty iterator = serializedObject.GetIterator();
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    if (iterator.propertyPath == "m_Script")
                    {
                        enterChildren = false;
                        continue;
                    }

                    EditorGUILayout.PropertyField(iterator, true);
                    enterChildren = false;
                }

                serializedObject.ApplyModifiedProperties();
            }
        }

        private sealed class InlineEditorTarget : ScriptableObject
        {
            public int sampleValue;
        }
    }
}
#endif
