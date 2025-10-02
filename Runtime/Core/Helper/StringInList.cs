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
            MethodInfo method = type.GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            );
            if (method != null)
            {
                _getStringList = () => ReflectionHelpers.InvokeStaticMethod(method) as string[];
            }
            else
            {
                Debug.LogError($"NO SUCH METHOD {methodName} FOR {type}");
                _getStringList = () => Array.Empty<string>();
            }
        }

        public string[] List => _getStringList();
    }
}
