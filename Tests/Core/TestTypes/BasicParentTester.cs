namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class BasicParentTester : MonoBehaviour
    {
        [ParentComponent]
        public SpriteRenderer parent;
    }
}
