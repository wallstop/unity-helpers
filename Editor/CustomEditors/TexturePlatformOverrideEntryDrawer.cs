// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Editor.CustomEditors
{
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Sprites;
    using PlatformPropertyNames = WallstopStudios.UnityHelpers.Editor.Sprites.TextureSettingsApplierWindow.PlatformOverrideEntry.SerializedPropertyNames;

    [CustomPropertyDrawer(typeof(TextureSettingsApplierWindow.PlatformOverrideEntry))]
    public sealed class TexturePlatformOverrideEntryDrawer : PropertyDrawer
    {
        private static string[] _cachedChoices;
        private static string[] _lastKnownRef;
        private const string CustomOptionLabel = "Custom";

        private static string[] GetChoices()
        {
            string[] known = TexturePlatformNameHelper.GetKnownPlatformNames();
            if (ReferenceEquals(known, _lastKnownRef) && _cachedChoices != null)
            {
                return _cachedChoices;
            }

            string[] arr = new string[known.Length + 1];
            for (int i = 0; i < known.Length; i++)
            {
                arr[i] = known[i];
            }
            arr[arr.Length - 1] = CustomOptionLabel;
            _lastKnownRef = known;
            _cachedChoices = arr;
            return _cachedChoices;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Layout: 1 line for platform + potential custom name, then each checkbox possibly adds a line
            float h = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            // custom name line
            string name = property
                .FindPropertyRelative(PlatformPropertyNames.PlatformName)
                .stringValue;
            string[] choices = GetChoices();
            if (GetSelectedIndex(name, choices) == choices.Length - 1) // Custom
            {
                h += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
            // For each apply field, add one line; if true, add an extra line for its value
            h += LineCount(property, PlatformPropertyNames.ApplyResizeAlgorithm, true);
            h += LineCount(property, PlatformPropertyNames.ApplyMaxTextureSize, true);
            h += LineCount(property, PlatformPropertyNames.ApplyFormat, true);
            h += LineCount(property, PlatformPropertyNames.ApplyCompression, true);
            h += LineCount(property, PlatformPropertyNames.ApplyCrunchCompression, true);
            return h;
        }

        private static float LineCount(
            SerializedProperty property,
            string applyName,
            bool includeValue
        )
        {
            float h = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            SerializedProperty apply = property.FindPropertyRelative(applyName);
            if (apply.boolValue && includeValue)
            {
                h += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect r = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            SerializedProperty nameProp = property.FindPropertyRelative(
                PlatformPropertyNames.PlatformName
            );
            string[] choices = GetChoices();
            int idx = GetSelectedIndex(nameProp.stringValue, choices);
            idx = EditorGUI.Popup(r, "Platform", idx, choices);
            string selected = choices[idx];
            if (selected == CustomOptionLabel)
            {
                r.y += r.height + EditorGUIUtility.standardVerticalSpacing;
                nameProp.stringValue = EditorGUI.TextField(r, "Custom Name", nameProp.stringValue);
            }
            else
            {
                nameProp.stringValue = selected;
            }

            DrawToggleWithValue(
                property,
                ref r,
                PlatformPropertyNames.ApplyResizeAlgorithm,
                PlatformPropertyNames.ResizeAlgorithm,
                "Resize Algorithm"
            );
            DrawToggleWithValue(
                property,
                ref r,
                PlatformPropertyNames.ApplyMaxTextureSize,
                PlatformPropertyNames.MaxTextureSize,
                "Max Texture Size"
            );
            DrawToggleWithValue(
                property,
                ref r,
                PlatformPropertyNames.ApplyFormat,
                PlatformPropertyNames.Format,
                "Format"
            );
            DrawToggleWithValue(
                property,
                ref r,
                PlatformPropertyNames.ApplyCompression,
                PlatformPropertyNames.Compression,
                "Compression"
            );
            DrawToggleWithValue(
                property,
                ref r,
                PlatformPropertyNames.ApplyCrunchCompression,
                PlatformPropertyNames.UseCrunchCompression,
                "Use Crunch Compression"
            );

            EditorGUI.EndProperty();
        }

        private static void DrawToggleWithValue(
            SerializedProperty property,
            ref Rect r,
            string applyName,
            string valueName,
            string label
        )
        {
            SerializedProperty apply = property.FindPropertyRelative(applyName);
            SerializedProperty val = property.FindPropertyRelative(valueName);

            r.y += r.height + EditorGUIUtility.standardVerticalSpacing;
            apply.boolValue = EditorGUI.ToggleLeft(r, label, apply.boolValue);
            if (apply.boolValue)
            {
                r.y += r.height + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.indentLevel++;
                EditorGUI.PropertyField(r, val, GUIContent.none);
                EditorGUI.indentLevel--;
            }
        }

        private static int GetSelectedIndex(string name, string[] choices)
        {
            if (string.IsNullOrEmpty(name))
            {
                return 0; // Default by convention
            }

            for (int i = 0; i < choices.Length - 1; i++)
            {
                if (choices[i] == name)
                {
                    return i;
                }
            }
            return choices.Length - 1; // Custom
        }
    }
#endif
}
