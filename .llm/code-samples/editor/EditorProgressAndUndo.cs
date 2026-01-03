// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

// Common editor patterns - progress bars, undo support, drag and drop

namespace WallstopStudios.UnityHelpers.Examples
{
#if UNITY_EDITOR
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public static class EditorCommonPatterns
    {
        // Progress bar for long operations
        public static void ProcessAssetsWithProgress(List<string> assetPaths)
        {
            int total = assetPaths.Count;
            try
            {
                for (int i = 0; i < total; i++)
                {
                    string path = assetPaths[i];

                    if (
                        EditorUtility.DisplayCancelableProgressBar(
                            "Processing Assets",
                            $"Processing {Path.GetFileName(path)} ({i + 1}/{total})",
                            (float)i / total
                        )
                    )
                    {
                        break; // User cancelled
                    }

                    ProcessSingleAsset(path);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void ProcessSingleAsset(string path) { }

        // Undo support patterns
        public static void UndoPatterns(Object targetObject)
        {
            // Record object before modification
            Undo.RecordObject(targetObject, "Descriptive Undo Name");
            // targetObject.someField = newValue;

            // Group multiple operations
            Undo.SetCurrentGroupName("Complex Operation");
            int undoGroup = Undo.GetCurrentGroup();
            // ... multiple changes ...
            Undo.CollapseUndoOperations(undoGroup);

            // Register newly created objects
            GameObject newObj = new GameObject("Created Object");
            Undo.RegisterCreatedObjectUndo(newObj, "Create Object");
        }

        // Drag and drop handling
        public static void HandleDragAndDrop(Rect dropArea)
        {
            Event evt = Event.current;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                {
                    if (!dropArea.Contains(evt.mousePosition))
                    {
                        return;
                    }

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (Object draggedObject in DragAndDrop.objectReferences)
                        {
                            string path = AssetDatabase.GetAssetPath(draggedObject);
                            // Process dropped object
                        }
                    }

                    evt.Use();
                    break;
                }
            }
        }

        // User dialogs with suppression for batch/test mode
        private static bool SuppressUserPrompts { get; set; }

        public static bool ConfirmAction(string message)
        {
            if (SuppressUserPrompts)
            {
                return true; // Auto-confirm in batch/test mode
            }

            return EditorUtility.DisplayDialog("Confirm Action", message, "Yes", "No");
        }

        public static string SelectFolder()
        {
            if (SuppressUserPrompts)
            {
                return null;
            }

            return EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
        }
    }
#endif
}
