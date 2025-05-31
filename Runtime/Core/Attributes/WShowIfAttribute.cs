namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using System.Linq;
    using UnityEngine;

    public sealed class WShowIfAttribute : PropertyAttribute
    {
        public readonly string conditionField;
        public readonly bool inverse;
        public object[] expectedValues;

        public WShowIfAttribute(
            string conditionField,
            bool inverse = false,
            object[] expectedValues = null
        )
        {
            this.conditionField = conditionField;
            this.inverse = inverse;
            this.expectedValues = expectedValues?.ToArray() ?? Array.Empty<object>();
        }
    }
}
