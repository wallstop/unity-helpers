namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
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
        private const string AttributeName = "IntDropdownAttribute";
        private readonly Func<int[]> _getOptions;

        /// <summary>
        /// Initializes the attribute with the list of integers that should be exposed in the inspector.
        /// </summary>
        /// <param name="options">One or more selectable integer values.</param>
        public IntDropdownAttribute(params int[] options)
        {
            _getOptions = DropdownValueProvider<int>.FromList(options);
        }

        /// <summary>
        /// Initializes the attribute using a method provider that supplies integer values.
        /// </summary>
        /// <param name="providerType">Type containing the static provider method.</param>
        /// <param name="methodName">Parameterless static method returning int[] or IEnumerable&lt;int&gt;.</param>
        public IntDropdownAttribute(Type providerType, string methodName)
        {
            _getOptions = DropdownValueProvider<int>.FromMethod(
                providerType,
                methodName,
                AttributeName
            );
        }

        /// <summary>
        /// Gets the set of allowed integer values that the dropdown will display.
        /// </summary>
        public int[] Options => _getOptions();
    }
}
