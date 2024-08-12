namespace UnityHelpers.Utils
{
    using System;
    using UnityEngine;

    [DisallowMultipleComponent]
    public abstract class RuntimeSingleton<T> : MonoBehaviour where T : RuntimeSingleton<T>
    {
        protected static T _instance;

        protected virtual bool Preserve => true;

        public static T Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                Type type = typeof(T);
                GameObject instance = new($"{type.Name}-Singleton", type);
                if (instance.TryGetComponent(out _instance) && _instance.Preserve)
                {
                    DontDestroyOnLoad(instance);
                }

                return _instance;
            }
        }
    }
}