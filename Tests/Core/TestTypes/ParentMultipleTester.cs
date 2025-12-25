namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ParentMultipleTester : MonoBehaviour
    {
        [ParentComponent(IncludeInactive = true)]
        public SpriteRenderer[] allParents;

        [ParentComponent(IncludeInactive = true)]
        public List<SpriteRenderer> allParentsList;
    }
}
