namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Extension;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Utils;
    using static RelationalComponentProcessor;

    /// <summary>
    /// Automatically assigns parent components (components up the transform hierarchy) to the decorated field.
    /// Supports single components, <see cref="System.Array"/>s, <see cref="System.Collections.Generic.List{T}"/>,
    /// and <see cref="System.Collections.Generic.HashSet{T}"/> collection types.
    /// </summary>
    /// <remarks>
    /// Call <see cref="ParentComponentExtensions.AssignParentComponents"/> (or
    /// <see cref="RelationalComponentExtensions.AssignRelationalComponents(UnityEngine.Component)"/>) to populate the field.
    /// This is typically done in <c>Awake()</c> or <c>OnEnable()</c>.
    ///
    /// By default, searches include the current <see cref="GameObject"/>; set <see cref="OnlyAncestors"/> to exclude it.
    /// Limit traversal with <see cref="MaxDepth"/> (depth 1 = immediate parent only). Combine with filters like
    /// <see cref="BaseRelationalComponentAttribute.TagFilter"/> and <see cref="BaseRelationalComponentAttribute.NameFilter"/>.
    /// Interfaces and base types are supported when <see cref="BaseRelationalComponentAttribute.AllowInterfaces"/> is true (default).
    ///
    /// IMPORTANT: This attribute populates fields at runtime, not during Unity serialization in Edit mode.
    /// Fields populated by this attribute will not be serialized by Unity.
    ///
    /// <seealso cref="BaseRelationalComponentAttribute"/>
    /// <seealso cref="ParentComponentExtensions.AssignParentComponents(UnityEngine.Component)"/>
    /// <seealso cref="RelationalComponentExtensions.AssignRelationalComponents(UnityEngine.Component)"/>
    /// </remarks>
    /// <example>
    /// Typical parent searches with depth and filters:
    /// <code><![CDATA[
    /// using UnityEngine;
    /// using WallstopStudios.UnityHelpers.Core.Attributes;
    ///
    /// public interface IHealth { int Current { get; } }
    ///
    /// public class ChildComponent : MonoBehaviour
    /// {
    ///     // Immediate parent only
    ///     [ParentComponent(OnlyAncestors = true, MaxDepth = 1)]
    ///     private Transform directParent;
    ///
    ///     // Search up to 3 levels for a specific tag
    ///     [ParentComponent(OnlyAncestors = true, MaxDepth = 3, TagFilter = "Player")]
    ///     private Collider2D playerAncestorCollider;
    ///
    ///     // Interface lookup up the chain
    ///     [ParentComponent]
    ///     private IHealth healthProvider;
    ///
    ///     // Collect multiple up the chain (stops at MaxCount)
    ///     [ParentComponent(MaxCount = 2)]
    ///     private Rigidbody2D[] firstTwoRigidbodies;
    ///
    ///     private void Awake()
    ///     {
    ///         this.AssignParentComponents();
    ///     }
    /// }
    /// ]]></code>
    /// </example>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ParentComponentAttribute : BaseRelationalComponentAttribute
    {
        /// <summary>
        /// If true, excludes components on the current <see cref="GameObject"/> and only searches parent transforms.
        /// If false, includes components on the current <see cref="GameObject"/> in the search. Default: false.
        /// </summary>
        public bool OnlyAncestors { get; set; } = false;

        /// <summary>
        /// Maximum depth to search up the hierarchy. 0 means unlimited. Default: 0.
        /// Depth 1 = immediate parent only, depth 2 = parent and grandparent, etc.
        /// </summary>
        public int MaxDepth { get; set; } = 0;
    }

    public static class ParentComponentExtensions
    {
        private static readonly Dictionary<
            Type,
            FieldMetadata<ParentComponentAttribute>[]
        > FieldsByType = new();

        /// <summary>
        /// Assigns fields on <paramref name="component"/> marked with <see cref="ParentComponentAttribute"/>.
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
        ///     this.AssignParentComponents();
        /// }
        /// ]]></code>
        /// </example>
        public static void AssignParentComponents(this Component component)
        {
            Type componentType = component.GetType();
            FieldMetadata<ParentComponentAttribute>[] fields = FieldsByType.GetOrAdd(
                componentType,
                type => GetFieldMetadata<ParentComponentAttribute>(type)
            );

            foreach (FieldMetadata<ParentComponentAttribute> field in fields)
            {
                if (ShouldSkipAssignment(field, component))
                {
                    continue;
                }

                bool foundParent;
                FilterParameters filters = new(field.attribute);
                Transform root = component.transform;
                if (field.attribute.OnlyAncestors)
                {
                    root = root.parent;
                }

                if (root == null)
                {
                    SetEmptyCollection(component, field);
                    foundParent = false;
                }
                else
                {
                    switch (field.kind)
                    {
                        case FieldKind.Array:
                        {
                            using PooledResource<List<Component>> parentComponentBuffer =
                                Buffers<Component>.List.Get(out List<Component> parentComponents);
                            GetParentComponents(
                                root,
                                field.elementType,
                                field.attribute,
                                field.isInterface,
                                parentComponents
                            );

                            int filteredCount = FilterComponentsInPlace(
                                parentComponents,
                                filters,
                                field.attribute,
                                field.elementType,
                                field.isInterface,
                                filterDisabledComponents: false
                            );

                            Array correctTypedArray = field.arrayCreator(filteredCount);
                            for (int i = 0; i < filteredCount; ++i)
                            {
                                correctTypedArray.SetValue(parentComponents[i], i);
                            }

                            field.setter(component, correctTypedArray);
                            foundParent = filteredCount > 0;
                            break;
                        }
                        case FieldKind.List:
                        {
                            using PooledResource<List<Component>> parentComponentBuffer =
                                Buffers<Component>.List.Get(out List<Component> parentComponents);
                            GetParentComponents(
                                root,
                                field.elementType,
                                field.attribute,
                                field.isInterface,
                                parentComponents
                            );

                            int filteredCount = FilterComponentsInPlace(
                                parentComponents,
                                filters,
                                field.attribute,
                                field.elementType,
                                field.isInterface,
                                filterDisabledComponents: false
                            );

                            IList instance = field.listCreator(filteredCount);
                            for (int i = 0; i < filteredCount; ++i)
                            {
                                instance.Add(parentComponents[i]);
                            }

                            field.setter(component, instance);
                            foundParent = filteredCount > 0;
                            break;
                        }
                        case FieldKind.HashSet:
                        {
                            using PooledResource<List<Component>> parentComponentBuffer =
                                Buffers<Component>.List.Get(out List<Component> parentComponents);
                            GetParentComponents(
                                root,
                                field.elementType,
                                field.attribute,
                                field.isInterface,
                                parentComponents
                            );

                            int filteredCount = FilterComponentsInPlace(
                                parentComponents,
                                filters,
                                field.attribute,
                                field.elementType,
                                field.isInterface,
                                filterDisabledComponents: false
                            );

                            object instance = field.hashSetCreator(filteredCount);
                            for (int i = 0; i < filteredCount; ++i)
                            {
                                field.hashSetAdder(instance, parentComponents[i]);
                            }

                            field.setter(component, instance);
                            foundParent = filteredCount > 0;
                            break;
                        }
                        default:
                        {
                            foundParent = TryGetFirstParentComponent(
                                root,
                                field.elementType,
                                field.attribute,
                                field.isInterface,
                                out Component parentComponent
                            );

                            if (foundParent)
                            {
                                field.setter(component, parentComponent);
                            }

                            break;
                        }
                    }
                }

                if (!foundParent)
                {
                    LogMissingComponentError(component, field, "parent");
                }
            }
        }

        private static List<Component> GetParentComponents(
            Transform root,
            Type elementType,
            ParentComponentAttribute attribute,
            bool isInterface,
            List<Component> buffer
        )
        {
            buffer.Clear();
            if (isInterface && attribute.AllowInterfaces)
            {
                // For interfaces, we need to manually traverse the hierarchy
                Transform current = root;
                int depth = 0;
                int maxDepth = attribute.MaxDepth > 0 ? attribute.MaxDepth : int.MaxValue;

                using PooledResource<List<Component>> parentComponentBuffer =
                    Buffers<Component>.List.Get(out List<Component> components);
                while (current != null && depth < maxDepth)
                {
                    GetComponentsOfType(
                        current,
                        elementType,
                        isInterface,
                        attribute.AllowInterfaces,
                        components
                    );
                    buffer.AddRange(components);

                    current = current.parent;
                    depth++;
                }

                return buffer;
            }

            // Use Unity's built-in method for concrete types
            Component[] allParents = root.GetComponentsInParent(
                elementType,
                includeInactive: attribute.IncludeInactive
            );

            // Filter by depth if needed
            if (attribute.MaxDepth > 0)
            {
                foreach (Component comp in allParents)
                {
                    int depth = GetDepthFromTransform(root, comp.transform);
                    // depth is steps from root: 0 = root itself, 1 = root.parent, etc.
                    // MaxDepth is how many levels to search, so depth should be < MaxDepth
                    if (depth < attribute.MaxDepth)
                    {
                        buffer.Add(comp);
                    }
                }
            }
            else
            {
                buffer.AddRange(allParents);
            }

            return buffer;
        }

        private static bool TryGetFirstParentComponent(
            Transform root,
            Type elementType,
            ParentComponentAttribute attribute,
            bool isInterface,
            out Component result
        )
        {
            FilterParameters filters = new(attribute);
            Transform current = root;
            int depth = 0;
            int maxDepth = attribute.MaxDepth > 0 ? attribute.MaxDepth : int.MaxValue;

            using PooledResource<List<Component>> scratch = Buffers<Component>.List.Get(
                out List<Component> components
            );
            while (current != null && depth < maxDepth)
            {
                if (
                    TryResolveSingleComponent(
                        current,
                        filters,
                        elementType,
                        isInterface,
                        attribute.AllowInterfaces,
                        components,
                        out Component resolved,
                        filterDisabledComponents: false
                    )
                )
                {
                    result = resolved;
                    return true;
                }

                current = current.parent;
                depth++;
            }

            result = null;
            return false;
        }

        private static int GetDepthFromTransform(Transform start, Transform target)
        {
            int depth = 0;
            Transform current = start;
            while (current != null && current != target)
            {
                current = current.parent;
                depth++;
            }
            return current == target ? depth : int.MaxValue;
        }
    }
}
