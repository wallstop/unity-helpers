namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    /// <summary>
    /// Specifies where a button group should be rendered in the inspector.
    /// This allows per-group overrides independent of the global Unity Helpers settings.
    /// </summary>
    /// <remarks>
    /// When multiple buttons in the same group specify different placement values,
    /// the first declared button's placement is used and a warning is displayed in the inspector.
    /// Buttons without a group name ignore this setting.
    /// </remarks>
    public enum WButtonGroupPlacement
    {
        /// <summary>
        /// Use the global Unity Helpers setting for button placement.
        /// This is the default behavior.
        /// </summary>
        UseGlobalSetting = 0,

        /// <summary>
        /// Render this button group above the default inspector properties,
        /// regardless of the global setting.
        /// </summary>
        Top = 1,

        /// <summary>
        /// Render this button group below the default inspector properties,
        /// regardless of the global setting.
        /// </summary>
        Bottom = 2,
    }
}
