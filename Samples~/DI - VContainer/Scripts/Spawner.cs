// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace Samples.UnityHelpers.DI.VContainer
{
    using global::VContainer;
    using UnityEngine;
    using UnityEngine.Pool;
    using WallstopStudios.UnityHelpers.Integrations.VContainer;

    public sealed class Spawner : MonoBehaviour
    {
        [SerializeField]
        private RelationalConsumer _componentPrefab;

        [SerializeField]
        private GameObject _hierarchyPrefab;

        [SerializeField]
        private Transform _defaultParent;

        [Inject]
        private IObjectResolver _resolver;

        private ObjectPool<RelationalConsumer> _componentPool;

        private void Awake()
        {
            _componentPool = RelationalObjectPools.CreatePoolWithRelations(
                createFunc: () => Instantiate(_componentPrefab),
                actionOnGet: OnGetFromPool,
                actionOnRelease: OnReleaseToPool
            );
        }

        /// <summary>
        /// Instantiate a component prefab through VContainer so dependencies and relational fields
        /// are populated in one call.
        /// </summary>
        public RelationalConsumer SpawnComponent(Transform parent)
        {
            Transform targetParent = parent != null ? parent : _defaultParent;
            RelationalConsumer instance = _resolver.InstantiateComponentWithRelations(
                _componentPrefab,
                targetParent
            );
            return instance;
        }

        /// <summary>
        /// Instantiate a hierarchy prefab and hydrate every attributed component beneath it.
        /// </summary>
        public GameObject SpawnHierarchy(Transform parent)
        {
            Transform targetParent = parent != null ? parent : _defaultParent;
            GameObject root = _resolver.InstantiateGameObjectWithRelations(
                _hierarchyPrefab,
                targetParent,
                includeInactiveChildren: true
            );
            return root;
        }

        /// <summary>
        /// Rent a component from a simple pool, then inject and assign it through the resolver.
        /// </summary>
        public RelationalConsumer SpawnFromPool(Transform parent)
        {
            Transform targetParent = parent != null ? parent : _defaultParent;
            RelationalConsumer instance = _componentPool.GetWithRelations(_resolver);
            if (targetParent != null)
            {
                instance.transform.SetParent(targetParent, false);
            }
            return instance;
        }

        /// <summary>
        /// Return an instance to the pool.
        /// </summary>
        public void ReturnToPool(RelationalConsumer instance)
        {
            if (instance == null)
            {
                return;
            }

            _componentPool.Release(instance);
        }

        /// <summary>
        /// Hydrate an existing hierarchy that was created outside of the resolver (e.g., scene tools).
        /// </summary>
        public void HydrateExistingHierarchy(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            _resolver.AssignRelationalHierarchy(root, includeInactiveChildren: true);
        }

        private void OnGetFromPool(RelationalConsumer consumer)
        {
            if (consumer == null)
            {
                return;
            }

            consumer.gameObject.SetActive(true);
        }

        private void OnReleaseToPool(RelationalConsumer consumer)
        {
            if (consumer == null)
            {
                return;
            }

            consumer.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_componentPool != null)
            {
                _componentPool.Clear();
                _componentPool = null;
            }
        }
    }
}
