namespace Samples.UnityHelpers.DI.Zenject
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class RelationalConsumer : MonoBehaviour
    {
        [SiblingComponent]
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.green;
            }
        }
    }
}
