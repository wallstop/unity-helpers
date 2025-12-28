// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace Samples.UnityHelpers.DI.Zenject
{
    using global::Zenject;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Integrations.Zenject;

    public sealed class SpawnerZenject : MonoBehaviour
    {
        [SerializeField]
        private RelationalConsumer _componentPrefab;

        [SerializeField]
        private GameObject _hierarchyPrefab;

        [SerializeField]
        private Transform _defaultParent;

        [Inject]
        private DiContainer _container;

        [Inject(Optional = true)]
        private RelationalConsumerPool _pool;

        /// <summary>
        /// Instantiate a component prefab through Zenject so dependencies and relational fields are populated automatically.
        /// </summary>
        public RelationalConsumer SpawnComponent(Transform parent)
        {
            Transform targetParent = parent != null ? parent : _defaultParent;
            return _container.InstantiateComponentWithRelations(_componentPrefab, targetParent);
        }

        /// <summary>
        /// Instantiate a hierarchy prefab and hydrate every attributed component beneath it.
        /// </summary>
        public GameObject SpawnHierarchy(Transform parent)
        {
            Transform targetParent = parent != null ? parent : _defaultParent;
            return _container.InstantiateGameObjectWithRelations(
                _hierarchyPrefab,
                targetParent,
                includeInactiveChildren: true
            );
        }

        /// <summary>
        /// Rent an instance from a Zenject memory pool (falls back to SpawnComponent when the pool is not bound).
        /// </summary>
        public RelationalConsumer SpawnFromPool(Transform parent)
        {
            if (_pool == null)
            {
                return SpawnComponent(parent);
            }

            RelationalConsumer instance = _pool.Spawn();
            Transform targetParent = parent != null ? parent : _defaultParent;
            if (targetParent != null)
            {
                instance.transform.SetParent(targetParent, false);
            }
            return instance;
        }

        /// <summary>
        /// Return an instance to the pool.
        /// </summary>
        public void DespawnToPool(RelationalConsumer instance)
        {
            if (_pool == null || instance == null)
            {
                return;
            }

            _pool.Despawn(instance);
        }

        /// <summary>
        /// Hydrate an existing hierarchy that was created outside of the container (editor tooling etc.).
        /// </summary>
        public void HydrateExistingHierarchy(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            _container.AssignRelationalHierarchy(root, includeInactiveChildren: true);
        }
    }
}
