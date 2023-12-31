namespace UnityHelpers.Core.Attributes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NotNullAttribute : Attribute
    {
    }

    public static class NotNullAttributeExtensions
    {
        public static void CheckForNulls(this object o)
        {
#if UNITY_EDITOR
            IEnumerable<FieldInfo> properties = o.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(prop => Attribute.IsDefined(prop, typeof(NotNullAttribute)));

            foreach (FieldInfo field in properties)
            {
                object fieldValue = field.GetValue(o);

                if (fieldValue == null)
                {
                    throw new ArgumentNullException(field.Name);
                }
            }
#endif
        }
    }
}
