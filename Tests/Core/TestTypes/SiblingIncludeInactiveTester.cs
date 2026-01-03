// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class SiblingIncludeInactiveTester : MonoBehaviour
    {
        [SiblingComponent(IncludeInactive = true)]
        public BoxCollider includeInactiveSingle;

        [SiblingComponent(IncludeInactive = true)]
        public BoxCollider[] includeInactiveArray;

        [SiblingComponent(IncludeInactive = true)]
        public List<BoxCollider> includeInactiveList;
    }
}
