// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tags
{
    using System;

    /// <summary>
    /// Apply to components that must always be serialized into AttributeMetadataCache,
    /// even if they would otherwise be filtered as test-only.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class AlwaysIncludeInAttributeMetadataCacheAttribute : System.Attribute { }
}
