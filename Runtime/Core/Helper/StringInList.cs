namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;
    using System.Reflection;
    using UnityEngine;

    public sealed class StringInList : PropertyAttribute
    {
        private readonly Func<string[]> _getStringList;

        public StringInList(params string[] list)
        {
            _getStringList = () => list;
        }

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

        public string[] List => _getStringList();
    }
}
