// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ParentMaxDepthTester : MonoBehaviour
    {
        [ParentComponent(OnlyAncestors = true, MaxDepth = 1)]
        public SpriteRenderer depth1Only;

        [ParentComponent(OnlyAncestors = true, MaxDepth = 2)]
        public SpriteRenderer[] depth2Array;

        [ParentComponent(OnlyAncestors = true)]
        public List<SpriteRenderer> allDepthList;
    }
}
