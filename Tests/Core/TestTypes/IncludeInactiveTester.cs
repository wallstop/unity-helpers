using System.Collections.Generic;
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class IncludeInactiveTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, IncludeInactive = false, TagFilter = "Player")]
        public List<SpriteRenderer> onlyActivePlayers;
    }
}
