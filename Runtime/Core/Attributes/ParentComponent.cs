namespace UnityHelpers.Core.Attributes
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
    public sealed class ParentComponentAttribute : Attribute
    {
        public bool optional = false;
        public bool onlyAncestors = false;
    }

    public static class ParentComponentExtensions
    {
        private static readonly Dictionary<
            Type,
            (FieldInfo field, Action<object, object> setter)[]
        > FieldsByType = new();

        public static void AssignParentComponents(this Component component)
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
                        .Where(field =>
                            Attribute.IsDefined(field, typeof(ParentComponentAttribute))
                        )
                        .Select(field => (field, ReflectionHelpers.CreateFieldSetter(type, field)))
                        .ToArray();
                }
            );

            foreach ((FieldInfo field, Action<object, object> setter) in fields)
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

                        Array correctTypedArray = ReflectionHelpers.CreateArray(
                            parentComponentType,
                            parentComponents.Length
                        );
                        Array.Copy(parentComponents, correctTypedArray, parentComponents.Length);
                        setter(component, correctTypedArray);
                    }
                    else if (
                        fieldType.IsGenericType
                        && fieldType.GetGenericTypeDefinition() == typeof(List<>)
                    )
                    {
                        parentComponentType = fieldType.GenericTypeArguments[0];

                        Component[] parents = parent.GetComponentsInParent(
                            parentComponentType,
                            true
                        );

                        IList instance = ReflectionHelpers.CreateList(
                            parentComponentType,
                            parents.Length
                        );

                        foundParent = false;
                        foreach (Component parentComponent in parents)
                        {
                            instance.Add(parentComponent);
                            foundParent = true;
                        }

                        setter(component, instance);
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
                            setter(component, childComponent);
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

                        Array correctTypedArray = ReflectionHelpers.CreateArray(
                            parentComponentType,
                            parentComponents.Length
                        );
                        Array.Copy(parentComponents, correctTypedArray, parentComponents.Length);
                        setter(component, correctTypedArray);
                    }
                    else if (
                        fieldType.IsGenericType
                        && fieldType.GetGenericTypeDefinition() == typeof(List<>)
                    )
                    {
                        parentComponentType = fieldType.GenericTypeArguments[0];

                        Component[] parents = component.GetComponentsInParent(
                            parentComponentType,
                            true
                        );

                        IList instance = ReflectionHelpers.CreateList(
                            parentComponentType,
                            parents.Length
                        );

                        foundParent = false;
                        foreach (Component parentComponent in parents)
                        {
                            instance.Add(parentComponent);
                            foundParent = true;
                        }
                        setter(component, instance);
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
                            setter(component, childComponent);
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
