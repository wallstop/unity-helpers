namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class UntaggedFilterTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, TagFilter = "Untagged")]
        public List<SpriteRenderer> untagged;
    }
}
