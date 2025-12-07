namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class ParentInterfaceTester : MonoBehaviour
    {
        [ParentComponent]
        public ITestInterface interfaceParent;

        [ParentComponent]
        public ITestInterface[] interfaceParentArray;
    }
}
