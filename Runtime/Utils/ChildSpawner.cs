namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Extension;
    using UnityEngine;
    using UnityEngine.Serialization;

    /// <summary>
    /// Selects the MonoBehaviour lifecycle events that should trigger prefab instantiation.
    /// </summary>
    [Flags]
    public enum ChildSpawnMethod
    {
        /// <summary>
        /// No child creation will occur. Mainly retained for serialization compatibility.
        /// </summary>
        [Obsolete]
        None = 0,

        /// <summary>
        /// Spawn children during <see cref="MonoBehaviour.Awake"/>.
        /// </summary>
        Awake = 1 << 0,

        /// <summary>
        /// Spawn children when the component is enabled.
        /// </summary>
        OnEnabled = 1 << 1,

        /// <summary>
        /// Spawn children during <see cref="MonoBehaviour.Start"/>.
        /// </summary>
        Start = 1 << 2,
    }

    /// <summary>
    /// Instantiates a curated list of prefabs as children of the current GameObject while ensuring
    /// duplicates across scenes are avoided and optional DontDestroyOnLoad behaviour is applied.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ChildSpawner : MonoBehaviour
    {
        private static readonly HashSet<GameObject> SpawnedPrefabs = new();

        [FormerlySerializedAs("dontDestroyOnLoad")]
        [SerializeField]
        internal bool _dontDestroyOnLoad = true;

        [SerializeField]
        internal ChildSpawnMethod _spawnMethod = ChildSpawnMethod.Start;

        /// <summary>
        /// Prefabs that are spawned in all environments where the component executes.
        /// </summary>
        [SerializeField]
        internal GameObject[] _prefabs = Array.Empty<GameObject>();

        /// <summary>
        /// Prefabs spawned when running inside the Unity editor only.
        /// </summary>
        [SerializeField]
        internal GameObject[] _editorOnlyPrefabs = Array.Empty<GameObject>();

        /// <summary>
        /// Prefabs spawned when running in the editor or a development build.
        /// </summary>
        [SerializeField]
        internal GameObject[] _developmentOnlyPrefabs = Array.Empty<GameObject>();

        private readonly HashSet<GameObject> _spawnedPrefabs = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ClearSpawnedPrefabs()
        {
            SpawnedPrefabs.Clear();
        }

        private void Awake()
        {
            if (_spawnMethod.HasFlagNoAlloc(ChildSpawnMethod.Awake))
            {
                Spawn();
            }
        }

        private void OnEnable()
        {
            if (_spawnMethod.HasFlagNoAlloc(ChildSpawnMethod.OnEnabled))
            {
                Spawn();
            }
        }

        private void Start()
        {
            if (_spawnMethod.HasFlagNoAlloc(ChildSpawnMethod.Start))
            {
                Spawn();
            }
        }

        /// <summary>
        /// Performs the spawning process for all configured prefab collections, applying naming
        /// suffixes and duplicate checks for each group.
        /// </summary>
        private void Spawn()
        {
            TrySetDontDestroyOnLoad();
            if (
                _prefabs
                    .Concat(_editorOnlyPrefabs)
                    .Concat(_developmentOnlyPrefabs)
                    .Distinct()
                    .Count()
                != (_prefabs.Length + _editorOnlyPrefabs.Length + _developmentOnlyPrefabs.Length)
            )
            {
                IEnumerable<string> duplicateChildNames = _prefabs
                    .Concat(_editorOnlyPrefabs)
                    .Concat(_developmentOnlyPrefabs)
                    .GroupBy(x => x)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key != null ? group.Key.name : "null");
                this.LogError(
                    $"Duplicate child prefab detected: {string.Join(",", duplicateChildNames)}"
                );
            }

            int count = 0;
            foreach (GameObject prefab in _prefabs)
            {
                GameObject child = Spawn(prefab);
                if (child != null)
                {
                    child.name = $"{child.name} ({count++:00})";
                }
            }

            foreach (GameObject prefab in _prefabs)
            {
                if (prefab != null)
                {
                    _spawnedPrefabs.Add(prefab);
                }
            }

#if UNITY_EDITOR
            if (Application.isEditor)
            {
                foreach (GameObject prefab in _editorOnlyPrefabs)
                {
                    GameObject child = Spawn(prefab);
                    if (child != null)
                    {
                        child.name = $"{child.name} (EDITOR-ONLY {count++:00})";
                    }
                }

                foreach (GameObject prefab in _editorOnlyPrefabs)
                {
                    if (prefab != null)
                    {
                        _spawnedPrefabs.Add(prefab);
                    }
                }
            }
#endif

            if (Application.isEditor || Debug.isDebugBuild)
            {
                foreach (GameObject prefab in _developmentOnlyPrefabs)
                {
                    GameObject child = Spawn(prefab);
                    if (child != null)
                    {
                        child.name = $"{child.name} (DEVELOPMENT-ONLY {count++:00})";
                    }
                }

                foreach (GameObject prefab in _developmentOnlyPrefabs)
                {
                    if (prefab != null)
                    {
                        _spawnedPrefabs.Add(prefab);
                    }
                }
            }
        }

        /// <summary>
        /// Removes Unity's default "(Clone)" suffix from instantiated prefab names.
        /// </summary>
        /// <param name="child">The instantiated child whose name should be cleaned.</param>
        private static void CleanName(GameObject child)
        {
            child.name = child.name.Replace("(Clone)", string.Empty);
        }

        /// <summary>
        /// Instantiates <paramref name="prefab"/> as a child of this component if it has not been
        /// spawned previously, guarding against duplicate DontDestroyOnLoad instances.
        /// </summary>
        /// <param name="prefab">Prefab to spawn.</param>
        /// <returns>The instantiated child instance, or <c>null</c> if the spawn is skipped.</returns>
        private GameObject Spawn(GameObject prefab)
        {
            if (prefab == null)
            {
                this.LogError($"Unexpectedly null prefab - cannot spawn.");
                return null;
            }

            if (_spawnedPrefabs.Contains(prefab))
            {
                return null;
            }

            if (SpawnedPrefabs.Contains(prefab))
            {
                return null;
            }

            GameObject child = Instantiate(prefab, transform);
            CleanName(child);
            if (
                child.IsDontDestroyOnLoad()
                || gameObject.IsDontDestroyOnLoad()
                || prefab.IsDontDestroyOnLoad()
            )
            {
                SpawnedPrefabs.Add(prefab);
            }
            return child;
        }

        /// <summary>
        /// Applies <see cref="Object.DontDestroyOnLoad(UnityEngine.Object)"/> when configured to
        /// keep the spawner alive between scene loads.
        /// </summary>
        private void TrySetDontDestroyOnLoad()
        {
            if (_dontDestroyOnLoad && Application.isPlaying && !gameObject.IsDontDestroyOnLoad())
            {
                DontDestroyOnLoad(gameObject);
            }
        }
    }
}
