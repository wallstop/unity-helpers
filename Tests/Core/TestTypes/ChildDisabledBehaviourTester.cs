// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ChildDisabledBehaviourTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, IncludeInactive = false)]
        public BoxCollider activeOnly;

        [ChildComponent(OnlyDescendants = true, IncludeInactive = false)]
        public BoxCollider[] activeOnlyArray;

        [ChildComponent(OnlyDescendants = true, IncludeInactive = true)]
        public BoxCollider includeInactive;

        [ChildComponent(OnlyDescendants = true, IncludeInactive = true)]
        public BoxCollider[] includeInactiveArray;
    }
}
