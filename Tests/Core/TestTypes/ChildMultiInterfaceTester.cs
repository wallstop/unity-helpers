namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ChildMultiInterfaceTester : MonoBehaviour
    {
        [ChildComponent(OnlyDescendants = true)]
        public ITestInterface[] allInterfaces;
    }
}
