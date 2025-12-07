namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class SiblingArrayTester : MonoBehaviour
    {
        [SiblingComponent]
        public BoxCollider[] siblings;
    }
}
