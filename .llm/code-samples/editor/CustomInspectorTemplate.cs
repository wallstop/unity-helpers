// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

// Custom Inspector template - complete custom editor with pooling

namespace WallstopStudios.UnityHelpers.Editor.CustomEditors
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Utils;

    [CustomEditor(typeof(MyComponent))]
    [CanEditMultipleObjects]
    public sealed class MyComponentEditor : Editor
    {
        private static readonly WallstopGenericPool<
            Dictionary<string, SerializedProperty>
        > PropertyLookupPool = new(
            () => new Dictionary<string, SerializedProperty>(16, StringComparer.Ordinal),
            onRelease: d => d.Clear()
        );

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            // Draw default script field (disabled)
            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            if (scriptProperty != null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(scriptProperty, true);
                }
                EditorGUILayout.Space();
            }

            // Draw custom properties
            SerializedProperty myProperty = serializedObject.FindProperty("_myField");
            if (myProperty != null)
            {
                EditorGUILayout.PropertyField(myProperty);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}

// Key Custom Inspector Patterns:
// 1. Multi-Object Editing - Include [CanEditMultipleObjects] for selection support
// 2. Script Field - Draw m_Script property disabled at top
// 3. Property Pooling - Use WallstopGenericPool for dictionary caches in performance-sensitive inspectors
// 4. Update/Apply Cycle - Always call UpdateIfRequiredOrScript() and ApplyModifiedProperties()
