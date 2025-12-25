namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class SiblingOptionalTester : MonoBehaviour
    {
        [SiblingComponent(Optional = true)]
        public BoxCollider optionalCollider;
    }
}
