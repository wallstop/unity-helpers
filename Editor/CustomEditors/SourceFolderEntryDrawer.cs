namespace WallstopStudios.UnityHelpers.Editor.CustomEditors
{
#if UNITY_EDITOR
    using System;
    using Sprites;
    using UnityEngine;
    using UnityEditor;
    using System.Collections.Generic;
    using System.IO;
    using Core.Helper;
    using System.Linq;

    [CustomPropertyDrawer(typeof(SourceFolderEntry))]
    public sealed class SourceFolderEntryDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, bool> RegexesFoldoutState = new(
            StringComparer.Ordinal
        );

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
                int originalIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel++;

                float currentY = foldoutRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                float availableWidth = position.width - EditorGUI.indentLevel * 15f;
                float startX = position.x + EditorGUI.indentLevel * 15f;

                SerializedProperty folderPathProp = property.FindPropertyRelative(
                    nameof(SourceFolderEntry.folderPath)
                );
                SerializedProperty regexesProp = property.FindPropertyRelative(
                    nameof(SourceFolderEntry.regexes)
                );

                Rect folderPathLabelRect = new(
                    startX,
                    currentY,
                    availableWidth,
                    EditorGUIUtility.singleLineHeight
                );
                EditorGUI.LabelField(folderPathLabelRect, "Folder Path", EditorStyles.boldLabel);
                currentY += folderPathLabelRect.height + EditorGUIUtility.standardVerticalSpacing;

                Rect pathFieldRect = new(
                    startX,
                    currentY,
                    availableWidth - 75,
                    EditorGUIUtility.singleLineHeight
                );
                Rect browseButtonRect = new(
                    pathFieldRect.xMax + 5,
                    currentY,
                    70,
                    EditorGUIUtility.singleLineHeight
                );

                EditorGUI.BeginChangeCheck();
                string newPath = EditorGUI.TextField(
                    pathFieldRect,
                    new GUIContent("Path:"),
                    folderPathProp.stringValue
                );
                if (EditorGUI.EndChangeCheck())
                {
                    folderPathProp.stringValue = newPath;
                }

                if (GUI.Button(browseButtonRect, "Browse..."))
                {
                    string initialBrowsePath = Application.dataPath;
                    if (
                        !string.IsNullOrWhiteSpace(folderPathProp.stringValue)
                        && Directory.Exists(folderPathProp.stringValue)
                    )
                    {
                        initialBrowsePath = folderPathProp.stringValue;
                    }

                    string selectedPathSys = EditorUtility.OpenFolderPanel(
                        "Select Source Folder",
                        initialBrowsePath,
                        ""
                    );
                    if (!string.IsNullOrWhiteSpace(selectedPathSys))
                    {
                        string processedPath = selectedPathSys.SanitizePath();
                        if (
                            processedPath.StartsWith(
                                Application.dataPath.SanitizePath(),
                                StringComparison.Ordinal
                            )
                        )
                        {
                            processedPath =
                                "Assets"
                                + processedPath.Substring(
                                    Application.dataPath.SanitizePath().Length
                                );
                        }
                        folderPathProp.stringValue = processedPath;

                        string toolName = "SpriteAtlasTool_Drawer";
                        string contextKey =
                            $"{property.serializedObject.targetObject.GetType().Name}_{folderPathProp.propertyPath}";
                        PersistentDirectorySettings.Instance.RecordPath(
                            toolName,
                            contextKey,
                            processedPath
                        );
                        property.serializedObject.ApplyModifiedProperties();
                        GUI.FocusControl(null);
                    }
                }
                currentY += pathFieldRect.height + EditorGUIUtility.standardVerticalSpacing;

                string historyContextKey =
                    $"{property.serializedObject.targetObject.GetType().Name}_{folderPathProp.propertyPath}";
                DirectoryUsageData[] historyPaths = PersistentDirectorySettings.Instance.GetPaths(
                    nameof(ScriptableSpriteAtlasEditor),
                    historyContextKey,
                    true,
                    3
                );
                if (historyPaths.Any())
                {
                    Rect historyLabelRect = new(
                        startX,
                        currentY,
                        availableWidth,
                        EditorGUIUtility.singleLineHeight
                    );
                    EditorGUI.LabelField(historyLabelRect, "History:", EditorStyles.miniBoldLabel);
                    currentY += historyLabelRect.height;

                    foreach (DirectoryUsageData dirData in historyPaths)
                    {
                        Rect historyButtonRect = new(
                            startX + 15f,
                            currentY,
                            availableWidth - 15f,
                            EditorGUIUtility.singleLineHeight
                        );
                        if (
                            GUI.Button(
                                historyButtonRect,
                                new GUIContent($"({dirData.count}) {dirData.path}", dirData.path),
                                EditorStyles.miniButtonLeft
                            )
                        )
                        {
                            folderPathProp.stringValue = dirData.path;
                            PersistentDirectorySettings.Instance.RecordPath(
                                nameof(ScriptableSpriteAtlasEditor),
                                historyContextKey,
                                dirData.path
                            );
                            property.serializedObject.ApplyModifiedProperties();
                            GUI.FocusControl(null);
                        }
                        currentY += historyButtonRect.height;
                    }
                    currentY += EditorGUIUtility.standardVerticalSpacing;
                }

                Rect regexFoldoutLabelRect = new(
                    startX,
                    currentY,
                    availableWidth,
                    EditorGUIUtility.singleLineHeight
                );
                string regexesFoldoutKey =
                    property.serializedObject.targetObject.name
                    + property.propertyPath
                    + ".regexesList";
                RegexesFoldoutState.TryAdd(regexesFoldoutKey, true);
                RegexesFoldoutState[regexesFoldoutKey] = EditorGUI.Foldout(
                    regexFoldoutLabelRect,
                    RegexesFoldoutState[regexesFoldoutKey],
                    "Regexes (AND logic)",
                    true
                );
                currentY += regexFoldoutLabelRect.height + EditorGUIUtility.standardVerticalSpacing;
                if (RegexesFoldoutState[regexesFoldoutKey])
                {
                    int listElementIndent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel++;
                    Rect sizeFieldRect = new(
                        startX,
                        currentY,
                        availableWidth,
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
                            startX,
                            currentY,
                            availableWidth,
                            EditorGUIUtility.singleLineHeight
                        );
                        EditorGUI.BeginChangeCheck();
                        string newStringValue = EditorGUI.TextField(
                            elementRect,
                            $"Element {i}",
                            elementProp.stringValue
                        );
                        if (EditorGUI.EndChangeCheck())
                        {
                            elementProp.stringValue = newStringValue;
                        }
                        currentY += elementRect.height + EditorGUIUtility.standardVerticalSpacing;
                    }
                    EditorGUI.indentLevel = listElementIndent;
                }

                EditorGUI.indentLevel = originalIndent;
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
            {
                return height;
            }

            height += EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty folderPathProp = property.FindPropertyRelative(
                nameof(SourceFolderEntry.folderPath)
            );
            string historyToolName = "SpriteAtlasTool_Drawer";
            string historyContextKey =
                $"{property.serializedObject.targetObject.GetType().Name}_{folderPathProp.propertyPath}";
            DirectoryUsageData[] historyPaths = PersistentDirectorySettings.Instance.GetPaths(
                historyToolName,
                historyContextKey,
                true,
                3
            );
            if (historyPaths.Any())
            {
                height += EditorGUIUtility.singleLineHeight;
                height += historyPaths.Length * EditorGUIUtility.singleLineHeight;
                height += EditorGUIUtility.standardVerticalSpacing;
            }

            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            string regexesFoldoutKey = property.propertyPath + ".regexesList";
            bool isRegexesExpanded = RegexesFoldoutState.GetValueOrDefault(regexesFoldoutKey, true);

            if (isRegexesExpanded)
            {
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
            height += EditorGUIUtility.standardVerticalSpacing;
            return height;
        }
    }
#endif
}
