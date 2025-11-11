namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Draws an integer field as a dropdown list using a predefined set of numeric options.
    /// Designed for authoring config objects, ScriptableObjects, and MonoBehaviours that should be restricted to a known set of valid identifiers (layer masks, level IDs, refresh rates, etc.).
    /// </summary>
    /// <remarks>
    /// The attribute supports both inline value lists and late-bound providers resolved through reflection.
    /// Late-binding enables values to track data sets (for example, enums translated to int IDs or editor preferences stored elsewhere).
    /// </remarks>
    /// <example>
    /// Inline list:
    /// <code>
    /// [IntDropdown(0, 30, 60, 120)]
    /// public int frameRate;
    /// </code>
    /// Late-bound provider:
    /// <code>
    /// [IntDropdown(typeof(FrameRateLibrary), nameof(FrameRateLibrary.GetSupportedFrameRates))]
    /// public int frameRate;
    ///
    /// private static class FrameRateLibrary
    /// {
    ///     internal static IEnumerable&lt;int&gt; GetSupportedFrameRates() =&gt; new[] { 30, 60, 120 };
    /// }
    /// </code>
    /// </example>
    public sealed class IntDropdownAttribute : PropertyAttribute
    {
        private const string AttributeName = "IntDropdownAttribute";
        private readonly Func<int[]> _getOptions;

        /// <summary>
        /// Initializes the attribute with an inline list of integer values that should be exposed in the inspector.
        /// </summary>
        /// <param name="options">One or more selectable integer values. The order defined here becomes the dropdown order.</param>
        public IntDropdownAttribute(params int[] options)
        {
            _getOptions = DropdownValueProvider<int>.FromList(options);
        }

        /// <summary>
        /// Initializes the attribute using a static provider method that supplies integer values.
        /// </summary>
        /// <remarks>
        /// The provider must be a parameterless static method that returns either <see cref="int"/>[] or <see cref="System.Collections.Generic.IEnumerable{T}"/> of <see cref="int"/>.
        /// The method is evaluated each time the inspector needs to render the property, allowing the dropdown contents to stay in sync with external state.
        /// </remarks>
        /// <param name="providerType">Type containing the static provider method (for example, <c>typeof(SettingsCache)</c>).</param>
        /// <param name="methodName">Name of the parameterless static method returning the options (for example, <c>nameof(SettingsCache.GetDifficultyIds)</c>).</param>
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
        /// The array is fetched from the configured provider whenever the inspector requests it.
        /// </summary>
        public int[] Options => _getOptions();
    }
}
