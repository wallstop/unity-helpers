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

        // Runtime lookups using Type as key (much faster than string comparison)
        private Dictionary<Type, string[]> _typeFieldsLookup;
        private Dictionary<Type, RelationalFieldMetadata[]> _relationalFieldsLookup;
        private Dictionary<ElementTypeKey, Type> _elementTypeLookup; // Cache resolved element types

        public string[] AllAttributeNames => _allAttributeNames;

        private void OnEnable()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            // Build Type-based lookups for fast runtime access
            _typeFieldsLookup = new Dictionary<Type, string[]>(_typeMetadata.Length);
            foreach (TypeFieldMetadata metadata in _typeMetadata)
            {
                Type type = Type.GetType(metadata.typeName);
                if (type != null)
                {
                    _typeFieldsLookup[type] = metadata.fieldNames;
                }
            }

            _relationalFieldsLookup = new Dictionary<Type, RelationalFieldMetadata[]>(
                _relationalTypeMetadata.Length
            );
            _elementTypeLookup = new Dictionary<ElementTypeKey, Type>();

            foreach (RelationalTypeMetadata metadata in _relationalTypeMetadata)
            {
                Type type = Type.GetType(metadata.typeName);
                if (type != null)
                {
                    _relationalFieldsLookup[type] = metadata.fields;

                    // Pre-resolve element types
                    foreach (RelationalFieldMetadata field in metadata.fields)
                    {
                        if (!string.IsNullOrEmpty(field.elementTypeName))
                        {
                            Type elementType = Type.GetType(field.elementTypeName);
                            if (elementType != null)
                            {
                                _elementTypeLookup[new ElementTypeKey(type, field.fieldName)] =
                                    elementType;
                            }
                        }
                    }
                }
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
