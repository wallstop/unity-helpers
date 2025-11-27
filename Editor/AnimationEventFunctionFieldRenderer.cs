namespace WallstopStudios.UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System;
    using UnityEditor;
    using UnityEngine;

    internal static class AnimationEventFunctionFieldRenderer
    {
        public static void DrawFunctionFields(
            AnimationEventItem item,
            bool explicitMode,
            Action<string> recordUndo
        )
        {
            AnimationEvent animEvent = item.animationEvent;
            EditorGUI.BeginChangeCheck();
            string newFunctionName = EditorGUILayout.TextField(
                "FunctionName",
                animEvent.functionName ?? string.Empty
            );
            if (EditorGUI.EndChangeCheck())
            {
                recordUndo?.Invoke("Change Animation Event Function");
                animEvent.functionName = newFunctionName;
                item.selectedType = null;
                item.selectedMethod = null;
            }

            if (explicitMode)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            item.search = EditorGUILayout.TextField("Method Search", item.search);
            if (EditorGUI.EndChangeCheck())
            {
                item.cachedLookup = null;
            }

            EditorGUI.BeginChangeCheck();
            item.typeSearch = EditorGUILayout.TextField("Type Search", item.typeSearch);
            if (EditorGUI.EndChangeCheck())
            {
                item.cachedLookup = null;
            }
        }
    }
#endif
}
