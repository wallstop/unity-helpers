namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;

    internal sealed class TestInterfaceComponent2 : MonoBehaviour, ITestInterface
    {
        public string GetTestValue() => "Test";
    }
}
