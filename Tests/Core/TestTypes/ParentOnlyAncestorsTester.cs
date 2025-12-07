using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
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
