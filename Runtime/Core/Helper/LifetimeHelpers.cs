namespace UnityHelpers.Core.Helper
{
    using Object = UnityEngine.Object;

    public static class LifetimeHelpers
    {
        public static void Destroy<T>(this T source, float? afterTime = null) where T : Object
        {
            source.SmartDestroy(afterTime);
        }
    }
}
