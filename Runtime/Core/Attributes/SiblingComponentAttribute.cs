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
    /// Supports single components, arrays, List&lt;T&gt;, and HashSet&lt;T&gt; collection types.
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
                        using PooledResource<List<Component>> componentBuffer =
                            Buffers<Component>.List.Get(out List<Component> components);
                        GetComponentsOfType(
                            component.gameObject,
                            metadata.elementType,
                            metadata.isInterface,
                            metadata.attribute.AllowInterfaces,
                            components
                        );

                        int filteredCount = FilterComponentsInPlace(
                            components,
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
                            component.gameObject,
                            metadata.elementType,
                            metadata.isInterface,
                            metadata.attribute.AllowInterfaces,
                            components
                        );

                        int filteredCount = FilterComponentsInPlace(
                            components,
                            metadata.attribute,
                            metadata.elementType,
                            metadata.isInterface
                        );

                        IList instance = metadata.listCreator(filteredCount);
                        for (int i = 0; i < filteredCount; ++i)
                        {
                            instance.Add(components[i]);
                        }

                        metadata.setter(component, instance);
                        foundSibling = filteredCount > 0;
                        break;
                    }
                    case FieldKind.HashSet:
                    {
                        using PooledResource<List<Component>> componentBuffer =
                            Buffers<Component>.List.Get(out List<Component> components);
                        GetComponentsOfType(
                            component.gameObject,
                            metadata.elementType,
                            metadata.isInterface,
                            metadata.attribute.AllowInterfaces,
                            components
                        );

                        int filteredCount = FilterComponentsInPlace(
                            components,
                            metadata.attribute,
                            metadata.elementType,
                            metadata.isInterface
                        );

                        object instance = metadata.hashSetCreator(filteredCount);
                        for (int i = 0; i < filteredCount; ++i)
                        {
                            metadata.hashSetAdder(instance, components[i]);
                        }

                        metadata.setter(component, instance);

                        foundSibling = filteredCount > 0;
                        break;
                    }
                    default:
                    {
                        Component siblingComponent = null;
                        if (metadata.attribute.IncludeInactive || isGameObjectActive)
                        {
                            siblingComponent = TryResolveSingleComponent(
                                component.gameObject,
                                metadata.attribute,
                                metadata.elementType,
                                metadata.isInterface,
                                metadata.attribute.AllowInterfaces,
                                null
                            );
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
