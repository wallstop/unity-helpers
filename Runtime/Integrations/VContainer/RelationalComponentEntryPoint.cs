#if VCONTAINER_PRESENT
namespace WallstopStudios.UnityHelpers.Integrations.VContainer
{
    using System;
    using System.Collections.Generic;
    using global::VContainer.Unity;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Tags;
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// Entry point registered with VContainer to hydrate relational attributes once the container is
    /// fully built.
    /// </summary>
    public sealed class RelationalComponentEntryPoint : IInitializable
    {
        private readonly IRelationalComponentAssigner _assigner;
        private readonly AttributeMetadataCache _metadataCache;
        private readonly RelationalSceneAssignmentOptions _options;

        public RelationalComponentEntryPoint(
            IRelationalComponentAssigner assigner,
            AttributeMetadataCache metadataCache,
            RelationalSceneAssignmentOptions options
        )
        {
            _assigner = assigner ?? throw new ArgumentNullException(nameof(assigner));
            _metadataCache = metadataCache;
            _options = options;
        }

        public void Initialize()
        {
            AttributeMetadataCache cache = _metadataCache ?? AttributeMetadataCache.Instance;
            if (cache == null)
            {
                return;
            }

            using PooledResource<List<Type>> pooledTypes = Buffers<Type>.List.Get(
                out List<Type> relationalTypes
            );
            cache.CollectRelationalComponentTypes(relationalTypes);

            if (relationalTypes.Count == 0)
            {
                return;
            }

            bool includeInactive = _options.IncludeInactive;
            Scene activeScene = SceneManager.GetActiveScene();

            foreach (Type componentType in relationalTypes)
            {
                if (componentType == null)
                {
                    continue;
                }

                UnityEngine.Object[] located = includeInactive
                    ? UnityEngine.Object.FindObjectsOfType(componentType, true)
                    : UnityEngine.Object.FindObjectsOfType(componentType, false);

                foreach (UnityEngine.Object candidate in located)
                {
                    if (candidate is not Component component)
                    {
                        continue;
                    }

                    if (component.gameObject.scene != activeScene)
                    {
                        continue;
                    }

                    _assigner.Assign(component);
                }
            }
        }
    }
}
#endif
