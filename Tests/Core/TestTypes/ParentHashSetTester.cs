using System.Collections.Generic;
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class ParentHashSetTester : MonoBehaviour
    {
        [ParentComponent]
        public HashSet<SpriteRenderer> parentRenderers;
    }
}
