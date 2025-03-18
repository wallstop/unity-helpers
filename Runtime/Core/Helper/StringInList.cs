namespace UnityHelpers.Core.Helper
{
    using System;
    using System.Reflection;
    using UnityEngine;

    public sealed class StringInList : PropertyAttribute
    {
        public delegate string[] GetStringList();

        public StringInList(params string[] list)
        {
            List = list;
        }

        public StringInList(Type type, string methodName)
        {
            MethodInfo method = type.GetMethod(methodName);
            if (method != null)
            {
                List = method.Invoke(null, null) as string[];
            }
            else
            {
                Debug.LogError($"NO SUCH METHOD {methodName} FOR {type}");
            }
        }

        public string[] List { get; private set; }
    }
}
