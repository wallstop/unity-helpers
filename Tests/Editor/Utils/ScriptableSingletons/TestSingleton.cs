#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Utils;

    internal sealed class TestSingleton : ScriptableObjectSingleton<TestSingleton>
    {
        public int testValue = 42;
        public string payload;
    }
}
#endif
