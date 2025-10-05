namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Extension;
    using UnityEngine;
    using UnityEngine.Serialization;

    [Flags]
    public enum ChildSpawnMethod
    {
        [Obsolete]
        None = 0,
        Awake = 1 << 0,
        OnEnabled = 1 << 1,
        Start = 1 << 2,
    }

    [DisallowMultipleComponent]
    public sealed class ChildSpawner : MonoBehaviour
    {
        private static readonly HashSet<GameObject> SpawnedPrefabs = new();

        [FormerlySerializedAs("dontDestroyOnLoad")]
        [SerializeField]
        internal bool _dontDestroyOnLoad = true;

        [SerializeField]
        internal ChildSpawnMethod _spawnMethod = ChildSpawnMethod.Start;

        [SerializeField]
        internal GameObject[] _prefabs = Array.Empty<GameObject>();

        [SerializeField]
        internal GameObject[] _editorOnlyPrefabs = Array.Empty<GameObject>();

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

        private static void CleanName(GameObject child)
        {
            child.name = child.name.Replace("(Clone)", string.Empty);
        }

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

        private void TrySetDontDestroyOnLoad()
        {
            if (_dontDestroyOnLoad && Application.isPlaying && !gameObject.IsDontDestroyOnLoad())
            {
                DontDestroyOnLoad(gameObject);
            }
        }
    }
}
