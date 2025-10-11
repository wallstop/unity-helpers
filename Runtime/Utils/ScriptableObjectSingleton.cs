namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.Linq;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.Helper;
#if UNITY_EDITOR
    using UnityEditor;
#endif
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

            // Return empty string to search from Resources root when no attribute is specified
            return string.Empty;
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

                if (
                    instances == null
                    || instances.Length == 0
                        && !string.Equals(path, string.Empty, StringComparison.OrdinalIgnoreCase)
                )
                {
                    instances = Resources.LoadAll<T>(string.Empty);
                }

                if (instances == null || instances.Length == 0)
                {
                    // As a last resort in editor, return any already-loaded instances of this type.
                    // This supports tests that create instances programmatically and save them as assets.
                    T[] found = Resources.FindObjectsOfTypeAll<T>();
                    if (found is { Length: > 0 })
                    {
                        instances = found;
                    }
                }

#if UNITY_EDITOR
                if (instances == null || instances.Length == 0)
                {
                    // Editor-only fallback: try direct path lookups under Assets/Resources
                    // This supports editor tests for types defined in editor assemblies.
                    string typeName = type.Name;
                    string[] candidates;
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        candidates = new[]
                        {
                            $"Assets/Resources/{path}/{typeName}.asset",
                            $"Assets/Resources/{typeName}.asset",
                        };
                    }
                    else
                    {
                        candidates = new[] { $"Assets/Resources/{typeName}.asset" };
                    }

                    bool found = false;
                    foreach (string candidate in candidates)
                    {
                        // Try loading as specific type first
                        T atPath = AssetDatabase.LoadAssetAtPath<T>(candidate);
                        if (atPath != null)
                        {
                            instances = new[] { atPath };
                            found = true;
                            break;
                        }

                        // For nested classes without script files, LoadAssetAtPath may fail.
                        // Try LoadAllAssetsAtPath which can load "broken" assets.
                        // Note: For nested classes, Unity may return "missing" object references (null).
                        // We check if a GUID exists to know if a file is present, then handle missing objects.
                        string guid = AssetDatabase.AssetPathToGUID(candidate);
                        if (!string.IsNullOrEmpty(guid))
                        {
                            // Asset file exists, try to load it
                            UnityEngine.Object[] allAtPath = AssetDatabase.LoadAllAssetsAtPath(
                                candidate
                            );

                            if (allAtPath is { Length: > 0 })
                            {
                                foreach (UnityEngine.Object obj in allAtPath)
                                {
                                    // Unity returns non-null entries for missing objects, but they fail null checks
                                    // Use Unity's object == null check which handles missing references
                                    if (obj == null)
                                    {
                                        continue;
                                    }

                                    if (type.IsInstanceOfType(obj))
                                    {
                                        instances = new[] { (T)obj };
                                        found = true;
                                        break;
                                    }
                                }
                                if (found)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
#endif

                if (instances == null || instances.Length == 0)
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
                    {
                        return null;
                    }
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
        public static bool HasInstance => LazyInstance.IsValueCreated && LazyInstance.Value != null;

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
