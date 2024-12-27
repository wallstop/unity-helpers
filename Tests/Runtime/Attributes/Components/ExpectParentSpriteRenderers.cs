namespace UnityHelpers.Tests.Attributes.Components
{
    using System.Collections.Generic;
    using Core.Attributes;
    using UnityEngine;

    [DisallowMultipleComponent]
    public sealed class ExpectParentSpriteRenderers : MonoBehaviour
    {
        [ParentComponent(onlyAncestors = true)]
        public List<SpriteRenderer> inclusiveParentList;

        [ParentComponent(onlyAncestors = false)]
        public List<SpriteRenderer> exclusiveParentList;

        [ParentComponent(onlyAncestors = true)]
        public SpriteRenderer[] inclusiveParentArray;

        [ParentComponent(onlyAncestors = false)]
        public SpriteRenderer[] exclusiveParentArray;

        [ParentComponent(onlyAncestors = true)]
        public SpriteRenderer inclusiveParent;

        [ParentComponent(onlyAncestors = false)]
        public SpriteRenderer exclusiveParent;
    }
}
