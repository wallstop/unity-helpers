namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Inspector attribute that constrains a string field to a set of allowed values.
    /// Use when a serialized string should only contain known tokens (scene names, animation states, localization keys, etc.).
    /// </summary>
    /// <remarks>
    /// Supports either a fixed list or a static method that returns a string[] at edit time.
    /// The associated PropertyDrawer can render a dropdown with search, pagination, and autocomplete.
    /// </remarks>
    /// <example>
    /// Inline list:
    /// <code>
    /// [StringInList("Easy", "Normal", "Hard")]
    /// public string difficulty;
    /// </code>
    /// Provider-based list:
    /// <code>
    /// [StringInList(typeof(DialogKeys), nameof(DialogKeys.GetAllKeys))]
    /// public string dialogKey;
    ///
    /// private static class DialogKeys
    /// {
    ///     internal static IEnumerable&lt;string&gt; GetAllKeys()
    ///     {
    ///         return LocalizationTable.AllKeys;
    ///     }
    /// }
    /// </code>
    /// </example>
    public sealed class StringInListAttribute : PropertyAttribute
    {
        private const string AttributeName = "StringInListAttribute";
        private readonly Func<string[]> _getStringList;

        /// <summary>
        /// Uses a fixed list of allowed strings.
        /// Ideal for short, stable option sets baked into the class or created through constants.
        /// </summary>
        /// <param name="list">List of legal string values. The array is captured by reference.</param>
        public StringInListAttribute(params string[] list)
        {
            _getStringList = DropdownValueProvider<string>.FromList(list);
        }

        /// <summary>
        /// Uses a static method on a type to obtain the allowed strings.
        /// </summary>
        /// <remarks>
        /// The provider method must be parameterless, static, and return either <see cref="string"/>[] or <see cref="System.Collections.Generic.IEnumerable{T}"/> of <see cref="string"/>.
        /// This is useful for binding existing data sources (project settings, addressables keys, etc.) without duplicating lists.
        /// </remarks>
        /// <param name="type">Type that defines the static method that supplies the values.</param>
        /// <param name="methodName">Static method name returning string[].</param>
        public StringInListAttribute(Type type, string methodName)
        {
            _getStringList = DropdownValueProvider<string>.FromMethod(
                type,
                methodName,
                AttributeName
            );
        }

        /// <summary>
        /// Retrieves the allowed string options for the decorated field.
        /// </summary>
        public string[] List => _getStringList();
    }
}
