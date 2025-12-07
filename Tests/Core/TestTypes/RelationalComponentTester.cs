using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class RelationalComponentTester : MonoBehaviour
    {
        [ParentComponent(OnlyAncestors = true)]
        public Rigidbody parentBody;

        [SiblingComponent]
        public BoxCollider siblingCollider;

        [ChildComponent(OnlyDescendants = true)]
        public CapsuleCollider childCollider;
    }
}
