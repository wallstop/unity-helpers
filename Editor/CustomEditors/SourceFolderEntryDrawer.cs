namespace WallstopStudios.UnityHelpers.Editor.CustomEditors
{
#if UNITY_EDITOR
    using System;
    using Sprites;
    using UnityEngine;
    using UnityEditor;
    using System.Collections.Generic;
    using Core.Helper;

    [CustomPropertyDrawer(typeof(SourceFolderEntry))]
    public sealed class SourceFolderEntryDrawer : PropertyDrawer
    {
        private const string HistoryToolName = nameof(SourceFolderEntryDrawer);
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
                float indentOffset = EditorGUI.indentLevel * 15f;
                float startX = position.x + indentOffset;
                float availableWidth = position.width - indentOffset;

                SerializedProperty folderPathProp = property.FindPropertyRelative(
                    nameof(SourceFolderEntry.folderPath)
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
                    string initialBrowsePath = Application.dataPath; /* ... */
                    string selectedPathSys = EditorUtility.OpenFolderPanel(
                        "Select Source Folder",
                        initialBrowsePath,
                        ""
                    );
                    if (!string.IsNullOrEmpty(selectedPathSys))
                    {
                        string processedPath = selectedPathSys.SanitizePath();
                        if (processedPath.StartsWith(Application.dataPath.SanitizePath()))
                        {
                            processedPath =
                                "Assets"
                                + processedPath.Substring(
                                    Application.dataPath.SanitizePath().Length
                                );
                        }
                        folderPathProp.stringValue = processedPath;
                        string contextKey = GetFolderPathFoldoutKey(folderPathProp);
                        PersistentDirectorySettings.Instance.RecordPath(
                            HistoryToolName,
                            contextKey,
                            processedPath
                        );
                        property.serializedObject.ApplyModifiedProperties();
                        GUI.FocusControl(null);
                    }
                }
                currentY += pathFieldRect.height + EditorGUIUtility.standardVerticalSpacing;
                string historyContextKey = GetFolderPathFoldoutKey(folderPathProp);

                Rect historyParentRect = new(
                    startX,
                    currentY,
                    availableWidth,
                    position.yMax - currentY
                );

                PersistentDirectoryGUI.DrawFrequentPathsWithEditorGUI(
                    historyParentRect,
                    ref currentY,
                    HistoryToolName,
                    historyContextKey,
                    chosenPath =>
                    {
                        folderPathProp.stringValue = chosenPath;
                        PersistentDirectorySettings.Instance.RecordPath(
                            HistoryToolName,
                            historyContextKey,
                            chosenPath
                        );
                        property.serializedObject.ApplyModifiedProperties();
                        GUI.FocusControl(null);
                    }
                );

                currentY += EditorGUIUtility.standardVerticalSpacing;
                Rect regexFoldoutLabelRect = new(
                    startX,
                    currentY,
                    availableWidth,
                    EditorGUIUtility.singleLineHeight
                );

                string regexesFoldoutKey = GetRegexFoldoutKey(property);
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
                    SerializedProperty regexesProp = property.FindPropertyRelative(
                        nameof(SourceFolderEntry.regexes)
                    );
                    float regexStartX = startX + 15f;
                    float regexWidth = availableWidth - 15f;

                    for (int i = 0; i < regexesProp.arraySize; i++)
                    {
                        SerializedProperty elemProp = regexesProp.GetArrayElementAtIndex(i);
                        Rect fieldRect = new(
                            regexStartX,
                            currentY,
                            regexWidth - 25f,
                            EditorGUIUtility.singleLineHeight
                        );
                        EditorGUI.BeginChangeCheck();
                        string newVal = EditorGUI.TextField(
                            fieldRect,
                            $"Regex {i}:",
                            elemProp.stringValue
                        );
                        if (EditorGUI.EndChangeCheck())
                        {
                            elemProp.stringValue = newVal;
                        }

                        Rect remRect = new(
                            fieldRect.xMax + 4f,
                            currentY,
                            25f,
                            EditorGUIUtility.singleLineHeight
                        );
                        if (GUI.Button(remRect, "–"))
                        {
                            regexesProp.DeleteArrayElementAtIndex(i);
                            property.serializedObject.ApplyModifiedProperties();
                        }

                        currentY +=
                            EditorGUIUtility.singleLineHeight
                            + EditorGUIUtility.standardVerticalSpacing;
                    }

                    Rect addRect = new(
                        regexStartX,
                        currentY,
                        regexWidth,
                        EditorGUIUtility.singleLineHeight
                    );

                    if (GUI.Button(addRect, "+ Add Regex"))
                    {
                        int idx = regexesProp.arraySize;
                        regexesProp.InsertArrayElementAtIndex(idx);
                        regexesProp.GetArrayElementAtIndex(idx).stringValue = string.Empty;
                        property.serializedObject.ApplyModifiedProperties();
                    }
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

            string historyContextKey = GetFolderPathFoldoutKey(folderPathProp);

            height += PersistentDirectoryGUI.GetDrawFrequentPathsHeightEditorGUI(
                HistoryToolName,
                historyContextKey
            );

            height += EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            string regexesFoldoutKey = GetRegexFoldoutKey(property);
            bool isRegexesExpanded = RegexesFoldoutState.GetValueOrDefault(regexesFoldoutKey, true);
            if (isRegexesExpanded)
            {
                SerializedProperty regexesProp = property.FindPropertyRelative(
                    nameof(SourceFolderEntry.regexes)
                );
                height +=
                    (1 + regexesProp.arraySize)
                    * (
                        EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing
                    );
            }
            height += EditorGUIUtility.standardVerticalSpacing;
            return height;
        }

        private static string GetHistoryContextKey(SerializedProperty property)
        {
            return (
                    property.serializedObject.targetObject != null
                        ? property.serializedObject.targetObject.name
                        : "NULL"
                ) + ".DefaultHistoryContext";
        }

        private static string GetFolderPathFoldoutKey(SerializedProperty property)
        {
            return (
                    property.serializedObject.targetObject != null
                        ? property.serializedObject.targetObject.name
                        : "NULL"
                ) + ".folderList";
        }

        private static string GetRegexFoldoutKey(SerializedProperty property)
        {
            return (
                    property.serializedObject.targetObject != null
                        ? property.serializedObject.targetObject.name
                        : "NULL"
                )
                + property.propertyPath
                + ".regexesList";
        }
    }
#endif
}
