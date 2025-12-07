namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ParentMaxCountTester : MonoBehaviour
    {
        [ParentComponent(MaxCount = 2)]
        public SpriteRenderer[] limitedParents;

        [ParentComponent]
        public SpriteRenderer[] allParents;
    }
}
