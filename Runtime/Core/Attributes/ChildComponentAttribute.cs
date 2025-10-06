namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Extension;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Utils;
    using static RelationalComponentProcessor;

    /// <summary>
    /// Automatically assigns child components (components down the transform hierarchy) to the decorated field.
    /// Supports single components, arrays, and List&lt;T&gt; collection types.
    /// </summary>
    /// <remarks>
    /// Call <see cref="ChildComponentExtensions.AssignChildComponents"/> to populate the field.
    /// This is typically done in Awake() or OnEnable().
    /// By default, searches include the current GameObject. Use <see cref="OnlyDescendants"/> to exclude it.
    /// Children are searched in breadth-first order.
    ///
    /// IMPORTANT: This attribute populates fields at runtime, not during Unity serialization in Edit mode.
    /// Fields populated by this attribute will not be serialized by Unity.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ChildComponentAttribute : BaseRelationalComponentAttribute
    {
        /// <summary>
        /// If true, excludes components on the current GameObject and only searches descendant transforms.
        /// If false, includes components on the current GameObject in the search. Default: true.
        /// </summary>
        public bool OnlyDescendants { get; set; } = false;

        /// <summary>
        /// Maximum depth to search down the hierarchy. 0 means unlimited. Default: 0.
        /// Depth 1 = immediate children only, depth 2 = children and grandchildren, etc.
        /// </summary>
        public int MaxDepth { get; set; } = 0;
    }

    public static class ChildComponentExtensions
    {
        private static readonly Dictionary<
            Type,
            FieldMetadata<ChildComponentAttribute>[]
        > FieldsByType = new();

        public static void AssignChildComponents(this Component component)
        {
            Type componentType = component.GetType();
            FieldMetadata<ChildComponentAttribute>[] fields = FieldsByType.GetOrAdd(
                componentType,
                type => GetFieldMetadata<ChildComponentAttribute>(type)
            );

            foreach (FieldMetadata<ChildComponentAttribute> metadata in fields)
            {
                if (ShouldSkipAssignment(metadata, component))
                {
                    continue;
                }

                bool foundChild;

                using PooledResource<List<Transform>> childBufferResource =
                    Buffers<Transform>.List.Get();
                List<Transform> childBuffer = childBufferResource.resource;

                switch (metadata.kind)
                {
                    case FieldKind.Array:
                    {
                        using PooledResource<List<Component>> cacheResource =
                            Buffers<Component>.List.Get();
                        List<Component> cache = cacheResource.resource;

                        using PooledResource<List<Component>> filteredBuffer =
                            Buffers<Component>.List.Get(out List<Component> filtered);
                        CollectChildComponents(component, metadata, childBuffer, cache);

                        FilterComponents(
                            cache,
                            metadata.attribute,
                            metadata.elementType,
                            metadata.isInterface,
                            filtered
                        );

                        Array correctTypedArray = metadata.arrayCreator(filtered.Count);
                        for (int i = 0; i < filtered.Count; ++i)
                        {
                            correctTypedArray.SetValue(filtered[i], i);
                        }

                        metadata.setter(component, correctTypedArray);
                        foundChild = filtered.Count > 0;
                        break;
                    }
                    case FieldKind.List:
                    {
                        using PooledResource<List<Component>> cacheResource =
                            Buffers<Component>.List.Get();
                        List<Component> cache = cacheResource.resource;

                        CollectChildComponents(component, metadata, childBuffer, cache);

                        using PooledResource<List<Component>> filteredBuffer =
                            Buffers<Component>.List.Get(out List<Component> filtered);
                        FilterComponents(
                            cache,
                            metadata.attribute,
                            metadata.elementType,
                            metadata.isInterface,
                            filtered
                        );

                        IList instance = metadata.listCreator(filtered.Count);
                        for (int i = 0; i < filtered.Count; ++i)
                        {
                            instance.Add(filtered[i]);
                        }

                        foundChild = filtered.Count > 0;
                        metadata.setter(component, instance);
                        break;
                    }
                    default:
                    {
                        foundChild = false;
                        Component childComponent = null;

                        using PooledResource<List<Component>> childComponentBuffer =
                            Buffers<Component>.List.Get();
                        List<Component> childComponents = childComponentBuffer.resource;

                        foreach (
                            Transform child in component.IterateOverAllChildrenRecursivelyBreadthFirst(
                                childBuffer,
                                includeSelf: !metadata.attribute.OnlyDescendants,
                                maxDepth: metadata.attribute.MaxDepth
                            )
                        )
                        {
                            childComponents.Clear();
                            using PooledResource<List<Component>> componentBuffer =
                                Buffers<Component>.List.Get(out List<Component> components);
                            GetComponentsOfType(
                                child.gameObject,
                                metadata.elementType,
                                metadata.isInterface,
                                metadata.attribute.AllowInterfaces,
                                components
                            );

                            childComponents.AddRange(components);
                            using PooledResource<List<Component>> filteredBuffer =
                                Buffers<Component>.List.Get(out List<Component> filtered);

                            FilterComponents(
                                childComponents,
                                metadata.attribute,
                                metadata.elementType,
                                metadata.isInterface,
                                filtered
                            );

                            if (filtered.Count > 0)
                            {
                                childComponent = filtered[0];
                                foundChild = true;
                                break;
                            }
                        }

                        if (foundChild)
                        {
                            metadata.setter(component, childComponent);
                        }

                        break;
                    }
                }

                if (!foundChild)
                {
                    LogMissingComponentError(component, metadata, "child");
                }
            }
        }

        private static List<Component> CollectChildComponents(
            Component component,
            FieldMetadata<ChildComponentAttribute> metadata,
            List<Transform> childBuffer,
            List<Component> cache
        )
        {
            using PooledResource<List<Component>> childComponentBuffer =
                Buffers<Component>.List.Get();
            List<Component> childComponents = childComponentBuffer.resource;

            using PooledResource<List<Component>> componentBuffer = Buffers<Component>.List.Get(
                out List<Component> components
            );
            foreach (
                Transform child in component.IterateOverAllChildrenRecursivelyBreadthFirst(
                    childBuffer,
                    includeSelf: !metadata.attribute.OnlyDescendants,
                    maxDepth: metadata.attribute.MaxDepth
                )
            )
            {
                childComponents.Clear();
                GetComponentsOfType(
                    child.gameObject,
                    metadata.elementType,
                    metadata.isInterface,
                    metadata.attribute.AllowInterfaces,
                    components
                );
                childComponents.AddRange(components);
                cache.AddRange(childComponents);
            }

            return cache;
        }
    }
}
