namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Property drawer for <see cref="WNotNullAttribute"/> that displays a warning or error
    /// in the inspector when the decorated field is null.
    /// </summary>
    [CustomPropertyDrawer(typeof(WNotNullAttribute))]
    public sealed class WNotNullPropertyDrawer : PropertyDrawer
    {
        private const float HelpBoxPadding = 2f;
        private const string DefaultWarningMessageFormat = "{0} must not be null";
        private const string NullValueMessage = "Field is null or unassigned";

        private static readonly Dictionary<string, float> HelpBoxHeightCache = new(
            System.StringComparer.Ordinal
        );
        private static readonly GUIContent ReusableContent = new();

        /// <summary>
        /// Gets the total property height including the help box when the field is null.
        /// </summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float baseHeight = EditorGUI.GetPropertyHeight(property, label, true);

            if (IsPropertyNull(property))
            {
                string message = GetMessage(property);
                float helpBoxHeight = GetHelpBoxHeight(message);
                return baseHeight + helpBoxHeight + HelpBoxPadding;
            }

            return baseHeight;
        }

        /// <summary>
        /// Draws the property field and displays a help box warning/error when the field is null.
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            try
            {
                bool isNull = IsPropertyNull(property);
                float propertyHeight = EditorGUI.GetPropertyHeight(property, label, true);

                if (isNull)
                {
                    string message = GetMessage(property);
                    float helpBoxHeight = GetHelpBoxHeight(message);

                    Rect helpBoxRect = new(position.x, position.y, position.width, helpBoxHeight);
                    Rect propertyRect = new(
                        position.x,
                        position.y + helpBoxHeight + HelpBoxPadding,
                        position.width,
                        propertyHeight
                    );

                    MessageType messageType = GetMessageType();
                    EditorGUI.HelpBox(helpBoxRect, message, messageType);
                    EditorGUI.PropertyField(propertyRect, property, label, true);
                }
                else
                {
                    EditorGUI.PropertyField(position, property, label, true);
                }
            }
            finally
            {
                EditorGUI.EndProperty();
            }
        }

        /// <summary>
        /// Creates a UI Toolkit visual element for the property, including help box when null.
        /// </summary>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement container = new();
            container.style.flexDirection = FlexDirection.Column;

            HelpBox helpBox = new(GetMessage(property), GetHelpBoxMessageType());
            helpBox.style.display = IsPropertyNull(property)
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            PropertyField propertyField = new(property);
            propertyField.label = property.displayName;

            propertyField.RegisterValueChangeCallback(evt =>
            {
                bool isNull = IsPropertyNull(property);
                helpBox.text = GetMessage(property);
                helpBox.style.display = isNull ? DisplayStyle.Flex : DisplayStyle.None;
            });

            container.Add(helpBox);
            container.Add(propertyField);

            return container;
        }

        /// <summary>
        /// Checks if the property value is null.
        /// </summary>
        internal static bool IsPropertyNull(SerializedProperty property)
        {
            if (property == null)
            {
                return true;
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue == null;
                case SerializedPropertyType.ExposedReference:
                    return property.exposedReferenceValue == null;
                case SerializedPropertyType.ManagedReference:
                    return property.managedReferenceValue == null;
                case SerializedPropertyType.String:
                    return string.IsNullOrEmpty(property.stringValue);
                default:
                    return false;
            }
        }

        private string GetMessage(SerializedProperty property)
        {
            WNotNullAttribute notNullAttribute = attribute as WNotNullAttribute;
            return GetMessage(property, notNullAttribute);
        }

        private MessageType GetMessageType()
        {
            WNotNullAttribute notNullAttribute = attribute as WNotNullAttribute;
            return GetMessageType(notNullAttribute);
        }

        private HelpBoxMessageType GetHelpBoxMessageType()
        {
            WNotNullAttribute notNullAttribute = attribute as WNotNullAttribute;
            return GetHelpBoxMessageType(notNullAttribute);
        }

        /// <summary>
        /// Gets the validation message for a property with the given attribute.
        /// </summary>
        internal static string GetMessage(
            SerializedProperty property,
            WNotNullAttribute notNullAttribute
        )
        {
            if (notNullAttribute != null && !string.IsNullOrEmpty(notNullAttribute.CustomMessage))
            {
                return notNullAttribute.CustomMessage;
            }

            string fieldName = property?.displayName ?? NullValueMessage;
            return string.Format(DefaultWarningMessageFormat, fieldName);
        }

        /// <summary>
        /// Gets the IMGUI MessageType for the given attribute.
        /// </summary>
        internal static MessageType GetMessageType(WNotNullAttribute notNullAttribute)
        {
            if (notNullAttribute == null)
            {
                return MessageType.Warning;
            }

            return notNullAttribute.MessageType switch
            {
                WNotNullMessageType.Error => MessageType.Error,
                _ => MessageType.Warning,
            };
        }

        /// <summary>
        /// Gets the UI Toolkit HelpBoxMessageType for the given attribute.
        /// </summary>
        internal static HelpBoxMessageType GetHelpBoxMessageType(WNotNullAttribute notNullAttribute)
        {
            if (notNullAttribute == null)
            {
                return HelpBoxMessageType.Warning;
            }

            return notNullAttribute.MessageType switch
            {
                WNotNullMessageType.Error => HelpBoxMessageType.Error,
                _ => HelpBoxMessageType.Warning,
            };
        }

        /// <summary>
        /// Draws a validation HelpBox for the property if it is null.
        /// Call this from custom editors for array/list properties that won't have
        /// their PropertyDrawer invoked at the array level.
        /// </summary>
        /// <returns>True if a HelpBox was drawn, false otherwise.</returns>
        internal static bool DrawValidationHelpBoxIfNeeded(
            SerializedProperty property,
            WNotNullAttribute notNullAttribute
        )
        {
            if (!IsPropertyNull(property))
            {
                return false;
            }

            string message = GetMessage(property, notNullAttribute);
            MessageType messageType = GetMessageType(notNullAttribute);
            EditorGUILayout.HelpBox(message, messageType);
            return true;
        }

        private static float GetHelpBoxHeight(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return EditorGUIUtility.singleLineHeight * 2f;
            }

            if (HelpBoxHeightCache.TryGetValue(message, out float cachedHeight))
            {
                return cachedHeight;
            }

            ReusableContent.text = message;
            GUIStyle helpBoxStyle = EditorStyles.helpBox;
            float minHeight = EditorGUIUtility.singleLineHeight * 2f;
            float viewWidth = 600f;
            try
            {
                viewWidth = Mathf.Max(0f, EditorGUIUtility.currentViewWidth);
            }
            catch
            {
                // Called outside OnGUI context; use fallback
                viewWidth = 600f;
            }
            float calculatedHeight = helpBoxStyle.CalcHeight(ReusableContent, viewWidth - 40f);
            float height = Mathf.Max(minHeight, calculatedHeight);

            HelpBoxHeightCache[message] = height;
            return height;
        }

        /// <summary>
        /// Clears the height cache. Useful for tests or when font settings change.
        /// </summary>
        internal static void ClearHeightCache()
        {
            HelpBoxHeightCache.Clear();
        }
    }
#endif
}
