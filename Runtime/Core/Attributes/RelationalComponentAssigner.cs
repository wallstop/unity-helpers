namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using System.Collections.Generic;
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
        }

        /// <inheritdoc />
        public bool HasRelationalAssignments(Type componentType)
        {
            if (componentType == null)
            {
                return false;
            }

            AttributeMetadataCache cache = _metadataCache ?? AttributeMetadataCache.Instance;
            if (cache == null)
            {
                return false;
            }

            return cache.TryGetRelationalFields(
                    componentType,
                    out AttributeMetadataCache.RelationalFieldMetadata[] fields
                )
                && fields.Length > 0;
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
