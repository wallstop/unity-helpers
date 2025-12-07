namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ParentTagFilterTester : MonoBehaviour
    {
        [ParentComponent(TagFilter = "Player")]
        public SpriteRenderer playerTaggedParent;

        [ParentComponent]
        public SpriteRenderer[] allParents;
    }
}
