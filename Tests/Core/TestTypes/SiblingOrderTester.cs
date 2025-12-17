namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class SiblingOrderTester : MonoBehaviour
    {
        [SiblingComponent]
        public List<BoxCollider> colliders;
    }
}
