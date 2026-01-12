// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class SiblingInactiveTester : MonoBehaviour
    {
        [SiblingComponent(IncludeInactive = true)]
        public BoxCollider includeInactive;

        [SiblingComponent(IncludeInactive = false, Optional = true)]
        public BoxCollider excludeInactive;
    }
}
