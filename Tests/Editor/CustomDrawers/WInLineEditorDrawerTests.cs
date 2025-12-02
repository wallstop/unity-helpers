#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;

    public sealed class WInLineEditorDrawerTests
    {
        private const string CollapsedTargetPropertyName = nameof(InlineEditorHost.collapsedTarget);

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
            float expectedHeight = EditorGUIUtility.singleLineHeight;
            Assert.That(inlineHeight, Is.EqualTo(expectedHeight).Within(0.01f));
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

        private sealed class InlineEditorTarget : ScriptableObject
        {
            public int sampleValue;
        }
    }
}
#endif
