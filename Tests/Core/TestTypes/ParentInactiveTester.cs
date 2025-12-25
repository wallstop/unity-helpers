namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ParentInactiveTester : MonoBehaviour
    {
        [ParentComponent(IncludeInactive = false)]
        public SpriteRenderer activeOnly;

        [ParentComponent(IncludeInactive = true)]
        public SpriteRenderer inactiveOnly;

        [ParentComponent(IncludeInactive = false)]
        public SpriteRenderer[] activeOnlyArray;

        [ParentComponent(IncludeInactive = true)]
        public SpriteRenderer[] inactiveOnlyArray;
    }
}
