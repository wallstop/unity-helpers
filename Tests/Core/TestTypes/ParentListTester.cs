namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ParentListTester : MonoBehaviour
    {
        [ParentComponent]
        public List<SpriteRenderer> parents;
    }
}
