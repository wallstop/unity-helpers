namespace WallstopStudios.UnityHelpers.Tests.Attributes
{
    using System;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Tests.Utils;

    [TestFixture]
    public sealed class WInLineEditorPropertyDrawerTests : CommonTestBase
    {
        private static readonly FieldInfo AttributeField = typeof(PropertyDrawer).GetField(
            "m_Attribute",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        private static readonly MethodInfo CalculateInlineHeightMethod =
            typeof(WInLineEditorPropertyDrawer).GetMethod(
                "CalculateInlineHeight",
                BindingFlags.NonPublic | BindingFlags.Static
            );

        private static readonly Type CacheType = typeof(WInLineEditorPropertyDrawer).GetNestedType(
            "InlineEditorCache",
            BindingFlags.NonPublic
        );

        private static readonly FieldInfo CacheHasPreviewField = CacheType?.GetField(
            "hasPreview",
            BindingFlags.Public | BindingFlags.Instance
        );

        private static readonly FieldInfo CacheInspectorHeightField = CacheType?.GetField(
            "inspectorContentHeight",
            BindingFlags.Public | BindingFlags.Instance
        );

        [Test]
        public void NullReferenceUsesBaseHeight()
        {
            InlineEditorHost container = CreateScriptableObject<InlineEditorHost>();
            SerializedObject serializedObject = new(container);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(InlineEditorHost.inlineData)
            );
            Assert.NotNull(property);

            WInLineEditorAttribute inlineAttribute = new(
                WInLineEditorMode.AlwaysExpanded,
                inspectorHeight: 160f
            );
            WInLineEditorPropertyDrawer drawer = CreateDrawer(inlineAttribute);

            float baseHeight = EditorGUI.GetPropertyHeight(
                property,
                GUIContent.none,
                includeChildren: false
            );
            float height = drawer.GetPropertyHeight(property, GUIContent.none);

            Assert.That(height, Is.EqualTo(baseHeight));
        }

        [Test]
        public void AlwaysExpandedAddsInlineHeightWhenReferenceAssigned()
        {
            InlineEditorHost container = CreateScriptableObject<InlineEditorHost>();
            InlineData payload = CreateScriptableObject<InlineData>();
            payload.value = 42;

            container.inlineData = payload;

            SerializedObject serializedObject = new(container);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(InlineEditorHost.inlineData)
            );
            Assert.NotNull(property);

            WInLineEditorAttribute inlineAttribute = new(
                WInLineEditorMode.AlwaysExpanded,
                inspectorHeight: 140f
            );
            WInLineEditorPropertyDrawer drawer = CreateDrawer(inlineAttribute);

            float baseHeight = EditorGUI.GetPropertyHeight(
                property,
                GUIContent.none,
                includeChildren: false
            );
            float height = drawer.GetPropertyHeight(property, GUIContent.none);

            float inlineHeight = CalculateInlineHeight(inlineAttribute, hasPreview: false);
            float expected = baseHeight + EditorGUIUtility.standardVerticalSpacing + inlineHeight;
            Assert.That(height, Is.EqualTo(expected).Within(0.5f));
        }

        [Test]
        public void FoldoutCollapsedRemainsClosedUntilExpanded()
        {
            InlineEditorHost container = CreateScriptableObject<InlineEditorHost>();
            InlineData payload = CreateScriptableObject<InlineData>();
            container.collapsibleData = payload;

            SerializedObject serializedObject = new(container);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(InlineEditorHost.collapsibleData)
            );
            Assert.NotNull(property);

            WInLineEditorAttribute inlineAttribute = new(
                WInLineEditorMode.FoldoutCollapsed,
                inspectorHeight: 120f
            );
            WInLineEditorPropertyDrawer drawer = CreateDrawer(inlineAttribute);

            float collapsedHeight = drawer.GetPropertyHeight(property, GUIContent.none);
            float baseHeight = EditorGUI.GetPropertyHeight(
                property,
                GUIContent.none,
                includeChildren: false
            );
            Assert.That(collapsedHeight, Is.EqualTo(baseHeight));

            property.isExpanded = true;
            float expandedHeight = drawer.GetPropertyHeight(property, GUIContent.none);
            float inlineHeight = CalculateInlineHeight(inlineAttribute, hasPreview: false);
            float expected = baseHeight + EditorGUIUtility.standardVerticalSpacing + inlineHeight;
            Assert.That(expandedHeight, Is.EqualTo(expected).Within(0.5f));
        }

        [Test]
        public void PreviewHeightIncludedWhenAvailable()
        {
            InlineEditorHost container = CreateScriptableObject<InlineEditorHost>();
            Texture2D texture = new Texture2D(8, 8);
            Track(texture);
            container.previewTexture = texture;

            SerializedObject serializedObject = new(container);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(InlineEditorHost.previewTexture)
            );
            Assert.NotNull(property);

            WInLineEditorAttribute attributeWithoutPreview = new(
                WInLineEditorMode.AlwaysExpanded,
                inspectorHeight: 120f,
                drawObjectField: true,
                drawHeader: true,
                drawPreview: false
            );
            WInLineEditorPropertyDrawer drawerWithoutPreview = CreateDrawer(
                attributeWithoutPreview
            );
            float heightWithoutPreview = drawerWithoutPreview.GetPropertyHeight(
                property,
                GUIContent.none
            );

            WInLineEditorAttribute attributeWithPreview = new(
                WInLineEditorMode.AlwaysExpanded,
                inspectorHeight: 120f,
                drawObjectField: true,
                drawHeader: true,
                drawPreview: true,
                previewHeight: 48f
            );
            WInLineEditorPropertyDrawer drawerWithPreview = CreateDrawer(attributeWithPreview);
            float heightWithPreview = drawerWithPreview.GetPropertyHeight(
                property,
                GUIContent.none
            );

            float baseHeight = EditorGUI.GetPropertyHeight(
                property,
                GUIContent.none,
                includeChildren: false
            );
            float inlineWithoutPreview = CalculateInlineHeight(
                attributeWithoutPreview,
                hasPreview: true
            );
            float expectedWithoutPreview =
                baseHeight + EditorGUIUtility.standardVerticalSpacing + inlineWithoutPreview;
            float inlineWithPreview = CalculateInlineHeight(attributeWithPreview, hasPreview: true);
            float expectedWithPreview =
                baseHeight + EditorGUIUtility.standardVerticalSpacing + inlineWithPreview;

            Assert.That(heightWithoutPreview, Is.EqualTo(expectedWithoutPreview).Within(0.5f));
            Assert.That(heightWithPreview, Is.EqualTo(expectedWithPreview).Within(0.5f));
            Assert.That(
                heightWithPreview - heightWithoutPreview,
                Is.EqualTo(attributeWithPreview.previewHeight).Within(1f)
            );
        }

        [Test]
        public void HiddenObjectFieldStillDrawsInlineArea()
        {
            InlineEditorHost container = CreateScriptableObject<InlineEditorHost>();
            InlineData payload = CreateScriptableObject<InlineData>();
            container.headerOnlyData = payload;

            SerializedObject serializedObject = new(container);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(InlineEditorHost.headerOnlyData)
            );
            Assert.NotNull(property);

            WInLineEditorAttribute inlineAttribute = new(
                WInLineEditorMode.AlwaysExpanded,
                inspectorHeight: 150f,
                drawObjectField: false
            );
            WInLineEditorPropertyDrawer drawer = CreateDrawer(inlineAttribute);

            float height = drawer.GetPropertyHeight(property, GUIContent.none);
            float inlineHeight = CalculateInlineHeight(inlineAttribute, hasPreview: false);
            float expected =
                EditorGUIUtility.singleLineHeight
                + EditorGUIUtility.standardVerticalSpacing
                + inlineHeight;
            Assert.That(height, Is.EqualTo(expected).Within(0.5f));

            float width = 320f;
            Rect position = new Rect(0f, 0f, width, height);
            Assert.DoesNotThrow(() => ExecuteDrawerGUI(drawer, position, property));
        }

        private static WInLineEditorPropertyDrawer CreateDrawer(WInLineEditorAttribute attribute)
        {
            WInLineEditorPropertyDrawer drawer = new();
            Assert.NotNull(AttributeField);
            AttributeField.SetValue(drawer, attribute);
            return drawer;
        }

        private static float CalculateInlineHeight(
            WInLineEditorAttribute attribute,
            bool hasPreview
        )
        {
            Assert.NotNull(CalculateInlineHeightMethod);
            Assert.NotNull(CacheType);
            object cache = Activator.CreateInstance(CacheType);
            if (CacheHasPreviewField != null)
            {
                CacheHasPreviewField.SetValue(cache, hasPreview);
            }
            if (CacheInspectorHeightField != null)
            {
                CacheInspectorHeightField.SetValue(cache, Mathf.Max(attribute.inspectorHeight, 0f));
            }

            object result = CalculateInlineHeightMethod.Invoke(
                null,
                new[] { (object)attribute, cache }
            );
            return result is float castResult ? castResult : 0f;
        }

        private static void ExecuteDrawerGUI(
            WInLineEditorPropertyDrawer drawer,
            Rect position,
            SerializedProperty property
        )
        {
            Event previousEvent = Event.current;
            try
            {
                Event layoutEvent = new Event { type = EventType.Layout };
                Event.current = layoutEvent;
                drawer.OnGUI(position, property, GUIContent.none);

                Event repaintEvent = new Event { type = EventType.Repaint };
                Event.current = repaintEvent;
                drawer.OnGUI(position, property, GUIContent.none);
            }
            finally
            {
                Event.current = previousEvent;
            }
        }

        private sealed class InlineEditorHost : ScriptableObject
        {
            [WInLineEditor(WInLineEditorMode.AlwaysExpanded, inspectorHeight: 120f)]
            public InlineData inlineData;

            [WInLineEditor(WInLineEditorMode.FoldoutCollapsed, inspectorHeight: 120f)]
            public InlineData collapsibleData;

            [WInLineEditor(
                WInLineEditorMode.AlwaysExpanded,
                inspectorHeight: 120f,
                drawPreview: true,
                previewHeight: 48f
            )]
            public Texture2D previewTexture;

            [WInLineEditor(
                WInLineEditorMode.FoldoutExpanded,
                inspectorHeight: 150f,
                drawObjectField: false
            )]
            public InlineData headerOnlyData;
        }

        private sealed class InlineData : ScriptableObject
        {
            public string title = "Payload";
            public int value = 5;
            public float ratio = 0.5f;
        }
    }
}
