namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using UnityEngine;

    /// <summary>
    /// Draws an integer field as a dropdown list using a predefined set of numeric options.
    /// </summary>
    /// <example>
    /// <code>
    /// [IntDropdown(0, 30, 60, 120)]
    /// public int frameRate;
    /// </code>
    /// </example>
    public sealed class IntDropdownAttribute : PropertyAttribute
    {
        /// <summary>
        /// Gets the set of allowed integer values that the dropdown will display.
        /// </summary>
        public int[] Options { get; }

        /// <summary>
        /// Initializes the attribute with the list of integers that should be exposed in the inspector.
        /// </summary>
        /// <param name="options">One or more selectable integer values.</param>
        public IntDropdownAttribute(params int[] options)
        {
            Options = options;
        }
    }
}
