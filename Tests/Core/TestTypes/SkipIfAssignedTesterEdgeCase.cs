namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class SkipIfAssignedTesterEdgeCase : MonoBehaviour
    {
        [SiblingComponent(SkipIfAssigned = true)]
        public SpriteRenderer alreadyAssigned;
    }
}
