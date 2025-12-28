// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using System;
    using System.Collections.Generic;
    using Sirenix.OdinInspector.Editor;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers.Utils;
    using WallstopStudios.UnityHelpers.Editor.Settings;

    /// <summary>
    /// Odin Inspector attribute drawer for <see cref="IntDropDownAttribute"/>.
    /// Renders integer fields decorated with IntDropDown as dropdown selectors.
    /// </summary>
    /// <remarks>
    /// This drawer ensures IntDropDown works correctly when Odin Inspector is installed
    /// and classes derive from SerializedMonoBehaviour or SerializedScriptableObject,
    /// where Unity's standard PropertyDrawer system is bypassed.
    /// </remarks>
    public sealed class IntDropDownOdinDrawer : OdinAttributeDrawer<IntDropDownAttribute>
    {
        private static readonly Dictionary<int, string[]> DisplayOptionsCache = new();

        /// <summary>
        /// Draws the property as a dropdown selector with the integer options provided by the attribute.
        /// </summary>
        /// <param name="label">The label to display for the property.</param>
        protected override void DrawPropertyLayout(GUIContent label)
        {
            IntDropDownAttribute intDropDown = Attribute;
            if (intDropDown == null)
            {
                CallNextDrawer(label);
                return;
            }

            Type valueType = Property.ValueEntry?.TypeOfValue;
            if (valueType != typeof(int))
            {
                EditorGUILayout.HelpBox(
                    $"[IntDropDown] Type mismatch: field is {valueType?.Name ?? "unknown"}, but IntDropDown requires int.",
                    MessageType.Error
                );
                return;
            }

            object parentValue = Property.Parent?.ValueEntry?.WeakSmartValue;
            int[] options = intDropDown.GetOptions(parentValue) ?? Array.Empty<int>();

            if (options.Length == 0)
            {
                EditorGUILayout.HelpBox("No options available for IntDropDown.", MessageType.Info);
                return;
            }

            object currentValue = Property.ValueEntry?.WeakSmartValue;
            int currentInt = currentValue is int intValue ? intValue : 0;
            int currentIndex = Array.IndexOf(options, currentInt);

            int pageSize = Mathf.Max(1, UnityHelpersSettings.GetStringInListPageLimit());
            string[] displayOptions = GetOrCreateDisplayOptions(options);

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
                    currentInt
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
            int[] options,
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
            int[] options,
            string[] displayOptions,
            int currentIndex,
            int currentInt
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
                    : DropDownShared.GetCachedIntString(currentInt);

            if (GUI.Button(fieldRect, displayValue, EditorStyles.popup))
            {
                ShowPopupMenu(fieldRect, options, displayOptions, currentIndex);
            }
        }

        private void ShowPopupMenu(
            Rect buttonRect,
            int[] options,
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

        private void ApplySelection(int value)
        {
            Property.ValueEntry.WeakSmartValue = value;
        }

        internal static string[] GetOrCreateDisplayOptions(int[] options)
        {
            if (options == null || options.Length == 0)
            {
                return Array.Empty<string>();
            }

            int hashCode = DropDownShared.ComputeOptionsHash(options);
            if (DisplayOptionsCache.TryGetValue(hashCode, out string[] cached))
            {
                if (cached.Length == options.Length)
                {
                    bool match = true;
                    for (int i = 0; i < options.Length && match; i++)
                    {
                        if (
                            !string.Equals(
                                cached[i],
                                DropDownShared.GetCachedIntString(options[i]),
                                StringComparison.Ordinal
                            )
                        )
                        {
                            match = false;
                        }
                    }
                    if (match)
                    {
                        return cached;
                    }
                }
            }

            string[] displayOptions = new string[options.Length];
            for (int i = 0; i < options.Length; i++)
            {
                displayOptions[i] = DropDownShared.GetCachedIntString(options[i]);
            }
            DisplayOptionsCache[hashCode] = displayOptions;
            return displayOptions;
        }
    }
#endif
}
