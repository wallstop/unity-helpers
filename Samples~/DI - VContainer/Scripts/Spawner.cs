namespace Samples.UnityHelpers.DI.VContainer
{
    using global::VContainer;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Integrations.VContainer;

    public sealed class Spawner : MonoBehaviour
    {
        [SerializeField]
        private RelationalConsumer _prefab;

        [Inject]
        private IObjectResolver _resolver;

        public RelationalConsumer Build(Transform parent)
        {
            RelationalConsumer instance = Instantiate(_prefab, parent);
            return _resolver.BuildUpWithRelations(instance);
        }
    }
}
