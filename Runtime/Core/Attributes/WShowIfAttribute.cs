namespace UnityHelpers.Core.Attributes
{
    using UnityEngine;

    public sealed class WShowIfAttribute : PropertyAttribute
    {
        public readonly string conditionField;
        public readonly bool inverse;

        public WShowIfAttribute(string conditionField, bool inverse = false)
        {
            this.conditionField = conditionField;
            this.inverse = inverse;
        }
    }
}
