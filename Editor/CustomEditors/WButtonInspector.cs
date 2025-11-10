namespace WallstopStudios.UnityHelpers.Editor.CustomEditors
{
#if UNITY_EDITOR
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.WButton;

    [CustomEditor(typeof(UnityEngine.Object), true)]
    [CanEditMultipleObjects]
    public sealed class WButtonInspector : Editor
    {
        private readonly Dictionary<int, WButtonPaginationState> _paginationStates =
            new Dictionary<int, WButtonPaginationState>();

        public override void OnInspectorGUI()
        {
            List<WButtonMethodContext> triggeredContexts = new List<WButtonMethodContext>();

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

            bool topDrawn = WButtonGUI.DrawButtons(
                this,
                WButtonPlacement.Top,
                _paginationStates,
                triggeredContexts
            );
            if (topDrawn)
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

            bool bottomDrawn = WButtonGUI.DrawButtons(
                this,
                WButtonPlacement.Bottom,
                _paginationStates,
                triggeredContexts
            );
            if (bottomDrawn)
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
