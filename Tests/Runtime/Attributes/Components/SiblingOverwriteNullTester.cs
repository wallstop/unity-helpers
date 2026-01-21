// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Attributes.Components
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Tests.Core.TestTypes;

    public sealed class SiblingOverwriteNullTester : MonoBehaviour
    {
        [SiblingComponent(Optional = true)]
        public BoxCollider concreteField;

        [SiblingComponent(Optional = true)]
        public ITestInterface interfaceField;
    }
}
