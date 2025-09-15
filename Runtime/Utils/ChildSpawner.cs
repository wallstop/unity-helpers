namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Extension;
    using UnityEngine;

    [Flags]
    public enum ChildSpawnMethod
    {
        None = 0,
        Awake = 1 << 0,
        OnEnabled = 1 << 1,
        Start = 1 << 2,
    }

    [DisallowMultipleComponent]
    public sealed class ChildSpawner : MonoBehaviour
    {
        private static readonly HashSet<GameObject> SpawnedPrefabs = new();

        [SerializeField]
        private ChildSpawnMethod _spawnMethod = ChildSpawnMethod.Start;

        [SerializeField]
        private GameObject[] _prefabs = Array.Empty<GameObject>();

        [SerializeField]
        private GameObject[] _editorOnlyPrefabs = Array.Empty<GameObject>();

        [SerializeField]
        private GameObject[] _developmentOnlyPrefabs = Array.Empty<GameObject>();

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
            }

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
            }
        }

        private static void CleanName(GameObject child)
        {
            child.name = child.name.Replace("(Clone)", string.Empty);
        }

        private GameObject Spawn(GameObject prefab)
        {
            if (SpawnedPrefabs.Contains(prefab))
            {
                return null;
            }

            GameObject child = Instantiate(prefab, transform);
            CleanName(child);
            if (child.IsDontDestroyOnLoad() || gameObject.IsDontDestroyOnLoad())
            {
                SpawnedPrefabs.Add(prefab);
            }
            return child;
        }
    }
}
