namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;

    internal sealed class AnotherInterfaceComponent2 : MonoBehaviour, ITestInterface
    {
        public string GetTestValue() => "Another";
    }
}
