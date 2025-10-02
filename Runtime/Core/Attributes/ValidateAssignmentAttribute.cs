namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Extension;
    using Helper;
    using Object = UnityEngine.Object;

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ValidateAssignmentAttribute : Attribute { }

    public static class ValidateAssignmentExtensions
    {
        private static readonly Dictionary<Type, FieldInfo[]> FieldsByType = new();

        private static FieldInfo[] GetOrAdd(Type objectType)
        {
            return FieldsByType.GetOrAdd(
                objectType,
                type =>
                    type.GetFields(
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                        )
                        .Where(prop =>
                            prop.IsAttributeDefined<ValidateAssignmentAttribute>(
                                out _,
                                inherit: false
                            )
                        )
                        .ToArray()
            );
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
            try
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
            catch
            {
                return true;
            }
        }

        private static bool IsFieldInvalid(FieldInfo field, Object o)
        {
            object fieldValue = field.GetValue(o);

            return fieldValue switch
            {
                Object unityObject => unityObject == null,
                string stringValue => string.IsNullOrWhiteSpace(stringValue),
                IList list => list.Count <= 0,
                ICollection collection => collection.Count <= 0,
                IEnumerable enumerable => IsInvalid(enumerable),
                _ => fieldValue == null,
            };
        }

        internal static bool IsValueInvalid(object value)
        {
            return value switch
            {
                Object unityObject => unityObject == null,
                string stringValue => string.IsNullOrWhiteSpace(stringValue),
                IList list => list.Count <= 0,
                ICollection collection => collection.Count <= 0,
                IEnumerable enumerable => IsInvalid(enumerable),
                _ => value == null,
            };
        }
    }
}
