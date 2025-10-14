namespace Samples.UnityHelpers.DI.Zenject
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Minimal component that demonstrates relational attributes being resolved by the Zenject integration.
    /// </summary>
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
