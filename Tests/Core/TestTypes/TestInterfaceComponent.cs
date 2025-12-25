namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;

    public sealed class TestInterfaceComponent : MonoBehaviour, ITestInterface
    {
        public string GetTestValue() => "Test";
    }
}
