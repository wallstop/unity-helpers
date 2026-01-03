// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;

    /// <summary>
    /// When applied to a class that derives from <see cref="WallstopStudios.UnityHelpers.Utils.ScriptableObjectSingleton{T}"/>,
    /// prevents the <c>ScriptableObjectSingletonCreator</c> from automatically creating an asset for this type.
    /// Use this attribute on test-only singleton types or singletons that should only be created explicitly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ExcludeFromSingletonCreationAttribute : Attribute { }
}
