// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.AssetProcessors
{
    using System;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test handler that intentionally throws an exception to verify error isolation.
    /// Used to test that exceptions in one handler do not prevent other handlers from executing.
    /// </summary>
    internal static class TestExceptionThrowingHandler
    {
        private static int _invocationCount;
        private static bool _shouldThrow = true;

        public static int InvocationCount => _invocationCount;

        public static bool ShouldThrow
        {
            get => _shouldThrow;
            set => _shouldThrow = value;
        }

        public static void Clear()
        {
            _invocationCount = 0;
            _shouldThrow = true;
        }

        [DetectAssetChanged(typeof(TestDetectableAsset))]
        private static void OnAssetChangedWithException(AssetChangeContext context)
        {
            _invocationCount++;
            if (_shouldThrow)
            {
                throw new InvalidOperationException(
                    "TestExceptionThrowingHandler intentionally threw an exception for testing."
                );
            }
        }
    }
}
#endif
