// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class SiblingMaxCountTester : MonoBehaviour
    {
        [SiblingComponent(MaxCount = 2)]
        public BoxCollider[] limitedSiblings;

        [SiblingComponent]
        public BoxCollider[] allSiblings;
    }
}
