// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class PrewarmTesterComponent : MonoBehaviour
    {
        [ParentComponent(OnlyAncestors = true)]
        public Rigidbody parentBody;

        [SiblingComponent]
        public BoxCollider siblingCollider;

        [ChildComponent(OnlyDescendants = true, MaxDepth = 1)]
        public Collider[] childColliders;
    }
}
