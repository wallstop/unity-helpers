// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    internal sealed class ParentInactiveTester2 : MonoBehaviour
    {
        [ParentComponent(IncludeInactive = true)]
        public SpriteRenderer includeInactive;

        [ParentComponent(IncludeInactive = false)]
        public SpriteRenderer excludeInactive;
    }
}
