// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace Samples.UnityHelpers.DI.VContainer
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Minimal component that demonstrates how relational attributes are hydrated by the DI integration.
    /// </summary>
    public sealed class RelationalConsumer : MonoBehaviour
    {
        [SiblingComponent]
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            // In scenes the lifetime scope entry point (or additive-scene listener) hydrates this field.
            // For runtime instances, see the different spawn helpers in Spawner.
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.cyan;
            }
        }
    }
}
