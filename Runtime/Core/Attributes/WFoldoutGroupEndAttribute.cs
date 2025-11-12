namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Terminates automatic member inclusion for the active <see cref="WFoldoutGroupAttribute"/> instances.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Property,
        AllowMultiple = true,
        Inherited = true
    )]
    public sealed class WFoldoutGroupEndAttribute : Attribute
    {
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

        public IReadOnlyList<string> GroupNames { get; }
    }
}
