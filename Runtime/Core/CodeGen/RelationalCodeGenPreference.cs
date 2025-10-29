namespace WallstopStudios.UnityHelpers.Core.CodeGen
{
    /// <summary>
    /// Represents the code generation preference for relational component attributes.
    /// </summary>
    public enum RelationalCodeGenPreference : byte
    {
        /// <summary>
        /// Inherit the preference from the project-level default.
        /// </summary>
        Inherit = 0,

        /// <summary>
        /// Disable code generation for the attributed field.
        /// </summary>
        Disabled = 1,

        /// <summary>
        /// Enable code generation for the attributed field when supported.
        /// </summary>
        Enabled = 2,
    }

    /// <summary>
    /// Identifies the relational attribute category.
    /// </summary>
    public enum RelationalAttributeKind : byte
    {
        Sibling = 0,
        Parent = 1,
        Child = 2,
    }
}
