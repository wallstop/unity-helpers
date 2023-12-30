namespace Core.Attributes
{
    using Extension;
    using JetBrains.Annotations;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Field)]
    [MeansImplicitUse]
    public sealed class SiblingComponentAttribute : Attribute
    {
        public bool optional = false;
    }

    public static class SiblingComponentExtensions
    {
        private static readonly Dictionary<Type, List<FieldInfo>> FieldsByType = new();

        public static void AssignSiblingComponents(this Component component)
        {
            Type componentType = component.GetType();
            List<FieldInfo> fields = FieldsByType.GetOrAdd(componentType,
                type =>
                {
                    FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    return fields
                        .Where(prop => Attribute.IsDefined(prop, typeof(SiblingComponentAttribute)))
                        .ToList();
                });

            foreach (FieldInfo field in fields)
            {
                Type fieldType = field.FieldType;
                bool isArray = fieldType.IsArray;
                Type siblingComponentType = isArray ? fieldType.GetElementType() : fieldType;

                bool foundSibling;
                if (isArray)
                {
                    Component[] siblingComponents = component.GetComponents(siblingComponentType);
                    foundSibling = 0 < siblingComponents.Length;

                    Array correctTypedArray = Array.CreateInstance(siblingComponentType, siblingComponents.Length);
                    Array.Copy(siblingComponents, correctTypedArray, siblingComponents.Length);
                    field.SetValue(component, correctTypedArray);
                }
                else
                {
                    if (component.TryGetComponent(siblingComponentType, out Component siblingComponent))
                    {
                        foundSibling = true;
                        field.SetValue(component, siblingComponent);
                    }
                    else
                    {
                        foundSibling = false;
                    }
                }

                if (!foundSibling)
                {
                    if (field.GetCustomAttributes(typeof(SiblingComponentAttribute), false)[0] is SiblingComponentAttribute {optional: false} _)
                    {
                        component.LogError($"Unable to find sibling component of type {fieldType}");
                    }
                }
            }
        }
    }
}
