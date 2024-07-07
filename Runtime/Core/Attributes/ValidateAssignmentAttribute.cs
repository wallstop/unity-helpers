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
    public sealed class ValidateAssignmentAttribute : Attribute { }

    public static class ValidateAssignmentExtensions
    {
        private static readonly Dictionary<Type, FieldInfo[]> FieldsByType = new();

        private static FieldInfo[] GetOrAdd(Type objectType)
        {
            return FieldsByType.GetOrAdd(
                objectType, type => type
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(prop => Attribute.IsDefined(prop, typeof(ValidateAssignmentAttribute)))
                    .ToArray());
        }

        public static void ValidateAssignments(this Object o)
        {
#if UNITY_EDITOR
            Type objectType = o.GetType();
            FieldInfo[] fields = GetOrAdd(objectType);

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
            FieldInfo[] fields = GetOrAdd(objectType);

            foreach (FieldInfo field in fields)
            {
                if (IsFieldInvalid(field, o))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsInvalid(IEnumerable enumerable)
        {
            IEnumerator enumerator = enumerable.GetEnumerator();
            try
            {
                return !enumerator.MoveNext();
            }
            finally
            {
                if (enumerator is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        private static bool IsFieldInvalid(FieldInfo field, Object o)
        {
            object fieldValue = field.GetValue(o);

            return fieldValue switch
            {
                Object unityObject => !unityObject,
                string stringValue => string.IsNullOrWhiteSpace(stringValue),
                IList list => list.Count <= 0,
                ICollection collection => collection.Count <= 0,
                IEnumerable enumerable => IsInvalid(enumerable),
                _ => fieldValue == null
            };
        }
    }
}