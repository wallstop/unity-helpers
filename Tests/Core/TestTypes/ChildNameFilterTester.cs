namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ChildNameFilterTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, NameFilter = "Player")]
        public List<SpriteRenderer> playerNamedChildren;

        [ChildComponent(OnlyDescendants = true)]
        public List<SpriteRenderer> allChildren;
    }
}
