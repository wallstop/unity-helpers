// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace Samples.UnityHelpers.DI.VContainer
{
    using System;
    using global::VContainer;
    using global::VContainer.Unity;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Integrations.VContainer;

    public sealed class GameLifetimeScope : LifetimeScope
    {
        [SerializeField]
        [Tooltip(
            "Include inactive GameObjects when the integration scans the active scene after the container builds."
        )]
        private bool _includeInactiveSceneObjects = true;

        [SerializeField]
        [Tooltip("Prefer the optimized single-pass scan when hydrating scene objects.")]
        private bool _useSinglePassScan = true;

        [SerializeField]
        [Tooltip(
            "Register a listener so additively loaded scenes automatically hydrate relational fields."
        )]
        private bool _listenForAdditiveScenes = true;

        protected override void Configure(IContainerBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            RelationalSceneAssignmentOptions options = new RelationalSceneAssignmentOptions(
                _includeInactiveSceneObjects,
                _useSinglePassScan
            );

            builder.RegisterRelationalComponents(options, _listenForAdditiveScenes);
        }
    }
}
