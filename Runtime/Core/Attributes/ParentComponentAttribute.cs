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
                ParentComponentAttribute attribute,
                Action<object, object> setter,
                Func<object, object> getter,
                FieldKind kind,
                Type elementType,
                Func<int, Array> arrayCreator,
                Func<int, IList> listCreator
            )[]
        > FieldsByType = new();

        public static void AssignParentComponents(this Component component)
        {
            Type componentType = component.GetType();
            (
                FieldInfo field,
                ParentComponentAttribute attribute,
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
                                    out ParentComponentAttribute attribute,
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
                    ParentComponentAttribute attribute,
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

                bool foundParent;
                Transform root = component.transform;
                if (attribute.onlyAncestors)
                {
                    root = root.parent;
                }

                if (root == null)
                {
                    switch (kind)
                    {
                        case FieldKind.Array:
                        {
                            setter(component, arrayCreator(0));
                            break;
                        }
                        case FieldKind.List:
                        {
                            setter(component, listCreator(0));
                            break;
                        }
                    }

                    foundParent = false;
                }
                else
                {
                    switch (kind)
                    {
                        case FieldKind.Array:
                        {
                            Component[] parentComponents = root.GetComponentsInParent(
                                elementType,
                                attribute.includeInactive
                            );

                            Array correctTypedArray = arrayCreator(parentComponents.Length);
                            Array.Copy(
                                parentComponents,
                                correctTypedArray,
                                parentComponents.Length
                            );
                            setter(component, correctTypedArray);
                            foundParent = parentComponents.Length > 0;
                            break;
                        }
                        case FieldKind.List:
                        {
                            Component[] parents = root.GetComponentsInParent(
                                elementType,
                                attribute.includeInactive
                            );

                            IList instance = listCreator(parents.Length);
                            for (int i = 0; i < parents.Length; ++i)
                            {
                                instance.Add(parents[i]);
                            }

                            setter(component, instance);
                            foundParent = parents.Length > 0;
                            break;
                        }
                        default:
                        {
                            Component parentComponent = root.GetComponentInParent(
                                elementType,
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

                            break;
                        }
                    }
                }

                if (!foundParent && !attribute.optional)
                {
                    component.LogError(
                        $"Unable to find parent component of type {field.FieldType}"
                    );
                }
            }
        }
    }
}
