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
        private enum FieldKind : byte
        {
            Single = 0,
            Array = 1,
            List = 2,
        }

        private static readonly Dictionary<
            Type,
            (
                FieldInfo field,
                ChildComponentAttribute attribute,
                Action<object, object> setter,
                Func<object, object> getter,
                FieldKind kind,
                Type elementType,
                Func<int, Array> arrayCreator,
                Func<int, IList> listCreator
            )[]
        > FieldsByType = new();

        public static void AssignChildComponents(this Component component)
        {
            Type componentType = component.GetType();
            (
                FieldInfo field,
                ChildComponentAttribute attribute,
                Action<object, object> setter,
                Func<object, object> getter,
                FieldKind kind,
                Type elementType,
                Func<int, Array> arrayCreator,
                Func<int, IList> listCreator
            )[] fields = FieldsByType.GetOrAdd(
                componentType,
                type =>
                {
                    FieldInfo[] fields = type.GetFields(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );
                    return fields
                        .Select(field =>
                        {
                            if (
                                !field.IsAttributeDefined(
                                    out ChildComponentAttribute attribute,
                                    inherit: false
                                )
                            )
                            {
                                return (null, null, null, null, default, null, null, null);
                            }

                            Type fieldType = field.FieldType;
                            FieldKind kind;
                            Type elementType;
                            Func<int, Array> arrayCreator = null;
                            Func<int, IList> listCreator = null;

                            if (fieldType.IsArray)
                            {
                                kind = FieldKind.Array;
                                elementType = fieldType.GetElementType();
                                arrayCreator = ReflectionHelpers.GetArrayCreator(elementType);
                            }
                            else if (
                                fieldType.IsGenericType
                                && fieldType.GetGenericTypeDefinition() == typeof(List<>)
                            )
                            {
                                kind = FieldKind.List;
                                elementType = fieldType.GenericTypeArguments[0];
                                listCreator = ReflectionHelpers.GetListWithCapacityCreator(
                                    elementType
                                );
                            }
                            else
                            {
                                kind = FieldKind.Single;
                                elementType = fieldType;
                            }

                            return (
                                field,
                                attribute,
                                ReflectionHelpers.GetFieldSetter(field),
                                ReflectionHelpers.GetFieldGetter(field),
                                kind,
                                elementType,
                                arrayCreator,
                                listCreator
                            );
                        })
                        .Where(tuple => tuple.attribute != null)
                        .ToArray();
                }
            );

            foreach (
                (
                    FieldInfo field,
                    ChildComponentAttribute attribute,
                    Action<object, object> setter,
                    Func<object, object> getter,
                    FieldKind kind,
                    Type elementType,
                    Func<int, Array> arrayCreator,
                    Func<int, IList> listCreator
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

                bool foundChild;

                using PooledResource<List<Transform>> childBufferResource =
                    Buffers<Transform>.List.Get();
                List<Transform> childBuffer = childBufferResource.resource;
                switch (kind)
                {
                    case FieldKind.Array:
                    {
                        using PooledResource<List<Component>> componentResource =
                            Buffers<Component>.List.Get();
                        List<Component> cache = componentResource.resource;

                        using PooledResource<List<Component>> childComponentBuffer =
                            Buffers<Component>.List.Get();
                        List<Component> childComponents = childComponentBuffer.resource;
                        if (attribute.includeInactive)
                        {
                            foreach (
                                Transform child in component.IterateOverAllChildrenRecursivelyBreadthFirst(
                                    childBuffer,
                                    includeSelf: !attribute.onlyDescendents
                                )
                            )
                            {
                                child.GetComponents(elementType, childComponents);
                                cache.AddRange(childComponents);
                            }
                        }
                        else
                        {
                            foreach (
                                Transform child in component.IterateOverAllChildrenRecursivelyBreadthFirst(
                                    childBuffer,
                                    includeSelf: !attribute.onlyDescendents
                                )
                            )
                            {
                                if (!child.gameObject.activeInHierarchy)
                                {
                                    continue;
                                }
                                child.GetComponents(elementType, childComponents);
                                foreach (Component childComponent in childComponents)
                                {
                                    if (!childComponent.IsComponentEnabled())
                                    {
                                        continue;
                                    }

                                    cache.Add(childComponent);
                                }
                            }
                        }

                        Array correctTypedArray = arrayCreator(cache.Count);
                        for (int i = 0; i < cache.Count; ++i)
                        {
                            correctTypedArray.SetValue(cache[i], i);
                        }

                        setter(component, correctTypedArray);
                        foundChild = cache.Count > 0;
                        break;
                    }
                    case FieldKind.List:
                    {
                        using PooledResource<List<Component>> cacheResource =
                            Buffers<Component>.List.Get();
                        List<Component> cache = cacheResource.resource;

                        using PooledResource<List<Component>> childComponentBuffer =
                            Buffers<Component>.List.Get();
                        List<Component> childComponents = childComponentBuffer.resource;
                        if (attribute.includeInactive)
                        {
                            foreach (
                                Transform child in component.IterateOverAllChildrenRecursivelyBreadthFirst(
                                    childBuffer,
                                    includeSelf: !attribute.onlyDescendents
                                )
                            )
                            {
                                child.GetComponents(elementType, childComponents);
                                cache.AddRange(childComponents);
                            }
                        }
                        else
                        {
                            foreach (
                                Transform child in component.IterateOverAllChildrenRecursivelyBreadthFirst(
                                    childBuffer,
                                    includeSelf: !attribute.onlyDescendents
                                )
                            )
                            {
                                if (!child.gameObject.activeInHierarchy)
                                {
                                    continue;
                                }

                                child.GetComponents(elementType, childComponents);
                                foreach (Component childComponent in childComponents)
                                {
                                    if (!childComponent.IsComponentEnabled())
                                    {
                                        continue;
                                    }

                                    cache.Add(childComponent);
                                }
                            }
                        }

                        IList instance = listCreator(cache.Count);
                        for (int i = 0; i < cache.Count; ++i)
                        {
                            instance.Add(cache[i]);
                        }

                        foundChild = cache.Count > 0;
                        setter(component, instance);
                        break;
                    }
                    default:
                    {
                        foundChild = false;
                        Component childComponent = null;
                        using PooledResource<List<Component>> childComponentBuffer =
                            Buffers<Component>.List.Get();
                        List<Component> childComponents = childComponentBuffer.resource;
                        if (attribute.includeInactive)
                        {
                            foreach (
                                Transform child in component.IterateOverAllChildrenRecursivelyBreadthFirst(
                                    childBuffer,
                                    includeSelf: !attribute.onlyDescendents
                                )
                            )
                            {
                                child.GetComponents(elementType, childComponents);
                                foreach (Component entry in childComponents)
                                {
                                    childComponent = entry;
                                    foundChild = true;
                                    break;
                                }

                                if (foundChild)
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            foreach (
                                Transform child in component.IterateOverAllChildrenRecursivelyBreadthFirst(
                                    childBuffer,
                                    includeSelf: !attribute.onlyDescendents
                                )
                            )
                            {
                                if (!child.gameObject.activeInHierarchy)
                                {
                                    continue;
                                }

                                child.GetComponents(elementType, childComponents);
                                foreach (Component entry in childComponents)
                                {
                                    if (!entry.IsComponentEnabled())
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
                        }

                        if (foundChild)
                        {
                            setter(component, childComponent);
                        }

                        break;
                    }
                }

                if (!foundChild && !attribute.optional)
                {
                    component.LogError($"Unable to find child component of type {field.FieldType}");
                }
            }
        }
    }
}
