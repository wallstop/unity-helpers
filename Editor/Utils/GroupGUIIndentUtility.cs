namespace WallstopStudios.UnityHelpers.Editor.Utils
{
#if UNITY_EDITOR
    using System;
    using UnityEditor;
    using UnityEngine;

    internal static class GroupGUIIndentUtility
    {
        internal static void ExecuteWithIndentCompensation(Action drawAction)
        {
            if (drawAction == null)
            {
                return;
            }

            int originalIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = Mathf.Max(0, originalIndent - 1);
            try
            {
                drawAction();
            }
            finally
            {
                EditorGUI.indentLevel = originalIndent;
            }
        }
    }
#endif
}
