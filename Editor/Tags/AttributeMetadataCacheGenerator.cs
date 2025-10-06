namespace WallstopStudios.UnityHelpers.Editor.Tags
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using global::UnityEditor;
    using global::UnityEngine;
    using UnityHelpers.Core.Attributes;
    using UnityHelpers.Tags;
    using static UnityHelpers.Tags.AttributeMetadataCache;

    /// <summary>
    /// Editor script that generates AttributeMetadataCache at edit-time using TypeCache.
    /// This eliminates the need for runtime reflection.
    /// </summary>
    [InitializeOnLoad]
    public static class AttributeMetadataCacheGenerator
    {
        static AttributeMetadataCacheGenerator()
        {
            EditorApplication.delayCall += GenerateCache;
        }

        [MenuItem("Tools/WallstopStudios/Regenerate Attribute Metadata Cache")]
        public static void RegenerateCacheMenuItem()
        {
            GenerateCache();
            Debug.Log("Attribute Metadata Cache regenerated successfully.");
        }

        private static void GenerateCache()
        {
            try
            {
                // Use TypeCache to get all types derived from AttributesComponent at compile-time
                List<Type> attributeComponentTypes = TypeCache
                    .GetTypesDerivedFrom<AttributesComponent>()
                    .Where(type => !type.IsAbstract)
                    .ToList();

                if (attributeComponentTypes.Count == 0)
                {
                    // No types found yet, might be during initial compilation
                    return;
                }

                // Collect all unique attribute field names across all types
                HashSet<string> allAttributeNames = new(StringComparer.Ordinal);
                List<TypeFieldMetadata> typeMetadataList = new();

                foreach (Type type in attributeComponentTypes)
                {
                    FieldInfo[] fields = type.GetFields(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );

                    List<string> fieldNames = new();
                    foreach (FieldInfo field in fields)
                    {
                        if (field.FieldType == typeof(UnityHelpers.Tags.Attribute))
                        {
                            fieldNames.Add(field.Name);
                            allAttributeNames.Add(field.Name);
                        }
                    }

                    if (fieldNames.Count > 0)
                    {
                        typeMetadataList.Add(
                            new TypeFieldMetadata(type.FullName, fieldNames.ToArray())
                        );
                    }
                }

                // Sort for consistency
                string[] sortedAttributeNames = allAttributeNames.OrderBy(name => name).ToArray();

                // Scan for relational attributes
                List<RelationalTypeMetadata> relationalMetadataList = ScanRelationalAttributes();

                // Get or create the cache asset
                AttributeMetadataCache cache = GetOrCreateCache();

                // Update the cache
                cache.SetMetadata(
                    sortedAttributeNames,
                    typeMetadataList.ToArray(),
                    relationalMetadataList.ToArray()
                );

                // Save the asset
                AssetDatabase.SaveAssets();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to generate AttributeMetadataCache: {ex}");
            }
        }

        private static List<RelationalTypeMetadata> ScanRelationalAttributes()
        {
            List<RelationalTypeMetadata> result = new();

            // Get all Component types using TypeCache
            List<Type> componentTypes = TypeCache
                .GetTypesDerivedFrom<Component>()
                .Where(type => !type.IsAbstract && !type.IsGenericType)
                .ToList();

            foreach (Type type in componentTypes)
            {
                List<RelationalFieldMetadata> fieldMetadataList = new();

                FieldInfo[] fields = type.GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                foreach (FieldInfo field in fields)
                {
                    RelationalAttributeKind? attributeKind = null;

                    if (field.IsDefined(typeof(ParentComponentAttribute), false))
                    {
                        attributeKind = RelationalAttributeKind.Parent;
                    }
                    else if (field.IsDefined(typeof(ChildComponentAttribute), false))
                    {
                        attributeKind = RelationalAttributeKind.Child;
                    }
                    else if (field.IsDefined(typeof(SiblingComponentAttribute), false))
                    {
                        attributeKind = RelationalAttributeKind.Sibling;
                    }

                    if (!attributeKind.HasValue)
                    {
                        continue;
                    }

                    // Determine field kind and element type
                    Type fieldType = field.FieldType;
                    FieldKind fieldKind;
                    Type elementType;

                    if (fieldType.IsArray)
                    {
                        fieldKind = FieldKind.Array;
                        elementType = fieldType.GetElementType();
                    }
                    else if (
                        fieldType.IsGenericType
                        && fieldType.GetGenericTypeDefinition() == typeof(List<>)
                    )
                    {
                        fieldKind = FieldKind.List;
                        elementType = fieldType.GenericTypeArguments[0];
                    }
                    else
                    {
                        fieldKind = FieldKind.Single;
                        elementType = fieldType;
                    }

                    // Determine if element type is an interface or base type
                    bool isInterface =
                        elementType.IsInterface
                        || (!elementType.IsSealed && elementType != typeof(Component));

                    fieldMetadataList.Add(
                        new RelationalFieldMetadata(
                            field.Name,
                            attributeKind.Value,
                            fieldKind,
                            elementType.FullName,
                            isInterface
                        )
                    );
                }

                if (fieldMetadataList.Count > 0)
                {
                    result.Add(
                        new RelationalTypeMetadata(type.FullName, fieldMetadataList.ToArray())
                    );
                }
            }

            return result;
        }

        private static AttributeMetadataCache GetOrCreateCache()
        {
            // The ScriptableObjectSingletonCreator will automatically create the instance
            // if it doesn't exist, so we can just access it directly
            if (!AttributeMetadataCache.HasInstance)
            {
                // Force initialization by accessing Instance
                _ = AttributeMetadataCache.Instance;
            }

            return AttributeMetadataCache.Instance;
        }
    }
}
