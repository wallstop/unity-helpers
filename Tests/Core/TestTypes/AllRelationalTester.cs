// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class AllRelationalTester : MonoBehaviour
    {
        [ParentComponent(OnlyAncestors = true)]
        public SpriteRenderer parentRenderer;

        [ChildComponent(OnlyDescendants = false)]
        public SpriteRenderer childRenderer;

        [SiblingComponent]
        public BoxCollider siblingCollider;
    }
}
