namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [CustomPropertyDrawer(typeof(IntDropdownAttribute))]
    public sealed class IntDropdownDrawer : PropertyDrawer
    {
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
            string[] displayedOptions = Array.ConvertAll(options, Convert.ToString);

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
