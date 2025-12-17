namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class SiblingDisabledTester : MonoBehaviour
    {
        [SiblingComponent]
        public BoxCollider siblingCollider;
    }
}
