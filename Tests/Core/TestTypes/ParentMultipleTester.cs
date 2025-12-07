using System.Collections.Generic;
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class ParentMultipleTester : MonoBehaviour
    {
        [ParentComponent(IncludeInactive = true)]
        public SpriteRenderer[] allParents;

        [ParentComponent(IncludeInactive = true)]
        public List<SpriteRenderer> allParentsList;
    }
}
