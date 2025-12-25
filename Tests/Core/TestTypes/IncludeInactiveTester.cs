namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class IncludeInactiveTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, IncludeInactive = false, TagFilter = "Player")]
        public List<SpriteRenderer> onlyActivePlayers;
    }
}
