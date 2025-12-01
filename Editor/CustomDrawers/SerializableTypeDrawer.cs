namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    /// <summary>
    /// Thin wrapper that forwards SerializableType editing to the underlying StringInList-enabled field.
    /// </summary>
    [CustomPropertyDrawer(typeof(SerializableType))]
    public sealed class SerializableTypeDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty typeNameProperty = FindTypeNameProperty(property);
            if (typeNameProperty == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }
            return EditorGUI.GetPropertyHeight(typeNameProperty, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty typeNameProperty = FindTypeNameProperty(property);
            if (typeNameProperty == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            EditorGUI.PropertyField(position, typeNameProperty, label, true);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty typeNameProperty = FindTypeNameProperty(property);
            if (typeNameProperty == null)
            {
                return new PropertyField(property);
            }

            return new PropertyField(typeNameProperty, property.displayName);
        }

        private static SerializedProperty FindTypeNameProperty(SerializedProperty property)
        {
            return property.FindPropertyRelative(
                SerializableType.SerializedPropertyNames.AssemblyQualifiedName
            );
        }
    }
#endif
}
