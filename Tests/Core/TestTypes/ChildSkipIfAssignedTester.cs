// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ChildSkipIfAssignedTester : MonoBehaviour
    {
        [ChildComponent(SkipIfAssigned = true)]
        public SpriteRenderer preAssignedChild;

        [ChildComponent(SkipIfAssigned = true)]
        public SpriteRenderer[] preAssignedChildArray;

        [ChildComponent(SkipIfAssigned = true)]
        public List<SpriteRenderer> preAssignedChildList;

        [ChildComponent]
        public SpriteRenderer normalChild;
    }
}
