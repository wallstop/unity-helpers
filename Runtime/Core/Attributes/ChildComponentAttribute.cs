﻿namespace UnityHelpers.Core.Attributes
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
        public bool onlyDescendents = false;
    }

    public static class ChildComponentExtensions
    {
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
                        .Select(field => (field, ReflectionHelpers.CreateFieldSetter(type, field)))
                        .ToArray();
                }
            );

            foreach ((FieldInfo field, Action<object, object> setter) in fields)
            {
                Type fieldType = field.FieldType;
                bool isArray = fieldType.IsArray;
                Type childComponentType = isArray ? fieldType.GetElementType() : fieldType;

                bool foundChild;
                if (field.GetCustomAttribute<ChildComponentAttribute>().onlyDescendents)
                {
                    if (isArray)
                    {
                        List<Component> children = new();
                        foreach (Transform child in component.IterateOverAllChildren())
                        {
                            children.AddRange(
                                child.GetComponentsInChildren(childComponentType, true)
                            );
                        }

                        foundChild = 0 < children.Count;

                        Array correctTypedArray = ReflectionHelpers.CreateArray(
                            childComponentType,
                            children.Count
                        );
                        Array.Copy(children.ToArray(), correctTypedArray, children.Count);
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
                                    child.GetComponentsInChildren(childComponentType, true)
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
                        foreach (Transform child in component.IterateOverAllChildren())
                        {
                            childComponent = child.GetComponent(childComponentType);
                            if (childComponent != null)
                            {
                                foundChild = true;
                                break;
                            }
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
                            true
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
                                true
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
                            true
                        );
                        foundChild = childComponent != null;
                        if (foundChild)
                        {
                            setter(component, childComponent);
                        }
                    }
                }

                if (!foundChild)
                {
                    if (
                        field.GetCustomAttributes(typeof(ChildComponentAttribute), false)[0]
                        is ChildComponentAttribute { optional: false } _
                    )
                    {
                        component.LogError($"Unable to find child component of type {fieldType}");
                    }
                }
            }
        }
    }
}
