namespace WallstopStudios.UnityHelpers.Tags
{
    using System;
    using System.Collections.Generic;
    using Core.Attributes;
    using Core.Helper;
    using UnityEngine;
    using Utils;

    /// <summary>
    /// Serialized cache of attribute metadata to avoid runtime reflection.
    /// This asset is automatically generated in the Editor.
    /// When the prewarm toggle is enabled, a runtime hook pre-initializes relational component
    /// reflection helpers before the first scene loads to avoid first-use stalls.
    /// </summary>
    [ScriptableSingletonPath("Wallstop Studios/AttributeMetadataCache")]
    [AutoLoadSingleton(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public sealed class AttributeMetadataCache : ScriptableObjectSingleton<AttributeMetadataCache>
    {
        [Header("Initialization")]
        [Tooltip(
            "If enabled, pre-warms RelationalComponent reflection caches at runtime before the first scene loads. Useful to avoid first-use stalls on IL2CPP or slow devices."
        )]
        [SerializeField]
        private bool _prewarmRelationalOnLoad = false;

        public enum RelationalAttributeKind : byte
        {
            [Obsolete("Default uninitialized value - should never be used")]
            Unknown = 0,
            Parent = 1,
            Child = 2,
            Sibling = 3,
        }

        public enum FieldKind : byte
        {
            [Obsolete("Default uninitialized value - should never be used")]
            None = 0,
            Single = 1,
            Array = 2,
            List = 3,
            HashSet = 4,
        }

        [Serializable]
        public sealed class TypeFieldMetadata
        {
            public string typeName;
            public string[] fieldNames;

            public TypeFieldMetadata(string typeName, string[] fieldNames)
            {
                this.typeName = typeName;
                this.fieldNames = fieldNames;
            }
        }

        [Serializable]
        public sealed class RelationalFieldMetadata
        {
            public string fieldName;
            public RelationalAttributeKind attributeKind;
            public FieldKind fieldKind;
            public string elementTypeName;
            public bool isInterface;

            public RelationalFieldMetadata(
                string fieldName,
                RelationalAttributeKind attributeKind,
                FieldKind fieldKind,
                string elementTypeName,
                bool isInterface
            )
            {
                this.fieldName = fieldName;
                this.attributeKind = attributeKind;
                this.fieldKind = fieldKind;
                this.elementTypeName = elementTypeName;
                this.isInterface = isInterface;
            }
        }

        public readonly struct ResolvedRelationalFieldMetadata
        {
            public ResolvedRelationalFieldMetadata(
                string fieldName,
                RelationalAttributeKind attributeKind,
                FieldKind fieldKind,
                Type elementType,
                bool isInterface
            )
            {
                FieldName = fieldName;
                AttributeKind = attributeKind;
                FieldKind = fieldKind;
                ElementType = elementType;
                IsInterface = isInterface;
            }

            public string FieldName { get; }

            public RelationalAttributeKind AttributeKind { get; }

            public FieldKind FieldKind { get; }

            public Type ElementType { get; }

            public bool IsInterface { get; }
        }

        [Serializable]
        public sealed class RelationalTypeMetadata
        {
            public string typeName;
            public RelationalFieldMetadata[] fields;

            public RelationalTypeMetadata(string typeName, RelationalFieldMetadata[] fields)
            {
                this.typeName = typeName;
                this.fields = fields;
            }
        }

        [SerializeField]
        private string[] _allAttributeNames = Array.Empty<string>();

        [NonSerialized]
        private string[] _computedAllAttributeNames;

        [NonSerialized]
        private bool _computedAllAttributeNamesIncludesTests;

        [SerializeField]
        private TypeFieldMetadata[] _typeMetadata = Array.Empty<TypeFieldMetadata>();

        [SerializeField]
        internal RelationalTypeMetadata[] _relationalTypeMetadata =
            Array.Empty<RelationalTypeMetadata>();

        [Serializable]
        public sealed class AutoLoadSingletonEntry
        {
            public string typeName;
            public SingletonAutoLoadKind kind;
            public RuntimeInitializeLoadType loadType;

            public AutoLoadSingletonEntry() { }

            public AutoLoadSingletonEntry(
                string typeName,
                SingletonAutoLoadKind kind,
                RuntimeInitializeLoadType loadType
            )
            {
                this.typeName = typeName;
                this.kind = kind;
                this.loadType = loadType;
            }
        }

        [SerializeField]
        private AutoLoadSingletonEntry[] _autoLoadSingletons =
            Array.Empty<AutoLoadSingletonEntry>();

        internal string[] SerializedAttributeNames => _allAttributeNames ?? Array.Empty<string>();

        internal TypeFieldMetadata[] SerializedTypeMetadata =>
            _typeMetadata ?? Array.Empty<TypeFieldMetadata>();

        internal RelationalTypeMetadata[] SerializedRelationalTypeMetadata =>
            _relationalTypeMetadata ?? Array.Empty<RelationalTypeMetadata>();

        internal AutoLoadSingletonEntry[] SerializedAutoLoadSingletons =>
            _autoLoadSingletons ?? Array.Empty<AutoLoadSingletonEntry>();

        // Compound key for element type lookup
        private readonly struct ElementTypeKey : IEquatable<ElementTypeKey>
        {
            private readonly Type componentType;
            private readonly string fieldName;

            public ElementTypeKey(Type componentType, string fieldName)
            {
                this.componentType = componentType;
                this.fieldName = fieldName;
            }

            public bool Equals(ElementTypeKey other)
            {
                return componentType == other.componentType
                    && string.Equals(fieldName, other.fieldName, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is ElementTypeKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return Objects.HashCode(componentType, fieldName);
            }
        }

        private readonly object _lookupLock = new();

        // Runtime lookups using Type as key (much faster than string comparison)
        private Dictionary<Type, string[]> _typeFieldsLookup;
        private Dictionary<Type, RelationalFieldMetadata[]> _relationalFieldsLookup;
        private Dictionary<Type, ResolvedRelationalFieldMetadata[]> _resolvedRelationalFieldsLookup;
        private Dictionary<ElementTypeKey, Type> _elementTypeLookup; // Cache resolved element types

        private static readonly object _typeResolutionLock = new();
        private static readonly Dictionary<string, Type> _resolvedTypeCache = new(
            StringComparer.Ordinal
        );
        private static readonly object _missingTypeLock = new();
        private static readonly HashSet<string> _loggedMissingTypeKeys = new(
            StringComparer.Ordinal
        );

        public string[] AllAttributeNames
        {
            get
            {
                bool hasTestAssemblies = AttributeMetadataFilters.HasTestAssembliesLoaded();

                if (
                    _computedAllAttributeNames == null
                    || (hasTestAssemblies && !_computedAllAttributeNamesIncludesTests)
                    || (!hasTestAssemblies && _computedAllAttributeNamesIncludesTests)
                )
                {
                    string[] baseNames = _allAttributeNames ?? Array.Empty<string>();

                    _computedAllAttributeNames = hasTestAssemblies
                        ? AttributeMetadataFilters.MergeWithExcludedAttributeNames(baseNames)
                        : baseNames;

                    _computedAllAttributeNamesIncludesTests = hasTestAssemblies;
                }

                return _computedAllAttributeNames;
            }
        }

        public AutoLoadSingletonEntry[] AutoLoadSingletons => SerializedAutoLoadSingletons;

        private void OnEnable()
        {
            _computedAllAttributeNames = null;
            _computedAllAttributeNamesIncludesTests = false;
            BuildLookup();
        }

#if UNITY_2019_1_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimePrewarmRelationalIfEnabled()
        {
            try
            {
                AttributeMetadataCache inst = Instance;
                if (inst != null && inst._prewarmRelationalOnLoad)
                {
                    RelationalComponentInitializer.Report report =
                        RelationalComponentInitializer.Initialize(types: null, logSummary: false);
                    Debug.Log(
                        $"AttributeMetadataCache: Relational prewarm enabled. TypesWarmed={report.TypesWarmed}, FieldsWarmed={report.FieldsWarmed}, Warnings={report.Warnings}, Errors={report.Errors}."
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"AttributeMetadataCache: Exception during relational prewarm on load: {ex.Message}\n{ex}"
                );
            }
        }
#endif

        private void BuildLookup()
        {
            lock (_lookupLock)
            {
                if (
                    _typeFieldsLookup != null
                    && _relationalFieldsLookup != null
                    && _resolvedRelationalFieldsLookup != null
                    && _elementTypeLookup != null
                )
                {
                    return;
                }

                Dictionary<Type, string[]> typeFieldsLookup = new(_typeMetadata?.Length ?? 0);
                Dictionary<Type, RelationalFieldMetadata[]> relationalFieldsLookup = new(
                    _relationalTypeMetadata?.Length ?? 0
                );
                Dictionary<Type, ResolvedRelationalFieldMetadata[]> resolvedRelationalFieldsLookup =
                    new(_relationalTypeMetadata?.Length ?? 0);
                Dictionary<ElementTypeKey, Type> elementTypeLookup = new(
                    _relationalTypeMetadata?.Length ?? 0
                );

                if (_typeMetadata != null)
                {
                    foreach (TypeFieldMetadata metadata in _typeMetadata)
                    {
                        if (!TryResolveType(metadata?.typeName, out Type componentType))
                        {
                            LogMissingType(metadata?.typeName, "attribute component");
                            continue;
                        }

                        string[] fieldNames = metadata.fieldNames ?? Array.Empty<string>();
                        typeFieldsLookup[componentType] = fieldNames;
                    }
                }

                if (_relationalTypeMetadata != null)
                {
                    foreach (RelationalTypeMetadata metadata in _relationalTypeMetadata)
                    {
                        if (!TryResolveType(metadata?.typeName, out Type relationalType))
                        {
                            LogMissingType(metadata?.typeName, "relational component");
                            continue;
                        }

                        RelationalFieldMetadata[] fields =
                            metadata.fields ?? Array.Empty<RelationalFieldMetadata>();

                        relationalFieldsLookup[relationalType] = fields;

                        ResolvedRelationalFieldMetadata[] resolvedFields =
                            new ResolvedRelationalFieldMetadata[fields.Length];

                        for (int i = 0; i < fields.Length; i++)
                        {
                            RelationalFieldMetadata field = fields[i];
                            Type elementType = null;

                            if (!string.IsNullOrWhiteSpace(field.elementTypeName))
                            {
                                if (TryResolveType(field.elementTypeName, out elementType))
                                {
                                    elementTypeLookup[
                                        new ElementTypeKey(relationalType, field.fieldName)
                                    ] = elementType;
                                }
                                else
                                {
                                    LogMissingType(field.elementTypeName, "relational element");
                                }
                            }

                            resolvedFields[i] = new ResolvedRelationalFieldMetadata(
                                field.fieldName,
                                field.attributeKind,
                                field.fieldKind,
                                elementType,
                                field.isInterface
                            );
                        }

                        resolvedRelationalFieldsLookup[relationalType] = resolvedFields;
                    }
                }

                _typeFieldsLookup = typeFieldsLookup;
                _relationalFieldsLookup = relationalFieldsLookup;
                _resolvedRelationalFieldsLookup = resolvedRelationalFieldsLookup;
                _elementTypeLookup = elementTypeLookup;
            }
        }

#if UNITY_INCLUDE_TESTS
        public void ForceRebuildForTests()
        {
            lock (_lookupLock)
            {
                _typeFieldsLookup = null;
                _relationalFieldsLookup = null;
                _resolvedRelationalFieldsLookup = null;
                _elementTypeLookup = null;
            }

            BuildLookup();
        }
#endif

        public bool TryGetFieldNames(Type type, out string[] fieldNames)
        {
            if (_typeFieldsLookup == null)
            {
                BuildLookup();
            }
            return _typeFieldsLookup.TryGetValue(type, out fieldNames);
        }

        public bool TryGetRelationalFields(
            Type type,
            out RelationalFieldMetadata[] relationalFields
        )
        {
            if (_relationalFieldsLookup == null)
            {
                BuildLookup();
            }
            return _relationalFieldsLookup.TryGetValue(type, out relationalFields);
        }

        /// <summary>
        /// Populates <paramref name="destination"/> with the set of component types that declare
        /// relational component fields.
        /// </summary>
        /// <param name="destination">List that receives relational component types.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="destination"/> is null.</exception>
        public void CollectRelationalComponentTypes(List<Type> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (_relationalFieldsLookup == null)
            {
                BuildLookup();
            }

            foreach (KeyValuePair<Type, RelationalFieldMetadata[]> pair in _relationalFieldsLookup)
            {
                Type componentType = pair.Key;
                if (componentType == null)
                {
                    continue;
                }

                if (pair.Value != null && pair.Value.Length > 0)
                {
                    destination.Add(componentType);
                }
            }
        }

        public bool TryGetResolvedRelationalFields(
            Type type,
            out ResolvedRelationalFieldMetadata[] relationalFields
        )
        {
            if (_resolvedRelationalFieldsLookup == null)
            {
                BuildLookup();
            }
            return _resolvedRelationalFieldsLookup.TryGetValue(type, out relationalFields);
        }

        public bool TryGetElementType(Type componentType, string fieldName, out Type elementType)
        {
            if (_elementTypeLookup == null)
            {
                BuildLookup();
            }
            return _elementTypeLookup.TryGetValue(
                new ElementTypeKey(componentType, fieldName),
                out elementType
            );
        }

        private static bool TryResolveType(string typeName, out Type type)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                type = null;
                return false;
            }

            lock (_typeResolutionLock)
            {
                if (_resolvedTypeCache.TryGetValue(typeName, out type))
                {
                    return type != null;
                }

                type = ReflectionHelpers.TryResolveType(typeName);
                _resolvedTypeCache[typeName] = type;
                return type != null;
            }
        }

        private static void LogMissingType(string typeName, string context)
        {
            if (string.IsNullOrWhiteSpace(typeName) || string.IsNullOrWhiteSpace(context))
            {
                return;
            }

            string key = string.Concat(context, ":", typeName);
            lock (_missingTypeLock)
            {
                if (!_loggedMissingTypeKeys.Add(key))
                {
                    return;
                }
            }

            Debug.LogWarning(
                string.Format(
                    "AttributeMetadataCache: Unable to resolve {0} type '{1}'. The cached entry will be ignored. Regenerate the cache to refresh the metadata.",
                    context,
                    typeName
                )
            );
        }

#if UNITY_EDITOR
        public void SetMetadata(
            string[] allAttributeNames,
            TypeFieldMetadata[] typeMetadata,
            RelationalTypeMetadata[] relationalTypeMetadata,
            AutoLoadSingletonEntry[] autoLoadSingletons
        )
        {
            string[] normalizedAttributeNames = SortAttributeNames(allAttributeNames);
            TypeFieldMetadata[] normalizedTypeMetadata = SortTypeMetadata(typeMetadata);
            RelationalTypeMetadata[] normalizedRelationalMetadata = SortRelationalTypeMetadata(
                relationalTypeMetadata
            );
            AutoLoadSingletonEntry[] normalizedAutoLoad = SortAutoLoadSingletonEntries(
                autoLoadSingletons
            );

            _allAttributeNames = normalizedAttributeNames;
            _typeMetadata = normalizedTypeMetadata;
            _relationalTypeMetadata = normalizedRelationalMetadata;
            _autoLoadSingletons = normalizedAutoLoad;
            _computedAllAttributeNames = null;
            _computedAllAttributeNamesIncludesTests = false;
            _typeFieldsLookup = null;
            _relationalFieldsLookup = null;
            _resolvedRelationalFieldsLookup = null;
            _elementTypeLookup = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private static string[] SortAttributeNames(string[] attributeNames)
        {
            if (attributeNames == null || attributeNames.Length == 0)
            {
                return Array.Empty<string>();
            }

            string[] result = new string[attributeNames.Length];
            Array.Copy(attributeNames, result, attributeNames.Length);
            Array.Sort(result, StringComparer.Ordinal);
            return result;
        }

        private static TypeFieldMetadata[] SortTypeMetadata(TypeFieldMetadata[] typeMetadata)
        {
            if (typeMetadata == null || typeMetadata.Length == 0)
            {
                return Array.Empty<TypeFieldMetadata>();
            }

            int nonNullCount = 0;
            for (int i = 0; i < typeMetadata.Length; i++)
            {
                if (typeMetadata[i] != null)
                {
                    nonNullCount++;
                }
            }

            if (nonNullCount == 0)
            {
                return Array.Empty<TypeFieldMetadata>();
            }

            TypeFieldMetadata[] result = new TypeFieldMetadata[nonNullCount];
            int resultIndex = 0;
            for (int i = 0; i < typeMetadata.Length; i++)
            {
                TypeFieldMetadata metadata = typeMetadata[i];
                if (metadata == null)
                {
                    continue;
                }

                string typeName = metadata.typeName ?? string.Empty;
                string[] fieldNames = metadata.fieldNames ?? Array.Empty<string>();
                string[] sortedFieldNames =
                    fieldNames.Length == 0 ? Array.Empty<string>() : CopyAndSort(fieldNames);

                result[resultIndex] = new TypeFieldMetadata(typeName, sortedFieldNames);
                resultIndex++;
            }

            Array.Sort(result, CompareTypeFieldMetadata);
            return result;
        }

        private static RelationalTypeMetadata[] SortRelationalTypeMetadata(
            RelationalTypeMetadata[] relationalTypeMetadata
        )
        {
            if (relationalTypeMetadata == null || relationalTypeMetadata.Length == 0)
            {
                return Array.Empty<RelationalTypeMetadata>();
            }

            int nonNullCount = 0;
            for (int i = 0; i < relationalTypeMetadata.Length; i++)
            {
                if (relationalTypeMetadata[i] != null)
                {
                    nonNullCount++;
                }
            }

            if (nonNullCount == 0)
            {
                return Array.Empty<RelationalTypeMetadata>();
            }

            RelationalTypeMetadata[] result = new RelationalTypeMetadata[nonNullCount];
            int resultIndex = 0;
            for (int i = 0; i < relationalTypeMetadata.Length; i++)
            {
                RelationalTypeMetadata metadata = relationalTypeMetadata[i];
                if (metadata == null)
                {
                    continue;
                }

                string typeName = metadata.typeName ?? string.Empty;
                RelationalFieldMetadata[] sortedFields = SortRelationalFields(metadata.fields);
                result[resultIndex] = new RelationalTypeMetadata(typeName, sortedFields);
                resultIndex++;
            }

            Array.Sort(result, CompareRelationalTypeMetadata);
            return result;
        }

        private static RelationalFieldMetadata[] SortRelationalFields(
            RelationalFieldMetadata[] relationalFields
        )
        {
            if (relationalFields == null || relationalFields.Length == 0)
            {
                return Array.Empty<RelationalFieldMetadata>();
            }

            int nonNullCount = 0;
            for (int i = 0; i < relationalFields.Length; i++)
            {
                if (relationalFields[i] != null)
                {
                    nonNullCount++;
                }
            }

            if (nonNullCount == 0)
            {
                return Array.Empty<RelationalFieldMetadata>();
            }

            RelationalFieldMetadata[] result = new RelationalFieldMetadata[nonNullCount];
            int resultIndex = 0;
            for (int i = 0; i < relationalFields.Length; i++)
            {
                RelationalFieldMetadata field = relationalFields[i];
                if (field == null)
                {
                    continue;
                }

                result[resultIndex] = new RelationalFieldMetadata(
                    field.fieldName,
                    field.attributeKind,
                    field.fieldKind,
                    field.elementTypeName,
                    field.isInterface
                );
                resultIndex++;
            }

            Array.Sort(result, CompareRelationalFieldMetadata);
            return result;
        }

        private static string[] CopyAndSort(string[] values)
        {
            string[] result = new string[values.Length];
            Array.Copy(values, result, values.Length);
            Array.Sort(result, StringComparer.Ordinal);
            return result;
        }

        private static int CompareTypeFieldMetadata(TypeFieldMetadata left, TypeFieldMetadata right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            int typeNameComparison = string.CompareOrdinal(left.typeName, right.typeName);
            if (typeNameComparison != 0)
            {
                return typeNameComparison;
            }

            string[] leftFields = left.fieldNames ?? Array.Empty<string>();
            string[] rightFields = right.fieldNames ?? Array.Empty<string>();

            int lengthComparison = leftFields.Length.CompareTo(rightFields.Length);
            if (lengthComparison != 0)
            {
                return lengthComparison;
            }

            for (int i = 0; i < leftFields.Length; i++)
            {
                int fieldComparison = string.CompareOrdinal(leftFields[i], rightFields[i]);
                if (fieldComparison != 0)
                {
                    return fieldComparison;
                }
            }

            return 0;
        }

        private static int CompareRelationalTypeMetadata(
            RelationalTypeMetadata left,
            RelationalTypeMetadata right
        )
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            int typeNameComparison = string.CompareOrdinal(left.typeName, right.typeName);
            if (typeNameComparison != 0)
            {
                return typeNameComparison;
            }

            RelationalFieldMetadata[] leftFields =
                left.fields ?? Array.Empty<RelationalFieldMetadata>();
            RelationalFieldMetadata[] rightFields =
                right.fields ?? Array.Empty<RelationalFieldMetadata>();

            int lengthComparison = leftFields.Length.CompareTo(rightFields.Length);
            if (lengthComparison != 0)
            {
                return lengthComparison;
            }

            for (int i = 0; i < leftFields.Length; i++)
            {
                int fieldComparison = CompareRelationalFieldMetadata(leftFields[i], rightFields[i]);
                if (fieldComparison != 0)
                {
                    return fieldComparison;
                }
            }

            return 0;
        }

        private static int CompareRelationalFieldMetadata(
            RelationalFieldMetadata left,
            RelationalFieldMetadata right
        )
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            int fieldNameComparison = string.CompareOrdinal(left.fieldName, right.fieldName);
            if (fieldNameComparison != 0)
            {
                return fieldNameComparison;
            }

            int attributeComparison = left.attributeKind.CompareTo(right.attributeKind);
            if (attributeComparison != 0)
            {
                return attributeComparison;
            }

            int fieldKindComparison = left.fieldKind.CompareTo(right.fieldKind);
            if (fieldKindComparison != 0)
            {
                return fieldKindComparison;
            }

            int elementTypeComparison = string.CompareOrdinal(
                left.elementTypeName,
                right.elementTypeName
            );
            if (elementTypeComparison != 0)
            {
                return elementTypeComparison;
            }

            if (left.isInterface == right.isInterface)
            {
                return 0;
            }

            return left.isInterface ? -1 : 1;
        }

        private static AutoLoadSingletonEntry[] SortAutoLoadSingletonEntries(
            AutoLoadSingletonEntry[] entries
        )
        {
            if (entries == null || entries.Length == 0)
            {
                return Array.Empty<AutoLoadSingletonEntry>();
            }

            List<AutoLoadSingletonEntry> result = new(entries.Length);
            foreach (AutoLoadSingletonEntry entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.typeName))
                {
                    continue;
                }

                result.Add(new AutoLoadSingletonEntry(entry.typeName, entry.kind, entry.loadType));
            }

            if (result.Count == 0)
            {
                return Array.Empty<AutoLoadSingletonEntry>();
            }

            result.Sort((left, right) => string.CompareOrdinal(left.typeName, right.typeName));
            return result.ToArray();
        }
#endif
    }
}
