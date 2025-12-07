using System.Collections.Generic;
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class SiblingOrderTester : MonoBehaviour
    {
        [SiblingComponent]
        public List<BoxCollider> colliders;
    }
}
