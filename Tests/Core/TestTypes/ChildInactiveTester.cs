// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ChildInactiveTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, IncludeInactive = false)]
        public SpriteRenderer activeOnly;

        [ChildComponent(OnlyDescendants = true, IncludeInactive = false)]
        public SpriteRenderer[] activeOnlyArray;

        [ChildComponent(OnlyDescendants = true, IncludeInactive = true)]
        public SpriteRenderer[] includeInactiveArray;
    }
}
