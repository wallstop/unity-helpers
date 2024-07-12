namespace UnityHelpers.Utils
{
    using UnityEngine;

    [DisallowMultipleComponent]
    public abstract class RuntimeSingleton<T> : MonoBehaviour where T : RuntimeSingleton<T>
    {
        private static T _instance;

        protected virtual bool Preserve => true;

        public static T Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                GameObject instance = new(typeof(T).Name + "Singleton", typeof(T));
                if (instance.TryGetComponent(out _instance) && _instance.Preserve)
                {
                    DontDestroyOnLoad(instance);
                }

                return _instance;
            }
        }
    }
}