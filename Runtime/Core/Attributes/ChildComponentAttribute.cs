namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Extension;
    using Helper;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Utils;
    using Object = UnityEngine.Object;

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ChildComponentAttribute : Attribute
    {
        public bool optional = false;
        public bool includeInactive = true;
        public bool onlyDescendents = false;
        public bool skipIfAssigned = false;
    }

    public static class ChildComponentExtensions
    {
        private static readonly Dictionary<
            Type,
            (
                FieldInfo field,
                ChildComponentAttribute attribute,
                Action<object, object> setter,
                Func<object, object> getter
            )[]
        > FieldsByType = new();

        public static void AssignChildComponents(this Component component)
        {
            Type componentType = component.GetType();
            (
                FieldInfo field,
                ChildComponentAttribute attribute,
                Action<object, object> setter,
                Func<object, object> getter
            )[] fields = FieldsByType.GetOrAdd(
                componentType,
                type =>
                {
                    FieldInfo[] fields = type.GetFields(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );
                    return fields
                        .Select(field =>
                            field.IsAttributeDefined(
                                out ChildComponentAttribute attribute,
                                inherit: false
                            )
                                ? (
                                    field,
                                    attribute,
                                    ReflectionHelpers.GetFieldSetter(field),
                                    ReflectionHelpers.GetFieldGetter(field)
                                )
                                : (null, null, null, null)
                        )
                        .Where(tuple => tuple.attribute != null)
                        .ToArray();
                }
            );

            foreach (
                (
                    FieldInfo field,
                    ChildComponentAttribute attribute,
                    Action<object, object> setter,
                    Func<object, object> getter
                ) in fields
            )
            {
                if (attribute.skipIfAssigned)
                {
                    object currentValue = getter(component);
                    if (ValueHelpers.IsAssigned(currentValue))
                    {
                        continue;
                    }
                }

                Type fieldType = field.FieldType;
                bool isArray = fieldType.IsArray;
                Type childComponentType = isArray ? fieldType.GetElementType() : fieldType;
                bool foundChild;

                using PooledResource<List<Transform>> childBufferResource =
                    Buffers<Transform>.List.Get();
                List<Transform> childBuffer = childBufferResource.resource;
                if (isArray)
                {
                    using PooledResource<List<Component>> componentResource =
                        Buffers<Component>.List.Get();
                    List<Component> cache = componentResource.resource;

                    using PooledResource<List<Component>> childComponentBuffer =
                        Buffers<Component>.List.Get();
                    List<Component> childComponents = childComponentBuffer.resource;
                    foreach (
                        Transform child in component.IterateOverAllChildrenRecursivelyBreadthFirst(
                            childBuffer,
                            includeSelf: !attribute.onlyDescendents
                        )
                    )
                    {
                        if (!attribute.includeInactive && !child.gameObject.activeInHierarchy)
                        {
                            continue;
                        }
                        child.GetComponents(childComponentType, childComponents);
                        foreach (Component childComponent in childComponents)
                        {
                            if (
                                !attribute.includeInactive
                                && !ReflectionHelpers.IsComponentEnabled(childComponent)
                            )
                            {
                                continue;
                            }

                            cache.Add(childComponent);
                        }
                    }

                    foundChild = 0 < cache.Count;

                    using PooledResource<Component[]> arrayResource =
                        WallstopFastArrayPool<Component>.Get(cache.Count);
                    Component[] array = arrayResource.resource;
                    cache.CopyTo(array);

                    Array correctTypedArray = ReflectionHelpers.CreateArray(
                        childComponentType,
                        cache.Count
                    );
                    Array.Copy(array, correctTypedArray, cache.Count);
                    setter(component, correctTypedArray);
                }
                else if (
                    fieldType.IsGenericType
                    && fieldType.GetGenericTypeDefinition() == typeof(List<>)
                )
                {
                    childComponentType = fieldType.GenericTypeArguments[0];
                    IList instance = ReflectionHelpers.CreateList(childComponentType);
                    using PooledResource<List<Component>> childComponentBuffer =
                        Buffers<Component>.List.Get();
                    List<Component> childComponents = childComponentBuffer.resource;
                    foreach (
                        Transform child in component.IterateOverAllChildrenRecursivelyBreadthFirst(
                            childBuffer,
                            includeSelf: !attribute.onlyDescendents
                        )
                    )
                    {
                        if (!attribute.includeInactive && !child.gameObject.activeInHierarchy)
                        {
                            continue;
                        }

                        child.GetComponents(childComponentType, childComponents);
                        foreach (Component childComponent in childComponents)
                        {
                            if (
                                !attribute.includeInactive
                                && !ReflectionHelpers.IsComponentEnabled(childComponent)
                            )
                            {
                                continue;
                            }

                            instance.Add(childComponent);
                        }
                    }

                    foundChild = instance.Count > 0;
                    setter(component, instance);
                }
                else
                {
                    foundChild = false;
                    Component childComponent = null;
                    using PooledResource<List<Component>> childComponentBuffer =
                        Buffers<Component>.List.Get();
                    List<Component> childComponents = childComponentBuffer.resource;
                    foreach (
                        Transform child in component.IterateOverAllChildrenRecursivelyBreadthFirst(
                            childBuffer,
                            includeSelf: !attribute.onlyDescendents
                        )
                    )
                    {
                        if (!attribute.includeInactive && !child.gameObject.activeInHierarchy)
                        {
                            continue;
                        }
                        child.GetComponents(childComponentType, childComponents);
                        foreach (Component entry in childComponents)
                        {
                            if (
                                !attribute.includeInactive
                                && !ReflectionHelpers.IsComponentEnabled(entry)
                            )
                            {
                                continue;
                            }
                            childComponent = entry;
                            foundChild = true;
                            break;
                        }

                        if (foundChild)
                        {
                            break;
                        }
                    }
                    if (foundChild)
                    {
                        setter(component, childComponent);
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
