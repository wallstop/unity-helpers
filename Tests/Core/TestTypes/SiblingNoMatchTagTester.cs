using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class SiblingNoMatchTagTester : MonoBehaviour
    {
        [SiblingComponent(TagFilter = "Player")]
        public BoxCollider siblingCollider;
    }
}
