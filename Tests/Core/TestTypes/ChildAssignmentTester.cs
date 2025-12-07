namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ChildAssignmentTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, IncludeInactive = false)]
        public SpriteRenderer activeOnly;

        [ChildComponent(OnlyDescendants = true, IncludeInactive = true)]
        public SpriteRenderer inactive;

        [ChildComponent(OnlyDescendants = true, IncludeInactive = false)]
        public List<SpriteRenderer> descendentsActiveOnlyList;

        [ChildComponent(OnlyDescendants = true, IncludeInactive = true)]
        public List<SpriteRenderer> descendentsAllList;

        [ChildComponent(OnlyDescendants = true, IncludeInactive = false)]
        public SpriteRenderer[] descendentsActiveOnlyArray;

        [ChildComponent(OnlyDescendants = true, IncludeInactive = true)]
        public SpriteRenderer[] descendentsAllArray;
    }
}
