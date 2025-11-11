namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Inspector attribute that constrains a string field to a set of allowed values.
    /// </summary>
    /// <remarks>
    /// Supports either a fixed list or a static method that returns a string[] at edit time.
    /// The associated PropertyDrawer can render a dropdown for selection.
    /// </remarks>
    public sealed class StringInListAttribute : PropertyAttribute
    {
        private const string AttributeName = "StringInListAttribute";
        private readonly Func<string[]> _getStringList;

        /// <summary>
        /// Uses a fixed list of allowed strings.
        /// </summary>
        public StringInListAttribute(params string[] list)
        {
            _getStringList = DropdownValueProvider<string>.FromList(list);
        }

        /// <summary>
        /// Uses a static method on a type to obtain the allowed strings.
        /// </summary>
        /// <param name="type">Type that defines a static method.</param>
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
        /// Returns the allowed string list.
        /// </summary>
        public string[] List => _getStringList();
    }
}
