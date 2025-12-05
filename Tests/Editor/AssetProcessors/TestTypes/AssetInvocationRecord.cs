#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.AssetProcessors
{
    using System;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Record of an asset change handler invocation for testing purposes.
    /// </summary>
    internal readonly struct AssetInvocationRecord
    {
        public AssetInvocationRecord(Type assetType, AssetChangeFlags flags)
        {
            AssetType = assetType;
            Flags = flags;
        }

        public Type AssetType { get; }

        public AssetChangeFlags Flags { get; }
    }
}
#endif
