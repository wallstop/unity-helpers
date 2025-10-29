namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Utils;
    using static RelationalComponentProcessor;

    /// <summary>
    /// Automatically assigns sibling components (components on the same <see cref="GameObject"/>) to the decorated field.
    /// Supports single components, <see cref="System.Array"/>s, <see cref="System.Collections.Generic.List{T}"/>,
    /// and <see cref="System.Collections.Generic.HashSet{T}"/> collection types.
    /// </summary>
    /// <remarks>
    /// Call <see cref="SiblingComponentExtensions.AssignSiblingComponents"/> (or
    /// <see cref="RelationalComponentExtensions.AssignRelationalComponents(UnityEngine.Component)"/>) to populate the field.
    /// This is typically done in <c>Awake()</c> or <c>OnEnable()</c>.
    ///
    /// Use optional filters to refine results: <see cref="BaseRelationalComponentAttribute.TagFilter"/> (by tag),
    /// <see cref="BaseRelationalComponentAttribute.NameFilter"/> (substring match on name), and
    /// <see cref="BaseRelationalComponentAttribute.IncludeInactive"/> (include disabled/inactive components).
    ///
    /// IMPORTANT: This attribute populates fields at runtime, not during Unity serialization in Edit mode.
    /// Fields populated by this attribute will not be serialized by Unity.
    ///
    /// <seealso cref="BaseRelationalComponentAttribute"/>
    /// <seealso cref="SiblingComponentExtensions.AssignSiblingComponents(UnityEngine.Component)"/>
    /// <seealso cref="RelationalComponentExtensions.AssignRelationalComponents(UnityEngine.Component)"/>
    /// </remarks>
    /// <example>
    /// Assign common sibling components with filters and collections:
    /// <code><![CDATA[
    /// using UnityEngine;
    /// using WallstopStudios.UnityHelpers.Core.Attributes;
    ///
    /// public class Enemy : MonoBehaviour
    /// {
    ///     // Single assignment (required by default)
    ///     [SiblingComponent] private Animator animator;
    ///
    ///     // Optional – do not log an error if not present
    ///     [SiblingComponent(Optional = true)] private Rigidbody2D rb;
    ///
    ///     // Multiple results – collect all on the same GameObject
    ///     [SiblingComponent] private List<Collider2D> allSiblingColliders;
    ///
    ///     // Filter by tag and name substring
    ///     [SiblingComponent(TagFilter = "Visual", NameFilter = "Sprite")]
    ///     private Component[] visualComponents;
    ///
    ///     private void Awake()
    ///     {
    ///         this.AssignSiblingComponents();
    ///         // or: this.AssignRelationalComponents();
    ///     }
    /// }
    /// ]]></code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SiblingComponentAttribute : BaseRelationalComponentAttribute { }

    public static class SiblingComponentExtensions
    {
        private static readonly Dictionary<
            Type,
            FieldMetadata<SiblingComponentAttribute>[]
        > FieldsByType = new();

        /// <summary>
        /// Assigns fields on <paramref name="component"/> marked with <see cref="SiblingComponentAttribute"/>.
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
        ///     this.AssignSiblingComponents();
        /// }
        /// ]]></code>
        /// </example>
        public static void AssignSiblingComponents(this Component component)
        {
            Type componentType = component.GetType();
            FieldMetadata<SiblingComponentAttribute>[] fields = FieldsByType.GetOrAdd(
                componentType,
                type => GetFieldMetadata<SiblingComponentAttribute>(type)
            );

            foreach (FieldMetadata<SiblingComponentAttribute> metadata in fields)
            {
                if (ShouldSkipAssignment(metadata, component))
                {
                    continue;
                }

                bool foundSibling;

                if (metadata.kind == FieldKind.Single)
                {
                    foundSibling = TryAssignSingleSibling(component, metadata);
                }
                else
                {
                    FilterParameters filters = new(metadata.attribute);
                    if (
                        !metadata.isInterface
                        && !filters.RequiresPostProcessing
                        && metadata.attribute.MaxCount <= 0
                    )
                    {
                        foundSibling = TryAssignSiblingCollectionFast(component, metadata);
                    }
                    else
                    {
                        switch (metadata.kind)
                        {
                            case FieldKind.Array:
                            {
                                using PooledResource<List<Component>> componentBuffer =
                                    Buffers<Component>.List.Get(out List<Component> components);
                                GetComponentsOfType(
                                    component,
                                    metadata.elementType,
                                    metadata.isInterface,
                                    metadata.attribute.AllowInterfaces,
                                    components
                                );

                                int filteredCount =
                                    !filters.RequiresPostProcessing
                                    && metadata.attribute.MaxCount <= 0
                                        ? components.Count
                                        : FilterComponentsInPlace(
                                            components,
                                            filters,
                                            metadata.attribute,
                                            metadata.elementType,
                                            metadata.isInterface
                                        );

                                Array correctTypedArray = metadata.arrayCreator(filteredCount);
                                for (int i = 0; i < filteredCount; ++i)
                                {
                                    correctTypedArray.SetValue(components[i], i);
                                }

                                metadata.setter(component, correctTypedArray);
                                foundSibling = filteredCount > 0;
                                break;
                            }
                            case FieldKind.List:
                            {
                                using PooledResource<List<Component>> componentBuffer =
                                    Buffers<Component>.List.Get(out List<Component> components);
                                GetComponentsOfType(
                                    component,
                                    metadata.elementType,
                                    metadata.isInterface,
                                    metadata.attribute.AllowInterfaces,
                                    components
                                );

                                int filteredCount =
                                    !filters.RequiresPostProcessing
                                    && metadata.attribute.MaxCount <= 0
                                        ? components.Count
                                        : FilterComponentsInPlace(
                                            components,
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
                                    instance.Add(components[i]);
                                }

                                foundSibling = filteredCount > 0;
                                break;
                            }
                            case FieldKind.HashSet:
                            {
                                using PooledResource<List<Component>> componentBuffer =
                                    Buffers<Component>.List.Get(out List<Component> components);
                                GetComponentsOfType(
                                    component,
                                    metadata.elementType,
                                    metadata.isInterface,
                                    metadata.attribute.AllowInterfaces,
                                    components
                                );

                                int filteredCount =
                                    !filters.RequiresPostProcessing
                                    && metadata.attribute.MaxCount <= 0
                                        ? components.Count
                                        : FilterComponentsInPlace(
                                            components,
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
                                    metadata.hashSetAdder(instance, components[i]);
                                }

                                foundSibling = filteredCount > 0;
                                break;
                            }
                            default:
                            {
                                foundSibling = TryAssignSingleSibling(component, metadata);
                                break;
                            }
                        }
                    }
                }

                if (!foundSibling)
                {
                    LogMissingComponentError(component, metadata, "sibling");
                }
            }
        }

        private static bool TryAssignSingleSibling(
            Component component,
            FieldMetadata<SiblingComponentAttribute> metadata
        )
        {
            SiblingComponentAttribute attribute = metadata.attribute;

            if (metadata.isInterface && !attribute.AllowInterfaces)
            {
                return false;
            }

            bool hasSimpleFilters =
                attribute.IncludeInactive
                && attribute.TagFilter == null
                && attribute.NameFilter == null;

            if (!metadata.isInterface && hasSimpleFilters)
            {
                if (component.TryGetComponent(metadata.elementType, out Component sibling))
                {
                    metadata.setter(component, sibling);
                    return true;
                }
                return false;
            }

            FilterParameters filters = new(attribute);
            if (
                TryResolveSingleComponent(
                    component,
                    filters,
                    metadata.elementType,
                    metadata.isInterface,
                    attribute.AllowInterfaces,
                    null,
                    out Component resolved
                )
            )
            {
                metadata.setter(component, resolved);
                return true;
            }
            return false;
        }

        private static bool TryAssignSiblingCollectionFast(
            Component component,
            FieldMetadata<SiblingComponentAttribute> metadata
        )
        {
            Array componentsArray = SiblingComponentFastInvoker.GetArray(
                component,
                metadata.elementType
            );
            int count = componentsArray.Length;

            switch (metadata.kind)
            {
                case FieldKind.Array:
                {
                    metadata.setter(component, componentsArray);
                    return count > 0;
                }
                case FieldKind.List:
                {
                    object existing = metadata.getter(component);
                    IList instance = existing as IList;
                    if (instance != null)
                    {
                        instance.Clear();
                    }
                    else
                    {
                        instance = metadata.listCreator(count);
                        metadata.setter(component, instance);
                    }

                    for (int i = 0; i < count; ++i)
                    {
                        instance.Add(componentsArray.GetValue(i));
                    }

                    return count > 0;
                }
                case FieldKind.HashSet:
                {
                    object instance = metadata.getter(component);
                    if (instance != null && metadata.hashSetClearer != null)
                    {
                        metadata.hashSetClearer(instance);
                    }
                    else
                    {
                        instance = metadata.hashSetCreator(count);
                        metadata.setter(component, instance);
                    }

                    for (int i = 0; i < count; ++i)
                    {
                        metadata.hashSetAdder(instance, componentsArray.GetValue(i));
                    }

                    return count > 0;
                }
                default:
                {
                    return TryAssignSingleSibling(component, metadata);
                }
            }
        }
    }

    internal static class SiblingComponentFastInvoker
    {
        private static readonly ConcurrentDictionary<Type, Func<Component, Array>> ArrayGetters =
            new();

        private static readonly MethodInfo GetComponentsGenericDefinition = typeof(Component)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .First(method =>
                method.Name == nameof(Component.GetComponents)
                && method.IsGenericMethodDefinition
                && method.GetParameters().Length == 0
            );

        internal static Array GetArray(Component component, Type elementType)
        {
            return ArrayGetters.GetOrAdd(elementType, CreateArrayGetter)(component);
        }

        private static Func<Component, Array> CreateArrayGetter(Type elementType)
        {
            MethodInfo closedMethod = GetComponentsGenericDefinition.MakeGenericMethod(elementType);
            ParameterExpression componentParameter = Expression.Parameter(
                typeof(Component),
                "component"
            );
            MethodCallExpression invoke = Expression.Call(componentParameter, closedMethod);
            UnaryExpression convert = Expression.Convert(invoke, typeof(Array));
            return Expression.Lambda<Func<Component, Array>>(convert, componentParameter).Compile();
        }
    }
}
