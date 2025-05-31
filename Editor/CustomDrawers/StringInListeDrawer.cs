namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using UnityEditor;
    using UnityEngine;
    using Core.Helper;

    [CustomPropertyDrawer(typeof(StringInList))]
    public sealed class StringInListDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isArray && property.propertyType == SerializedPropertyType.Generic)
            {
                int arraySize = property.arraySize;

                float singleLine = EditorGUIUtility.singleLineHeight;
                float spacing = EditorGUIUtility.standardVerticalSpacing;
                return singleLine + arraySize * (singleLine + spacing);
            }

            return base.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (attribute is not StringInList stringInList)
            {
                return;
            }

            string[] list = stringInList.List;

            if (property.propertyType == SerializedPropertyType.String)
            {
                int index = Mathf.Max(0, Array.IndexOf(list, property.stringValue));
                index = EditorGUI.Popup(position, property.displayName, index, list);
                if (index < 0 || list.Length <= index)
                {
                    base.OnGUI(position, property, label);
                    return;
                }

                property.stringValue = list[index];
            }
            else if (property.propertyType == SerializedPropertyType.Integer)
            {
                property.intValue = EditorGUI.Popup(
                    position,
                    property.displayName,
                    property.intValue,
                    list
                );
            }
            else if (property.isArray && property.propertyType == SerializedPropertyType.Generic)
            {
                EditorGUI.BeginProperty(position, label, property);

                int originalIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel++;
                try
                {
                    Rect sizeRect = new(
                        position.x,
                        position.y,
                        position.width,
                        EditorGUIUtility.singleLineHeight
                    );
                    int newSize = EditorGUI.IntField(
                        sizeRect,
                        property.displayName + " Size",
                        property.arraySize
                    );
                    if (newSize < 0)
                    {
                        newSize = 0;
                    }

                    if (newSize != property.arraySize)
                    {
                        property.arraySize = newSize;
                    }

                    for (int i = 0; i < property.arraySize; i++)
                    {
                        SerializedProperty elemProp = property.GetArrayElementAtIndex(i);
                        Rect elementRect = new(
                            position.x,
                            position.y
                                + (
                                    EditorGUIUtility.singleLineHeight
                                    + EditorGUIUtility.standardVerticalSpacing
                                ) * (i + 1),
                            position.width,
                            EditorGUIUtility.singleLineHeight
                        );

                        if (elemProp.propertyType == SerializedPropertyType.String)
                        {
                            int currentIndex = Mathf.Max(
                                0,
                                Array.IndexOf(list, elemProp.stringValue)
                            );
                            currentIndex = EditorGUI.Popup(
                                elementRect,
                                $"Element {i}",
                                currentIndex,
                                list
                            );
                            if (currentIndex >= 0 && currentIndex < list.Length)
                            {
                                elemProp.stringValue = list[currentIndex];
                            }
                        }
                        else
                        {
                            EditorGUI.PropertyField(elementRect, elemProp);
                        }
                    }
                }
                finally
                {
                    EditorGUI.indentLevel = originalIndent;
                    EditorGUI.EndProperty();
                }
            }
            else
            {
                base.OnGUI(position, property, label);
            }
        }
    }
#endif
}
