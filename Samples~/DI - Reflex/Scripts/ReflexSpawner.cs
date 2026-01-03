// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace Samples.UnityHelpers.DI.Reflex
{
    using Reflex.Core;
    using Reflex.Extensions;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Integrations.Reflex;

    /// <summary>
    /// Demonstrates the Reflex container helpers for instantiation and relational assignment.
    /// </summary>
    public sealed class ReflexSpawner : MonoBehaviour
    {
        [SerializeField]
        private ReflexRelationalConsumer _componentPrefab;

        [SerializeField]
        private GameObject _hierarchyPrefab;

        [SerializeField]
        private Transform _defaultParent;

        private Container _sceneContainer;

        private void Awake()
        {
            _sceneContainer = gameObject.scene.GetSceneContainer();
        }

        /// <summary>
        /// Instantiate a component prefab through Reflex so dependencies and relational fields are ready.
        /// </summary>
        public ReflexRelationalConsumer SpawnComponent(Transform parent)
        {
            if (_sceneContainer == null || _componentPrefab == null)
            {
                return null;
            }

            Transform targetParent = parent != null ? parent : _defaultParent;
            ReflexRelationalConsumer instance = _sceneContainer.InstantiateComponentWithRelations(
                _componentPrefab,
                targetParent
            );
            return instance;
        }

        /// <summary>
        /// Instantiate a hierarchy prefab and hydrate the entire tree.
        /// </summary>
        public GameObject SpawnHierarchy(Transform parent)
        {
            if (_sceneContainer == null || _hierarchyPrefab == null)
            {
                return null;
            }

            Transform targetParent = parent != null ? parent : _defaultParent;
            GameObject root = _sceneContainer.InstantiateGameObjectWithRelations(
                _hierarchyPrefab,
                targetParent,
                includeInactiveChildren: true
            );
            return root;
        }

        /// <summary>
        /// Hydrate a GameObject that was spawned outside of Reflex.
        /// </summary>
        public void HydrateExistingHierarchy(GameObject root, bool includeInactiveChildren)
        {
            if (_sceneContainer == null || root == null)
            {
                return;
            }

            _sceneContainer.InjectGameObjectWithRelations(root, includeInactiveChildren);
        }
    }
}
