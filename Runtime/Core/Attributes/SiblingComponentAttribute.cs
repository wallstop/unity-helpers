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
    public sealed class SiblingComponentAttribute : Attribute
    {
        public bool optional = false;
    }

    public static class SiblingComponentExtensions
    {
        private static readonly Dictionary<
            Type,
            (FieldInfo field, SiblingComponentAttribute attribute, Action<object, object> setter)[]
        > FieldsByType = new();

        public static void AssignSiblingComponents(this Component component)
        {
            Type componentType = component.GetType();
            (
                FieldInfo field,
                SiblingComponentAttribute attribute,
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
                            field.IsAttributeDefined(out SiblingComponentAttribute attribute)
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
                    SiblingComponentAttribute attribute,
                    Action<object, object> setter
                ) in fields
            )
            {
                Type fieldType = field.FieldType;
                bool isArray = fieldType.IsArray;
                Type siblingComponentType = isArray ? fieldType.GetElementType() : fieldType;

                bool foundSibling;
                if (isArray)
                {
                    using PooledResource<List<Component>> componentBufferResource =
                        Buffers<Component>.List.Get();
                    List<Component> siblingComponents = componentBufferResource.resource;
                    component.GetComponents(siblingComponentType, siblingComponents);
                    foundSibling = 0 < siblingComponents.Count;

                    Array correctTypedArray = ReflectionHelpers.CreateArray(
                        siblingComponentType,
                        siblingComponents.Count
                    );
                    for (int i = 0; i < siblingComponents.Count; i++)
                    {
                        Component siblingComponent = siblingComponents[i];
                        correctTypedArray.SetValue(siblingComponent, i);
                    }

                    setter(component, correctTypedArray);
                }
                else if (
                    fieldType.IsGenericType
                    && fieldType.GetGenericTypeDefinition() == typeof(List<>)
                )
                {
                    siblingComponentType = fieldType.GenericTypeArguments[0];

                    using PooledResource<List<Component>> componentBufferResource =
                        Buffers<Component>.List.Get();
                    List<Component> siblingComponents = componentBufferResource.resource;
                    component.GetComponents(siblingComponentType, siblingComponents);

                    IList instance = ReflectionHelpers.CreateList(
                        siblingComponentType,
                        siblingComponents.Count
                    );

                    foundSibling = false;
                    foreach (Component siblingComponent in siblingComponents)
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

                if (!foundSibling && !attribute.optional)
                {
                    component.LogError($"Unable to find sibling component of type {fieldType}");
                }
            }
        }
    }
}
