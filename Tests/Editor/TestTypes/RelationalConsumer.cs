// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class RelationalConsumer : MonoBehaviour
    {
        [SiblingComponent]
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        private SpriteRenderer _spriteRenderer;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

        public SpriteRenderer SR => _spriteRenderer;
    }
}
