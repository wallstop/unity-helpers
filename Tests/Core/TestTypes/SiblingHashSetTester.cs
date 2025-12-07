using System.Collections.Generic;
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class SiblingHashSetTester : MonoBehaviour
    {
        [SiblingComponent]
        public HashSet<BoxCollider> siblingColliders;
    }
}
