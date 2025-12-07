using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class SiblingMixedActiveTester : MonoBehaviour
    {
        [SiblingComponent(IncludeInactive = false)]
        public BoxCollider[] activeOnly;

        [SiblingComponent(IncludeInactive = true)]
        public BoxCollider[] includeInactive;
    }
}
