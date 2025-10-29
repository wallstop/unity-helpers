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
    /// Supports single components, <see cref="System.Array"/>s, <see cref="System.Collections.Generic.List{T}"/>,
    /// and <see cref="System.Collections.Generic.HashSet{T}"/> collection types.
    /// </summary>
    /// <remarks>
    /// Call <see cref="ChildComponentExtensions.AssignChildComponents"/> (or
    /// <see cref="RelationalComponentExtensions.AssignRelationalComponents(UnityEngine.Component)"/>) to populate the field.
    /// This is typically done in <c>Awake()</c> or <c>OnEnable()</c>.
    ///
    /// By default, searches include the current <see cref="GameObject"/>; set <see cref="OnlyDescendants"/> to exclude it.
    /// Limit traversal with <see cref="MaxDepth"/> (depth 1 = immediate children only). Children are visited in breadth-first order.
    /// Combine with filters like <see cref="BaseRelationalComponentAttribute.TagFilter"/> and
    /// <see cref="BaseRelationalComponentAttribute.NameFilter"/>. Interfaces and base types are supported when
    /// <see cref="BaseRelationalComponentAttribute.AllowInterfaces"/> is true (default).
    ///
    /// IMPORTANT: This attribute populates fields at runtime, not during Unity serialization in Edit mode.
    /// Fields populated by this attribute will not be serialized by Unity.
    ///
    /// <seealso cref="BaseRelationalComponentAttribute"/>
    /// <seealso cref="ChildComponentExtensions.AssignChildComponents(UnityEngine.Component)"/>
    /// <seealso cref="RelationalComponentExtensions.AssignRelationalComponents(UnityEngine.Component)"/>
    /// </remarks>
    /// <example>
    /// Typical child searches with depth limits and collections:
    /// <code><![CDATA[
    /// using UnityEngine;
    /// using WallstopStudios.UnityHelpers.Core.Attributes;
    ///
    /// public class EnemyRoot : MonoBehaviour
    /// {
    ///     // Immediate children only
    ///     [ChildComponent(OnlyDescendants = true, MaxDepth = 1)]
    ///     private Transform[] immediateChildren;
    ///
    ///     // Find the first matching descendant with a tag
    ///     [ChildComponent(OnlyDescendants = true, TagFilter = "Weapon")]
    ///     private Collider2D weaponCollider;
    ///
    ///     // Gather into a hash set (no duplicates)
    ///     [ChildComponent(OnlyDescendants = true, MaxCount = 10)]
    ///     private HashSet<Rigidbody2D> firstTenRigidbodies;
    ///
    ///     private void Awake()
    ///     {
    ///         this.AssignChildComponents();
    ///     }
    /// }
    /// ]]></code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ChildComponentAttribute : BaseRelationalComponentAttribute
    {
        /// <summary>
        /// If true, excludes components on the current <see cref="GameObject"/> and only searches descendant transforms.
        /// If false, includes components on the current <see cref="GameObject"/> in the search. Default: false.
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

        /// <summary>
        /// Assigns fields on <paramref name="component"/> marked with <see cref="ChildComponentAttribute"/>.
        /// </summary>
        /// <param name="component">The component whose fields will be populated.</param>
        /// <remarks>
        /// Typical call site is <c>Awake()</c> or <c>OnEnable()</c>. For convenience, you can also call
        /// <see cref="RelationalComponentExtensions.AssignRelationalComponents(UnityEngine.Component)"/> to assign all relational attributes.
        /// </remarks>
        /// <example>
        /// <code><![CDATA[
        /// void Awake()
        /// {
        ///     this.AssignChildComponents();
        /// }
        /// ]]></code>
        /// </example>
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
                FilterParameters filters = new(metadata.attribute);

                using PooledResource<List<Transform>> childBufferResource =
                    Buffers<Transform>.List.Get();
                List<Transform> childBuffer = childBufferResource.resource;

                if (metadata.kind == FieldKind.Single)
                {
                    Component childComponent;
                    if (
                        TryAssignChildSingleFast(component, metadata, out childComponent)
                        || TryAssignChildSingleFallback(
                            component,
                            metadata,
                            filters,
                            childBuffer,
                            out childComponent
                        )
                    )
                    {
                        metadata.setter(component, childComponent);
                        foundChild = true;
                    }
                    else
                    {
                        foundChild = false;
                    }
                }
                else
                {
                    switch (metadata.kind)
                    {
                        case FieldKind.Array:
                        {
                            using PooledResource<List<Component>> cacheResource =
                                Buffers<Component>.List.Get();
                            List<Component> cache = cacheResource.resource;

                            CollectChildComponents(component, metadata, childBuffer, cache);

                            int filteredCount = FilterComponentsInPlace(
                                cache,
                                filters,
                                metadata.attribute,
                                metadata.elementType,
                                metadata.isInterface
                            );

                            Array correctTypedArray = metadata.arrayCreator(filteredCount);
                            for (int i = 0; i < filteredCount; ++i)
                            {
                                correctTypedArray.SetValue(cache[i], i);
                            }

                            metadata.setter(component, correctTypedArray);
                            foundChild = filteredCount > 0;
                            break;
                        }
                        case FieldKind.List:
                        {
                            using PooledResource<List<Component>> cacheResource =
                                Buffers<Component>.List.Get();
                            List<Component> cache = cacheResource.resource;

                            CollectChildComponents(component, metadata, childBuffer, cache);

                            int filteredCount = FilterComponentsInPlace(
                                cache,
                                filters,
                                metadata.attribute,
                                metadata.elementType,
                                metadata.isInterface
                            );

                            object existing = metadata.getter(component);
                            IList instance = existing as IList;
                            if (instance != null)
                            {
                                instance.Clear();
                            }
                            else
                            {
                                instance = metadata.listCreator(filteredCount);
                                metadata.setter(component, instance);
                            }
                            for (int i = 0; i < filteredCount; ++i)
                            {
                                instance.Add(cache[i]);
                            }

                            foundChild = filteredCount > 0;
                            break;
                        }
                        case FieldKind.HashSet:
                        {
                            using PooledResource<List<Component>> cacheResource =
                                Buffers<Component>.List.Get();
                            List<Component> cache = cacheResource.resource;

                            CollectChildComponents(component, metadata, childBuffer, cache);

                            int filteredCount = FilterComponentsInPlace(
                                cache,
                                filters,
                                metadata.attribute,
                                metadata.elementType,
                                metadata.isInterface
                            );

                            object instance = metadata.getter(component);
                            if (instance != null && metadata.hashSetClearer != null)
                            {
                                metadata.hashSetClearer(instance);
                            }
                            else
                            {
                                instance = metadata.hashSetCreator(filteredCount);
                                metadata.setter(component, instance);
                            }
                            for (int i = 0; i < filteredCount; ++i)
                            {
                                metadata.hashSetAdder(instance, cache[i]);
                            }

                            foundChild = filteredCount > 0;
                            break;
                        }
                        default:
                        {
                            foundChild = false;
                            break;
                        }
                    }
                }

                if (!foundChild)
                {
                    LogMissingComponentError(component, metadata, "child");
                }
            }
        }

        private static bool TryAssignChildSingleFast(
            Component component,
            FieldMetadata<ChildComponentAttribute> metadata,
            out Component childComponent
        )
        {
            childComponent = null;
            ChildComponentAttribute attribute = metadata.attribute;

            if (
                metadata.isInterface
                || attribute.MaxDepth != 0
                || attribute.TagFilter != null
                || attribute.NameFilter != null
            )
            {
                return false;
            }

            Component[] results = component.GetComponentsInChildren(
                metadata.elementType,
                attribute.IncludeInactive
            );

            if (results == null || results.Length == 0)
            {
                return false;
            }

            Transform componentTransform = component.transform;
            for (int i = 0; i < results.Length; ++i)
            {
                Component candidate = results[i];
                if (candidate == null)
                {
                    continue;
                }

                if (attribute.OnlyDescendants && candidate.transform == componentTransform)
                {
                    continue;
                }

                childComponent = candidate;
                return true;
            }

            return false;
        }

        private static bool TryAssignChildSingleFallback(
            Component component,
            FieldMetadata<ChildComponentAttribute> metadata,
            FilterParameters filters,
            List<Transform> childBuffer,
            out Component childComponent
        )
        {
            bool needsScratch = metadata.isInterface || filters.RequiresPostProcessing;
            List<Component> scratchList = null;
            PooledResource<List<Component>> scratch = default;
            if (needsScratch)
            {
                scratch = Buffers<Component>.List.Get(out scratchList);
            }

            childComponent = null;

            foreach (
                Transform child in component.IterateOverAllChildrenRecursivelyBreadthFirst(
                    childBuffer,
                    includeSelf: !metadata.attribute.OnlyDescendants,
                    maxDepth: metadata.attribute.MaxDepth
                )
            )
            {
                if (
                    TryResolveSingleComponent(
                        child,
                        filters,
                        metadata.elementType,
                        metadata.isInterface,
                        metadata.attribute.AllowInterfaces,
                        scratchList,
                        out Component resolved
                    )
                )
                {
                    childComponent = resolved;
                    break;
                }
            }

            if (needsScratch)
            {
                scratch.Dispose();
            }

            return childComponent != null;
        }

        private static List<Component> CollectChildComponents(
            Component component,
            FieldMetadata<ChildComponentAttribute> metadata,
            List<Transform> childBuffer,
            List<Component> cache
        )
        {
            cache.Clear();
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
                GetComponentsOfType(
                    child,
                    metadata.elementType,
                    metadata.isInterface,
                    metadata.attribute.AllowInterfaces,
                    components
                );
                cache.AddRange(components);
            }
            return cache;
        }
    }
}
