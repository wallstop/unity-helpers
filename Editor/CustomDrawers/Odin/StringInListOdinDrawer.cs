namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using System;
    using Sirenix.OdinInspector.Editor;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers.Utils;
    using WallstopStudios.UnityHelpers.Editor.Settings;

    /// <summary>
    /// Odin Inspector attribute drawer for <see cref="StringInListAttribute"/>.
    /// Renders string fields decorated with StringInList as dropdown selectors.
    /// </summary>
    /// <remarks>
    /// This drawer ensures StringInList works correctly when Odin Inspector is installed
    /// and classes derive from SerializedMonoBehaviour or SerializedScriptableObject,
    /// where Unity's standard PropertyDrawer system is bypassed.
    /// </remarks>
    public sealed class StringInListOdinDrawer : OdinAttributeDrawer<StringInListAttribute>
    {
        /// <summary>
        /// Draws the property as a dropdown selector with the string options provided by the attribute.
        /// </summary>
        /// <param name="label">The label to display for the property.</param>
        protected override void DrawPropertyLayout(GUIContent label)
        {
            StringInListAttribute stringInList = Attribute;
            if (stringInList == null)
            {
                CallNextDrawer(label);
                return;
            }

            object parentValue = Property.Parent?.ValueEntry?.WeakSmartValue;
            string[] options = stringInList.GetOptions(parentValue) ?? Array.Empty<string>();

            if (options.Length == 0)
            {
                EditorGUILayout.HelpBox("No options available for StringInList.", MessageType.Info);
                return;
            }

            Type valueType = Property.ValueEntry?.TypeOfValue;
            if (valueType == null)
            {
                CallNextDrawer(label);
                return;
            }

            // Handle string field
            if (valueType == typeof(string))
            {
                DrawStringDropDown(label, options);
                return;
            }

            // Handle int field (index-based)
            if (valueType == typeof(int))
            {
                DrawIntIndexDropDown(label, options);
                return;
            }

            // Handle SerializableType
            if (valueType == typeof(SerializableType))
            {
                DrawSerializableTypeDropDown(label, options, stringInList);
                return;
            }

            // Unsupported type
            EditorGUILayout.HelpBox(
                $"[StringInList] Type mismatch: field is {valueType.Name}, but StringInList requires string, int, or SerializableType.",
                MessageType.Error
            );
        }

        private void DrawStringDropDown(GUIContent label, string[] options)
        {
            object currentValue = Property.ValueEntry?.WeakSmartValue;
            string currentString = currentValue as string ?? string.Empty;

            int currentIndex = Array.IndexOf(options, currentString);
            string[] displayOptions = GetDisplayOptions(options, Attribute);

            int pageSize = Mathf.Max(1, UnityHelpersSettings.GetStringInListPageLimit());
            Rect controlRect = EditorGUILayout.GetControlRect(
                true,
                EditorGUIUtility.singleLineHeight
            );

            if (options.Length > pageSize)
            {
                DrawPopupDropDown(
                    controlRect,
                    label,
                    options,
                    displayOptions,
                    currentIndex,
                    currentString,
                    ApplyStringSelection
                );
            }
            else
            {
                int newIndex = EditorGUI.Popup(
                    controlRect,
                    label.text,
                    currentIndex,
                    displayOptions
                );
                if (newIndex >= 0 && newIndex < options.Length && newIndex != currentIndex)
                {
                    ApplyStringSelection(options[newIndex]);
                }
            }
        }

        private void DrawIntIndexDropDown(GUIContent label, string[] options)
        {
            object currentValue = Property.ValueEntry?.WeakSmartValue;
            int currentIndex = currentValue is int intValue ? intValue : -1;

            // Clamp index to valid range
            if (currentIndex < 0 || currentIndex >= options.Length)
            {
                currentIndex = -1;
            }

            string[] displayOptions = GetDisplayOptions(options, Attribute);

            int pageSize = Mathf.Max(1, UnityHelpersSettings.GetStringInListPageLimit());
            Rect controlRect = EditorGUILayout.GetControlRect(
                true,
                EditorGUIUtility.singleLineHeight
            );

            string currentDisplay =
                currentIndex >= 0 && currentIndex < displayOptions.Length
                    ? displayOptions[currentIndex]
                    : DropDownShared.GetCachedIntString(currentValue is int idx ? idx : -1);

            if (options.Length > pageSize)
            {
                DrawPopupDropDown(
                    controlRect,
                    label,
                    options,
                    displayOptions,
                    currentIndex,
                    currentDisplay,
                    ApplyIntIndexSelection
                );
            }
            else
            {
                int newIndex = EditorGUI.Popup(
                    controlRect,
                    label.text,
                    currentIndex,
                    displayOptions
                );
                if (newIndex >= 0 && newIndex < options.Length && newIndex != currentIndex)
                {
                    ApplyIntIndexSelection(options[newIndex]);
                }
            }
        }

        private void DrawSerializableTypeDropDown(
            GUIContent label,
            string[] options,
            StringInListAttribute attribute
        )
        {
            object currentValue = Property.ValueEntry?.WeakSmartValue;
            string currentAssemblyQualifiedName = string.Empty;

            if (currentValue is SerializableType serializableType)
            {
                currentAssemblyQualifiedName =
                    serializableType.AssemblyQualifiedName ?? string.Empty;
            }

            int currentIndex = Array.IndexOf(options, currentAssemblyQualifiedName);
            string[] displayOptions = GetDisplayOptions(options, attribute);

            int pageSize = Mathf.Max(1, UnityHelpersSettings.GetStringInListPageLimit());
            Rect controlRect = EditorGUILayout.GetControlRect(
                true,
                EditorGUIUtility.singleLineHeight
            );

            string currentDisplay =
                currentIndex >= 0 && currentIndex < displayOptions.Length
                    ? displayOptions[currentIndex]
                    : "(None)";

            if (options.Length > pageSize)
            {
                DrawPopupDropDown(
                    controlRect,
                    label,
                    options,
                    displayOptions,
                    currentIndex,
                    currentDisplay,
                    ApplySerializableTypeSelection
                );
            }
            else
            {
                int newIndex = EditorGUI.Popup(
                    controlRect,
                    label.text,
                    currentIndex,
                    displayOptions
                );
                if (newIndex >= 0 && newIndex < options.Length && newIndex != currentIndex)
                {
                    ApplySerializableTypeSelection(options[newIndex]);
                }
            }
        }

        private void DrawPopupDropDown(
            Rect position,
            GUIContent label,
            string[] options,
            string[] displayOptions,
            int currentIndex,
            string currentDisplay,
            Action<string> applySelection
        )
        {
            Rect labelRect = new(
                position.x,
                position.y,
                EditorGUIUtility.labelWidth,
                position.height
            );

            Rect fieldRect = new(
                position.x + EditorGUIUtility.labelWidth + 2f,
                position.y,
                position.width - EditorGUIUtility.labelWidth - 2f,
                position.height
            );

            if (label != null && label != GUIContent.none)
            {
                EditorGUI.LabelField(labelRect, label);
            }

            if (GUI.Button(fieldRect, currentDisplay, EditorStyles.popup))
            {
                ShowPopupMenu(fieldRect, options, displayOptions, currentIndex, applySelection);
            }
        }

        private void ShowPopupMenu(
            Rect buttonRect,
            string[] options,
            string[] displayOptions,
            int currentIndex,
            Action<string> applySelection
        )
        {
            GenericMenu menu = new();
            for (int i = 0; i < options.Length; i++)
            {
                int capturedIndex = i;
                bool isSelected = i == currentIndex;
                menu.AddItem(
                    new GUIContent(displayOptions[i]),
                    isSelected,
                    () => applySelection(options[capturedIndex])
                );
            }
            menu.DropDown(buttonRect);
        }

        private void ApplyStringSelection(string value)
        {
            Property.ValueEntry.WeakSmartValue = value;
        }

        private void ApplyIntIndexSelection(string value)
        {
            object parentValue = Property.Parent?.ValueEntry?.WeakSmartValue;
            string[] options = Attribute.GetOptions(parentValue) ?? Array.Empty<string>();
            int index = Array.IndexOf(options, value);
            if (index >= 0)
            {
                Property.ValueEntry.WeakSmartValue = index;
            }
        }

        private void ApplySerializableTypeSelection(string assemblyQualifiedName)
        {
            Type resolvedType = null;
            if (!string.IsNullOrEmpty(assemblyQualifiedName))
            {
                resolvedType = Type.GetType(assemblyQualifiedName, throwOnError: false);
            }
            Property.ValueEntry.WeakSmartValue = new SerializableType(resolvedType);
        }

        private static string[] GetDisplayOptions(string[] options, StringInListAttribute attribute)
        {
            if (IsSerializableTypeProvider(attribute))
            {
                string[] names = SerializableTypeCatalog.GetDisplayNames();
                if (names != null && names.Length == options.Length)
                {
                    return names;
                }
            }

            return options;
        }

        private static bool IsSerializableTypeProvider(StringInListAttribute attribute)
        {
            return attribute?.ProviderType == typeof(SerializableTypeCatalog)
                && string.Equals(
                    attribute.ProviderMethodName,
                    nameof(SerializableTypeCatalog.GetAssemblyQualifiedNames),
                    StringComparison.Ordinal
                );
        }
    }
#endif
}
