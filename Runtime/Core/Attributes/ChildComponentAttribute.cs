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
        private static readonly List<Component> ChildCache = new();
        private static readonly Dictionary<
            Type,
            (FieldInfo field, Action<object, object> setter)[]
        > FieldsByType = new();

        public static void AssignChildComponents(this Component component)
        {
            Type componentType = component.GetType();
            (FieldInfo field, Action<object, object> setter)[] fields = FieldsByType.GetOrAdd(
                componentType,
                type =>
                {
                    FieldInfo[] fields = type.GetFields(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );
                    return fields
                        .Where(field => Attribute.IsDefined(field, typeof(ChildComponentAttribute)))
                        .Select(field => (field, ReflectionHelpers.GetFieldSetter(field)))
                        .ToArray();
                }
            );

            foreach ((FieldInfo field, Action<object, object> setter) in fields)
            {
                Type fieldType = field.FieldType;
                bool isArray = fieldType.IsArray;
                Type childComponentType = isArray ? fieldType.GetElementType() : fieldType;

                bool foundChild;
                ChildComponentAttribute customAttribute =
                    field.GetCustomAttribute<ChildComponentAttribute>();
                if (customAttribute.onlyDescendents)
                {
                    if (isArray)
                    {
                        ChildCache.Clear();
                        foreach (Transform child in component.IterateOverAllChildren())
                        {
                            foreach (
                                Component childComponent in child.GetComponentsInChildren(
                                    childComponentType,
                                    customAttribute.includeInactive
                                )
                            )
                            {
                                ChildCache.Add(childComponent);
                            }
                        }

                        foundChild = 0 < ChildCache.Count;

                        Array correctTypedArray = ReflectionHelpers.CreateArray(
                            childComponentType,
                            ChildCache.Count
                        );
                        Array.Copy(ChildCache.ToArray(), correctTypedArray, ChildCache.Count);
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
                        foreach (
                            Component childComponent in component
                                .IterateOverAllChildren()
                                .SelectMany(child =>
                                    child.GetComponentsInChildren(
                                        childComponentType,
                                        customAttribute.includeInactive
                                    )
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
                        foundChild = false;
                        Component childComponent = null;
                        foreach (
                            Transform child in component.IterateOverAllChildrenRecursivelyBreadthFirst()
                        )
                        {
                            childComponent = child.GetComponent(childComponentType);
                            if (
                                childComponent == null
                                || (
                                    !customAttribute.includeInactive
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
                            customAttribute.includeInactive
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
                                customAttribute.includeInactive
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
                            customAttribute.includeInactive
                        );
                        foundChild = childComponent != null;
                        if (foundChild)
                        {
                            setter(component, childComponent);
                        }
                    }
                }

                if (
                    !foundChild
                    && field.GetCustomAttributes(typeof(ChildComponentAttribute), false)[0]
                        is ChildComponentAttribute { optional: false }
                )
                {
                    component.LogError($"Unable to find child component of type {fieldType}");
                }
            }
        }
    }
}
