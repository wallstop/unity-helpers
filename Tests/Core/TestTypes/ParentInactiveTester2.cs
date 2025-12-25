namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    internal sealed class ParentInactiveTester2 : MonoBehaviour
    {
        [ParentComponent(IncludeInactive = true)]
        public SpriteRenderer includeInactive;

        [ParentComponent(IncludeInactive = false)]
        public SpriteRenderer excludeInactive;
    }
}
