namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using Object = UnityEngine.Object;

    /// <summary>
    /// Convenience helpers for Unity object lifetimes.
    /// </summary>
    public static class LifetimeHelpers
    {
        /// <summary>
        /// Destroys a Unity object using SmartDestroy (DestroyImmediate in edit mode, Destroy in play mode).
        /// </summary>
        /// <param name="afterTime">Optional delay in seconds for runtime Destroy.</param>
        public static void Destroy<T>(this T source, float? afterTime = null)
            where T : Object
        {
            source.SmartDestroy(afterTime);
        }
    }
}
