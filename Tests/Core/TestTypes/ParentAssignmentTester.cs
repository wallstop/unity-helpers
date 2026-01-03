// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ParentAssignmentTester : MonoBehaviour
    {
        [ParentComponent(OnlyAncestors = true, IncludeInactive = false)]
        public SpriteRenderer ancestorsActiveOnly;

        [ParentComponent(OnlyAncestors = true, IncludeInactive = true)]
        public SpriteRenderer ancestorsIncludeInactive;

        [ParentComponent(IncludeInactive = true)]
        public List<SpriteRenderer> allParents;
    }
}
