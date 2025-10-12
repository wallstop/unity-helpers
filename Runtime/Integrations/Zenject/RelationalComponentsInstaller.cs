#if ZENJECT_PRESENT
namespace WallstopStudios.UnityHelpers.Integrations.Zenject
{
    using global::Zenject;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Tags;

    /// <summary>
    /// Zenject installer that wires up the relational component assigner and optional scene
    /// initializer.
    /// </summary>
    [AddComponentMenu("Wallstop Studios/Relational Components/Zenject Installer")]
    public sealed class RelationalComponentsInstaller : MonoInstaller
    {
        [SerializeField]
        [Tooltip(
            "When enabled the installer will register a scene-level initializer that populates relational"
                + " fields immediately after Zenject finishes constructing the container."
        )]
        private bool _assignSceneOnInitialize = true;

        [SerializeField]
        [Tooltip(
            "Include inactive GameObjects when scanning the active scene for relational fields during"
                + " initialization."
        )]
        private bool _includeInactiveObjects = true;

        public override void InstallBindings()
        {
            AttributeMetadataCache cacheInstance = AttributeMetadataCache.Instance;
            if (cacheInstance != null && !Container.HasBinding(typeof(AttributeMetadataCache)))
            {
                Container.Bind<AttributeMetadataCache>().FromInstance(cacheInstance);
            }

            if (!Container.HasBinding(typeof(IRelationalComponentAssigner)))
            {
                Container
                    .Bind<IRelationalComponentAssigner>()
                    .To<RelationalComponentAssigner>()
                    .AsSingle();
            }

            if (_assignSceneOnInitialize)
            {
                if (!Container.HasBinding(typeof(RelationalSceneAssignmentOptions)))
                {
                    Container.BindInstance(
                        new RelationalSceneAssignmentOptions(_includeInactiveObjects)
                    );
                }

                Container.BindInterfacesTo<RelationalComponentSceneInitializer>().AsSingle();
            }
        }
    }
}
#endif
