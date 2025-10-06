namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Extension;
    using Helper;
    using Tags;
    using UnityEngine;

    /// <summary>
    /// Base class for relational component attributes that provides common functionality
    /// for finding and assigning components based on hierarchy relationships.
    /// </summary>
    public abstract class BaseRelationalComponentAttribute : System.Attribute
    {
        /// <summary>
        /// If true, no error is logged when the component is not found. Default: false.
        /// </summary>
        public bool Optional { get; set; } = false;

        /// <summary>
        /// If true, includes disabled Behaviour components and components on inactive GameObjects.
        /// If false, only enabled components on active GameObjects are assigned. Default: true.
        /// </summary>
        public bool IncludeInactive { get; set; } = true;

        /// <summary>
        /// If true, skips assignment if the field already has a non-null value (for single components)
        /// or a non-empty collection (for arrays/lists). Default: false.
        /// </summary>
        public bool SkipIfAssigned { get; set; } = false;

        /// <summary>
        /// Maximum number of components to find. 0 means unlimited. Default: 0.
        /// Only applies to arrays and lists.
        /// </summary>
        public int MaxCount { get; set; } = 0;

        /// <summary>
        /// If set, only finds components on GameObjects with this tag.
        /// </summary>
        public string TagFilter { get; set; } = null;

        /// <summary>
        /// If set, only finds components on GameObjects whose names contain this string.
        /// </summary>
        public string NameFilter { get; set; } = null;

        /// <summary>
        /// If true, allows searching for interface types and base types, not just concrete Component types.
        /// Default: true.
        /// </summary>
        public bool AllowInterfaces { get; set; } = true;
    }

    /// <summary>
    /// Shared infrastructure for relational component attribute processing.
    /// </summary>
    internal static class RelationalComponentProcessor
    {
        internal enum FieldKind : byte
        {
            Single = 0,
            Array = 1,
            List = 2,
        }

        // Map from cache enum to processor enum
        private static FieldKind MapFieldKind(AttributeMetadataCache.FieldKind cacheKind)
        {
            return cacheKind switch
            {
#pragma warning disable CS0618 // Type or member is obsolete
                AttributeMetadataCache.FieldKind.None => FieldKind.Single,
#pragma warning restore CS0618
                AttributeMetadataCache.FieldKind.Single => FieldKind.Single,
                AttributeMetadataCache.FieldKind.Array => FieldKind.Array,
                AttributeMetadataCache.FieldKind.List => FieldKind.List,
                _ => FieldKind.Single,
            };
        }

        internal readonly struct FieldMetadata<TAttribute>
            where TAttribute : BaseRelationalComponentAttribute
        {
            public readonly FieldInfo field;
            public readonly TAttribute attribute;
            public readonly Action<object, object> setter;
            public readonly Func<object, object> getter;
            public readonly FieldKind kind;
            public readonly Type elementType;
            public readonly Func<int, Array> arrayCreator;
            public readonly Func<int, IList> listCreator;
            public readonly bool isInterface;

            public FieldMetadata(
                FieldInfo field,
                TAttribute attribute,
                Action<object, object> setter,
                Func<object, object> getter,
                FieldKind kind,
                Type elementType,
                Func<int, Array> arrayCreator,
                Func<int, IList> listCreator,
                bool isInterface
            )
            {
                this.field = field;
                this.attribute = attribute;
                this.setter = setter;
                this.getter = getter;
                this.kind = kind;
                this.elementType = elementType;
                this.arrayCreator = arrayCreator;
                this.listCreator = listCreator;
                this.isInterface = isInterface;
            }
        }

        internal static FieldMetadata<TAttribute>[] GetFieldMetadata<TAttribute>(Type componentType)
            where TAttribute : BaseRelationalComponentAttribute
        {
            // Try to use cached metadata first
            AttributeMetadataCache cache = AttributeMetadataCache.Instance;
            AttributeMetadataCache.RelationalAttributeKind targetKind =
                GetRelationalKind<TAttribute>();

            if (
                cache != null
                && cache.TryGetRelationalFields(
                    componentType,
                    out AttributeMetadataCache.RelationalFieldMetadata[] cachedFields
                )
            )
            {
                // Filter cached fields by attribute type
                List<FieldMetadata<TAttribute>> result = new List<FieldMetadata<TAttribute>>();

                foreach (AttributeMetadataCache.RelationalFieldMetadata cachedField in cachedFields)
                {
                    if (cachedField.attributeKind != targetKind)
                    {
                        continue;
                    }

                    // Get the field info
                    FieldInfo field = componentType.GetField(
                        cachedField.fieldName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );

                    if (field == null)
                    {
                        continue;
                    }

                    // Get the attribute
                    if (!field.IsAttributeDefined(out TAttribute attribute, inherit: false))
                    {
                        continue;
                    }

                    // Get pre-resolved element type from cache
                    if (
                        !cache.TryGetElementType(
                            componentType,
                            cachedField.fieldName,
                            out Type elementType
                        )
                    )
                    {
                        continue;
                    }

                    FieldKind kind = MapFieldKind(cachedField.fieldKind);
                    Func<int, Array> arrayCreator = null;
                    Func<int, IList> listCreator = null;

                    if (kind == FieldKind.Array)
                    {
                        arrayCreator = ReflectionHelpers.GetArrayCreator(elementType);
                    }
                    else if (kind == FieldKind.List)
                    {
                        listCreator = ReflectionHelpers.GetListWithCapacityCreator(elementType);
                    }

                    result.Add(
                        new FieldMetadata<TAttribute>(
                            field,
                            attribute,
                            ReflectionHelpers.GetFieldSetter(field),
                            ReflectionHelpers.GetFieldGetter(field),
                            kind,
                            elementType,
                            arrayCreator,
                            listCreator,
                            cachedField.isInterface
                        )
                    );
                }

                return result.ToArray();
            }

            // Fallback to runtime reflection
            FieldInfo[] fields = componentType.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            return fields
                .Select(field =>
                {
                    if (!field.IsAttributeDefined(out TAttribute attribute, inherit: false))
                    {
                        return null;
                    }

                    Type fieldType = field.FieldType;
                    FieldKind kind;
                    Type elementType;
                    Func<int, Array> arrayCreator = null;
                    Func<int, IList> listCreator = null;

                    if (fieldType.IsArray)
                    {
                        kind = FieldKind.Array;
                        elementType = fieldType.GetElementType();
                        arrayCreator = ReflectionHelpers.GetArrayCreator(elementType);
                    }
                    else if (
                        fieldType.IsGenericType
                        && fieldType.GetGenericTypeDefinition() == typeof(List<>)
                    )
                    {
                        kind = FieldKind.List;
                        elementType = fieldType.GenericTypeArguments[0];
                        listCreator = ReflectionHelpers.GetListWithCapacityCreator(elementType);
                    }
                    else
                    {
                        kind = FieldKind.Single;
                        elementType = fieldType;
                    }

                    bool isInterface =
                        elementType.IsInterface
                        || (!elementType.IsSealed && elementType != typeof(Component));

                    return (FieldMetadata<TAttribute>?)
                        new FieldMetadata<TAttribute>(
                            field,
                            attribute,
                            ReflectionHelpers.GetFieldSetter(field),
                            ReflectionHelpers.GetFieldGetter(field),
                            kind,
                            elementType,
                            arrayCreator,
                            listCreator,
                            isInterface
                        );
                })
                .Where(nullable => nullable.HasValue)
                .Select(nullable => nullable.Value)
                .ToArray();
        }

        private static AttributeMetadataCache.RelationalAttributeKind GetRelationalKind<TAttribute>()
            where TAttribute : BaseRelationalComponentAttribute
        {
            Type attributeType = typeof(TAttribute);

            if (attributeType == typeof(ParentComponentAttribute))
            {
                return AttributeMetadataCache.RelationalAttributeKind.Parent;
            }
            else if (attributeType == typeof(ChildComponentAttribute))
            {
                return AttributeMetadataCache.RelationalAttributeKind.Child;
            }
            else if (attributeType == typeof(SiblingComponentAttribute))
            {
                return AttributeMetadataCache.RelationalAttributeKind.Sibling;
            }

#pragma warning disable CS0618 // Type or member is obsolete
            return AttributeMetadataCache.RelationalAttributeKind.Unknown;
#pragma warning restore CS0618
        }

        internal static bool ShouldSkipAssignment<TAttribute>(
            FieldMetadata<TAttribute> metadata,
            object component
        )
            where TAttribute : BaseRelationalComponentAttribute
        {
            if (!metadata.attribute.SkipIfAssigned)
            {
                return false;
            }

            object currentValue = metadata.getter(component);
            return ValueHelpers.IsAssigned(currentValue);
        }

        internal static bool MatchesFilters(
            GameObject gameObject,
            BaseRelationalComponentAttribute attribute
        )
        {
            if (attribute.TagFilter != null && !gameObject.CompareTag(attribute.TagFilter))
            {
                return false;
            }

            if (attribute.NameFilter != null && !gameObject.name.Contains(attribute.NameFilter))
            {
                return false;
            }

            return true;
        }

        internal static void LogMissingComponentError<TAttribute>(
            Component component,
            FieldMetadata<TAttribute> metadata,
            string relationshipType
        )
            where TAttribute : BaseRelationalComponentAttribute
        {
            if (!metadata.attribute.Optional)
            {
                component.LogError(
                    $"Unable to find {relationshipType} component of type {metadata.field.FieldType} for field '{metadata.field.Name}'"
                );
            }
        }

        internal static void SetEmptyCollection<TAttribute>(
            Component component,
            FieldMetadata<TAttribute> metadata
        )
            where TAttribute : BaseRelationalComponentAttribute
        {
            switch (metadata.kind)
            {
                case FieldKind.Array:
                    metadata.setter(component, metadata.arrayCreator(0));
                    break;
                case FieldKind.List:
                    metadata.setter(component, metadata.listCreator(0));
                    break;
            }
        }

        internal static List<Component> FilterComponents(
            IReadOnlyList<Component> components,
            BaseRelationalComponentAttribute attribute,
            Type elementType,
            bool isInterface,
            List<Component> filtered
        )
        {
            filtered.Clear();
            int maxCount = attribute.MaxCount > 0 ? attribute.MaxCount : int.MaxValue;

            for (int i = 0; i < components.Count; i++)
            {
                Component comp = components[i];
                if (filtered.Count >= maxCount)
                {
                    break;
                }

                if (comp == null)
                {
                    continue;
                }

                // Check if component matches the type (handle interfaces/base types)
                if (isInterface)
                {
                    if (!attribute.AllowInterfaces)
                    {
                        // Skip interfaces/base types when not allowed
                        continue;
                    }

                    if (!elementType.IsAssignableFrom(comp.GetType()))
                    {
                        continue;
                    }
                }
                else
                {
                    // For concrete types, ensure exact type match
                    if (!elementType.IsAssignableFrom(comp.GetType()))
                    {
                        continue;
                    }
                }

                // Check active state
                if (!attribute.IncludeInactive)
                {
                    if (!comp.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    if (!comp.IsComponentEnabled())
                    {
                        continue;
                    }
                }

                // Check filters
                if (!MatchesFilters(comp.gameObject, attribute))
                {
                    continue;
                }

                filtered.Add(comp);
            }

            return filtered;
        }

        internal static List<Component> GetComponentsOfType(
            GameObject gameObject,
            Type elementType,
            bool isInterface,
            bool allowInterfaces,
            List<Component> buffer
        )
        {
            buffer.Clear();
            if (isInterface && allowInterfaces)
            {
                // For interfaces and base types, we need to get all components and filter
                Component[] allComponents = gameObject.GetComponents<Component>();
                foreach (Component comp in allComponents)
                {
                    if (elementType.IsAssignableFrom(comp.GetType()))
                    {
                        buffer.Add(comp);
                    }
                }
            }
            else
            {
                buffer.AddRange(gameObject.GetComponents(elementType));
            }

            return buffer;
        }
    }
}
