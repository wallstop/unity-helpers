namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using Core.Attributes;
    using Core.Extension;
    using Core.Helper;
    using UnityEngine;
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif

    [DisallowMultipleComponent]
    public abstract class RuntimeSingleton<T> :
#if ODIN_INSPECTOR
        SerializedMonoBehaviour
#else
        MonoBehaviour
#endif
        where T : RuntimeSingleton<T>
    {
        public static bool HasInstance => _instance != null;

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

                _instance = FindAnyObjectByType<T>(FindObjectsInactive.Exclude);
                if (_instance != null)
                {
                    return _instance;
                }

                Type type = typeof(T);
                GameObject instance = new($"{type.Name}-Singleton", type);
                if (_instance == null)
                {
                    _ = instance.TryGetComponent(out _instance);
                }

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            this.AssignRelationalComponents();
            if (_instance == null)
            {
                _instance = this as T;
            }

            if (Preserve && Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        protected virtual void Start()
        {
            if (_instance == null || _instance == this)
            {
                return;
            }

            this.LogError($"Double singleton detected, {_instance.name} conflicts with {name}.");
            gameObject.Destroy();
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        protected virtual void OnApplicationQuit() { }
    }
}
