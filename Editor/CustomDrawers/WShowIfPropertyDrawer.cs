namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using System.Reflection;
    using Extensions;
    using UnityEditor;
    using UnityEngine;
    using Core.Attributes;

    [CustomPropertyDrawer(typeof(WShowIfAttribute))]
    public sealed class WShowIfPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return !ShouldShow(property) ? 0f : EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (ShouldShow(property))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        private bool ShouldShow(SerializedProperty property)
        {
            if (attribute is not WShowIfAttribute showIf)
            {
                return true;
            }

            SerializedProperty conditionProperty = property.serializedObject.FindProperty(
                showIf.conditionField
            );
            if (conditionProperty is not { propertyType: SerializedPropertyType.Boolean })
            {
                if (conditionProperty != null)
                {
                    return true;
                }

                // This might not be a unity object, so fall back to reflection
                object enclosingObject = property.GetEnclosingObject(out _);
                FieldInfo conditionField = enclosingObject
                    ?.GetType()
                    .GetField(
                        showIf.conditionField,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );
                object fieldValue = conditionField?.GetValue(enclosingObject);
                if (fieldValue is bool maybeCondition)
                {
                    return showIf.inverse ? !maybeCondition : maybeCondition;
                }

                int index = Array.IndexOf(showIf.expectedValues, fieldValue);
                if (showIf.inverse)
                {
                    return index < 0;
                }

                return 0 <= index;
            }

            bool condition = conditionProperty.boolValue;
            return showIf.inverse ? !condition : condition;
        }
    }
#endif
}
