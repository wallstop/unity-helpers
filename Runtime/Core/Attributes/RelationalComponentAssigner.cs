namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Tags;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// Default implementation of <see cref="IRelationalComponentAssigner"/> that delegates to the
    /// existing relational component extensions.
    /// </summary>
    public sealed class RelationalComponentAssigner : IRelationalComponentAssigner
    {
        private readonly AttributeMetadataCache _metadataCache;
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
                // Prefer ReflectionHelpers so Editor TypeCache can accelerate lookups
                bool has =
                    Helper
                        .ReflectionHelpers.GetFieldsWithAttribute<ParentComponentAttribute>(current)
                        .Any()
                    || Helper
                        .ReflectionHelpers.GetFieldsWithAttribute<ChildComponentAttribute>(current)
                        .Any()
                    || Helper
                        .ReflectionHelpers.GetFieldsWithAttribute<SiblingComponentAttribute>(
                            current
                        )
                        .Any();

                if (has)
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
