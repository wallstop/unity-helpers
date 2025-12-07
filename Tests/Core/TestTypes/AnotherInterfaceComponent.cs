namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using UnityEngine;

    public sealed class AnotherInterfaceComponent : MonoBehaviour, ITestInterface
    {
        public string GetTestValue() => "Another";
    }
}
