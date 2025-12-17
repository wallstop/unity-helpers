namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ParentOnlyAncestorsTester : MonoBehaviour
    {
        [ParentComponent(OnlyAncestors = true)]
        public SpriteRenderer ancestorOnly;

        [ParentComponent(OnlyAncestors = true)]
        public SpriteRenderer[] ancestorOnlyArray;

        [ParentComponent(OnlyAncestors = false)]
        public SpriteRenderer includeSelf;

        [ParentComponent(OnlyAncestors = false)]
        public SpriteRenderer[] includeSelfArray;
    }
}
