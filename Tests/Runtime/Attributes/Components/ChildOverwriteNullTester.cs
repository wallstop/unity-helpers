// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Attributes.Components
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Tests.Core.TestTypes;

    public sealed class ChildOverwriteNullTester : MonoBehaviour
    {
        [ChildComponent(Optional = true)]
        public BoxCollider concreteField;

        [ChildComponent(Optional = true)]
        public ITestInterface interfaceField;
    }
}
