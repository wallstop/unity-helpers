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
        private static readonly Dictionary<
            Type,
            (
                FieldInfo field,
                SiblingComponentAttribute attribute,
                Action<object, object> setter,
                Func<object, object> getter
            )[]
        > FieldsByType = new();

        public static void AssignSiblingComponents(this Component component)
        {
            Type componentType = component.GetType();
            (
                FieldInfo field,
                SiblingComponentAttribute attribute,
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
                                out SiblingComponentAttribute attribute,
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
                    SiblingComponentAttribute attribute,
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
                Type siblingComponentType = isArray ? fieldType.GetElementType() : fieldType;

                bool foundSibling;
                if (isArray)
                {
                    using PooledResource<List<Component>> componentBufferResource =
                        Buffers<Component>.List.Get();
                    List<Component> siblingComponents = componentBufferResource.resource;
                    component.GetComponents(siblingComponentType, siblingComponents);

                    using PooledResource<List<Component>> filteredResource =
                        Buffers<Component>.List.Get();
                    List<Component> filtered = filteredResource.resource;

                    if (attribute.includeInactive)
                    {
                        filtered = siblingComponents;
                    }
                    else if (component.gameObject.activeInHierarchy)
                    {
                        foreach (Component siblingComponent in siblingComponents)
                        {
                            if (!siblingComponent.IsComponentEnabled())
                            {
                                continue;
                            }
                            filtered.Add(siblingComponent);
                        }
                    }

                    foundSibling = 0 < filtered.Count;

                    Array correctTypedArray = ReflectionHelpers.CreateArray(
                        siblingComponentType,
                        filtered.Count
                    );
                    for (int i = 0; i < filtered.Count; i++)
                    {
                        Component siblingComponent = filtered[i];
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

                    if (attribute.includeInactive)
                    {
                        foreach (Component siblingComponent in siblingComponents)
                        {
                            instance.Add(siblingComponent);
                        }
                    }
                    else if (component.gameObject.activeInHierarchy)
                    {
                        foreach (Component siblingComponent in siblingComponents)
                        {
                            if (!siblingComponent.IsComponentEnabled())
                            {
                                continue;
                            }

                            instance.Add(siblingComponent);
                        }
                    }

                    setter(component, instance);
                    foundSibling = instance.Count > 0;
                }
                else
                {
                    Component siblingComponent = null;
                    if (attribute.includeInactive)
                    {
                        siblingComponent = component.GetComponent(siblingComponentType);
                    }
                    else if (component.gameObject.activeInHierarchy)
                    {
                        using PooledResource<List<Component>> componentBufferResource =
                            Buffers<Component>.List.Get();
                        List<Component> siblingComponents = componentBufferResource.resource;
                        component.GetComponents(siblingComponentType, siblingComponents);
                        foreach (Component candidate in siblingComponents)
                        {
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
                }

                if (!foundSibling && !attribute.optional)
                {
                    component.LogError($"Unable to find sibling component of type {fieldType}");
                }
            }
        }
    }
}
