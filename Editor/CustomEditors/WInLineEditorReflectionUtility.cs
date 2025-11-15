namespace WallstopStudios.UnityHelpers.Editor.CustomEditors
{
#if UNITY_EDITOR
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;

    internal static class WInLineEditorReflectionUtility
    {
        internal static FieldInfo ResolveFieldInfo(SerializedProperty property)
        {
            if (property == null)
            {
                return null;
            }

            SerializedObject owner = property.serializedObject;
            if (owner == null || owner.targetObject == null)
            {
                return null;
            }

            Type currentType = owner.targetObject.GetType();

            string propertyPath = property.propertyPath;
            if (string.IsNullOrEmpty(propertyPath))
            {
                return null;
            }

            string normalizedPath = propertyPath.Replace(".Array.data[", ".[");
            string[] elements = normalizedPath.Split('.');

            FieldInfo resolvedField = null;
            foreach (string rawElement in elements)
            {
                if (string.IsNullOrEmpty(rawElement))
                {
                    continue;
                }

                if (rawElement[0] == '[')
                {
                    currentType = GetElementType(currentType);
                    if (currentType == null)
                    {
                        return null;
                    }

                    continue;
                }

                string memberName = rawElement;
                int bracketIndex = memberName.IndexOf('[');
                if (bracketIndex >= 0)
                {
                    memberName = memberName.Substring(0, bracketIndex);
                }

                FieldInfo field = GetFieldFromHierarchy(currentType, memberName);
                if (field == null)
                {
                    return null;
                }

                resolvedField = field;
                currentType = field.FieldType;
            }

            return resolvedField;
        }

        private static FieldInfo GetFieldFromHierarchy(Type type, string fieldName)
        {
            const BindingFlags Flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            Type searchType = type;
            while (searchType != null)
            {
                FieldInfo field = searchType.GetField(fieldName, Flags);
                if (field != null)
                {
                    return field;
                }

                searchType = searchType.BaseType;
            }

            return null;
        }

        private static Type GetElementType(Type collectionType)
        {
            if (collectionType == null)
            {
                return null;
            }

            if (collectionType.IsArray)
            {
                return collectionType.GetElementType();
            }

            if (
                typeof(IList).IsAssignableFrom(collectionType)
                && collectionType.IsGenericType
                && collectionType.GetGenericArguments().Length == 1
            )
            {
                return collectionType.GetGenericArguments()[0];
            }

            if (
                typeof(IEnumerable).IsAssignableFrom(collectionType)
                && collectionType.IsGenericType
                && collectionType.GetGenericArguments().Length == 1
            )
            {
                return collectionType.GetGenericArguments()[0];
            }

            return null;
        }
    }
#endif
}
