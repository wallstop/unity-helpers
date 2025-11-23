namespace WallstopStudios.UnityHelpers.Tests.TestUtils
{
    using WallstopStudios.UnityHelpers.Core.Helper;

    internal static class UnityMainThreadDispatcherTestHelper
    {
        public static void DestroyDispatcherIfExists(bool immediate = false)
        {
            UnityMainThreadDispatcher.SetAutoCreationEnabled(false);
            UnityMainThreadDispatcher.DestroyExistingDispatcher(immediate);
        }

        public static void EnableAutoCreation()
        {
            UnityMainThreadDispatcher.SetAutoCreationEnabled(true);
        }
    }
}
