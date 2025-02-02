namespace UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System;
    using UnityEditor;
    using UnityEngine;
    using UnityHelpers.Core.Helper;

    [CustomPropertyDrawer(typeof(StringInList))]
    public class StringInListDrawer : PropertyDrawer
    {
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (attribute is not StringInList stringInList)
            {
                return;
            }

            string[] list = stringInList.List;

            switch (property.propertyType)
            {
                case SerializedPropertyType.String:
                {
                    int index = Mathf.Max(0, Array.IndexOf(list, property.stringValue));
                    index = EditorGUI.Popup(position, property.displayName, index, list);
                    if (index < 0 || list.Length <= index)
                    {
                        base.OnGUI(position, property, label);
                        return;
                    }

                    property.stringValue = list[index];
                    break;
                }
                case SerializedPropertyType.Integer:
                {
                    property.intValue = EditorGUI.Popup(
                        position,
                        property.displayName,
                        property.intValue,
                        list
                    );
                    break;
                }
                default:
                {
                    base.OnGUI(position, property, label);
                    break;
                }
            }
        }
    }
#endif
}
