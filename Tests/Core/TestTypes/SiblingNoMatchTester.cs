using System.Collections.Generic;
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class SiblingNoMatchTester : MonoBehaviour
    {
        [SiblingComponent]
        public BoxCollider siblingCollider;

        [SiblingComponent]
        public BoxCollider[] colliderArray;

        [SiblingComponent]
        public List<BoxCollider> colliderList;
    }
}
