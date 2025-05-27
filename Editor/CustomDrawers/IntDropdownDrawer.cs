namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
    using Core.Attributes;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(IntDropdownAttribute))]
    public sealed class IntDropdownDrawer : PropertyDrawer
    {
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
