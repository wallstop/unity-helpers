namespace Samples.UnityHelpers.DI.VContainer
{
    using System;
    using global::VContainer;
    using global::VContainer.Unity;
    using WallstopStudios.UnityHelpers.Integrations.VContainer;

    public sealed class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // Register the assigner + scene entry point
            builder.RegisterRelationalComponents();

            // Or customize scanning (active objects only)
            // builder.RegisterRelationalComponents(new RelationalSceneAssignmentOptions(includeInactive: false));
        }
    }
}
