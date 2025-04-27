﻿namespace WallstopStudios.UnityHelpers.Tests.Attributes.Components
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [DisallowMultipleComponent]
    public sealed class ExpectChildSpriteRenderers : MonoBehaviour
    {
        [ChildComponent(onlyDescendents = true)]
        public List<SpriteRenderer> inclusiveChildrenList;

        [ChildComponent(onlyDescendents = false)]
        public List<SpriteRenderer> exclusiveChildrenList;

        [ChildComponent(onlyDescendents = true)]
        public SpriteRenderer[] inclusiveChildrenArray;

        [ChildComponent(onlyDescendents = false)]
        public SpriteRenderer[] exclusiveChildrenArray;

        [ChildComponent(onlyDescendents = true)]
        public SpriteRenderer inclusiveChild;

        [ChildComponent(onlyDescendents = false)]
        public SpriteRenderer exclusiveChild;
    }
}
