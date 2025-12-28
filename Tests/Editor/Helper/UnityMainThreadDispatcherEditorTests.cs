// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;

    public sealed class UnityMainThreadDispatcherEditorTests
    {
        private const HideFlags ExpectedHideFlags =
            HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.NotEditable;

        [Test]
        public void DispatcherUsesSceneFriendlyHideFlagsInEditMode()
        {
            UnityMainThreadDispatcher existing =
                Object.FindObjectOfType<UnityMainThreadDispatcher>();
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject); // UNH-SUPPRESS: Cleanup pre-existing dispatcher before test
            }

            UnityMainThreadDispatcher dispatcher = UnityMainThreadDispatcher.Instance;
            try
            {
                Assert.IsFalse(Application.isPlaying, "Edit mode test must run outside play mode.");
                Assert.AreEqual(
                    ExpectedHideFlags,
                    dispatcher.hideFlags,
                    "Dispatcher hide flags should remain hidden and unsaved in edit mode."
                );
            }
            finally
            {
                if (dispatcher != null)
                {
                    Object.DestroyImmediate(dispatcher.gameObject); // UNH-SUPPRESS: Test cleanup in finally block
                }
            }
        }
    }
}
