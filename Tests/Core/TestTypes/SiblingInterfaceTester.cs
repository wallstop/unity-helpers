namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class SiblingInterfaceTester : MonoBehaviour
    {
        [SiblingComponent]
        public ITestInterface interfaceSibling;
    }
}
