namespace Samples.UnityHelpers.DI.Zenject
{
    using global::Zenject;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Integrations.Zenject;

    /// <summary>
    /// Example Zenject memory pool that automatically hydrates relational fields when items are spawned.
    /// Bind this pool in an installer with
    /// <code>
    /// Container.BindMemoryPool&lt;RelationalConsumer, RelationalConsumerPool&gt;()
    ///     .FromComponentInNewPrefab(componentPrefab)
    ///     .UnderTransform(poolRoot);
    /// </code>
    /// </summary>
    public sealed class RelationalConsumerPool : RelationalMemoryPool<RelationalConsumer>
    {
        protected override void OnSpawned(RelationalConsumer item)
        {
            base.OnSpawned(item);
            if (item != null)
            {
                item.gameObject.SetActive(true);
            }
        }

        protected override void OnDespawned(RelationalConsumer item)
        {
            if (item != null)
            {
                item.gameObject.SetActive(false);
            }

            base.OnDespawned(item);
        }
    }
}
