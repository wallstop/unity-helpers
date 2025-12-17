namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class SiblingHashSetTester : MonoBehaviour
    {
        [SiblingComponent]
        public HashSet<BoxCollider> siblingColliders;
    }
}
