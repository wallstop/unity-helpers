// MIT License - Copyright (c) 2024 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Attributes.Components
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [DisallowMultipleComponent]
    public sealed class ExpectParentSpriteRenderers : MonoBehaviour
    {
        [ParentComponent(OnlyAncestors = true)]
        public List<SpriteRenderer> inclusiveParentList;

        [ParentComponent(OnlyAncestors = false)]
        public List<SpriteRenderer> exclusiveParentList;

        [ParentComponent(OnlyAncestors = true)]
        public SpriteRenderer[] inclusiveParentArray;

        [ParentComponent(OnlyAncestors = false)]
        public SpriteRenderer[] exclusiveParentArray;

        [ParentComponent(OnlyAncestors = true)]
        public SpriteRenderer inclusiveParent;

        [ParentComponent(OnlyAncestors = false)]
        public SpriteRenderer exclusiveParent;
    }
}
