namespace WallstopStudios.UnityHelpers.Editor.CustomEditors
{
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;
    using UnityHelpers.Utils;

    [CustomEditor(typeof(PolygonCollider2DOptimizer))]
    public sealed class PolygonCollider2DOptimizerEditor : Editor
    {
        private SerializedProperty _tolerance;

        private void OnEnable()
        {
            _tolerance = serializedObject.FindProperty(
                nameof(PolygonCollider2DOptimizer.tolerance)
            );
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (EditorGUILayout.PropertyField(_tolerance))
            {
                serializedObject.ApplyModifiedProperties();
            }

            if (GUILayout.Button("Optimize"))
            {
                PolygonCollider2DOptimizer optimizer = target as PolygonCollider2DOptimizer;
                if (optimizer != null)
                {
                    optimizer.Refresh();
                }
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
#endif
}
