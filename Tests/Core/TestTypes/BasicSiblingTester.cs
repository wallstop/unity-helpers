namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class BasicSiblingTester : MonoBehaviour
    {
        [SiblingComponent]
        public BoxCollider sibling;
    }
}
