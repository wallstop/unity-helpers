namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using UnityEngine;

    internal static class SingletonAutoLoader
    {
        private static int _executed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void AutoLoadBeforeSplash()
        {
            ExecuteDescriptors(SingletonAutoLoadManifest.Entries);
        }

        private static void ExecuteDescriptors(
            IReadOnlyList<SingletonAutoLoadDescriptor> descriptors,
            bool enforceSingleExecution = true
        )
        {
            if (descriptors == null || descriptors.Count == 0)
            {
                return;
            }

            if (enforceSingleExecution)
            {
                if (Interlocked.Exchange(ref _executed, 1) == 1)
                {
                    return;
                }
            }
            else
            {
                _executed = 0;
            }

            for (int i = 0; i < descriptors.Count; i++)
            {
                SingletonAutoLoadDescriptor descriptor = descriptors[i];
                try
                {
                    descriptor.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError(
                        $"SingletonAutoLoader: Failed to auto-load {descriptor.TypeName}. {ex}"
                    );
                }
            }
        }

#if UNITY_INCLUDE_TESTS
        internal static void ExecuteForTests(params SingletonAutoLoadDescriptor[] descriptors)
        {
            ExecuteDescriptors(descriptors, enforceSingleExecution: false);
        }
#endif
    }
}
