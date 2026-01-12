// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

// Defensive programming patterns for editor code

namespace WallstopStudios.UnityHelpers.Examples
{
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public static class DefensiveEditorPatterns
    {
        // Safe SerializedProperty access pattern
        public static void SafePropertyDrawing(
            Rect position,
            SerializedProperty property,
            GUIContent label
        )
        {
            if (property == null)
            {
                return;
            }

            if (property.serializedObject == null || property.serializedObject.targetObject == null)
            {
                EditorGUI.LabelField(position, label, new GUIContent("(Missing Object)"));
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            try
            {
                // Draw property safely...
                EditorGUI.PropertyField(position, property, label, true);
            }
            finally
            {
                EditorGUI.EndProperty();
            }
        }

        // Safe asset operations
        public static T LoadAssetSafe<T>(string path)
            where T : Object
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        // Cache invalidation on target change
        private static Object _lastTarget;
        private static SerializedProperty _cachedProperty;

        public static SerializedProperty GetPropertyWithCacheInvalidation(SerializedObject so)
        {
            if (so == null || so.targetObject == null)
            {
                _cachedProperty = null;
                _lastTarget = null;
                return null;
            }

            if (_lastTarget != so.targetObject)
            {
                _cachedProperty = null;
                _lastTarget = so.targetObject;
            }

            if (_cachedProperty == null)
            {
                _cachedProperty = so.FindProperty("_fieldName");
            }

            return _cachedProperty;
        }
    }
#endif
}
