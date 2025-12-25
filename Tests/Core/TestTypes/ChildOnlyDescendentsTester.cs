namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ChildOnlyDescendentsTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true)]
        public SpriteRenderer descendentOnly;

        [ChildComponent(OnlyDescendants = true)]
        public SpriteRenderer[] descendentOnlyArray;

        [ChildComponent(OnlyDescendants = false)]
        public SpriteRenderer includeSelf;

        [ChildComponent(OnlyDescendants = false)]
        public SpriteRenderer[] includeSelfArray;
    }
}
