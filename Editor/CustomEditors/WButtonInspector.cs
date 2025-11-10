namespace WallstopStudios.UnityHelpers.Editor.CustomEditors
{
#if UNITY_EDITOR
    using System.Collections.Generic;
    using UnityEditor;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Editor.Utils.WButton;

    [CustomEditor(typeof(UnityEngine.Object), true)]
    [CanEditMultipleObjects]
    public sealed class WButtonInspector : Editor
    {
        private readonly Dictionary<int, WButtonPaginationState> _paginationStates = new();
        private readonly Dictionary<int, bool> _foldoutStates = new();

        public override void OnInspectorGUI()
        {
            List<WButtonMethodContext> triggeredContexts = new();

            serializedObject.UpdateIfRequiredOrScript();

            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            string scriptPath = null;
            if (scriptProperty != null)
            {
                scriptPath = scriptProperty.propertyPath;
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(scriptProperty, true);
                }
                EditorGUILayout.Space();
            }

            UnityHelpersSettings.WButtonActionsPlacement placement =
                UnityHelpersSettings.GetWButtonActionsPlacement();
            UnityHelpersSettings.WButtonFoldoutBehavior foldoutBehavior =
                UnityHelpersSettings.GetWButtonFoldoutBehavior();
            bool drawTop = placement == UnityHelpersSettings.WButtonActionsPlacement.Top;
            bool drawBottom = placement == UnityHelpersSettings.WButtonActionsPlacement.Bottom;

            if (
                drawTop
                && WButtonGUI.DrawButtons(
                    this,
                    WButtonPlacement.Top,
                    _paginationStates,
                    _foldoutStates,
                    foldoutBehavior,
                    triggeredContexts
                )
            )
            {
                EditorGUILayout.Space();
            }

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (!string.IsNullOrEmpty(scriptPath) && iterator.propertyPath == scriptPath)
                {
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, true);
            }

            serializedObject.ApplyModifiedProperties();

            if (
                drawBottom
                && WButtonGUI.DrawButtons(
                    this,
                    WButtonPlacement.Bottom,
                    _paginationStates,
                    _foldoutStates,
                    foldoutBehavior,
                    triggeredContexts
                )
            )
            {
                EditorGUILayout.Space();
            }

            if (triggeredContexts.Count > 0)
            {
                WButtonInvocationController.ProcessTriggeredMethods(triggeredContexts);
            }
        }
    }
#endif
}
