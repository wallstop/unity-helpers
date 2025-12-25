namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ParentArrayTester : MonoBehaviour
    {
        [ParentComponent]
        public SpriteRenderer[] parents;
    }
}
