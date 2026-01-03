// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

// PropertyDrawer template - custom attribute drawer with caching

namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [CustomPropertyDrawer(typeof(MyAttribute))]
    public sealed class MyAttributePropertyDrawer : PropertyDrawer
    {
        private const float HelpBoxPadding = 2f;

        private static readonly Dictionary<string, float> HeightCache = new(
            System.StringComparer.Ordinal
        );
        private static readonly GUIContent ReusableContent = new();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float baseHeight = EditorGUI.GetPropertyHeight(property, label, true);
            // Add additional height for custom elements
            return baseHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            try
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
            finally
            {
                EditorGUI.EndProperty();
            }
        }

        // Optional: UI Toolkit support
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement container = new();
            container.style.flexDirection = FlexDirection.Column;

            PropertyField propertyField = new(property);
            propertyField.label = property.displayName;

            container.Add(propertyField);
            return container;
        }
    }
#endif
}

// Key PropertyDrawer Patterns:
// 1. Caching - Use static Dictionary caches for height calculations (drawers run every frame)
// 2. Reusable GUIContent - Create static GUIContent instances to reduce allocations
// 3. BeginProperty/EndProperty - Always wrap drawing in EditorGUI.BeginProperty / EndProperty
// 4. Height Calculation - Override GetPropertyHeight if adding custom elements
