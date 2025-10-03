namespace WallstopStudios.UnityHelpers.Editor.Extensions
{
#if UNITY_EDITOR
    using System;
    using System.Reflection;
    using UnityEditor;

    public static class SerializedPropertyExtensions
    {
        /// <summary>
        /// Gets the instance object that contains the given SerializedProperty.
        /// </summary>
        /// <param name="property">The SerializedProperty.</param>
        /// <param name="fieldInfo">Outputs the FieldInfo of the referenced field.</param>
        /// <returns>The instance object that owns the field.</returns>
        public static object GetEnclosingObject(
            this SerializedProperty property,
            out FieldInfo fieldInfo
        )
        {
            fieldInfo = null;
            object obj = property.serializedObject.targetObject;
            if (obj == null)
            {
                return null;
            }
            Type type = obj.GetType();
            string[] pathParts = property.propertyPath.Split('.');

            if (
                string.Equals(property.name, "data", StringComparison.Ordinal)
                && pathParts.Length > 1
                && pathParts[^1].Contains('[')
                && pathParts[^1].Contains(']')
                && string.Equals(pathParts[^2], "Array", StringComparison.Ordinal)
            )
            {
                Array.Resize(ref pathParts, pathParts.Length - 2);
            }

            // Traverse the path but stop at the second-to-last field
            for (int i = 0; i < pathParts.Length - 1; ++i)
            {
                string fieldName = pathParts[i];

                if (string.Equals(fieldName, "Array", StringComparison.Ordinal))
                {
                    // Move to "data[i]", no need to length-check, we're guarded above
                    ++i;
                    fieldName = pathParts[i];
                    if (
                        !int.TryParse(
                            fieldName
                                .Replace("data[", string.Empty, StringComparison.Ordinal)
                                .Replace("]", string.Empty, StringComparison.Ordinal),
                            out int index
                        )
                    )
                    {
                        // Unexpected, die
                        fieldInfo = null;
                        return null;
                    }

                    obj = GetElementAtIndex(obj, index);
                    type = obj?.GetType();
                    UpdateField(fieldName, ref fieldInfo);

                    if (i == pathParts.Length - 2)
                    {
                        fieldName = pathParts[i + 1];
                        UpdateField(fieldName, ref fieldInfo);
                    }
                    continue;
                }

                UpdateField(fieldName, ref fieldInfo);
                if (fieldInfo == null)
                {
                    return null;
                }

                // Move deeper but stop before the last property in the path
                if (i < pathParts.Length - 2)
                {
                    obj = fieldInfo.GetValue(obj);
                    type = fieldInfo.FieldType;
                }
            }

            if (fieldInfo == null)
            {
                // Use the last segment of the possibly-trimmed path (actual field name), not property.name (which can be "data")
                if (pathParts.Length > 0)
                {
                    UpdateField(pathParts[^1], ref fieldInfo);
                }
            }

            return obj;

            void UpdateField(string fieldName, ref FieldInfo field)
            {
                FieldInfo newField = type?.GetField(
                    fieldName,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance
                );
                if (newField != null)
                {
                    field = newField;
                }
            }
        }

        /// <summary>
        /// Gets the FieldInfo and the instance object that owns the field for a given SerializedProperty.
        /// </summary>
        /// <param name="property">The SerializedProperty to reflect upon.</param>
        /// <param name="fieldInfo">Outputs the FieldInfo of the referenced field.</param>
        /// <returns>The instance object that owns the field.</returns>
        public static object GetTargetObjectWithField(
            this SerializedProperty property,
            out FieldInfo fieldInfo
        )
        {
            fieldInfo = null;
            object obj = property.serializedObject.targetObject;
            if (obj == null)
            {
                return null;
            }

            Type type = obj.GetType();
            string[] pathParts = property.propertyPath.Split('.');

            for (int i = 0; i < pathParts.Length; ++i)
            {
                string fieldName = pathParts[i];

                if (string.Equals(fieldName, "Array", StringComparison.Ordinal))
                {
                    // Move to "data[i]"
                    ++i;
                    if (pathParts.Length <= i)
                    {
                        break;
                    }

                    if (
                        !int.TryParse(
                            pathParts[i]
                                .Replace("data[", string.Empty, StringComparison.Ordinal)
                                .Replace("]", string.Empty, StringComparison.Ordinal),
                            out int index
                        )
                    )
                    {
                        // Unexpected, die
                        fieldInfo = null;
                        return null;
                    }
                    obj = GetElementAtIndex(obj, index);
                    type = obj?.GetType();
                    continue;
                }

                fieldInfo = type?.GetField(
                    fieldName,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance
                );
                if (fieldInfo == null)
                {
                    return null;
                }

                // Move deeper into the object tree
                obj = fieldInfo.GetValue(obj);
                type = fieldInfo.FieldType;
            }

            return obj;
        }

        private static object GetElementAtIndex(object obj, int index)
        {
            if (obj is System.Collections.IList list && index >= 0 && index < list.Count)
            {
                return list[index];
            }
            return null;
        }
    }
#endif
}
