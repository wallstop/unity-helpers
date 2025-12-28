// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using System.Collections.Generic;
    using Helper;
    using Tags;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// Default implementation of <see cref="IRelationalComponentAssigner"/> that delegates to the
    /// existing relational component extensions.
    /// </summary>
    /// <remarks>
    /// Thread-safety note: The <c>_metadataCache</c> reference is assigned once during construction and never changed.
    /// The <see cref="AttributeMetadataCache"/> instance itself is thread-safe for concurrent reads, as its internal
    /// dictionaries are only populated during static initialization before any instance is exposed.
    /// The <c>_hasAssignmentsCache</c> dictionary is protected by <c>_cacheLock</c> for concurrent access.
    /// </remarks>
    public sealed class RelationalComponentAssigner : IRelationalComponentAssigner
    {
        // Immutable after construction - assigned in constructor and never modified.
        // The AttributeMetadataCache instance is thread-safe for reads after initialization.
        private readonly AttributeMetadataCache _metadataCache;

        // Guarded by _cacheLock for all access.
        private readonly Dictionary<Type, bool> _hasAssignmentsCache;
        private readonly object _cacheLock = new();

        /// <summary>
        /// Creates a new assigner using the active <see cref="AttributeMetadataCache.Instance"/>.
        /// </summary>
        public RelationalComponentAssigner()
            : this(AttributeMetadataCache.Instance) { }

        /// <summary>
        /// Creates a new assigner using the supplied metadata cache.
        /// </summary>
        public RelationalComponentAssigner(AttributeMetadataCache metadataCache)
        {
            _metadataCache = metadataCache;
            _hasAssignmentsCache = new Dictionary<Type, bool>();
        }

        /// <inheritdoc />
        public bool HasRelationalAssignments(Type componentType)
        {
            if (componentType == null)
            {
                return false;
            }

            lock (_cacheLock)
            {
                if (_hasAssignmentsCache.TryGetValue(componentType, out bool cachedResult))
                {
                    return cachedResult;
                }
            }

            AttributeMetadataCache cache = _metadataCache ?? AttributeMetadataCache.Instance;
            if (cache == null)
            {
                bool reflectionResult = HasRelationalAttributesViaReflection(componentType);
                StoreCacheResult(componentType, reflectionResult);
                return reflectionResult;
            }

            Type current = componentType;
            while (current != null && typeof(Component).IsAssignableFrom(current))
            {
                if (
                    cache.TryGetRelationalFields(
                        current,
                        out AttributeMetadataCache.RelationalFieldMetadata[] fields
                    )
                    && fields.Length > 0
                )
                {
                    StoreCacheResult(componentType, true);
                    return true;
                }
                current = current.BaseType;
            }

            // Fallback: inspect fields via reflection to detect relational attributes
            bool result = HasRelationalAttributesViaReflection(componentType);
            StoreCacheResult(componentType, result);
            return result;
        }

        private static readonly Type[] RelationalAttributeTypes =
        {
            typeof(ParentComponentAttribute),
            typeof(ChildComponentAttribute),
            typeof(SiblingComponentAttribute),
        };

        private void StoreCacheResult(Type componentType, bool result)
        {
            lock (_cacheLock)
            {
                _hasAssignmentsCache[componentType] = result;
            }
        }

        private static bool HasRelationalAttributesViaReflection(Type componentType)
        {
            Type current = componentType;
            while (current != null && typeof(Component).IsAssignableFrom(current))
            {
                // IsDefined checks for exact attribute types, not derived types.
                // Must check each concrete relational attribute type separately.
                if (current.HasAnyFieldWithAttributes(RelationalAttributeTypes))
                {
                    return true;
                }

                current = current.BaseType;
            }

            return false;
        }

        /// <inheritdoc />
        public void Assign(Component component)
        {
            if (component == null)
            {
                return;
            }

            if (!HasRelationalAssignments(component.GetType()))
            {
                return;
            }

            component.AssignRelationalComponents();
        }

        /// <inheritdoc />
        public void Assign(IEnumerable<Component> components)
        {
            if (components == null)
            {
                return;
            }

            if (components is IReadOnlyList<Component> readonlyList)
            {
                for (int i = 0; i < readonlyList.Count; i++)
                {
                    Assign(readonlyList[i]);
                }
                return;
            }

            foreach (Component component in components)
            {
                Assign(component);
            }
        }

        /// <inheritdoc />
        public void AssignHierarchy(GameObject root, bool includeInactiveChildren = true)
        {
            if (root == null)
            {
                return;
            }

            using PooledResource<List<Component>> componentBuffer = Buffers<Component>.List.Get(
                out List<Component> components
            );

            root.GetComponentsInChildren(includeInactiveChildren, components);
            Assign(components);
        }
    }
}
