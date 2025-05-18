namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using UnityEngine;
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif

    public abstract class ScriptableSingleton<T> :
#if ODIN_INSPECTOR
        SerializedScriptableObject
#else
        ScriptableObject
#endif
        where T : ScriptableSingleton<T>
    {
        protected static readonly Lazy<T> LazyInstance = new(() =>
        {
            T[] instances = Resources.LoadAll<T>(string.Empty);
            switch (instances.Length)
            {
                case 1:
                {
                    return instances[0];
                }
                case 0:
                {
                    Debug.LogError($"Failed to find ScriptableSingleton of type {typeof(T).Name}.");
                    return null;
                }
            }

            Debug.LogWarning(
                $"Found multiple ScriptableSingletons of type {typeof(T).Name}, defaulting to first by name."
            );
            Array.Sort(instances, UnityObjectNameComparer<T>.Instance);
            return instances[0];
        });

        public static bool HasInstance => LazyInstance.IsValueCreated && LazyInstance.Value != null;

        public static T Instance => LazyInstance.Value;
    }
}
