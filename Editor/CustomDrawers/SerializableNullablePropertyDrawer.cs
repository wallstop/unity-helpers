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
            SerializedProperty valueProperty = property.FindPropertyRelative(
                SerializableNullableSerializedPropertyNames.Value
            );
            if (hasValueProperty == null)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            float lineHeight = EditorGUIUtility.singleLineHeight;
            if (!hasValueProperty.boolValue || valueProperty == null)
            {
                return lineHeight;
            }

            float valueHeight = EditorGUI.GetPropertyHeight(valueProperty, true);
            return Mathf.Max(lineHeight, valueHeight);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty hasValueProperty = property.FindPropertyRelative(
                SerializableNullableSerializedPropertyNames.HasValue
            );
            SerializedProperty valueProperty = property.FindPropertyRelative(
                SerializableNullableSerializedPropertyNames.Value
            );

            if (hasValueProperty == null || valueProperty == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            Rect fieldRect = EditorGUI.PrefixLabel(
                position,
                GUIUtility.GetControlID(FocusType.Passive),
                label
            );
            int previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float toggleWidth = EditorGUIUtility.singleLineHeight;
            float toggleHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float toggleY = position.y + (position.height - toggleHeight) * 0.5f;

            Rect toggleRect = new Rect(fieldRect.x, toggleY, toggleWidth, toggleHeight);

            EditorGUI.BeginChangeCheck();
            bool hasValue = hasValueProperty.boolValue;
            bool updatedHasValue = EditorGUI.Toggle(toggleRect, hasValue);
            if (EditorGUI.EndChangeCheck())
            {
                hasValueProperty.boolValue = updatedHasValue;
            }

            if (updatedHasValue)
            {
                float valueWidth = Mathf.Max(0f, fieldRect.width - toggleWidth - spacing);
                Rect valueRect = new Rect(
                    toggleRect.xMax + spacing,
                    position.y,
                    valueWidth,
                    position.height
                );
                EditorGUI.PropertyField(valueRect, valueProperty, GUIContent.none, true);
            }

            EditorGUI.indentLevel = previousIndent;

            EditorGUI.EndProperty();
        }
    }
#endif
}
