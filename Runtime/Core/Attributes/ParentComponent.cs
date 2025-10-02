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
    using Object = UnityEngine.Object;

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ParentComponentAttribute : Attribute
    {
        public bool optional = false;
        public bool includeInactive = true;
        public bool onlyAncestors = false;
        public bool skipIfAssigned = false;
    }

    public static class ParentComponentExtensions
    {
        private static readonly Dictionary<
            Type,
            (
                FieldInfo field,
                ParentComponentAttribute attribute,
                Action<object, object> setter,
                Func<object, object> getter
            )[]
        > FieldsByType = new();

        public static void AssignParentComponents(this Component component)
        {
            Type componentType = component.GetType();
            (
                FieldInfo field,
                ParentComponentAttribute attribute,
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
                                out ParentComponentAttribute attribute,
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
                    ParentComponentAttribute attribute,
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
                Type parentComponentType = isArray ? fieldType.GetElementType() : fieldType;
                bool foundParent;
                Transform root = component.transform;
                if (attribute.onlyAncestors)
                {
                    root = root.parent;
                }

                if (root == null)
                {
                    if (isArray)
                    {
                        Array correctTypedArray = ReflectionHelpers.CreateArray(
                            parentComponentType,
                            0
                        );
                        setter(component, correctTypedArray);
                    }
                    else if (
                        fieldType.IsGenericType
                        && fieldType.GetGenericTypeDefinition() == typeof(List<>)
                    )
                    {
                        parentComponentType = fieldType.GenericTypeArguments[0];
                        IList instance = ReflectionHelpers.CreateList(parentComponentType, 0);
                        setter(component, instance);
                    }

                    foundParent = false;
                }
                else
                {
                    if (isArray)
                    {
                        Component[] parentComponents = root.GetComponentsInParent(
                            parentComponentType,
                            attribute.includeInactive
                        );

                        Array correctTypedArray = ReflectionHelpers.CreateArray(
                            parentComponentType,
                            parentComponents.Length
                        );
                        Array.Copy(parentComponents, correctTypedArray, parentComponents.Length);
                        setter(component, correctTypedArray);
                        foundParent = 0 < parentComponents.Length;
                    }
                    else if (
                        fieldType.IsGenericType
                        && fieldType.GetGenericTypeDefinition() == typeof(List<>)
                    )
                    {
                        parentComponentType = fieldType.GenericTypeArguments[0];

                        Component[] parents = root.GetComponentsInParent(
                            parentComponentType,
                            attribute.includeInactive
                        );

                        IList instance = ReflectionHelpers.CreateList(
                            parentComponentType,
                            parents.Length
                        );

                        foreach (Component parentComponent in parents)
                        {
                            instance.Add(parentComponent);
                        }

                        setter(component, instance);
                        foundParent = instance.Count > 0;
                    }
                    else
                    {
                        Component parentComponent = root.GetComponentInParent(
                            parentComponentType,
                            attribute.includeInactive
                        );

                        if (parentComponent != null)
                        {
                            setter(component, parentComponent);
                            foundParent = true;
                        }
                        else
                        {
                            foundParent = false;
                        }
                    }
                }

                if (!foundParent && !attribute.optional)
                {
                    component.LogError($"Unable to find parent component of type {fieldType}");
                }
            }
        }
    }
}
