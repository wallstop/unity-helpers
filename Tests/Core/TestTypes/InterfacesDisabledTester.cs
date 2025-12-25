namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class InterfacesDisabledTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true, AllowInterfaces = false)]
        public ITestInterface iface;
    }
}
