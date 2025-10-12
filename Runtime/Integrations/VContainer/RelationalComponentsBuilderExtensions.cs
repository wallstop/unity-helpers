#if VCONTAINER_PRESENT
namespace WallstopStudios.UnityHelpers.Integrations.VContainer
{
    using System;
    using VContainer;
    using VContainer.Unity;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Tags;

    /// <summary>
    /// Convenience registration helpers for wiring relational component support into a
    /// <see cref="LifetimeScope"/>.
    /// </summary>
    public static class RelationalComponentsBuilderExtensions
    {
        /// <summary>
        /// Registers the relational component assigner and scene entry point with the supplied
        /// container builder.
        /// </summary>
        public static void RegisterRelationalComponents(
            this IContainerBuilder builder,
            RelationalSceneAssignmentOptions? options = null
        )
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            AttributeMetadataCache cacheInstance = AttributeMetadataCache.Instance;
            if (cacheInstance != null)
            {
                builder.RegisterInstance(cacheInstance).AsSelf();
            }

            RelationalSceneAssignmentOptions resolved =
                options ?? RelationalSceneAssignmentOptions.Default;

            builder
                .Register<RelationalComponentAssigner>(Lifetime.Singleton)
                .As<IRelationalComponentAssigner>()
                .AsSelf();

            builder.RegisterEntryPoint<RelationalComponentEntryPoint>().WithParameter(resolved);
        }
    }
}
#endif
