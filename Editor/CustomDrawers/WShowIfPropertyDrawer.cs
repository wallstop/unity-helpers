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
            if (conditionProperty == null)
            {
                string propertyPath = property.propertyPath;
                if (!string.IsNullOrEmpty(propertyPath))
                {
                    int separatorIndex = propertyPath.LastIndexOf('.');
                    string siblingPath =
                        separatorIndex == -1
                            ? showIf.conditionField
                            : propertyPath.Substring(0, separatorIndex + 1) + showIf.conditionField;
                    conditionProperty = property.serializedObject.FindProperty(siblingPath);
                }
            }
            if (conditionProperty != null)
            {
                if (TryEvaluateCondition(conditionProperty, showIf, out bool serializedResult))
                {
                    return serializedResult;
                }
                return true;
            }

            object enclosingObject = property.GetEnclosingObject(out _);
            if (enclosingObject == null)
            {
                return true;
            }

            Type type = enclosingObject.GetType();
            Dictionary<string, Func<object, object>> cachedFields = CachedFields.GetOrAdd(type);
            if (!cachedFields.TryGetValue(showIf.conditionField, out Func<object, object> accessor))
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
            return !TryEvaluateCondition(fieldValue, showIf, out bool reflectedResult)
                || reflectedResult;
        }

        private static bool TryEvaluateCondition(
            SerializedProperty conditionProperty,
            WShowIfAttribute showIf,
            out bool shouldShow
        )
        {
            if (conditionProperty == null)
            {
                shouldShow = true;
                return false;
            }

            if (conditionProperty.propertyType == SerializedPropertyType.Boolean)
            {
                bool condition = conditionProperty.boolValue;
                shouldShow = showIf.inverse ? !condition : condition;
                return true;
            }

            object conditionValue = conditionProperty.GetTargetObjectWithField(out _);
            return TryEvaluateCondition(conditionValue, showIf, out shouldShow);
        }

        private static bool TryEvaluateCondition(
            object conditionValue,
            WShowIfAttribute showIf,
            out bool shouldShow
        )
        {
            if (conditionValue is bool boolean)
            {
                shouldShow = showIf.inverse ? !boolean : boolean;
                return true;
            }

            object[] expectedValues = showIf.expectedValues;
            if (expectedValues == null || expectedValues.Length == 0)
            {
                shouldShow = true;
                return false;
            }

            bool match = false;
            foreach (object expectedValue in expectedValues)
            {
                if (ValuesEqual(conditionValue, expectedValue))
                {
                    match = true;
                    break;
                }
            }

            shouldShow = showIf.inverse ? !match : match;
            return true;
        }

        private static bool ValuesEqual(object actual, object expected)
        {
            if (ReferenceEquals(actual, expected))
            {
                return true;
            }

            if (actual == null || expected == null)
            {
                return false;
            }

            if (actual.Equals(expected))
            {
                return true;
            }

            Type actualType = actual.GetType();
            Type expectedType = expected.GetType();

            try
            {
                if (actualType.IsEnum || expectedType.IsEnum)
                {
                    long actualValue = Convert.ToInt64(actual);
                    long expectedValue = Convert.ToInt64(expected);
                    return actualValue == expectedValue;
                }
            }
            catch
            {
                return false;
            }

            if (actual is not IConvertible || expected is not IConvertible)
            {
                return false;
            }

            try
            {
                double actualValue = Convert.ToDouble(actual);
                double expectedValue = Convert.ToDouble(expected);
                return Math.Abs(actualValue - expectedValue) < double.Epsilon;
            }
            catch
            {
                return false;
            }
        }
    }
#endif
}
