namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ChildMaxCountTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, MaxCount = 3)]
        public List<SpriteRenderer> limitedChildren;

        [ChildComponent(OnlyDescendants = true)]
        public List<SpriteRenderer> allChildren;
    }
}
