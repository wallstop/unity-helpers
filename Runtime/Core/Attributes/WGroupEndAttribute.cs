namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Terminates automatic member inclusion for the active <see cref="WGroupAttribute"/> instances, letting you resume the normal inspector flow.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Important:</b> Place <see cref="WGroupEndAttribute"/> on the <b>last field you want included</b> in the group.
    /// The field with this attribute IS included in the group, and then the group closes for subsequent fields.
    /// When multiple groups are stacked on the same field, you can provide explicit names to close only the desired scopes.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [WGroup(\"Stats\", autoIncludeCount: WGroupAttribute.InfiniteAutoInclude)]
    /// public int health;
    ///
    /// public int stamina;
    ///
    /// [WGroupEnd(\"Stats\")]
    /// public float luck;        // Included in \"Stats\" group, then group closes
    ///
    /// public int gold;          // NOT in \"Stats\" group - comes after WGroupEnd
    /// </code>
    /// </example>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Property,
        AllowMultiple = true,
        Inherited = true
    )]
    public sealed class WGroupEndAttribute : Attribute
    {
        /// <summary>
        /// Creates a new end marker optionally targeting one or more specific groups.
        /// </summary>
        /// <param name="groupNames">
        /// Explicit group keys to close. When omitted, the attribute ends every currently open group that originated on the same member.
        /// </param>
        public WGroupEndAttribute(params string[] groupNames)
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
        /// Gets the normalized group names that should stop auto inclusion. An empty collection instructs the drawer to close all active groups.
        /// </summary>
        public IReadOnlyList<string> GroupNames { get; }
    }
}
