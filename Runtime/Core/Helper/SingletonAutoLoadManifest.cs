namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System.Collections.Generic;

    internal static partial class SingletonAutoLoadManifest
    {
        internal static readonly SingletonAutoLoadDescriptor[] Entries;

        static SingletonAutoLoadManifest()
        {
            List<SingletonAutoLoadDescriptor> buffer = new();
            Populate(buffer);
            Entries = buffer.ToArray();
        }

        static partial void Populate(List<SingletonAutoLoadDescriptor> target);
    }
}
