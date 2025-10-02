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
    public sealed class SiblingComponentAttribute : Attribute
    {
        public bool optional = false;
        public bool includeInactive = true;
        public bool skipIfAssigned = false;
    }

    public static class SiblingComponentExtensions
    {
        private enum FieldKind : byte
        {
            Single,
            Array,
            List,
        }

        private static readonly Dictionary<
            Type,
            (
                FieldInfo field,
                SiblingComponentAttribute attribute,
                Action<object, object> setter,
                Func<object, object> getter,
                FieldKind kind,
                Type elementType,
                Func<int, Array> arrayCreator,
                Func<int, IList> listCreator
            )[]
        > FieldsByType = new();

        public static void AssignSiblingComponents(this Component component)
        {
            Type componentType = component.GetType();
            (
                FieldInfo field,
                SiblingComponentAttribute attribute,
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
                                    out SiblingComponentAttribute attribute,
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
                    SiblingComponentAttribute attribute,
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

                bool foundSibling;
                switch (kind)
                {
                    case FieldKind.Array:
                    {
                        using PooledResource<List<Component>> componentBufferResource =
                            Buffers<Component>.List.Get();
                        List<Component> siblingComponents = componentBufferResource.resource;
                        component.GetComponents(elementType, siblingComponents);

                        using PooledResource<List<Component>> filteredResource =
                            Buffers<Component>.List.Get();
                        List<Component> filtered = filteredResource.resource;

                        if (attribute.includeInactive)
                        {
                            filtered = siblingComponents;
                        }
                        else if (component.gameObject.activeInHierarchy)
                        {
                            for (int i = 0; i < siblingComponents.Count; ++i)
                            {
                                Component siblingComponent = siblingComponents[i];
                                if (!siblingComponent.IsComponentEnabled())
                                {
                                    continue;
                                }
                                filtered.Add(siblingComponent);
                            }
                        }

                        foundSibling = filtered.Count > 0;

                        Array correctTypedArray = arrayCreator(filtered.Count);
                        for (int i = 0; i < filtered.Count; ++i)
                        {
                            correctTypedArray.SetValue(filtered[i], i);
                        }

                        setter(component, correctTypedArray);
                        break;
                    }
                    case FieldKind.List:
                    {
                        using PooledResource<List<Component>> componentBufferResource =
                            Buffers<Component>.List.Get();
                        List<Component> siblingComponents = componentBufferResource.resource;
                        component.GetComponents(elementType, siblingComponents);

                        int count = siblingComponents.Count;
                        if (!attribute.includeInactive && component.gameObject.activeInHierarchy)
                        {
                            count = 0;
                            for (int i = 0; i < siblingComponents.Count; ++i)
                            {
                                if (siblingComponents[i].IsComponentEnabled())
                                {
                                    ++count;
                                }
                            }
                        }

                        IList instance = listCreator(count);

                        if (attribute.includeInactive)
                        {
                            for (int i = 0; i < siblingComponents.Count; ++i)
                            {
                                instance.Add(siblingComponents[i]);
                            }
                        }
                        else if (component.gameObject.activeInHierarchy)
                        {
                            for (int i = 0; i < siblingComponents.Count; ++i)
                            {
                                Component siblingComponent = siblingComponents[i];
                                if (!siblingComponent.IsComponentEnabled())
                                {
                                    continue;
                                }

                                instance.Add(siblingComponent);
                            }
                        }

                        setter(component, instance);
                        foundSibling = instance.Count > 0;
                        break;
                    }
                    default:
                    {
                        Component siblingComponent = null;
                        if (attribute.includeInactive)
                        {
                            siblingComponent = component.GetComponent(elementType);
                        }
                        else if (component.gameObject.activeInHierarchy)
                        {
                            using PooledResource<List<Component>> componentBufferResource =
                                Buffers<Component>.List.Get();
                            List<Component> siblingComponents = componentBufferResource.resource;
                            component.GetComponents(elementType, siblingComponents);
                            for (int i = 0; i < siblingComponents.Count; ++i)
                            {
                                Component candidate = siblingComponents[i];
                                if (!candidate.IsComponentEnabled())
                                {
                                    continue;
                                }

                                siblingComponent = candidate;
                                break;
                            }
                        }

                        if (siblingComponent != null)
                        {
                            foundSibling = true;
                            setter(component, siblingComponent);
                        }
                        else
                        {
                            foundSibling = false;
                        }

                        break;
                    }
                }

                if (!foundSibling && !attribute.optional)
                {
                    component.LogError(
                        $"Unable to find sibling component of type {field.FieldType}"
                    );
                }
            }
        }
    }
}
