namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    [CustomPropertyDrawer(typeof(SerializableNullable<>), true)]
    public sealed class SerializableNullablePropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty hasValueProperty = property.FindPropertyRelative(
                SerializableNullableSerializedPropertyNames.HasValue
            );
            if (hasValueProperty == null)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float totalHeight = lineHeight;

            if (hasValueProperty.boolValue)
            {
                SerializedProperty valueProperty = property.FindPropertyRelative(
                    SerializableNullableSerializedPropertyNames.Value
                );
                float valueHeight =
                    valueProperty != null
                        ? EditorGUI.GetPropertyHeight(valueProperty, label, true)
                        : lineHeight;
                totalHeight += spacing + valueHeight;
            }

            return totalHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty hasValueProperty = property.FindPropertyRelative(
                SerializableNullableSerializedPropertyNames.HasValue
            );
            SerializedProperty valueProperty = property.FindPropertyRelative(
                SerializableNullableSerializedPropertyNames.Value
            );

            EditorGUI.BeginProperty(position, label, property);

            Rect toggleRect = new Rect(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight
            );

            EditorGUI.BeginChangeCheck();
            bool hasValue = hasValueProperty != null && hasValueProperty.boolValue;
            bool updatedHasValue = EditorGUI.ToggleLeft(toggleRect, label, hasValue);
            if (EditorGUI.EndChangeCheck() && hasValueProperty != null)
            {
                hasValueProperty.boolValue = updatedHasValue;
            }

            if (updatedHasValue && valueProperty != null)
            {
                float spacing = EditorGUIUtility.standardVerticalSpacing;
                Rect valueRect = new Rect(
                    position.x,
                    toggleRect.yMax + spacing,
                    position.width,
                    EditorGUI.GetPropertyHeight(valueProperty, true)
                );
                EditorGUI.indentLevel++;
                EditorGUI.PropertyField(valueRect, valueProperty, new GUIContent("Value"), true);
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }
#endif
}
