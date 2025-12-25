namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ChildMaxDepthTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, MaxDepth = 1)]
        public SpriteRenderer depth1Only;

        [ChildComponent(OnlyDescendants = true, MaxDepth = 2)]
        public SpriteRenderer[] depth2Array;

        [ChildComponent(OnlyDescendants = true)]
        public List<SpriteRenderer> allDepthList;
    }
}
