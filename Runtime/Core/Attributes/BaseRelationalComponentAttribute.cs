namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using Extension;
    using Helper;
    using Tags;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Utils;

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

            HashSet = 3,
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
                AttributeMetadataCache.FieldKind.HashSet => FieldKind.HashSet,
                _ => FieldKind.Single,
            };
        }

        private static FieldKind GetFieldKind(Type fieldType, out Type elementType)
        {
            if (fieldType == null)
            {
                elementType = null;
                return FieldKind.Single;
            }

            if (fieldType.IsArray)
            {
                elementType = fieldType.GetElementType();
                return FieldKind.Array;
            }

            if (fieldType.IsGenericType)
            {
                Type genericType = fieldType.GetGenericTypeDefinition();
                if (genericType == typeof(List<>))
                {
                    elementType = fieldType.GenericTypeArguments[0];
                    return FieldKind.List;
                }

                if (genericType == typeof(HashSet<>))
                {
                    elementType = fieldType.GenericTypeArguments[0];
                    return FieldKind.HashSet;
                }
            }

            elementType = fieldType;
            return FieldKind.Single;
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

            public readonly Func<int, object> hashSetCreator;

            public readonly Action<object, object> hashSetAdder;

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
                Func<int, object> hashSetCreator,
                Action<object, object> hashSetAdder,
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

                this.hashSetCreator = hashSetCreator;

                this.hashSetAdder = hashSetAdder;

                this.isInterface = isInterface;
            }
        }

        internal static FieldMetadata<TAttribute>[] GetFieldMetadata<TAttribute>(Type componentType)
            where TAttribute : BaseRelationalComponentAttribute
        {
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
                List<FieldMetadata<TAttribute>> result = new List<FieldMetadata<TAttribute>>();

                foreach (AttributeMetadataCache.RelationalFieldMetadata cachedField in cachedFields)
                {
                    if (cachedField.attributeKind != targetKind)
                    {
                        continue;
                    }

                    FieldInfo field = componentType.GetField(
                        cachedField.fieldName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );

                    if (field == null)
                    {
                        continue;
                    }

                    if (!field.IsAttributeDefined(out TAttribute attribute, inherit: false))
                    {
                        continue;
                    }

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
                    FieldKind actualKind = GetFieldKind(
                        field.FieldType,
                        out Type actualElementType
                    );

                    if (kind != actualKind)
                    {
                        kind = actualKind;
                    }

                    Type resolvedElementType = elementType ?? actualElementType ?? field.FieldType;

                    if (
                        kind == FieldKind.HashSet
                        && field.FieldType.IsGenericType
                        && field.FieldType.GetGenericTypeDefinition() == typeof(HashSet<>)
                        && resolvedElementType == field.FieldType
                    )
                    {
                        resolvedElementType = field.FieldType.GenericTypeArguments[0];
                    }

                    Func<int, Array> arrayCreator = null;
                    Func<int, IList> listCreator = null;
                    Func<int, object> hashSetCreator = null;
                    Action<object, object> hashSetAdder = null;

                    switch (kind)
                    {
                        case FieldKind.Array:
                            arrayCreator = ReflectionHelpers.GetArrayCreator(resolvedElementType);
                            break;
                        case FieldKind.List:
                            listCreator = ReflectionHelpers.GetListWithCapacityCreator(
                                resolvedElementType
                            );
                            break;
                        case FieldKind.HashSet:
                            hashSetCreator = ReflectionHelpers.GetHashSetWithCapacityCreator(
                                resolvedElementType
                            );
                            hashSetAdder = ReflectionHelpers.GetHashSetAdder(resolvedElementType);
                            break;
                    }

                    bool isInterface =
                        resolvedElementType != null
                        && (
                            resolvedElementType.IsInterface
                            || (
                                !resolvedElementType.IsSealed
                                && resolvedElementType != typeof(Component)
                            )
                        );

                    result.Add(
                        new FieldMetadata<TAttribute>(
                            field,
                            attribute,
                            ReflectionHelpers.GetFieldSetter(field),
                            ReflectionHelpers.GetFieldGetter(field),
                            kind,
                            resolvedElementType,
                            arrayCreator,
                            listCreator,
                            hashSetCreator,
                            hashSetAdder,
                            isInterface
                        )
                    );
                }

                return result.ToArray();
            }

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
                    FieldKind kind = GetFieldKind(fieldType, out Type elementType);

                    Func<int, Array> arrayCreator = null;
                    Func<int, IList> listCreator = null;
                    Func<int, object> hashSetCreator = null;
                    Action<object, object> hashSetAdder = null;

                    switch (kind)
                    {
                        case FieldKind.Array:
                            arrayCreator = ReflectionHelpers.GetArrayCreator(elementType);
                            break;
                        case FieldKind.List:
                            listCreator = ReflectionHelpers.GetListWithCapacityCreator(elementType);
                            break;
                        case FieldKind.HashSet:
                            hashSetCreator = ReflectionHelpers.GetHashSetWithCapacityCreator(
                                elementType
                            );
                            hashSetAdder = ReflectionHelpers.GetHashSetAdder(elementType);
                            break;
                    }

                    bool isInterface =
                        elementType != null
                        && (
                            elementType.IsInterface
                            || (!elementType.IsSealed && elementType != typeof(Component))
                        );

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
                            hashSetCreator,
                            hashSetAdder,
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

        private static bool MatchesFilters(
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

                case FieldKind.HashSet:

                    metadata.setter(component, metadata.hashSetCreator(0));

                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool PassesStateAndFilters(
            Component candidate,
            BaseRelationalComponentAttribute attribute,
            bool filterDisabledComponents = true
        )
        {
            if (candidate == null)
            {
                return false;
            }

            GameObject candidateGameObject = candidate.gameObject;

            if (!attribute.IncludeInactive)
            {
                if (!candidateGameObject.activeInHierarchy)
                {
                    return false;
                }

                if (filterDisabledComponents && !candidate.IsComponentEnabled())
                {
                    return false;
                }
            }

            return MatchesFilters(candidateGameObject, attribute);
        }

        private static bool ComponentMatches(
            Component candidate,
            BaseRelationalComponentAttribute attribute,
            Type elementType,
            bool isInterface,
            bool filterDisabledComponents = true
        )
        {
            if (candidate == null)
            {
                return false;
            }

            Type candidateType = candidate.GetType();

            if (isInterface)
            {
                if (!attribute.AllowInterfaces)
                {
                    return false;
                }
            }

            if (!elementType.IsAssignableFrom(candidateType))
            {
                return false;
            }

            return PassesStateAndFilters(candidate, attribute, filterDisabledComponents);
        }

        internal static int FilterComponentsInPlace(
            List<Component> components,
            BaseRelationalComponentAttribute attribute,
            Type elementType,
            bool isInterface,
            bool filterDisabledComponents = true
        )
        {
            int maxCount = attribute.MaxCount > 0 ? attribute.MaxCount : int.MaxValue;

            int writeIndex = 0;

            int count = components.Count;

            if (!isInterface)
            {
                for (int readIndex = 0; readIndex < count; readIndex++)
                {
                    Component comp = components[readIndex];

                    if (PassesStateAndFilters(comp, attribute, filterDisabledComponents))
                    {
                        components[writeIndex++] = comp;

                        if (writeIndex >= maxCount)
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                for (int readIndex = 0; readIndex < count; readIndex++)
                {
                    Component comp = components[readIndex];

                    if (
                        ComponentMatches(
                            comp,
                            attribute,
                            elementType,
                            isInterface,
                            filterDisabledComponents
                        )
                    )
                    {
                        components[writeIndex++] = comp;

                        if (writeIndex >= maxCount)
                        {
                            break;
                        }
                    }
                }
            }

            if (writeIndex < components.Count)
            {
                components.RemoveRange(writeIndex, components.Count - writeIndex);
            }

            return writeIndex;
        }

        internal static Component TryResolveSingleComponent(
            GameObject gameObject,
            BaseRelationalComponentAttribute attribute,
            Type elementType,
            bool isInterface,
            bool allowInterfaces,
            List<Component> scratch,
            bool filterDisabledComponents = true
        )
        {
            if (!isInterface)
            {
                if (
                    gameObject.TryGetComponent(elementType, out Component candidate)
                    && PassesStateAndFilters(candidate, attribute, filterDisabledComponents)
                )
                {
                    return candidate;
                }

                if (scratch != null)
                {
                    scratch.Clear();

                    gameObject.GetComponents(elementType, scratch);

                    for (int i = 0; i < scratch.Count; i++)
                    {
                        candidate = scratch[i];

                        if (PassesStateAndFilters(candidate, attribute, filterDisabledComponents))
                        {
                            return candidate;
                        }
                    }

                    return null;
                }

                using PooledResource<List<Component>> pooled = Buffers<Component>.List.Get(
                    out List<Component> components
                );

                gameObject.GetComponents(elementType, components);

                for (int i = 0; i < components.Count; i++)
                {
                    candidate = components[i];

                    if (PassesStateAndFilters(candidate, attribute, filterDisabledComponents))
                    {
                        return candidate;
                    }
                }

                return null;
            }

            if (!allowInterfaces)
            {
                return null;
            }

            if (scratch != null)
            {
                scratch.Clear();

                gameObject.GetComponents(typeof(Component), scratch);

                for (int i = 0; i < scratch.Count; i++)
                {
                    Component candidate = scratch[i];

                    if (
                        candidate != null
                        && elementType.IsAssignableFrom(candidate.GetType())
                        && PassesStateAndFilters(candidate, attribute, filterDisabledComponents)
                    )
                    {
                        return candidate;
                    }
                }

                return null;
            }
            else
            {
                using PooledResource<List<Component>> pooled = Buffers<Component>.List.Get(
                    out List<Component> components
                );

                gameObject.GetComponents(typeof(Component), components);

                for (int i = 0; i < components.Count; i++)
                {
                    Component candidate = components[i];

                    if (
                        candidate != null
                        && elementType.IsAssignableFrom(candidate.GetType())
                        && PassesStateAndFilters(candidate, attribute, filterDisabledComponents)
                    )
                    {
                        return candidate;
                    }
                }

                return null;
            }
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

            if (isInterface)
            {
                if (!allowInterfaces)
                {
                    return buffer;
                }

                gameObject.GetComponents(typeof(Component), buffer);

                int writeIndex = 0;

                int count = buffer.Count;

                for (int i = 0; i < count; i++)
                {
                    Component comp = buffer[i];

                    if (comp != null && elementType.IsAssignableFrom(comp.GetType()))
                    {
                        buffer[writeIndex++] = comp;
                    }
                }

                if (writeIndex < buffer.Count)
                {
                    buffer.RemoveRange(writeIndex, buffer.Count - writeIndex);
                }

                return buffer;
            }

            gameObject.GetComponents(elementType, buffer);

            return buffer;
        }
    }
}
