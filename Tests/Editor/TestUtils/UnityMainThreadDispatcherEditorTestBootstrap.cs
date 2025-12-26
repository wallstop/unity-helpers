namespace WallstopStudios.UnityHelpers.Tests.TestUtils
{
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using Object = UnityEngine.Object;

    [InitializeOnLoad]
    internal static class UnityMainThreadDispatcherEditorTestBootstrap
    {
        static UnityMainThreadDispatcherEditorTestBootstrap()
        {
            DisableDispatcherAutoCreation();
        }

        private static void DisableDispatcherAutoCreation()
        {
            UnityMainThreadDispatcher.SetAutoCreationEnabled(false);

            UnityMainThreadDispatcher dispatcher = null;
            UnityMainThreadDispatcher[] allDispatchers =
                Resources.FindObjectsOfTypeAll<UnityMainThreadDispatcher>();
            if (allDispatchers != null && allDispatchers.Length > 0)
            {
                dispatcher = allDispatchers[0];
            }

            if (dispatcher == null)
            {
                return;
            }

            GameObject dispatcherObject = dispatcher.gameObject;
            if (dispatcherObject == null)
            {
                return;
            }

            Object.DestroyImmediate(dispatcherObject); // UNH-SUPPRESS: Cleanup dispatcher in test teardown
        }
    }
}
