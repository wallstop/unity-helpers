namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
    using System;
    using System.Globalization;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [CustomPropertyDrawer(typeof(ValueDropdownAttribute))]
    public sealed class ValueDropdownDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (attribute is not ValueDropdownAttribute dropdownAttribute)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            object[] options = dropdownAttribute.Options ?? Array.Empty<object>();
            if (options.Length == 0 || !IsSupportedProperty(property))
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            int selectedIndex = ResolveSelectedIndex(
                property,
                dropdownAttribute.ValueType,
                options
            );
            if (selectedIndex < 0)
            {
                selectedIndex = 0;
            }

            string[] displayOptions = BuildDisplayLabels(options);

            EditorGUI.BeginProperty(position, label, property);
            try
            {
                int newIndex = EditorGUI.Popup(position, label.text, selectedIndex, displayOptions);
                if (newIndex >= 0 && newIndex < options.Length)
                {
                    ApplyOption(property, options[newIndex]);
                }
            }
            finally
            {
                EditorGUI.EndProperty();
            }
        }

        private static bool IsSupportedProperty(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.Integer
                || property.propertyType == SerializedPropertyType.Float
                || property.propertyType == SerializedPropertyType.String
                || property.propertyType == SerializedPropertyType.Enum;
        }

        private static int ResolveSelectedIndex(
            SerializedProperty property,
            Type valueType,
            object[] options
        )
        {
            for (int index = 0; index < options.Length; index += 1)
            {
                if (OptionMatches(property, valueType, options[index]))
                {
                    return index;
                }
            }

            return -1;
        }

        private static bool OptionMatches(
            SerializedProperty property,
            Type valueType,
            object option
        )
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return MatchesInteger(property, valueType, option);
                case SerializedPropertyType.Float:
                    return MatchesFloat(property, valueType, option);
                case SerializedPropertyType.String:
                    return MatchesString(property, option);
                case SerializedPropertyType.Enum:
                    return MatchesEnum(property, option);
                default:
                    return false;
            }
        }

        private static bool MatchesInteger(
            SerializedProperty property,
            Type valueType,
            object option
        )
        {
            if (option == null)
            {
                return false;
            }

            try
            {
                Type targetType = Nullable.GetUnderlyingType(valueType) ?? valueType;
                object converted = Convert.ChangeType(
                    property.longValue,
                    targetType,
                    CultureInfo.InvariantCulture
                );
                return Equals(converted, option);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool MatchesFloat(SerializedProperty property, Type valueType, object option)
        {
            if (option == null)
            {
                return false;
            }

            try
            {
                Type targetType = Nullable.GetUnderlyingType(valueType) ?? valueType;
                object converted = Convert.ChangeType(
                    property.doubleValue,
                    targetType,
                    CultureInfo.InvariantCulture
                );
                return Equals(converted, option);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool MatchesString(SerializedProperty property, object option)
        {
            if (option == null)
            {
                return string.IsNullOrEmpty(property.stringValue);
            }

            return string.Equals(property.stringValue, option as string, StringComparison.Ordinal);
        }

        private static bool MatchesEnum(SerializedProperty property, object option)
        {
            if (option == null)
            {
                return false;
            }

            if (option is Enum enumValue)
            {
                string optionName = enumValue.ToString();
                if (property.enumNames == null || property.enumNames.Length == 0)
                {
                    return false;
                }

                int enumIndex = property.enumValueIndex;
                if (enumIndex < 0 || enumIndex >= property.enumNames.Length)
                {
                    return false;
                }

                string currentName = property.enumNames[enumIndex];
                return string.Equals(currentName, optionName, StringComparison.Ordinal);
            }

            if (option is string optionString)
            {
                if (property.enumNames == null || property.enumNames.Length == 0)
                {
                    return false;
                }

                int enumIndex = property.enumValueIndex;
                if (enumIndex < 0 || enumIndex >= property.enumNames.Length)
                {
                    return false;
                }

                string currentName = property.enumNames[enumIndex];
                return string.Equals(currentName, optionString, StringComparison.Ordinal);
            }

            return false;
        }

        private static string[] BuildDisplayLabels(object[] options)
        {
            string[] labels = new string[options.Length];
            for (int index = 0; index < options.Length; index += 1)
            {
                labels[index] = FormatOption(options[index]);
            }

            return labels;
        }

        private static string FormatOption(object option)
        {
            if (option == null)
            {
                return "(null)";
            }

            if (option is IFormattable formattable)
            {
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            }

            return option.ToString();
        }

        private static void ApplyOption(SerializedProperty property, object selectedOption)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    ApplyInteger(property, selectedOption);
                    break;
                case SerializedPropertyType.Float:
                    ApplyFloat(property, selectedOption);
                    break;
                case SerializedPropertyType.String:
                    ApplyString(property, selectedOption);
                    break;
                case SerializedPropertyType.Enum:
                    ApplyEnum(property, selectedOption);
                    break;
            }
        }

        private static void ApplyInteger(SerializedProperty property, object selectedOption)
        {
            if (selectedOption == null)
            {
                return;
            }

            try
            {
                long value = Convert.ToInt64(selectedOption, CultureInfo.InvariantCulture);
                property.longValue = value;
            }
            catch (Exception) { }
        }

        private static void ApplyFloat(SerializedProperty property, object selectedOption)
        {
            if (selectedOption == null)
            {
                return;
            }

            try
            {
                double value = Convert.ToDouble(selectedOption, CultureInfo.InvariantCulture);
                property.doubleValue = value;
            }
            catch (Exception) { }
        }

        private static void ApplyString(SerializedProperty property, object selectedOption)
        {
            property.stringValue =
                selectedOption == null
                    ? string.Empty
                    : Convert.ToString(selectedOption, CultureInfo.InvariantCulture)
                        ?? string.Empty;
        }

        private static void ApplyEnum(SerializedProperty property, object selectedOption)
        {
            if (selectedOption == null)
            {
                return;
            }

            string optionName;
            if (selectedOption is Enum enumValue)
            {
                optionName = enumValue.ToString();
            }
            else if (selectedOption is string stringValue)
            {
                optionName = stringValue;
            }
            else
            {
                optionName = Convert.ToString(selectedOption, CultureInfo.InvariantCulture);
            }

            if (property.enumNames == null || property.enumNames.Length == 0)
            {
                return;
            }

            for (int index = 0; index < property.enumNames.Length; index += 1)
            {
                if (string.Equals(property.enumNames[index], optionName, StringComparison.Ordinal))
                {
                    property.enumValueIndex = index;
                    return;
                }
            }
        }
    }
}
