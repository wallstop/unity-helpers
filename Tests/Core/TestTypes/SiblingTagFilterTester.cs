// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class SiblingTagFilterTester : MonoBehaviour
    {
        [SiblingComponent(TagFilter = "Player")]
        public BoxCollider playerTaggedCollider;

        [SiblingComponent(TagFilter = "Player")]
        public SpriteRenderer[] playerTaggedRenderers;
    }
}
