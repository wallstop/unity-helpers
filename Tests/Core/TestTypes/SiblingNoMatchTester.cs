// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class SiblingNoMatchTester : MonoBehaviour
    {
        [SiblingComponent]
        public BoxCollider siblingCollider;

        [SiblingComponent]
        public BoxCollider[] colliderArray;

        [SiblingComponent]
        public List<BoxCollider> colliderList;
    }
}
