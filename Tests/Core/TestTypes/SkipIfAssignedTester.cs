namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class SkipIfAssignedTester : MonoBehaviour
    {
        [ParentComponent(SkipIfAssigned = true)]
        public SpriteRenderer preAssigned;

        [ParentComponent]
        public SpriteRenderer normal;
    }
}
