namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Extensions;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Helper;

    [CustomPropertyDrawer(typeof(WShowIfAttribute))]
    public sealed class WShowIfPropertyDrawer : PropertyDrawer
    {
        private static readonly Dictionary<
            Type,
            Dictionary<string, Func<object, object>>
        > CachedFields = new();

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
                if (enclosingObject == null)
                {
                    return true;
                }

                Type type = enclosingObject.GetType();
                Dictionary<string, Func<object, object>> cachedFields = CachedFields.GetOrAdd(type);
                if (
                    !cachedFields.TryGetValue(
                        showIf.conditionField,
                        out Func<object, object> accessor
                    )
                )
                {
                    FieldInfo field = type.GetField(
                        showIf.conditionField,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );
                    if (field == null)
                    {
                        Debug.LogError(
                            $"Failed to find conditional field {showIf.conditionField} on {type.Name}!"
                        );
                        accessor = _ => null;
                    }
                    else
                    {
                        accessor = ReflectionHelpers.GetFieldGetter(field);
                    }
                    cachedFields[showIf.conditionField] = accessor;
                }
                object fieldValue = accessor(enclosingObject);
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
