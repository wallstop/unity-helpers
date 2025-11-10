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

            DrawDefaultInspector();

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
                WButtonInvocationController.ProcessTriggeredMethods(this, triggeredContexts);
            }
        }
    }
#endif
}
