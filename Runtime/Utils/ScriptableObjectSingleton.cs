namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.Helper;
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif

    /// <summary>
    /// Provides a global, lazily loaded singleton pattern for <see cref="ScriptableObject"/> assets.
    /// Ensures that exactly one asset instance of <typeparamref name="T"/> is used at runtime.
    /// </summary>
    /// <remarks>
    /// Lookup order (lazy):
    /// 1) Load from a custom Resources subfolder when the type is decorated with
    ///    <see cref="WallstopStudios.UnityHelpers.Core.Attributes.ScriptableSingletonPathAttribute"/>.
    /// 2) Load from a folder named after the type (Resources/&lt;TypeName&gt;).
    /// 3) Load by exact type name in Resources root, then fallback to all matches in Resources.
    ///
    /// If multiple assets are found, a warning is logged and the first result ordered by name is returned.
    /// The editor utility “ScriptableObject Singleton Creator” automatically creates and relocates assets to
    /// the correct path on editor load — see EDITOR_TOOLS_GUIDE.md#scriptableobject-singleton-creator.
    ///
    /// ODIN compatibility: When the <c>ODIN_INSPECTOR</c> symbol is defined, this class derives from
    /// <c>Sirenix.OdinInspector.SerializedScriptableObject</c>; otherwise it derives from <see cref="ScriptableObject"/>.
    /// </remarks>
    /// <typeparam name="T">Concrete singleton ScriptableObject type that derives from this base.</typeparam>
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        internal static void ClearInstance()
        {
            if (!LazyInstance.IsValueCreated)
            {
                return;
            }

            LazyInstance.Value.Destroy();
            LazyInstance = CreateLazy();
        }

        protected internal static Lazy<T> LazyInstance = CreateLazy();

        private static Lazy<T> CreateLazy()
        {
            return new Lazy<T>(() =>
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

                if (instances == null || instances.Length == 0)
                {
                    // As a last resort in editor, return any already-loaded instances of this type.
                    // This supports tests that create instances programmatically and save them as assets.
                    T[] found = Resources.FindObjectsOfTypeAll<T>();
                    if (found != null && found.Length > 0)
                    {
                        instances = found;
                    }
                }

                if (instances == null)
                {
                    return null;
                }

                switch (instances.Length)
                {
                    case 1:
                    {
                        return instances[0];
                    }
                    case 0:
                        return null;
                }

                Debug.LogWarning(
                    $"Found multiple ScriptableSingletons of type {type.Name}, defaulting to first by name."
                );
                Array.Sort(instances, UnityObjectNameComparer<T>.Instance);
                return instances[0];
            });
        }

        /// <summary>
        /// Gets a value indicating whether the lazy instance has been created and is non‑null.
        /// </summary>
        public static bool HasInstance => LazyInstance.IsValueCreated;

        /// <summary>
        /// Gets the global asset instance, loading it from <c>Resources</c> on first access.
        /// </summary>
        /// <example>
        /// <code>
        /// [ScriptableSingletonPath("Settings/Audio")]
        /// public sealed class AudioSettings : ScriptableObjectSingleton&lt;AudioSettings&gt;
        /// {
        ///     public float musicVolume = 0.8f;
        /// }
        ///
        /// // Access anywhere
        /// float volume = AudioSettings.Instance.musicVolume;
        /// </code>
        /// </example>
        public static T Instance => LazyInstance.Value;
    }
}
