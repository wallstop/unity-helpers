namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
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

            int currentValue = property.intValue;

            int selectedIndex = Mathf.Max(0, System.Array.IndexOf(dropdown.Options, currentValue));
            string[] displayedOptions = System.Array.ConvertAll(
                dropdown.Options,
                opt => opt.ToString()
            );

            EditorGUI.BeginProperty(position, label, property);
            try
            {
                selectedIndex = EditorGUI.Popup(
                    position,
                    label.text,
                    selectedIndex,
                    displayedOptions
                );

                if (selectedIndex >= 0 && selectedIndex < dropdown.Options.Length)
                {
                    property.intValue = dropdown.Options[selectedIndex];
                }
            }
            finally
            {
                EditorGUI.EndProperty();
            }
        }
    }
}
