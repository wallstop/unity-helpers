using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class SiblingDisabledTester : MonoBehaviour
    {
        [SiblingComponent]
        public BoxCollider siblingCollider;
    }
}
