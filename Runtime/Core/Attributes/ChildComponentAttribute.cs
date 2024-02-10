namespace UnityHelpers.Core.Attributes
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
    public sealed class ChildComponentAttribute : Attribute
    {
        public bool optional = false;
    }

    public static class ChildComponentExtensions
    {
        private static readonly Dictionary<Type, List<FieldInfo>> FieldsByType = new();

        public static void AssignChildComponents(this Component component)
        {
            Type componentType = component.GetType();
            List<FieldInfo> fields = FieldsByType.GetOrAdd(componentType, type =>
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return fields
                    .Where(prop => Attribute.IsDefined(prop, typeof(ChildComponentAttribute)))
                    .ToList();
            });

            foreach (FieldInfo field in fields)
            {
                Type fieldType = field.FieldType;
                bool isArray = fieldType.IsArray;
                Type childComponentType = isArray ? fieldType.GetElementType() : fieldType;

                bool foundChild;
                if (isArray)
                {
                    Component[] childComponents = component.GetComponentsInChildren(childComponentType, true);
                    foundChild = 0 < childComponents.Length;
                    
                    Array correctTypedArray = Array.CreateInstance(childComponentType, childComponents.Length);
                    Array.Copy(childComponents, correctTypedArray, childComponents.Length);
                    field.SetValue(component, correctTypedArray);
                }
                else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    childComponentType = fieldType.GenericTypeArguments[0];
                    Type constructedListType = typeof(List<>).MakeGenericType(childComponentType);
                    IList instance = (IList) Activator.CreateInstance(constructedListType);

                    foundChild = false;
                    foreach (Component childComponent in component.GetComponentsInChildren(childComponentType, true))
                    {
                        instance.Add(childComponent);
                        foundChild = true;
                    }

                    field.SetValue(component, instance);
                }
                else
                {
                    Component childComponent = component.GetComponentInChildren(childComponentType, true);
                    foundChild = childComponent != null;
                    if (foundChild)
                    {
                        field.SetValue(component, childComponent);
                    }
                }

                if (!foundChild)
                {
                    if (field.GetCustomAttributes(typeof(ChildComponentAttribute), false)[0] is ChildComponentAttribute { optional: false } _)
                    {
                        component.LogError($"Unable to find child component of type {fieldType}");
                    }
                }
            }
        }
    }
}
