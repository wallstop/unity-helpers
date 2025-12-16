namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class WNotNullAttribute : Attribute { }

    public static class WNotNullAttributeExtensions
    {
        public static void CheckForNulls(this object o)
        {
#if UNITY_EDITOR
            IEnumerable<FieldInfo> properties =
                Helper.ReflectionHelpers.GetFieldsWithAttribute<WNotNullAttribute>(o.GetType());

            foreach (FieldInfo field in properties)
            {
                object fieldValue = field.GetValue(o);

                switch (fieldValue)
                {
                    case UnityEngine.Object unityObject when unityObject == null:
                        throw new ArgumentNullException(field.Name);
                    case null:
                        throw new ArgumentNullException(field.Name);
                }
            }
#endif
        }
    }
}
