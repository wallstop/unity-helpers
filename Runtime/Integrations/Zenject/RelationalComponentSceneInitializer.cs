#if ZENJECT_PRESENT
namespace WallstopStudios.UnityHelpers.Integrations.Zenject
{
    using System;
    using System.Collections.Generic;
    using global::Zenject;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Tags;
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// Scene-level entry point that assigns relational component fields immediately after Zenject
    /// finishes injecting the container.
    /// </summary>
    public sealed class RelationalComponentSceneInitializer : IInitializable
    {
        private readonly IRelationalComponentAssigner _assigner;
        private readonly AttributeMetadataCache _metadataCache;
        private readonly RelationalSceneAssignmentOptions _options;

        public RelationalComponentSceneInitializer(
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

                    // Ignore components that belong to scenes other than the active one to avoid
                    // touching additive scenes unintentionally.
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
