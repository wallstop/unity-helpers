namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ParentNameFilterTester : MonoBehaviour
    {
        [ParentComponent(NameFilter = "Player")]
        public SpriteRenderer playerNamedParent;

        [ParentComponent]
        public SpriteRenderer[] allParents;
    }
}
