namespace Samples.UnityHelpers.DI.VContainer
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class RelationalConsumer : MonoBehaviour
    {
        [SiblingComponent]
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            // In scenes, this is hydrated by the VContainer entry point after the container builds.
            // For runtime instances, see Spawner.Build.
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.cyan;
            }
        }
    }
}
