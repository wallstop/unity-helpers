namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.Helper;
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif

    public abstract class ScriptableObjectSingleton<T> :
#if ODIN_INSPECTOR
        SerializedScriptableObject
#else
        ScriptableObject
#endif
        where T : ScriptableObjectSingleton<T>
    {
        private static string GetResourcesPath()
        {
            Type type = typeof(T);
            ScriptableSingletonPathAttribute attribute =
                ReflectionHelpers.GetAttributeSafe<ScriptableSingletonPathAttribute>(type);
            if (attribute != null && !string.IsNullOrWhiteSpace(attribute.resourcesPath))
            {
                return attribute.resourcesPath;
            }

            return type.Name;
        }

        protected internal static readonly Lazy<T> LazyInstance = new(() =>
        {
            Type type = typeof(T);
            string path = GetResourcesPath();
            T[] instances = Resources.LoadAll<T>(path);

            if (instances == null || instances.Length == 0)
            {
                T named = Resources.Load<T>(type.Name);
                if (named != null)
                {
                    instances = new[] { named };
                }
            }

            if (instances == null || instances.Length == 0)
            {
                instances = Resources.LoadAll<T>(string.Empty);
            }

            if (instances == null)
            {
                Debug.LogError(
                    $"Failed to find ScriptableSingleton of {type.Name} - null instances."
                );
                return default;
            }

            switch (instances.Length)
            {
                case 1:
                {
                    return instances[0];
                }
                case 0:
                {
                    Debug.LogError(
                        $"Failed to find ScriptableSingleton of type {type.Name} - empty instances."
                    );
                    return null;
                }
            }

            Debug.LogWarning(
                $"Found multiple ScriptableSingletons of type {type.Name}, defaulting to first by name."
            );
            Array.Sort(instances, UnityObjectNameComparer<T>.Instance);
            return instances[0];
        });

        public static bool HasInstance => LazyInstance.IsValueCreated && LazyInstance.Value != null;

        public static T Instance => LazyInstance.Value;
    }
}
