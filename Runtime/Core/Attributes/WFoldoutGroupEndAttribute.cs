namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Terminates automatic member inclusion for the active <see cref="WFoldoutGroupAttribute"/> instances, letting you control exactly which fields stay inside the foldout.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The attribute can optionally keep the decorated member inside the foldout by toggling <see cref="IncludeElement"/>.
    /// This is useful when you want the final field to remain in the section while still preventing further members from being auto included.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [WFoldoutGroup(\"Advanced\", autoIncludeCount: WFoldoutGroupAttribute.InfiniteAutoInclude)]
    /// public float friction;
    ///
    /// [WFoldoutGroupEnd(IncludeElement = true)]
    /// public AnimationCurve easing; // remains inside the foldout but stops auto inclusion
    /// </code>
    /// </example>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Property,
        AllowMultiple = true,
        Inherited = true
    )]
    public sealed class WFoldoutGroupEndAttribute : Attribute
    {
        /// <summary>
        /// Creates a new end marker optionally scoped to specific foldout names.
        /// </summary>
        /// <param name="groupNames">
        /// Explicit group keys to close. When omitted, every open foldout group that originated on the same member is terminated.
        /// </param>
        public WFoldoutGroupEndAttribute(params string[] groupNames)
        {
            if (groupNames == null || groupNames.Length == 0)
            {
                GroupNames = Array.Empty<string>();
                return;
            }

            string[] normalized = new string[groupNames.Length];
            for (int index = 0; index < groupNames.Length; index++)
            {
                string name = groupNames[index];
                normalized[index] = string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
            }

            GroupNames = normalized;
        }

        /// <summary>
        /// Whether to include the marked element in the foldout group.
        /// When false (default), the marked element is excluded from the group.
        /// </summary>
        public bool IncludeElement { get; set; }

        /// <summary>
        /// Gets the normalized group names that should stop auto inclusion. An empty collection instructs the drawer to close all active foldouts.
        /// </summary>
        public IReadOnlyList<string> GroupNames { get; }
    }
}
