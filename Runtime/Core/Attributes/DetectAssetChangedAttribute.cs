namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [Flags]
    public enum AssetChangeFlags
    {
        None = 0,
        Created = 1 << 0,
        Deleted = 1 << 1,
    }

    /// <summary>
    /// Annotates an instance method that should run whenever assets of the specified type change.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class DetectAssetChangedAttribute : Attribute
    {
        public DetectAssetChangedAttribute(Type assetType)
            : this(assetType, AssetChangeFlags.Created | AssetChangeFlags.Deleted) { }

        public DetectAssetChangedAttribute(Type assetType, AssetChangeFlags flags)
        {
            if (assetType == null)
            {
                throw new ArgumentNullException(nameof(assetType));
            }

            if (!typeof(Object).IsAssignableFrom(assetType))
            {
                throw new ArgumentException(
                    $"{assetType.FullName} does not derive from {nameof(Object)}",
                    nameof(assetType)
                );
            }

            AssetType = assetType;
            Flags =
                flags == AssetChangeFlags.None
                    ? AssetChangeFlags.Created | AssetChangeFlags.Deleted
                    : flags;
        }

        public Type AssetType { get; }

        public AssetChangeFlags Flags { get; }
    }
}
