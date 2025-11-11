namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    /// Conditionally hides or shows a serialized field based on the value of another property on the same object.
    /// </summary>
    /// <example>
    /// <code>
    /// public bool advancedMode;
    ///
    /// [WShowIf(nameof(advancedMode))]
    /// public float advancedSetting;
    /// </code>
    /// </example>
    public sealed class WShowIfAttribute : PropertyAttribute
    {
        /// <summary>
        /// Gets the name of the field or property that determines visibility.
        /// </summary>
        public readonly string conditionField;

        /// <summary>
        /// Gets a value indicating whether the visibility rule should be inverted.
        /// </summary>
        public readonly bool inverse;

        /// <summary>
        /// Gets the explicit values that must match the condition field in order for the target to display.
        /// </summary>
        public object[] expectedValues;

        /// <summary>
        /// Configures a conditional visibility rule for an inspector field.
        /// </summary>
        /// <param name="conditionField">Name of the member used for evaluation.</param>
        /// <param name="inverse">Set to <c>true</c> to flip the visibility result.</param>
        /// <param name="expectedValues">Optional explicit values that should evaluate as visible.</param>
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
