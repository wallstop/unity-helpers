namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;
    using System.Reflection;
    using UnityEngine;

    public sealed class StringInList : PropertyAttribute
    {
        public delegate string[] GetStringList();

        private bool _shouldRefresh;
        private string[] _list;
        private Func<string[]> _getStringList;

        public StringInList(params string[] list)
        {
            _shouldRefresh = false;
            _list = list;
        }

        public StringInList(Type type, string methodName)
        {
            _shouldRefresh = true;
            MethodInfo method = type.GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            );
            if (method != null)
            {
                _getStringList = () => method.Invoke(null, null) as string[];
            }
            else
            {
                Debug.LogError($"NO SUCH METHOD {methodName} FOR {type}");
                _getStringList = () => Array.Empty<string>();
            }
        }

        public string[] List
        {
            get
            {
                if (_shouldRefresh)
                {
                    return _getStringList();
                }

                return _list;
            }
            private set { _list = value; }
        }
    }
}
