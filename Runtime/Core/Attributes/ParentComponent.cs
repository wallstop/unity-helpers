﻿namespace UnityHelpers.Core.Attributes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Extension;
    using JetBrains.Annotations;
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Field)]
    [MeansImplicitUse]
    public sealed class ParentComponentAttribute : Attribute
    {
        public bool optional = false;
        public bool onlyAncestors = false;
    }

    public static class ParentComponentExtensions
    {
        private static readonly Dictionary<Type, FieldInfo[]> FieldsByType = new();

        public static void AssignParentComponents(this Component component)
        {
            Type componentType = component.GetType();
            FieldInfo[] fields = FieldsByType.GetOrAdd(
                componentType,
                type =>
                {
                    FieldInfo[] fields = type.GetFields(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    );
                    return fields
                        .Where(field =>
                            Attribute.IsDefined(field, typeof(ParentComponentAttribute))
                        )
                        .ToArray();
                }
            );

            foreach (FieldInfo field in fields)
            {
                Type fieldType = field.FieldType;
                bool isArray = fieldType.IsArray;
                Type parentComponentType = isArray ? fieldType.GetElementType() : fieldType;

                bool foundParent;
                if (field.GetCustomAttribute<ParentComponentAttribute>().onlyAncestors)
                {
                    Transform parent = component.transform.parent;
                    if (parent == null)
                    {
                        foundParent = false;
                    }
                    else if (isArray)
                    {
                        Component[] parentComponents = parent.GetComponentsInParent(
                            parentComponentType,
                            true
                        );
                        foundParent = 0 < parentComponents.Length;

                        Array correctTypedArray = Array.CreateInstance(
                            parentComponentType,
                            parentComponents.Length
                        );
                        Array.Copy(parentComponents, correctTypedArray, parentComponents.Length);
                        field.SetValue(component, correctTypedArray);
                    }
                    else if (
                        fieldType.IsGenericType
                        && fieldType.GetGenericTypeDefinition() == typeof(List<>)
                    )
                    {
                        parentComponentType = fieldType.GenericTypeArguments[0];
                        Type constructedListType = typeof(List<>).MakeGenericType(
                            parentComponentType
                        );
                        IList instance = (IList)Activator.CreateInstance(constructedListType);

                        foundParent = false;
                        foreach (
                            Component parentComponent in parent.GetComponentsInParent(
                                parentComponentType,
                                true
                            )
                        )
                        {
                            instance.Add(parentComponent);
                            foundParent = true;
                        }

                        field.SetValue(component, instance);
                    }
                    else
                    {
                        Component childComponent = parent.GetComponentInParent(
                            parentComponentType,
                            true
                        );
                        foundParent = childComponent != null;
                        if (foundParent)
                        {
                            field.SetValue(component, childComponent);
                        }
                    }
                }
                else
                {
                    if (isArray)
                    {
                        Component[] parentComponents = component.GetComponentsInParent(
                            parentComponentType,
                            true
                        );
                        foundParent = 0 < parentComponents.Length;

                        Array correctTypedArray = Array.CreateInstance(
                            parentComponentType,
                            parentComponents.Length
                        );
                        Array.Copy(parentComponents, correctTypedArray, parentComponents.Length);
                        field.SetValue(component, correctTypedArray);
                    }
                    else if (
                        fieldType.IsGenericType
                        && fieldType.GetGenericTypeDefinition() == typeof(List<>)
                    )
                    {
                        parentComponentType = fieldType.GenericTypeArguments[0];
                        Type constructedListType = typeof(List<>).MakeGenericType(
                            parentComponentType
                        );
                        IList instance = (IList)Activator.CreateInstance(constructedListType);

                        foundParent = false;
                        foreach (
                            Component parentComponent in component.GetComponentsInParent(
                                parentComponentType,
                                true
                            )
                        )
                        {
                            instance.Add(parentComponent);
                            foundParent = true;
                        }

                        field.SetValue(component, instance);
                    }
                    else
                    {
                        Component childComponent = component.GetComponentInParent(
                            parentComponentType,
                            true
                        );
                        foundParent = childComponent != null;
                        if (foundParent)
                        {
                            field.SetValue(component, childComponent);
                        }
                    }
                }

                if (!foundParent)
                {
                    if (
                        field.GetCustomAttributes(typeof(ParentComponentAttribute), false)[0]
                        is ParentComponentAttribute { optional: false } _
                    )
                    {
                        component.LogError($"Unable to find parent component of type {fieldType}");
                    }
                }
            }
        }
    }
}
