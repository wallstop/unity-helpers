namespace WallstopStudios.UnityHelpers.Tests.Attributes.Components
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

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
