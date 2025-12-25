namespace WallstopStudios.UnityHelpers.Tests.TestUtils
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;

    internal static class UnityMainThreadDispatcherTestBootstrap
    {
        static UnityMainThreadDispatcherTestBootstrap()
        {
            DisableAutoCreation();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void DisableAutoCreation()
        {
            UnityMainThreadDispatcher.SetAutoCreationEnabled(false);
            UnityMainThreadDispatcherTestHelper.DestroyDispatcherIfExists(immediate: true);
        }
    }
}
