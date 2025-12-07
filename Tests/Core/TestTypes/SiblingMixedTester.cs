using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
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
