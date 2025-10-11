namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;
    using System.Reflection;
    using UnityEngine;

    /// <summary>
    /// Inspector attribute that constrains a string field to a set of allowed values.
    /// </summary>
    /// <remarks>
    /// Supports either a fixed list or a static method that returns a string[] at edit time.
    /// The associated PropertyDrawer can render a dropdown for selection.
    /// </remarks>
    public sealed class StringInList : PropertyAttribute
    {
        private readonly Func<string[]> _getStringList;

        /// <summary>
        /// Uses a fixed list of allowed strings.
        /// </summary>
        public StringInList(params string[] list)
        {
            _getStringList = () => list;
        }

        /// <summary>
        /// Uses a static method on a type to obtain the allowed strings.
        /// </summary>
        /// <param name="type">Type that defines a static method.</param>
        /// <param name="methodName">Static method name returning string[].</param>
        public StringInList(Type type, string methodName)
        {
            foreach (
                MethodInfo method in type.GetMethods(
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                )
            )
            {
                if (
                    string.Equals(method.Name, methodName, StringComparison.Ordinal)
                    && method.ReturnParameter != null
                    && method.ReturnParameter.ParameterType == typeof(string[])
                )
                {
                    MethodInfo localMethod = method;
                    _getStringList = () =>
                        ReflectionHelpers.InvokeStaticMethod(localMethod) as string[]
                        ?? Array.Empty<string>();
                    return;
                }
            }
            Debug.LogError($"NO SUCH METHOD {methodName} FOR {type}");
            _getStringList = () => Array.Empty<string>();
        }

        /// <summary>
        /// Returns the allowed string list.
        /// </summary>
        public string[] List => _getStringList();
    }
}
