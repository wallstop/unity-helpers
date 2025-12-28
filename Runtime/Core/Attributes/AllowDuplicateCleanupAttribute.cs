// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;

    /// <summary>
    /// When applied to a <see cref="WallstopStudios.UnityHelpers.Utils.ScriptableObjectSingleton{T}"/>
    /// subclass, enables automatic cleanup of duplicate singleton assets during editor initialization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Duplicate singleton assets can occur when:
    /// <list type="bullet">
    ///   <item>A <see cref="ScriptableSingletonPathAttribute"/> path is changed</item>
    ///   <item>Assets are manually copied or the package is re-imported</item>
    ///   <item>Migration from an older path structure creates new assets without removing old ones</item>
    /// </list>
    /// </para>
    /// <para>
    /// When this attribute is present, the <c>ScriptableObjectSingletonCreator</c> will:
    /// <list type="number">
    ///   <item>Identify the canonical asset path based on the current <see cref="ScriptableSingletonPathAttribute"/></item>
    ///   <item>Find all other assets of the same type under Assets/Resources</item>
    ///   <item>Compare duplicate assets to the canonical asset using Unity's serialization</item>
    ///   <item>Delete duplicates that have identical serialized content</item>
    ///   <item>Attempt to delete empty parent folders left behind</item>
    /// </list>
    /// </para>
    /// <para>
    /// Without this attribute, duplicates will only generate a warning in the console.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [ScriptableSingletonPath("Wallstop Studios")]
    /// [AllowDuplicateCleanup]
    /// public sealed class MySettings : ScriptableObjectSingleton&lt;MySettings&gt;
    /// {
    ///     // Settings fields...
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class AllowDuplicateCleanupAttribute : Attribute { }
}
