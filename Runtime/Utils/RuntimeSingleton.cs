namespace UnityHelpers.Utils
{
    using UnityEngine;

    [DisallowMultipleComponent]
    public abstract class RuntimeSingleton<T> : MonoBehaviour where T : RuntimeSingleton<T>
    {
        private static T _instance;

        protected virtual bool DontDestroyOnLoad => true;

        public static T Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                GameObject instance = new(typeof(T).Name + "Singleton", typeof(T));
                _ = instance.TryGetComponent(out _instance);
                if (_instance.DontDestroyOnLoad)
                {
                    DontDestroyOnLoad(instance);
                }
                return _instance;
            }
        }
    }
}