namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class SiblingMixedActiveTester : MonoBehaviour
    {
        [SiblingComponent(IncludeInactive = false)]
        public BoxCollider[] activeOnly;

        [SiblingComponent(IncludeInactive = true)]
        public BoxCollider[] includeInactive;
    }
}
