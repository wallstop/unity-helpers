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
                Object.DestroyImmediate(existing.gameObject);
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
                    Object.DestroyImmediate(dispatcher.gameObject);
                }
            }
        }
    }
}
