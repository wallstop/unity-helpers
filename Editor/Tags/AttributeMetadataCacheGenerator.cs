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

        internal static void GenerateCache()
        {
            try
            {
                // Gather all types derived from AttributesComponent, with a robust fallback
                List<Type> attributeComponentTypes = FindAttributeComponentTypes();

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
                            new TypeFieldMetadata(
                                GetAssemblyQualifiedTypeName(type),
                                fieldNames.ToArray()
                            )
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

        private static List<Type> FindAttributeComponentTypes()
        {
            // Primary: fast TypeCache-based discovery
            List<Type> types = TypeCache
                .GetTypesDerivedFrom<AttributesComponent>()
                .Where(AttributeMetadataFilters.ShouldSerialize)
                .ToList();

            if (types.Count > 0)
            {
                return types;
            }

            // Fallback: reflection-based scan across loaded assemblies
            HashSet<Type> results = new();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == null || assembly.IsDynamic)
                {
                    continue;
                }

                Type[] loaded;
                try
                {
                    loaded = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    loaded = ex.Types;
                }
                catch
                {
                    continue;
                }

                if (loaded == null || loaded.Length == 0)
                {
                    continue;
                }

                foreach (Type t in loaded)
                {
                    if (
                        t is { IsAbstract: false, IsGenericTypeDefinition: false }
                        && typeof(AttributesComponent).IsAssignableFrom(t)
                        && AttributeMetadataFilters.ShouldSerialize(t)
                    )
                    {
                        results.Add(t);
                    }
                }
            }

            return results.ToList();
        }

        private static List<RelationalTypeMetadata> ScanRelationalAttributes()
        {
            List<RelationalTypeMetadata> result = new();

            // Get all Component types using TypeCache with a reflection fallback for robustness
            List<Type> componentTypes = TypeCache
                .GetTypesDerivedFrom<Component>()
                .Where(type => !type.IsGenericType)
                .Where(AttributeMetadataFilters.ShouldSerialize)
                .ToList();

            if (componentTypes.Count == 0)
            {
                HashSet<Type> results = new();
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly == null || assembly.IsDynamic)
                    {
                        continue;
                    }

                    Type[] loaded;
                    try
                    {
                        loaded = assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        loaded = ex.Types;
                    }
                    catch
                    {
                        continue;
                    }

                    if (loaded == null)
                    {
                        continue;
                    }

                    foreach (Type t in loaded)
                    {
                        if (
                            t != null
                            && typeof(Component).IsAssignableFrom(t)
                            && !t.IsGenericType
                            && AttributeMetadataFilters.ShouldSerialize(t)
                        )
                        {
                            results.Add(t);
                        }
                    }
                }

                componentTypes = results.ToList();
            }

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
                    else
                    {
                        switch (fieldType.IsGenericType)
                        {
                            case true when fieldType.GetGenericTypeDefinition() == typeof(List<>):
                                fieldKind = FieldKind.List;
                                elementType = fieldType.GenericTypeArguments[0];
                                break;
                            case true
                                when fieldType.GetGenericTypeDefinition() == typeof(HashSet<>):
                                fieldKind = FieldKind.HashSet;
                                elementType = fieldType.GenericTypeArguments[0];
                                break;
                            default:
                                fieldKind = FieldKind.Single;
                                elementType = fieldType;
                                break;
                        }
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
                            GetAssemblyQualifiedTypeName(elementType),
                            isInterface
                        )
                    );
                }

                if (fieldMetadataList.Count > 0)
                {
                    result.Add(
                        new RelationalTypeMetadata(
                            GetAssemblyQualifiedTypeName(type),
                            fieldMetadataList.ToArray()
                        )
                    );
                }
            }

            return result;
        }

        private static string GetAssemblyQualifiedTypeName(Type type)
        {
            if (type == null)
            {
                return string.Empty;
            }

            return type.AssemblyQualifiedName ?? type.FullName ?? string.Empty;
        }

        private static AttributeMetadataCache GetOrCreateCache()
        {
            return AttributeMetadataCache.Instance;
        }
    }
}
