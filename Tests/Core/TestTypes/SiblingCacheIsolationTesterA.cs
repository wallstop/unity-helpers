namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class SiblingCacheIsolationTesterA : MonoBehaviour
    {
        [SiblingComponent]
        public BoxCollider siblingCollider;
    }
}
