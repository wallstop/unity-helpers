namespace WallstopStudios.UnityHelpers.Tests.TestUtils
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;

    internal static class UnityMainThreadDispatcherTestHelper
    {
        public static void DestroyDispatcherIfExists(bool immediate = false)
        {
            UnityMainThreadDispatcher.SetAutoCreationEnabled(false);
            UnityMainThreadDispatcher dispatcher = GetExistingDispatcher();

            if (dispatcher == null)
            {
                return;
            }

            GameObject dispatcherObject = dispatcher.gameObject;
            if (dispatcherObject == null)
            {
                return;
            }

            if (immediate || !Application.isPlaying)
            {
                Object.DestroyImmediate(dispatcherObject);
            }
            else
            {
                Object.Destroy(dispatcherObject);
            }
        }

        private static UnityMainThreadDispatcher GetExistingDispatcher()
        {
            UnityMainThreadDispatcher dispatcher;
            if (UnityMainThreadDispatcher.TryGetInstance(out dispatcher) && dispatcher != null)
            {
                return dispatcher;
            }

            UnityMainThreadDispatcher[] allDispatchers =
                Resources.FindObjectsOfTypeAll<UnityMainThreadDispatcher>();
            if (allDispatchers == null || allDispatchers.Length == 0)
            {
                return null;
            }

            return allDispatchers[0];
        }

        public static void EnableAutoCreation()
        {
            UnityMainThreadDispatcher.SetAutoCreationEnabled(true);
        }
    }
}
