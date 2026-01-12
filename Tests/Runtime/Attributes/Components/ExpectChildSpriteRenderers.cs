// MIT License - Copyright (c) 2024 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Attributes.Components
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [DisallowMultipleComponent]
    public sealed class ExpectChildSpriteRenderers : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true)]
        public List<SpriteRenderer> inclusiveChildrenList;

        [ChildComponent(OnlyDescendants = false)]
        public List<SpriteRenderer> exclusiveChildrenList;

        [ChildComponent(OnlyDescendants = true)]
        public SpriteRenderer[] inclusiveChildrenArray;

        [ChildComponent(OnlyDescendants = false)]
        public SpriteRenderer[] exclusiveChildrenArray;

        [ChildComponent(OnlyDescendants = true)]
        public SpriteRenderer inclusiveChild;

        [ChildComponent(OnlyDescendants = false)]
        public SpriteRenderer exclusiveChild;
    }
}
