// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

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
