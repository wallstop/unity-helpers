// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class SiblingExcludeInactiveTester : MonoBehaviour
    {
        [SiblingComponent(IncludeInactive = false)]
        public BoxCollider activeOnlySingle;

        [SiblingComponent(IncludeInactive = false)]
        public BoxCollider[] activeOnlyArray;

        [SiblingComponent(IncludeInactive = false)]
        public List<BoxCollider> activeOnlyList;
    }
}
