namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Extension;
    using Helper;
    using JetBrains.Annotations;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Utils;

    [AttributeUsage(AttributeTargets.Field)]
    [MeansImplicitUse]
    public sealed class ChildComponentAttribute : Attribute
    {
        public bool optional = false;
        public bool includeInactive = true;
        public bool onlyDescendents = false;
    }

    public static class ChildComponentExtensions
    {
        private static readonly Dictionary<
            Type,
            (FieldInfo field, ChildComponentAttribute attribute, Action<object, object> setter)[]
        > FieldsByType = new();

        public static void AssignChildComponents(this Component component)
        {
            Type componentType = component.GetType();
            (
                FieldInfo field,
                ChildComponentAttribute attribute,
                Action<object, object> setter
            )[] fields = FieldsByType.GetOrAdd(
                componentType,
                type =>
                {
                    FieldInfo[] fields = type.GetFields(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );
                    return fields
                        .Select(field =>
                            field.IsAttributeDefined(out ChildComponentAttribute attribute)
                                ? (field, attribute, ReflectionHelpers.GetFieldSetter(field))
                                : (null, null, null)
                        )
                        .Where(tuple => tuple.attribute != null)
                        .ToArray();
                }
            );

            foreach (
                (
                    FieldInfo field,
                    ChildComponentAttribute attribute,
                    Action<object, object> setter
                ) in fields
            )
            {
                Type fieldType = field.FieldType;
                bool isArray = fieldType.IsArray;
                Type childComponentType = isArray ? fieldType.GetElementType() : fieldType;
                bool foundChild;
                if (attribute.onlyDescendents)
                {
                    using PooledResource<List<Transform>> childBufferResource =
                        Buffers<Transform>.List.Get();
                    List<Transform> childBuffer = childBufferResource.resource;
                    if (isArray)
                    {
                        using PooledResource<List<Component>> componentResource =
                            Buffers<Component>.List.Get();
                        List<Component> cache = componentResource.resource;
                        foreach (Transform child in component.IterateOverAllChildren(childBuffer))
                        {
                            foreach (
                                Component childComponent in child.GetComponentsInChildren(
                                    childComponentType,
                                    attribute.includeInactive
                                )
                            )
                            {
                                cache.Add(childComponent);
                            }
                        }

                        foundChild = 0 < cache.Count;

                        Array correctTypedArray = ReflectionHelpers.CreateArray(
                            childComponentType,
                            cache.Count
                        );
                        Array.Copy(cache.ToArray(), correctTypedArray, cache.Count);
                        setter(component, correctTypedArray);
                    }
                    else if (
                        fieldType.IsGenericType
                        && fieldType.GetGenericTypeDefinition() == typeof(List<>)
                    )
                    {
                        childComponentType = fieldType.GenericTypeArguments[0];

                        IList instance = ReflectionHelpers.CreateList(childComponentType);
                        foundChild = false;
                        foreach (Transform child in component.IterateOverAllChildren(childBuffer))
                        {
                            foreach (
                                Component childComponent in child.GetComponentsInChildren(
                                    childComponentType,
                                    attribute.includeInactive
                                )
                            )
                            {
                                instance.Add(childComponent);
                                foundChild = true;
                            }
                        }

                        setter(component, instance);
                    }
                    else
                    {
                        foundChild = false;
                        Component childComponent = null;
                        foreach (
                            Transform child in component.IterateOverAllChildrenRecursivelyBreadthFirst(
                                childBuffer
                            )
                        )
                        {
                            childComponent = child.GetComponent(childComponentType);
                            if (
                                childComponent == null
                                || (
                                    !attribute.includeInactive
                                    && (
                                        !childComponent.gameObject.activeInHierarchy
                                        || childComponent is Behaviour { enabled: false }
                                    )
                                )
                            )
                            {
                                continue;
                            }

                            foundChild = true;
                            break;
                        }
                        if (foundChild)
                        {
                            setter(component, childComponent);
                        }
                    }
                }
                else
                {
                    if (isArray)
                    {
                        Component[] childComponents = component.GetComponentsInChildren(
                            childComponentType,
                            attribute.includeInactive
                        );
                        foundChild = 0 < childComponents.Length;

                        Array correctTypedArray = ReflectionHelpers.CreateArray(
                            childComponentType,
                            childComponents.Length
                        );
                        Array.Copy(childComponents, correctTypedArray, childComponents.Length);
                        setter(component, correctTypedArray);
                    }
                    else if (
                        fieldType.IsGenericType
                        && fieldType.GetGenericTypeDefinition() == typeof(List<>)
                    )
                    {
                        childComponentType = fieldType.GenericTypeArguments[0];

                        Component[] children = component.GetComponentsInChildren(
                            childComponentType,
                            true
                        );

                        IList instance = ReflectionHelpers.CreateList(
                            childComponentType,
                            children.Length
                        );
                        foundChild = false;
                        foreach (
                            Component childComponent in component.GetComponentsInChildren(
                                childComponentType,
                                attribute.includeInactive
                            )
                        )
                        {
                            instance.Add(childComponent);
                            foundChild = true;
                        }

                        setter(component, instance);
                    }
                    else
                    {
                        Component childComponent = component.GetComponentInChildren(
                            childComponentType,
                            attribute.includeInactive
                        );
                        foundChild = childComponent != null;
                        if (foundChild)
                        {
                            setter(component, childComponent);
                        }
                    }
                }

                if (!foundChild && !attribute.optional)
                {
                    component.LogError($"Unable to find child component of type {fieldType}");
                }
            }
        }
    }
}
