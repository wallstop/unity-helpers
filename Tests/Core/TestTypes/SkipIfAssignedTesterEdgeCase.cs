using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class SkipIfAssignedTesterEdgeCase : MonoBehaviour
    {
        [SiblingComponent(SkipIfAssigned = true)]
        public SpriteRenderer alreadyAssigned;
    }
}
