namespace Samples.UnityHelpers.DI.Zenject
{
    using global::Zenject;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Integrations.Zenject;

    public sealed class SpawnerZenject : MonoBehaviour
    {
        [SerializeField]
        private RelationalConsumer _prefab;

        [Inject]
        private DiContainer _container;

        public RelationalConsumer Build(Transform parent)
        {
            return _container.InstantiateComponentWithRelations(_prefab, parent);
        }
    }
}
