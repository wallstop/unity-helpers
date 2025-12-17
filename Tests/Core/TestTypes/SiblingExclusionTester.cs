namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class SiblingExclusionTester : MonoBehaviour
    {
        [SiblingComponent]
        public BoxCollider[] colliders;
    }
}
