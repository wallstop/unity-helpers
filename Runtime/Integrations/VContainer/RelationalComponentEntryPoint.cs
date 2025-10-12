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
    /// <remarks>
    /// This type is registered automatically by calling
    /// <see cref="RelationalComponentsBuilderExtensions"/>,
    /// and runs once on startup to assign relational fields across the active scene. It uses
    /// <see cref="AttributeMetadataCache"/> (when present) to quickly locate components that have
    /// relational attributes.
    /// </remarks>
    /// <example>
    /// <code>
    /// // LifetimeScope that enables relational assignments at scene start
    /// public sealed class GameLifetimeScope : LifetimeScope
    /// {
    ///     protected override void Configure(IContainerBuilder builder)
    ///     {
    ///         builder.RegisterRelationalComponents(
    ///             new RelationalSceneAssignmentOptions(includeInactive: true)
    ///         );
    ///     }
    /// }
    /// </code>
    /// </example>
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
            AttributeMetadataCache cache = _metadataCache;
            if (cache == null)
            {
                cache = AttributeMetadataCache.Instance;
                if (cache == null)
                {
                    return;
                }
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
