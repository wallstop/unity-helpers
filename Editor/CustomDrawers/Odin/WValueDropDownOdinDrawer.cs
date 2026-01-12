// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using System;
    using Sirenix.OdinInspector.Editor;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers.Utils;
    using WallstopStudios.UnityHelpers.Editor.Settings;

    /// <summary>
    /// Odin Inspector attribute drawer for <see cref="WValueDropDownAttribute"/>.
    /// Renders fields decorated with WValueDropDown as dropdown selectors.
    /// </summary>
    /// <remarks>
    /// This drawer ensures WValueDropDown works correctly when Odin Inspector is installed
    /// and classes derive from SerializedMonoBehaviour or SerializedScriptableObject,
    /// where Unity's standard PropertyDrawer system is bypassed.
    /// </remarks>
    public sealed class WValueDropDownOdinDrawer : OdinAttributeDrawer<WValueDropDownAttribute>
    {
        /// <summary>
        /// Draws the property as a dropdown selector with the options provided by the attribute.
        /// </summary>
        /// <param name="label">The label to display for the property.</param>
        protected override void DrawPropertyLayout(GUIContent label)
        {
            WValueDropDownAttribute dropdownAttribute = Attribute;
            if (dropdownAttribute == null)
            {
                CallNextDrawer(label);
                return;
            }

            object parentValue = Property.Parent?.ValueEntry?.WeakSmartValue;
            object[] options = dropdownAttribute.GetOptions(parentValue) ?? Array.Empty<object>();

            if (options.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "No options available for WValueDropDown.",
                    MessageType.Info
                );
                return;
            }

            object currentValue = Property.ValueEntry?.WeakSmartValue;
            int currentIndex = FindSelectedIndex(currentValue, options);

            int pageSize = Mathf.Max(1, UnityHelpersSettings.GetStringInListPageLimit());
            string[] displayOptions = GetDisplayOptions(options);

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
                    currentValue
                );
            }
            else
            {
                DrawNativeDropDown(controlRect, label, options, displayOptions, currentIndex);
            }
        }

        private void DrawNativeDropDown(
            Rect position,
            GUIContent label,
            object[] options,
            string[] displayOptions,
            int currentIndex
        )
        {
            int newIndex = EditorGUI.Popup(position, label.text, currentIndex, displayOptions);
            if (newIndex >= 0 && newIndex < options.Length && newIndex != currentIndex)
            {
                ApplySelection(options[newIndex]);
            }
        }

        private void DrawPopupDropDown(
            Rect position,
            GUIContent label,
            object[] options,
            string[] displayOptions,
            int currentIndex,
            object currentValue
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

            string displayValue =
                currentIndex >= 0 && currentIndex < displayOptions.Length
                    ? displayOptions[currentIndex]
                    : DropDownShared.FormatOption(currentValue);

            if (GUI.Button(fieldRect, displayValue, EditorStyles.popup))
            {
                ShowPopupMenu(fieldRect, options, displayOptions, currentIndex);
            }
        }

        private void ShowPopupMenu(
            Rect buttonRect,
            object[] options,
            string[] displayOptions,
            int currentIndex
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
                    () => ApplySelection(options[capturedIndex])
                );
            }
            menu.DropDown(buttonRect);
        }

        private void ApplySelection(object newValue)
        {
            Property.ValueEntry.WeakSmartValue = newValue;
        }

        internal static int FindSelectedIndex(object currentValue, object[] options)
        {
            if (currentValue == null)
            {
                return -1;
            }

            for (int i = 0; i < options.Length; i++)
            {
                if (DropDownShared.ValuesMatch(currentValue, options[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        internal static string[] GetDisplayOptions(object[] options)
        {
            string[] displayOptions = new string[options.Length];
            for (int i = 0; i < options.Length; i++)
            {
                displayOptions[i] = DropDownShared.FormatOption(options[i]);
            }
            return displayOptions;
        }
    }
#endif
}
