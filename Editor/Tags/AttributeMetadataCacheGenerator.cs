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
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using static UnityHelpers.Tags.AttributeMetadataCache;
    using ReflectionHelpers = WallstopStudios.UnityHelpers.Core.Helper.ReflectionHelpers;

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
            // Skip automatic cache generation during test runs to avoid Unity's internal modal dialogs
            // when asset operations fail, unless explicitly allowed.
            if (
                EditorUi.Suppress
                && !ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression
            )
            {
                return;
            }

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

                AutoLoadSingletonEntry[] autoLoadEntries = BuildAutoLoadSingletonEntries();

                // Get or create the cache asset
                AttributeMetadataCache cache = GetOrCreateCache();

                // Update the cache
                cache.SetMetadata(
                    sortedAttributeNames,
                    typeMetadataList.ToArray(),
                    relationalMetadataList.ToArray(),
                    autoLoadEntries
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
            List<Type> types = ReflectionHelpers
                .GetTypesDerivedFrom<AttributesComponent>(includeAbstract: false)
                .Where(AttributeMetadataFilters.ShouldSerialize)
                .ToList();

            if (types.Count > 0)
            {
                return types;
            }

            // Fallback: reflection-based scan via ReflectionHelpers
            HashSet<Type> results = new();
            foreach (Type t in ReflectionHelpers.GetAllLoadedTypes())
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
            return results.ToList();
        }

        private static List<RelationalTypeMetadata> ScanRelationalAttributes()
        {
            List<RelationalTypeMetadata> result = new();

            // Get all Component types using TypeCache with a reflection fallback for robustness
            List<Type> componentTypes = ReflectionHelpers
                .GetTypesDerivedFrom<Component>(includeAbstract: false)
                .Where(type => !type.IsGenericType)
                .Where(AttributeMetadataFilters.ShouldSerialize)
                .ToList();

            if (componentTypes.Count == 0)
            {
                HashSet<Type> results = new();
                foreach (Type t in ReflectionHelpers.GetAllLoadedTypes())
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
                    if (
                        ReflectionHelpers.HasAttributeSafe<ParentComponentAttribute>(
                            field,
                            inherit: false
                        )
                    )
                    {
                        attributeKind = RelationalAttributeKind.Parent;
                    }
                    else if (
                        ReflectionHelpers.HasAttributeSafe<ChildComponentAttribute>(
                            field,
                            inherit: false
                        )
                    )
                    {
                        attributeKind = RelationalAttributeKind.Child;
                    }
                    else if (
                        ReflectionHelpers.HasAttributeSafe<SiblingComponentAttribute>(
                            field,
                            inherit: false
                        )
                    )
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

        private static AutoLoadSingletonEntry[] BuildAutoLoadSingletonEntries()
        {
            List<AutoLoadSingletonEntry> entries = new();
            foreach (Type type in TypeCache.GetTypesWithAttribute<AutoLoadSingletonAttribute>())
            {
                if (type == null || type.IsAbstract || type.ContainsGenericParameters)
                {
                    continue;
                }

                AutoLoadSingletonAttribute attribute =
                    System
                        .Attribute.GetCustomAttributes(
                            type,
                            typeof(AutoLoadSingletonAttribute),
                            inherit: false
                        )
                        .FirstOrDefault() as AutoLoadSingletonAttribute;
                if (attribute == null)
                {
                    continue;
                }

                SingletonAutoLoadKind? kind = ResolveSingletonKind(type);
                if (!kind.HasValue)
                {
                    Debug.LogWarning(
                        $"AttributeMetadataCacheGenerator: {type.FullName} is marked with [AutoLoadSingleton] but does not derive from RuntimeSingleton<> or ScriptableObjectSingleton<>."
                    );
                    continue;
                }

                string typeName = GetAssemblyQualifiedTypeName(type);
                if (string.IsNullOrWhiteSpace(typeName))
                {
                    continue;
                }

                entries.Add(new AutoLoadSingletonEntry(typeName, kind.Value, attribute.LoadType));
            }

            entries.Sort((left, right) => string.CompareOrdinal(left.typeName, right.typeName));
            return entries.ToArray();
        }

        private static SingletonAutoLoadKind? ResolveSingletonKind(Type type)
        {
            if (
                IsSubclassOfRawGeneric(
                    type,
                    typeof(WallstopStudios.UnityHelpers.Utils.RuntimeSingleton<>)
                )
            )
            {
                return SingletonAutoLoadKind.Runtime;
            }

            if (
                IsSubclassOfRawGeneric(
                    type,
                    typeof(WallstopStudios.UnityHelpers.Utils.ScriptableObjectSingleton<>)
                )
            )
            {
                return SingletonAutoLoadKind.ScriptableObject;
            }

            return null;
        }

        private static bool IsSubclassOfRawGeneric(Type derived, Type openGeneric)
        {
            Type current = derived;
            while (current != null && current != typeof(object))
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == openGeneric)
                {
                    return true;
                }

                current = current.BaseType;
            }
            return false;
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
            // Try loading from the expected path first
            const string assetPath =
                "Assets/Resources/Wallstop Studios/AttributeMetadataCache.asset";
            const string resourcesLoadPath = "Wallstop Studios/AttributeMetadataCache";
            const string resourcesFolder = "Wallstop Studios";

            AttributeMetadataCache cache = AssetDatabase.LoadAssetAtPath<AttributeMetadataCache>(
                assetPath
            );
            if (cache != null)
            {
                UpdateMetadataEntry(assetPath, resourcesLoadPath, resourcesFolder);
                return cache;
            }

            // Try loading via the singleton Instance (may work if already created)
            cache = AttributeMetadataCache.Instance;
            if (cache != null)
            {
                return cache;
            }

            // Create the asset ourselves
            string directory = System.IO.Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory))
            {
                directory = directory.SanitizePath();

                // First, ensure the folder exists on disk. This prevents Unity's internal
                // "Moving file failed" modal dialog when CreateAsset tries to move a temp file
                // to a destination folder that doesn't exist.
                string projectRoot = System.IO.Path.GetDirectoryName(Application.dataPath);
                if (!string.IsNullOrEmpty(projectRoot))
                {
                    string absoluteDirectory = System.IO.Path.Combine(projectRoot, directory);
                    try
                    {
                        if (!System.IO.Directory.Exists(absoluteDirectory))
                        {
                            System.IO.Directory.CreateDirectory(absoluteDirectory);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(
                            $"AttributeMetadataCacheGenerator: Failed to create directory on disk '{absoluteDirectory}': {ex.Message}"
                        );
                        return null;
                    }
                }

                if (!AssetDatabase.IsValidFolder(directory))
                {
                    string[] segments = directory.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    string current = segments[0];
                    for (int i = 1; i < segments.Length; i++)
                    {
                        string next = $"{current}/{segments[i]}";
                        if (!AssetDatabase.IsValidFolder(next))
                        {
                            AssetDatabase.CreateFolder(current, segments[i]);
                        }
                        current = next;
                    }
                }
            }

            cache = ScriptableObject.CreateInstance<AttributeMetadataCache>();
            try
            {
                AssetDatabase.CreateAsset(cache, assetPath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"AttributeMetadataCacheGenerator: Failed to create cache asset: {ex.Message}"
                );
                if (cache != null)
                {
                    UnityEngine.Object.DestroyImmediate(cache);
                }
                return null;
            }
            AssetDatabase.SaveAssets();

            UpdateMetadataEntry(assetPath, resourcesLoadPath, resourcesFolder);

            return cache;
        }

        private static void UpdateMetadataEntry(
            string assetPath,
            string resourcesLoadPath,
            string resourcesFolder
        )
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (!string.IsNullOrEmpty(guid))
            {
                ScriptableObjectSingletonMetadataUtility.UpdateEntry(
                    typeof(AttributeMetadataCache),
                    resourcesLoadPath,
                    resourcesFolder,
                    guid
                );
            }
        }
    }
}
