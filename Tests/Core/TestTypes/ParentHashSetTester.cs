namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ParentHashSetTester : MonoBehaviour
    {
        [ParentComponent]
        public HashSet<SpriteRenderer> parentRenderers;
    }
}
