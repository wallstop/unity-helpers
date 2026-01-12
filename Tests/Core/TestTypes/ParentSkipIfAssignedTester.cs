// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ParentSkipIfAssignedTester : MonoBehaviour
    {
        [ParentComponent(SkipIfAssigned = true)]
        public SpriteRenderer preAssignedParent;

        [ParentComponent(SkipIfAssigned = true)]
        public SpriteRenderer[] preAssignedParentArray;

        [ParentComponent(SkipIfAssigned = true)]
        public List<SpriteRenderer> preAssignedParentList;

        [ParentComponent(OnlyAncestors = true)]
        public SpriteRenderer normalParent;
    }
}
