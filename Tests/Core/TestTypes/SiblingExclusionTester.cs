using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class SiblingExclusionTester : MonoBehaviour
    {
        [SiblingComponent]
        public BoxCollider[] colliders;
    }
}
