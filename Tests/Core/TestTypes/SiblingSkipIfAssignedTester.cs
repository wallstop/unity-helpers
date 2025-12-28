// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class SiblingSkipIfAssignedTester : MonoBehaviour
    {
        [SiblingComponent(SkipIfAssigned = true)]
        public BoxCollider preAssignedSibling;

        [SiblingComponent(SkipIfAssigned = true)]
        public BoxCollider[] preAssignedSiblingArray;

        [SiblingComponent(SkipIfAssigned = true)]
        public List<BoxCollider> preAssignedSiblingList;

        [SiblingComponent]
        public BoxCollider normalSibling;
    }
}
