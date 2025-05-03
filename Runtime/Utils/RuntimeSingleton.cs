namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using Core.Attributes;
    using Core.Extension;
    using Core.Helper;
    using UnityEngine;

    [DisallowMultipleComponent]
    public abstract class RuntimeSingleton<T> : MonoBehaviour
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

                Type type = typeof(T);
                GameObject instance = new($"{type.Name}-Singleton", type);
                if (
                    instance.TryGetComponent(out _instance)
                    && _instance.Preserve
                    && Application.isPlaying
                )
                {
                    DontDestroyOnLoad(instance);
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
    }
}
