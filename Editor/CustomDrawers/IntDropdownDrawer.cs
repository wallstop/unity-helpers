namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [CustomPropertyDrawer(typeof(IntDropdownAttribute))]
    public sealed class IntDropdownDrawer : PropertyDrawer
    {
        private static readonly Dictionary<int, string> IntToStringCache = new();
        private static readonly Dictionary<int, string[]> DisplayOptionsCache = new();

        private static string GetCachedIntString(int value)
        {
            if (!IntToStringCache.TryGetValue(value, out string cached))
            {
                cached = value.ToString();
                IntToStringCache[value] = cached;
            }
            return cached;
        }

        private static string[] GetOrCreateDisplayOptions(int[] options)
        {
            if (options == null || options.Length == 0)
            {
                return Array.Empty<string>();
            }

            int hashCode = ComputeOptionsHash(options);
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
                                GetCachedIntString(options[i]),
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
                displayOptions[i] = GetCachedIntString(options[i]);
            }
            DisplayOptionsCache[hashCode] = displayOptions;
            return displayOptions;
        }

        private static int ComputeOptionsHash(int[] options)
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < options.Length; i++)
                {
                    hash = hash * 31 + options[i];
                }
                return hash;
            }
        }

        /// <summary>
        /// Renders a dropdown that allows selecting one of the configured integer options.
        /// </summary>
        /// <param name="position">The rectangle reserved for drawing the control.</param>
        /// <param name="property">The backing serialized property.</param>
        /// <param name="label">The label displayed next to the field.</param>
        /// <example>
        /// <code>
        /// [IntDropdown(1, 2, 3)]
        /// public int qualityLevel;
        /// </code>
        /// </example>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (attribute is not IntDropdownAttribute dropdown)
            {
                return;
            }

            UnityEngine.Object context = property.serializedObject?.targetObject;
            int[] options = dropdown.GetOptions(context) ?? Array.Empty<int>();
            if (options.Length == 0)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            int currentValue = property.intValue;
            int selectedIndex = Mathf.Max(0, Array.IndexOf(options, currentValue));
            string[] displayedOptions = GetOrCreateDisplayOptions(options);

            EditorGUI.BeginProperty(position, label, property);
            try
            {
                selectedIndex = EditorGUI.Popup(
                    position,
                    label.text,
                    selectedIndex,
                    displayedOptions
                );

                if (selectedIndex >= 0 && selectedIndex < options.Length)
                {
                    property.intValue = options[selectedIndex];
                }
            }
            finally
            {
                EditorGUI.EndProperty();
            }
        }
    }
#endif
}
