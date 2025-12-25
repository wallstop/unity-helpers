#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

    [ExcludeFromSingletonCreation]
    internal sealed class TestSingleton : ScriptableObjectSingleton<TestSingleton>
    {
        public int testValue = 42;
        public string payload;
    }
}
#endif
