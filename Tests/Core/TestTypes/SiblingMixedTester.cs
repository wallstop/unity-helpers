// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class SiblingMixedTester : MonoBehaviour
    {
        [SiblingComponent]
        public BoxCollider siblingCollider;

        [SiblingComponent]
        public SpriteRenderer siblingRenderer;

        [SiblingComponent]
        public Rigidbody siblingRigidBody;
    }
}
