namespace WallstopStudios.UnityHelpers.Editor.CustomEditors
{
#if UNITY_EDITOR
    using System;
    using Sprites;
    using UnityEngine;
    using UnityEditor;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using Core.Helper;

    [CustomPropertyDrawer(typeof(SourceFolderEntry))]
    public sealed class SourceFolderEntryDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, bool> RegexesFoldoutState = new();

        private static int GetElementIndex(SerializedProperty property)
        {
            Match match = Regex.Match(property.propertyPath, @"\[(\d+)\]$");
            if (match.Success)
            {
                return int.Parse(match.Groups[1].Value);
            }
            return -1;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect foldoutRect = new(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight
            );
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                int originalIndentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel++;

                float currentY =
                    position.y
                    + EditorGUIUtility.singleLineHeight
                    + EditorGUIUtility.standardVerticalSpacing;

                SerializedProperty folderPathProp = property.FindPropertyRelative(
                    nameof(SourceFolderEntry.folderPath)
                );
                SerializedProperty regexesProp = property.FindPropertyRelative(
                    nameof(SourceFolderEntry.regexes)
                );

                Rect folderPathRect = new(
                    position.x,
                    currentY,
                    position.width,
                    EditorGUI.GetPropertyHeight(folderPathProp, true)
                );
                EditorGUI.PropertyField(
                    folderPathRect,
                    folderPathProp,
                    new GUIContent("Folder Path")
                );
                currentY += folderPathRect.height + EditorGUIUtility.standardVerticalSpacing;

                Rect buttonRect = new(
                    position.x + EditorGUI.indentLevel * 15f,
                    currentY,
                    position.width - EditorGUI.indentLevel * 15f,
                    EditorGUIUtility.singleLineHeight
                );
                if (GUI.Button(buttonRect, "Set/Change Folder Path for this Entry"))
                {
                    string initialPath = string.IsNullOrWhiteSpace(folderPathProp.stringValue)
                        ? Application.dataPath
                        : folderPathProp.stringValue;
                    if (!initialPath.StartsWith(Application.dataPath))
                    {
                        initialPath = Application.dataPath;
                    }

                    string selectedPath = EditorUtility.OpenFolderPanel(
                        "Select Source Folder",
                        Path.GetDirectoryName(initialPath),
                        Path.GetFileName(initialPath)
                    );

                    if (!string.IsNullOrWhiteSpace(selectedPath))
                    {
                        if (selectedPath.StartsWith(Application.dataPath))
                        {
                            string relativePath =
                                "Assets" + selectedPath.Substring(Application.dataPath.Length);
                            relativePath = relativePath.SanitizePath();

                            bool isPathUnique = true;
                            int currentIndex = GetElementIndex(property);

                            string parentPropertyPath = property.propertyPath.Substring(
                                0,
                                property.propertyPath.LastIndexOf(
                                    ".Array.data[",
                                    StringComparison.Ordinal
                                )
                            );
                            SerializedProperty parentListProp =
                                property.serializedObject.FindProperty(parentPropertyPath);

                            if (parentListProp is { isArray: true })
                            {
                                for (int i = 0; i < parentListProp.arraySize; i++)
                                {
                                    if (i == currentIndex)
                                    {
                                        continue;
                                    }

                                    SerializedProperty otherEntryProp =
                                        parentListProp.GetArrayElementAtIndex(i);
                                    SerializedProperty otherFolderPathProp =
                                        otherEntryProp.FindPropertyRelative(
                                            nameof(SourceFolderEntry.folderPath)
                                        );
                                    if (
                                        string.Equals(
                                            otherFolderPathProp.stringValue,
                                            relativePath,
                                            StringComparison.Ordinal
                                        )
                                    )
                                    {
                                        isPathUnique = false;
                                        Debug.LogWarning(
                                            $"Path '{relativePath}' is already used by another entry (Element {i}) in this configuration. Please choose a unique path or edit the existing entry."
                                        );
                                        EditorUtility.DisplayDialog(
                                            "Path Not Unique",
                                            $"The path '{relativePath}' is already used by Element {i} in this configuration. \n\nPlease choose a different path or manage the existing entry.",
                                            "OK"
                                        );
                                        break;
                                    }
                                }
                            }

                            if (isPathUnique)
                            {
                                folderPathProp.stringValue = relativePath;
                                property.serializedObject.ApplyModifiedProperties();
                            }
                        }
                        else
                        {
                            EditorUtility.DisplayDialog(
                                "Invalid Folder",
                                "The selected folder must be within the project's 'Assets' directory.",
                                "OK"
                            );
                        }
                    }
                }
                currentY += buttonRect.height + EditorGUIUtility.standardVerticalSpacing;

                string regexesFoldoutKey = property.propertyPath + ".regexes";
                RegexesFoldoutState.TryAdd(regexesFoldoutKey, true);

                Rect regexesLabelRect = new(
                    position.x,
                    currentY,
                    position.width,
                    EditorGUIUtility.singleLineHeight
                );
                RegexesFoldoutState[regexesFoldoutKey] = EditorGUI.Foldout(
                    regexesLabelRect,
                    RegexesFoldoutState[regexesFoldoutKey],
                    "Regexes (AND logic)",
                    true
                );
                currentY += regexesLabelRect.height + EditorGUIUtility.standardVerticalSpacing;

                if (RegexesFoldoutState[regexesFoldoutKey])
                {
                    int listIndent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel++;

                    Rect sizeFieldRect = new(
                        position.x,
                        currentY,
                        position.width,
                        EditorGUIUtility.singleLineHeight
                    );
                    EditorGUI.BeginChangeCheck();
                    int newSize = EditorGUI.IntField(sizeFieldRect, "Size", regexesProp.arraySize);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (newSize < 0)
                        {
                            newSize = 0;
                        }

                        regexesProp.arraySize = newSize;
                    }
                    currentY += sizeFieldRect.height + EditorGUIUtility.standardVerticalSpacing;

                    for (int i = 0; i < regexesProp.arraySize; i++)
                    {
                        SerializedProperty elementProp = regexesProp.GetArrayElementAtIndex(i);
                        Rect elementRect = new(
                            position.x,
                            currentY,
                            position.width,
                            EditorGUIUtility.singleLineHeight
                        );

                        EditorGUI.BeginChangeCheck();
                        string newValue = EditorGUI.TextField(
                            elementRect,
                            $"Element {i}",
                            elementProp.stringValue
                        );
                        if (EditorGUI.EndChangeCheck())
                        {
                            elementProp.stringValue = newValue;
                        }
                        currentY += elementRect.height + EditorGUIUtility.standardVerticalSpacing;
                    }
                    EditorGUI.indentLevel = listIndent;
                }
                EditorGUI.indentLevel = originalIndentLevel;
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (property.isExpanded)
            {
                height += EditorGUIUtility.standardVerticalSpacing;

                SerializedProperty folderPathProp = property.FindPropertyRelative(
                    nameof(SourceFolderEntry.folderPath)
                );
                height += EditorGUI.GetPropertyHeight(folderPathProp, true);
                height += EditorGUIUtility.standardVerticalSpacing;
                height += EditorGUIUtility.singleLineHeight;
                height += EditorGUIUtility.standardVerticalSpacing;
                height += EditorGUIUtility.singleLineHeight;
                string regexesFoldoutKey = property.propertyPath + ".regexes";
                bool isRegexesExpanded = RegexesFoldoutState.GetValueOrDefault(
                    regexesFoldoutKey,
                    true
                );

                if (!isRegexesExpanded)
                {
                    return height;
                }

                height += EditorGUIUtility.standardVerticalSpacing;
                SerializedProperty regexesProp = property.FindPropertyRelative(
                    nameof(SourceFolderEntry.regexes)
                );
                height +=
                    EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                for (int i = 0; i < regexesProp.arraySize; i++)
                {
                    height +=
                        EditorGUIUtility.singleLineHeight
                        + EditorGUIUtility.standardVerticalSpacing;
                }
            }
            return height;
        }
    }
#endif
}
