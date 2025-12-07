namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ChildCombinedTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, TagFilter = "Player", MaxCount = 2)]
        public List<SpriteRenderer> limitedPlayerChildren;
    }
}
