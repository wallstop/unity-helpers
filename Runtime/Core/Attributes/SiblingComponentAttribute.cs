namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Utils;
    using static RelationalComponentProcessor;

    /// <summary>
    /// Automatically assigns sibling components (components on the same GameObject) to the decorated field.
    /// Supports single components, arrays, and List&lt;T&gt; collection types.
    /// </summary>
    /// <remarks>
    /// Call <see cref="SiblingComponentExtensions.AssignSiblingComponents"/> to populate the field.
    /// This is typically done in Awake() or OnEnable().
    ///
    /// IMPORTANT: This attribute populates fields at runtime, not during Unity serialization in Edit mode.
    /// Fields populated by this attribute will not be serialized by Unity.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SiblingComponentAttribute : BaseRelationalComponentAttribute { }

    public static class SiblingComponentExtensions
    {
        private static readonly Dictionary<
            Type,
            FieldMetadata<SiblingComponentAttribute>[]
        > FieldsByType = new();

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
                bool isGameObjectActive = component.gameObject.activeInHierarchy;

                switch (metadata.kind)
                {
                    case FieldKind.Array:
                    {
                        using PooledResource<List<Component>> componentBufferResource =
                            Buffers<Component>.List.Get();
                        List<Component> siblingComponents = componentBufferResource.resource;

                        using PooledResource<List<Component>> componentBuffer =
                            Buffers<Component>.List.Get(out List<Component> components);
                        GetComponentsOfType(
                            component.gameObject,
                            metadata.elementType,
                            metadata.isInterface,
                            metadata.attribute.AllowInterfaces,
                            components
                        );
                        siblingComponents.AddRange(components);

                        using PooledResource<List<Component>> filteredBuffer =
                            Buffers<Component>.List.Get(out List<Component> filtered);
                        FilterComponents(
                            siblingComponents,
                            metadata.attribute,
                            metadata.elementType,
                            metadata.isInterface,
                            filtered
                        );

                        foundSibling = filtered.Count > 0;

                        Array correctTypedArray = metadata.arrayCreator(filtered.Count);
                        for (int i = 0; i < filtered.Count; ++i)
                        {
                            correctTypedArray.SetValue(filtered[i], i);
                        }

                        metadata.setter(component, correctTypedArray);
                        break;
                    }
                    case FieldKind.List:
                    {
                        using PooledResource<List<Component>> componentBufferResource =
                            Buffers<Component>.List.Get();
                        List<Component> siblingComponents = componentBufferResource.resource;

                        using PooledResource<List<Component>> componentBuffer =
                            Buffers<Component>.List.Get(out List<Component> components);
                        GetComponentsOfType(
                            component.gameObject,
                            metadata.elementType,
                            metadata.isInterface,
                            metadata.attribute.AllowInterfaces,
                            components
                        );
                        siblingComponents.AddRange(components);

                        using PooledResource<List<Component>> filteredBuffer =
                            Buffers<Component>.List.Get(out List<Component> filtered);
                        FilterComponents(
                            siblingComponents,
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

                        metadata.setter(component, instance);
                        foundSibling = instance.Count > 0;
                        break;
                    }
                    default:
                    {
                        Component siblingComponent = null;

                        if (metadata.attribute.IncludeInactive || isGameObjectActive)
                        {
                            using PooledResource<List<Component>> componentBufferResource =
                                Buffers<Component>.List.Get();
                            List<Component> siblingComponents = componentBufferResource.resource;

                            using PooledResource<List<Component>> componentBuffer =
                                Buffers<Component>.List.Get(out List<Component> components);

                            GetComponentsOfType(
                                component.gameObject,
                                metadata.elementType,
                                metadata.isInterface,
                                metadata.attribute.AllowInterfaces,
                                components
                            );
                            siblingComponents.AddRange(components);

                            using PooledResource<List<Component>> filteredBuffer =
                                Buffers<Component>.List.Get(out List<Component> filtered);
                            FilterComponents(
                                siblingComponents,
                                metadata.attribute,
                                metadata.elementType,
                                metadata.isInterface,
                                filtered
                            );

                            if (filtered.Count > 0)
                            {
                                siblingComponent = filtered[0];
                            }
                        }

                        if (siblingComponent != null)
                        {
                            foundSibling = true;
                            metadata.setter(component, siblingComponent);
                        }
                        else
                        {
                            foundSibling = false;
                        }

                        break;
                    }
                }

                if (!foundSibling)
                {
                    LogMissingComponentError(component, metadata, "sibling");
                }
            }
        }
    }
}
