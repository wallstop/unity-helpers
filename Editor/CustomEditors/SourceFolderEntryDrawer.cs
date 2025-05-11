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

            Rect foldoutRect = new Rect(
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
                // Calculate available width and startX based on current indent level for content
                // This means content will be indented under the "Element X" foldout.
                float indentOffset = EditorGUI.indentLevel * 15f; // Standard indent width per level
                float startX = position.x + indentOffset;
                float availableWidth = position.width - indentOffset;

                SerializedProperty folderPathProp = property.FindPropertyRelative("folderPath");
                SerializedProperty regexesProp = property.FindPropertyRelative("regexes");

                // --- Folder Path Section (manual EditorGUI) ---
                Rect folderPathLabelRect = new Rect(
                    startX,
                    currentY,
                    availableWidth,
                    EditorGUIUtility.singleLineHeight
                );
                EditorGUI.LabelField(folderPathLabelRect, "Folder Path", EditorStyles.boldLabel);
                currentY += folderPathLabelRect.height + EditorGUIUtility.standardVerticalSpacing;

                Rect pathFieldRect = new Rect(
                    startX,
                    currentY,
                    availableWidth - 75,
                    EditorGUIUtility.singleLineHeight
                );
                Rect browseButtonRect = new Rect(
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
                    // ... (Browse button logic remains the same, records path to history) ...
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

                // --- Draw Frequent Paths using the new EditorGUI version ---
                string historyToolName = "SpriteAtlasTool_Drawer";
                string historyContextKey =
                    $"{property.serializedObject.targetObject.GetType().Name}_{folderPathProp.propertyPath}";

                // Define the area for DrawFrequentPaths. It will draw starting at currentY.
                // It needs the full available width from this point.
                Rect historyParentRect = new Rect(
                    startX,
                    currentY,
                    availableWidth,
                    position.yMax - currentY
                ); // Give it remaining height

                // currentY will be updated by DrawFrequentPathsWithEditorGUI
                PersistentDirectoryGUI.DrawFrequentPathsWithEditorGUI(
                    historyParentRect, // This rect is relative to the PropertyDrawer's `position`
                    ref currentY, // Pass currentY by ref
                    historyToolName,
                    historyContextKey,
                    (chosenPath) =>
                    {
                        folderPathProp.stringValue = chosenPath;
                        PersistentDirectorySettings.Instance.RecordPath(
                            historyToolName,
                            historyContextKey,
                            chosenPath
                        );
                        property.serializedObject.ApplyModifiedProperties();
                        GUI.FocusControl(null);
                    }
                // Default topN and allowExpansion
                );
                // currentY is now updated to be below the history section
                // No need to explicitly add its height again here, as currentY tracks it.
                // --- End Folder Path Section ---


                // --- Regexes List Section (manual EditorGUI) ---
                // Adjust currentY if there wasn't any history, to add some space.
                // currentY already includes spacing if history was drawn by DrawFrequentPathsWithEditorGUI
                currentY += EditorGUIUtility.standardVerticalSpacing; // General space before regexes

                Rect regexFoldoutLabelRect = new Rect(
                    startX,
                    currentY,
                    availableWidth,
                    EditorGUIUtility.singleLineHeight
                );
                // ... (rest of regex list drawing using currentY, startX, availableWidth) ...
                string regexesFoldoutKey = property.propertyPath + ".regexesList";
                if (!RegexesFoldoutState.ContainsKey(regexesFoldoutKey))
                    RegexesFoldoutState[regexesFoldoutKey] = true;

                RegexesFoldoutState[regexesFoldoutKey] = EditorGUI.Foldout(
                    regexFoldoutLabelRect,
                    RegexesFoldoutState[regexesFoldoutKey],
                    "Regexes (AND logic)",
                    true
                );
                currentY += regexFoldoutLabelRect.height + EditorGUIUtility.standardVerticalSpacing;

                if (RegexesFoldoutState[regexesFoldoutKey])
                {
                    int listElementIndentLvl = EditorGUI.indentLevel; // Store current indent
                    EditorGUI.indentLevel++; // Indent elements under "Regexes" foldout

                    // Recalculate startX and availableWidth for this deeper indent level if needed,
                    // or assume controls inside will handle their own indent based on EditorGUI.indentLevel.
                    // For simple fields, current EditorGUI.indentLevel is usually enough.
                    float regexStartX = startX + 15f; // Example of further indent for elements
                    float regexAvailableWidth = availableWidth - 15f;

                    Rect sizeFieldRect = new Rect(
                        regexStartX,
                        currentY,
                        regexAvailableWidth,
                        EditorGUIUtility.singleLineHeight
                    );
                    // ... (Size field and loop for regex elements, using regexStartX, regexAvailableWidth, and updating currentY) ...
                    EditorGUI.BeginChangeCheck();
                    int newSize = EditorGUI.IntField(sizeFieldRect, "Size", regexesProp.arraySize);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (newSize < 0)
                            newSize = 0;
                        regexesProp.arraySize = newSize;
                    }
                    currentY += sizeFieldRect.height + EditorGUIUtility.standardVerticalSpacing;

                    for (int i = 0; i < regexesProp.arraySize; i++)
                    {
                        SerializedProperty elementProp = regexesProp.GetArrayElementAtIndex(i);
                        Rect elementRect = new Rect(
                            regexStartX,
                            currentY,
                            regexAvailableWidth,
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
                    EditorGUI.indentLevel = listElementIndentLvl; // Restore indent
                }

                EditorGUI.indentLevel = originalIndent;
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight; // "Element X" foldout

            if (property.isExpanded)
            {
                height += EditorGUIUtility.standardVerticalSpacing; // Space after "Element X"

                // Folder Path Section
                height +=
                    EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // "Folder Path" bold label
                height +=
                    EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Path TextField + Browse

                // History Paths using the new GetHeight method
                SerializedProperty folderPathProp = property.FindPropertyRelative("folderPath");
                string historyToolName = "SpriteAtlasTool_Drawer";
                // Ensure folderPathProp is valid before creating contextKey based on it
                string historyContextKey =
                    (folderPathProp != null && property.serializedObject.targetObject != null)
                        ? $"{property.serializedObject.targetObject.GetType().Name}_{folderPathProp.propertyPath}"
                        : "DefaultHistoryContext"; // Fallback context key

                height += PersistentDirectoryGUI.GetDrawFrequentPathsHeightEditorGUI(
                    historyToolName,
                    historyContextKey
                );
                // No extra spacing here, GetDrawFrequentPathsHeightEditorGUI includes its own final spacing if content drawn.

                // Regexes List Section
                height += EditorGUIUtility.standardVerticalSpacing; // General space before regexes
                height +=
                    EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // "Regexes" foldout label

                string regexesFoldoutKey = property.propertyPath + ".regexesList";
                bool isRegexesExpanded = RegexesFoldoutState.ContainsKey(regexesFoldoutKey)
                    ? RegexesFoldoutState[regexesFoldoutKey]
                    : true;

                if (isRegexesExpanded)
                {
                    SerializedProperty regexesProp = property.FindPropertyRelative("regexes");
                    height +=
                        EditorGUIUtility.singleLineHeight
                        + EditorGUIUtility.standardVerticalSpacing; // "Size" field
                    for (int i = 0; i < regexesProp.arraySize; i++)
                    {
                        height +=
                            EditorGUIUtility.singleLineHeight
                            + EditorGUIUtility.standardVerticalSpacing;
                    }
                }
                height += EditorGUIUtility.standardVerticalSpacing; // Bottom padding
            }
            return height;
        }
    }
#endif
}
