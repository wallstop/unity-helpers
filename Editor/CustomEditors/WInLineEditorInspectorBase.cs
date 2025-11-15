namespace WallstopStudios.UnityHelpers.Editor.CustomEditors
{
#if UNITY_EDITOR
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;

    internal abstract class WInLineEditorInspectorBase : Editor
    {
        private static readonly WInLineEditorPropertyDrawer InlineDrawer = new();

        public override void OnInspectorGUI()
        {
            if (serializedObject == null)
            {
                return;
            }

            serializedObject.UpdateIfRequiredOrScript();

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.propertyPath == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }
                    continue;
                }

                if (
                    TryResolveInlineSettings(
                        iterator,
                        out WInLineEditorAttribute inlineAttribute,
                        out FieldInfo resolvedField
                    )
                )
                {
                    DrawInlineProperty(iterator);
                }
                else
                {
                    EditorGUILayout.PropertyField(iterator, true);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawInlineProperty(SerializedProperty property)
        {
            GUIContent label = new(property.displayName);
            float height = InlineDrawer.GetPropertyHeight(property, label);
            Rect rect = EditorGUILayout.GetControlRect(true, height);
            EditorGUI.BeginProperty(rect, label, property);
            InlineDrawer.OnGUI(rect, property, label);
            EditorGUI.EndProperty();
        }

        private static bool TryResolveInlineSettings(
            SerializedProperty property,
            out WInLineEditorAttribute inlineAttribute,
            out FieldInfo resolvedField
        )
        {
            inlineAttribute = null;
            resolvedField = null;

            if (property == null)
            {
                return false;
            }

            FieldInfo field = WInLineEditorReflectionUtility.ResolveFieldInfo(property);
            if (field == null)
            {
                return false;
            }

            WInLineEditorAttribute attribute =
                field.GetCustomAttribute<WInLineEditorAttribute>(inherit: true)
                ?? ReflectionHelpers.GetAttributeSafe<WInLineEditorAttribute>(field, inherit: true);

            if (attribute == null)
            {
                return false;
            }

            resolvedField = field;
            inlineAttribute = attribute;
            return true;
        }
    }
#endif
}
