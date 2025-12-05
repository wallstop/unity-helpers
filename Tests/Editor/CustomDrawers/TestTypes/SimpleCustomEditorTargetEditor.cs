#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using UnityEditor;
    using WallstopStudios.UnityHelpers.Editor.Internal;

    /// <summary>
    /// Custom editor for SimpleCustomEditorTarget used to test inline editor behavior with custom editors.
    /// </summary>
    [CustomEditor(typeof(SimpleCustomEditorTarget))]
    internal sealed class SimpleCustomEditorTargetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            if (scriptProperty != null && !InlineInspectorContext.IsActive)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(scriptProperty, true);
                }
                EditorGUILayout.Space();
            }

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                if (iterator.propertyPath == "m_Script")
                {
                    enterChildren = false;
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, true);
                enterChildren = false;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
