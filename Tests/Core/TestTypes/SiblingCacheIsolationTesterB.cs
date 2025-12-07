using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class SiblingCacheIsolationTesterB : MonoBehaviour
    {
        [SiblingComponent]
        public BoxCollider siblingCollider;
    }
}
