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
            float collapsedHeight = MeasurePropertyHeight(
                propertyExpanded: false,
                setInlineExpanded: false
            );
            float expandedHeight = MeasurePropertyHeight(
                propertyExpanded: false,
                setInlineExpanded: true
            );
            Assert.That(expandedHeight, Is.GreaterThan(collapsedHeight));

            float collapsedAgainHeight = MeasurePropertyHeight(
                propertyExpanded: false,
                setInlineExpanded: false
            );
            Assert.That(collapsedAgainHeight, Is.EqualTo(collapsedHeight).Within(0.001f));
        }

        [Test]
        public void BuiltInInlineInspectorRemainsSuppressed()
        {
            float collapsedHeight = MeasurePropertyHeight(propertyExpanded: false);
            float expandedHeight = MeasurePropertyHeight(propertyExpanded: true);
            Assert.That(expandedHeight, Is.EqualTo(collapsedHeight));
        }

        private static float MeasurePropertyHeight(
            bool propertyExpanded,
            bool? setInlineExpanded = null
        )
        {
            WInLineEditorDrawer.ClearCachedStateForTesting();
            InlineEditorHost host = ScriptableObject.CreateInstance<InlineEditorHost>();
            InlineEditorTarget target = ScriptableObject.CreateInstance<InlineEditorTarget>();
            host.hideFlags = HideFlags.HideAndDontSave;
            target.hideFlags = HideFlags.HideAndDontSave;
            host.collapsedTarget = target;

            try
            {
                using SerializedObject serializedHost = new SerializedObject(host);
                serializedHost.Update();
                SerializedProperty property = serializedHost.FindProperty(
                    nameof(InlineEditorHost.collapsedTarget)
                );
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

        private sealed class InlineEditorTarget : ScriptableObject
        {
            public int sampleValue;
        }
    }
}
#endif
