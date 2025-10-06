namespace WallstopStudios.UnityHelpers.Tags
{
    using System;
    using System.Collections.Generic;
    using Core.Attributes;
    using UnityEngine;
    using Utils;

    /// <summary>
    /// Serialized cache of attribute metadata to avoid runtime reflection.
    /// This asset is automatically generated in the Editor.
    /// </summary>
    [ScriptableSingletonPath("WallstopStudios/AttributeMetadataCache")]
    public sealed class AttributeMetadataCache : ScriptableObjectSingleton<AttributeMetadataCache>
    {
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

        [SerializeField]
        private TypeFieldMetadata[] _typeMetadata = Array.Empty<TypeFieldMetadata>();

        [SerializeField]
        private RelationalTypeMetadata[] _relationalTypeMetadata =
            Array.Empty<RelationalTypeMetadata>();

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
                return componentType == other.componentType && fieldName == other.fieldName;
            }

            public override bool Equals(object obj)
            {
                return obj is ElementTypeKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((componentType?.GetHashCode() ?? 0) * 397)
                        ^ (fieldName?.GetHashCode() ?? 0);
                }
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

        public string[] AllAttributeNames => _allAttributeNames;

        private void OnEnable()
        {
            BuildLookup();
        }

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

                type = Type.GetType(typeName, throwOnError: false, ignoreCase: false);

                if (type == null && !typeName.Contains(','))
                {
                    foreach (
                        System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()
                    )
                    {
                        type = assembly.GetType(typeName, throwOnError: false, ignoreCase: false);
                        if (type != null)
                        {
                            break;
                        }
                    }
                }

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
            RelationalTypeMetadata[] relationalTypeMetadata
        )
        {
            _allAttributeNames = allAttributeNames;
            _typeMetadata = typeMetadata;
            _relationalTypeMetadata = relationalTypeMetadata;
            _typeFieldsLookup = null;
            _relationalFieldsLookup = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
