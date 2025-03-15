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
    public sealed class SiblingComponentAttribute : Attribute
    {
        public bool optional = false;
    }

    public static class SiblingComponentExtensions
    {
        private static readonly Dictionary<
            Type,
            (FieldInfo field, Action<object, object> setter)[]
        > FieldsByType = new();

        public static void AssignSiblingComponents(this Component component)
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
                            Attribute.IsDefined(field, typeof(SiblingComponentAttribute))
                        )
                        .Select(field => (field, ReflectionHelpers.CreateFieldSetter(type, field)))
                        .ToArray();
                }
            );

            foreach ((FieldInfo field, Action<object, object> setter) in fields)
            {
                Type fieldType = field.FieldType;
                bool isArray = fieldType.IsArray;
                Type siblingComponentType = isArray ? fieldType.GetElementType() : fieldType;

                bool foundSibling;
                if (isArray)
                {
                    Component[] siblingComponents = component.GetComponents(siblingComponentType);
                    foundSibling = 0 < siblingComponents.Length;

                    Array correctTypedArray = ReflectionHelpers.CreateArray(
                        siblingComponentType,
                        siblingComponents.Length
                    );
                    Array.Copy(siblingComponents, correctTypedArray, siblingComponents.Length);
                    setter(component, correctTypedArray);
                }
                else if (
                    fieldType.IsGenericType
                    && fieldType.GetGenericTypeDefinition() == typeof(List<>)
                )
                {
                    siblingComponentType = fieldType.GenericTypeArguments[0];

                    Component[] siblings = component.GetComponents(siblingComponentType);

                    IList instance = ReflectionHelpers.CreateList(
                        siblingComponentType,
                        siblings.Length
                    );

                    foundSibling = false;
                    foreach (Component siblingComponent in siblings)
                    {
                        instance.Add(siblingComponent);
                        foundSibling = true;
                    }

                    setter(component, instance);
                }
                else
                {
                    if (
                        component.TryGetComponent(
                            siblingComponentType,
                            out Component siblingComponent
                        )
                    )
                    {
                        foundSibling = true;
                        setter(component, siblingComponent);
                    }
                    else
                    {
                        foundSibling = false;
                    }
                }

                if (!foundSibling)
                {
                    if (
                        field.GetCustomAttributes(typeof(SiblingComponentAttribute), false)[0]
                        is SiblingComponentAttribute { optional: false } _
                    )
                    {
                        component.LogError($"Unable to find sibling component of type {fieldType}");
                    }
                }
            }
        }
    }
}
