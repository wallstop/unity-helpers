// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ChildHashSetInterfaceTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true)]
        public HashSet<ITestInterface> interfaceChildren;
    }
}
