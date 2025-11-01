namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Extension;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Utils;
    using static RelationalComponentProcessor;
#if UNITY_EDITOR
    using WallstopStudios.UnityHelpers.Core.Diagnostics;
#endif
#if UNITY_2020_2_OR_NEWER
    using Unity.Profiling;
#endif

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

#if UNITY_2020_2_OR_NEWER
        private static readonly ProfilerMarker ChildFastPathMarker = new ProfilerMarker(
            "RelationalComponents.Child.FastPath"
        );
        private static readonly ProfilerMarker ChildFallbackMarker = new ProfilerMarker(
            "RelationalComponents.Child.Fallback"
        );
#endif

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
            FieldMetadata<ChildComponentAttribute>[] fields = FieldsByType.GetOrAdd(
                component.GetType(),
                type => GetFieldMetadata<ChildComponentAttribute>(type)
            );
            AssignChildComponents(component, fields);
        }

        internal static void AssignChildComponents(
            Component component,
            FieldMetadata<ChildComponentAttribute>[] fields
        )
        {
            if (component == null || fields == null || fields.Length == 0)
            {
                return;
            }

            foreach (FieldMetadata<ChildComponentAttribute> metadata in fields)
            {
                if (ShouldSkipAssignment(metadata, component))
                {
                    continue;
                }

                bool foundChild = false;
                if (metadata.kind == FieldKind.Single)
                {
                    if (TryAssignChildSingleFast(component, metadata, out Component childComponent))
                    {
                        metadata.SetValue(component, childComponent);
                        foundChild = true;
                    }
                    else
                    {
                        FilterParameters filters = metadata.Filters;
                        using PooledResource<List<Transform>> childBufferResource =
                            Buffers<Transform>.List.Get(out List<Transform> childBuffer);
                        if (
                            TryAssignChildSingleFallback(
                                component,
                                metadata,
                                filters,
                                childBuffer,
                                out childComponent
                            )
                        )
                        {
                            metadata.SetValue(component, childComponent);
                            foundChild = true;
                        }
                    }
                }
                else
                {
                    FilterParameters filters = metadata.Filters;
                    if (
                        TryAssignChildCollectionFast(
                            component,
                            metadata,
                            filters,
                            out bool assignedAny
                        )
                    )
                    {
                        foundChild = assignedAny;
                    }
                    else
                    {
                        using PooledResource<List<Transform>> childBufferResource =
                            Buffers<Transform>.List.Get(out List<Transform> childBuffer);

                        switch (metadata.kind)
                        {
                            case FieldKind.Array:
                            {
                                using PooledResource<List<Component>> cacheResource =
                                    Buffers<Component>.List.Get(out List<Component> cache);
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

                                metadata.SetValue(component, correctTypedArray);
                                foundChild = filteredCount > 0;
                                break;
                            }
                            case FieldKind.List:
                            {
                                using PooledResource<List<Component>> cacheResource =
                                    Buffers<Component>.List.Get(out List<Component> cache);

                                CollectChildComponents(component, metadata, childBuffer, cache);

                                int filteredCount = FilterComponentsInPlace(
                                    cache,
                                    filters,
                                    metadata.attribute,
                                    metadata.elementType,
                                    metadata.isInterface
                                );

                                object existing = metadata.GetValue(component);
                                if (existing is IList instance)
                                {
                                    instance.Clear();
                                }
                                else
                                {
                                    instance = metadata.listCreator(filteredCount);
                                    metadata.SetValue(component, instance);
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
                                    Buffers<Component>.List.Get(out List<Component> cache);

                                CollectChildComponents(component, metadata, childBuffer, cache);

                                int filteredCount = FilterComponentsInPlace(
                                    cache,
                                    filters,
                                    metadata.attribute,
                                    metadata.elementType,
                                    metadata.isInterface
                                );

                                object instance = metadata.GetValue(component);
                                if (instance != null && metadata.hashSetClearer != null)
                                {
                                    metadata.hashSetClearer(instance);
                                }
                                else
                                {
                                    instance = metadata.hashSetCreator(filteredCount);
                                    metadata.SetValue(component, instance);
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
                                break;
                            }
                        }
                    }
                }

                if (!foundChild)
                {
                    LogMissingComponentError(component, metadata, "child");
                }
            }
        }

        internal static FieldMetadata<ChildComponentAttribute>[] GetOrCreateFields(Type type)
        {
            return FieldsByType.GetOrAdd(type, t => GetFieldMetadata<ChildComponentAttribute>(t));
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

        private static bool TryAssignChildCollectionFast(
            Component component,
            FieldMetadata<ChildComponentAttribute> metadata,
            FilterParameters filters,
            out bool assignedAny
        )
        {
            assignedAny = false;
            ChildComponentAttribute attribute = metadata.attribute;
            if (metadata.isInterface || filters.RequiresPostProcessing || attribute.MaxDepth > 0)
            {
#if UNITY_EDITOR
                RelationalComponentInstrumentation.RecordChildFastPath(false);
#endif
#if UNITY_2020_2_OR_NEWER
                ChildFallbackMarker.Begin();
                ChildFallbackMarker.End();
#endif
                return false;
            }

#if UNITY_2020_2_OR_NEWER
            using (ChildFastPathMarker.Auto())
#endif
            {
                Array children = ChildComponentFastInvoker.GetArray(
                    component,
                    metadata.elementType,
                    attribute.IncludeInactive
                );

                Array filtered = FilterChildArray(component, metadata, children);
                Array ordered = EnsureBreadthFirstOrder(component, metadata, filtered);
                assignedAny = AssignChildComponentsFromArray(component, metadata, ordered);
#if UNITY_EDITOR
                RelationalComponentInstrumentation.RecordChildFastPath(true);
#endif
                return true;
            }
        }

        private static Array FilterChildArray(
            Component component,
            FieldMetadata<ChildComponentAttribute> metadata,
            Array source
        )
        {
            Type elementType = metadata.elementType;
            if (source == null || source.Length == 0)
            {
                return Array.CreateInstance(elementType, 0);
            }

            ChildComponentAttribute attribute = metadata.attribute;
            bool onlyDescendants = attribute.OnlyDescendants;
            Transform self = component.transform;

            int maxCount = attribute.MaxCount;
            if (!onlyDescendants && maxCount <= 0)
            {
                return source;
            }

            int limit = maxCount > 0 ? Math.Min(maxCount, source.Length) : source.Length;
            Array staged = Array.CreateInstance(elementType, limit);
            int writeIndex = 0;

            for (int i = 0; i < source.Length && writeIndex < limit; ++i)
            {
                Component candidate = source.GetValue(i) as Component;
                if (candidate == null)
                {
                    continue;
                }

                if (onlyDescendants && candidate.transform == self)
                {
                    continue;
                }

                staged.SetValue(candidate, writeIndex++);
            }

            if (writeIndex == staged.Length)
            {
                return staged;
            }

            Array result = Array.CreateInstance(elementType, writeIndex);
            if (writeIndex > 0)
            {
                Array.Copy(staged, 0, result, 0, writeIndex);
            }

            return result;
        }

        private static Array EnsureBreadthFirstOrder(
            Component component,
            FieldMetadata<ChildComponentAttribute> metadata,
            Array source
        )
        {
            Type elementType = metadata.elementType;
            if (source == null)
            {
                return Array.CreateInstance(elementType, 0);
            }

            int length = source.Length;
            if (length <= 1)
            {
                return source;
            }

            ChildComponentAttribute attribute = metadata.attribute;
            using PooledResource<List<Transform>> traversalResource = Buffers<Transform>.List.Get(
                out List<Transform> traversal
            );
            component.IterateOverAllChildrenRecursivelyBreadthFirst(
                traversal,
                includeSelf: !attribute.OnlyDescendants,
                attribute.MaxDepth
            );

            using PooledResource<Dictionary<Transform, List<Component>>> groupedResource =
                DictionaryBuffer<Transform, List<Component>>.Dictionary.Get(
                    out Dictionary<Transform, List<Component>> grouped
                );
            using PooledResource<Dictionary<Transform, int>> positionsResource = DictionaryBuffer<
                Transform,
                int
            >.Dictionary.Get(out Dictionary<Transform, int> positions);

            for (int i = 0; i < length; ++i)
            {
                Component candidate = source.GetValue(i) as Component;
                if (candidate == null)
                {
                    continue;
                }

                Transform key = candidate.transform;
                if (!grouped.TryGetValue(key, out List<Component> list))
                {
                    list = new List<Component>();
                    grouped.Add(key, list);
                    positions.Add(key, 0);
                }

                list.Add(candidate);
            }

            Array ordered = Array.CreateInstance(elementType, length);
            int writeIndex = 0;

            for (int i = 0; i < traversal.Count && writeIndex < length; ++i)
            {
                Transform transform = traversal[i];
                if (!grouped.TryGetValue(transform, out List<Component> list))
                {
                    continue;
                }

                int position = positions[transform];
                while (position < list.Count && writeIndex < length)
                {
                    ordered.SetValue(list[position], writeIndex++);
                    position++;
                }

                if (position >= list.Count)
                {
                    grouped.Remove(transform);
                    positions.Remove(transform);
                }
                else
                {
                    positions[transform] = position;
                }
            }

            if (writeIndex < length && grouped.Count > 0)
            {
                foreach (KeyValuePair<Transform, List<Component>> pair in grouped)
                {
                    List<Component> list = pair.Value;
                    int position = positions[pair.Key];
                    while (position < list.Count && writeIndex < length)
                    {
                        ordered.SetValue(list[position], writeIndex++);
                        position++;
                    }
                    if (writeIndex >= length)
                    {
                        break;
                    }
                }
            }

            if (writeIndex >= length)
            {
                return ordered;
            }

            if (writeIndex == 0)
            {
                return Array.CreateInstance(elementType, 0);
            }

            Array trimmed = Array.CreateInstance(elementType, writeIndex);
            Array.Copy(ordered, 0, trimmed, 0, writeIndex);
            return trimmed;
        }

        private static bool AssignChildComponentsFromArray(
            Component component,
            FieldMetadata<ChildComponentAttribute> metadata,
            Array componentsArray
        )
        {
            if (componentsArray == null)
            {
                componentsArray = Array.CreateInstance(metadata.elementType, 0);
            }

            int count = componentsArray.Length;

            switch (metadata.kind)
            {
                case FieldKind.Array:
                {
                    Array instance = metadata.arrayCreator(count);
                    for (int i = 0; i < count; ++i)
                    {
                        instance.SetValue(componentsArray.GetValue(i), i);
                    }

                    metadata.SetValue(component, instance);
                    return count > 0;
                }
                case FieldKind.List:
                {
                    if (metadata.GetValue(component) is IList list)
                    {
                        list.Clear();
                    }
                    else
                    {
                        list = metadata.listCreator(count);
                        metadata.SetValue(component, list);
                    }

                    for (int i = 0; i < count; ++i)
                    {
                        list.Add(componentsArray.GetValue(i));
                    }

                    return count > 0;
                }
                case FieldKind.HashSet:
                {
                    object hashSet = metadata.GetValue(component);
                    if (hashSet != null && metadata.hashSetClearer != null)
                    {
                        metadata.hashSetClearer(hashSet);
                    }
                    else
                    {
                        hashSet = metadata.hashSetCreator(count);
                        metadata.SetValue(component, hashSet);
                    }

                    for (int i = 0; i < count; ++i)
                    {
                        metadata.hashSetAdder(hashSet, componentsArray.GetValue(i));
                    }

                    return count > 0;
                }
                default:
                {
                    return false;
                }
            }
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

    internal static class ChildComponentFastInvoker
    {
        private static readonly ConcurrentDictionary<
            Type,
            Func<Component, bool, Array>
        > ArrayGetters = new();

        private static readonly MethodInfo GetComponentsInChildrenGeneric = typeof(Component)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .First(method =>
                method.Name == nameof(Component.GetComponentsInChildren)
                && method.IsGenericMethodDefinition
                && method.GetParameters().Length == 1
                && method.GetParameters()[0].ParameterType == typeof(bool)
            );

        internal static Array GetArray(Component component, Type elementType, bool includeInactive)
        {
            return ArrayGetters.GetOrAdd(elementType, t => CreateArrayGetter(t))(
                component,
                includeInactive
            );
        }

        private static Func<Component, bool, Array> CreateArrayGetter(Type elementType)
        {
            MethodInfo closedMethod = GetComponentsInChildrenGeneric.MakeGenericMethod(elementType);
            ParameterExpression componentParameter = Expression.Parameter(
                typeof(Component),
                "component"
            );
            ParameterExpression includeInactiveParameter = Expression.Parameter(
                typeof(bool),
                "includeInactive"
            );
            MethodCallExpression invoke = Expression.Call(
                componentParameter,
                closedMethod,
                includeInactiveParameter
            );
            UnaryExpression convert = Expression.Convert(invoke, typeof(Array));
            return Expression
                .Lambda<Func<Component, bool, Array>>(
                    convert,
                    componentParameter,
                    includeInactiveParameter
                )
                .Compile();
        }
    }
}
