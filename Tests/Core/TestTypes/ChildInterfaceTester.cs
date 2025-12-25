namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ChildInterfaceTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true)]
        public ITestInterface interfaceChild;

        [ChildComponent(OnlyDescendants = true)]
        public List<ITestInterface> interfaceChildList;
    }
}
