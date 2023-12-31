namespace UnityHelpers.Core.Attributes
{
    using System;
    using System.Linq;
    using System.Reflection;
    using JetBrains.Annotations;
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Field)]
    [MeansImplicitUse]
    public sealed class AutomaticallyFindAttribute : Attribute
    {
        public readonly string Tag = string.Empty;
    }

    public static class AutomaticallyFindExtensions
    {
        public static void AutomaticallyFind(this Component component)
        {
            var properties = component
                .GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(prop => Attribute.IsDefined(prop, typeof(AutomaticallyFindAttribute)));

            foreach (FieldInfo field in properties)
            {
                Type fieldType = field.FieldType;
                Type type = fieldType.IsArray ? fieldType.GetElementType() : fieldType;

                if (type == typeof(GameObject))
                {
                    var attribute = field.GetCustomAttributes(typeof(AutomaticallyFindAttribute), false)[0] as AutomaticallyFindAttribute;
                    GameObject gameObject = GameObject.FindWithTag(attribute.Tag);
                    field.SetValue(component, gameObject);
                    return;
                }

                object foundObject = type.GetMethod("Find", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
                field.SetValue(component, foundObject);
            }
        }
    }
}
