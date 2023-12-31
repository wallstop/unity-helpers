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
    using Object = UnityEngine.Object;

    [AttributeUsage(AttributeTargets.Field)]
    [MeansImplicitUse]
    public sealed class ValidateAssignmentAttribute : Attribute
    {
    }

    public static class ValidateAssignmentExtensions
    {
        private static readonly Dictionary<Type, List<FieldInfo>> FieldsByType = new();

        public static void ValidateAssignments(this Object o)
        {
#if UNITY_EDITOR
            Type objectType = o.GetType();
            List<FieldInfo> fields = FieldsByType.GetOrAdd(objectType, type => type
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(prop => Attribute.IsDefined(prop, typeof(ValidateAssignmentAttribute))).ToList());

            foreach (FieldInfo field in fields)
            {
                bool logNotAssigned = IsFieldInvalid(field, o);

                if (logNotAssigned)
                {
                    o.LogNotAssigned(field.Name);
                }
            }
#endif
        }

        public static bool AreAnyAssignmentsInvalid(this Object o)
        {
            Type objectType = o.GetType();
            List<FieldInfo> fields = FieldsByType.GetOrAdd(objectType, type => type
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(prop => Attribute.IsDefined(prop, typeof(ValidateAssignmentAttribute))).ToList());

            return fields.Any(field => IsFieldInvalid(field, o));
        }

        private static bool IsFieldInvalid(FieldInfo field, Object o)
        {
            object fieldValue = field.GetValue(o);
            return fieldValue switch
            {
                IList list => list.Count <= 0,
                ICollection collection => collection.Count <= 0,
                Object unityObject => !unityObject,
                string stringValue => string.IsNullOrWhiteSpace(stringValue),
                _ => fieldValue == null
            };
        }
    }
}
