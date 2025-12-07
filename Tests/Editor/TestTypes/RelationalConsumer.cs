namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class RelationalConsumer : MonoBehaviour
    {
        [SiblingComponent]
        private SpriteRenderer _spriteRenderer;

        public SpriteRenderer SR => _spriteRenderer;
    }
}
