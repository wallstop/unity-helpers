namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Draws enum and dropdown-backed fields as a toolbar of toggle buttons.
    /// Designed to mirror Odin Inspector's <c>EnumToggleButtons</c> experience while remaining editor-only and dependency free.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When applied to a field backed by a <c>[Flags]</c> enum, each discrete flag is rendered as an individual toggle button.
    /// This makes it significantly easier to reason about composite bitmasks in the inspector compared to Unity's built-in mask field.
    /// </para>
    /// <para>
    /// The attribute can also be combined with <see cref="IntDropdownAttribute"/>, <see cref="StringInListAttribute"/>, or
    /// <see cref="ValueDropdownAttribute"/> to render those curated lists as toggle buttons instead of a popup.
    /// In this mode only a single option is active at a time, but the visual presentation becomes much clearer for small choice sets.
    /// </para>
    /// <para>
    /// Because everything is driven through serialized fields and reflection, the attribute remains editor-safe and transparent to build players.
    /// No runtime components are introduced and the backing value on the component or asset stays unchanged.
    /// </para>
    /// <para>
    /// Large option sets automatically paginate based on the Unity Helpers project settings.
    /// Set <see cref="EnablePagination"/> to <see langword="false"/> or supply a custom <see cref="PageSize"/> to override that behaviour.
    /// </para>
    /// </remarks>
    /// <example>
    /// Flag-based enums:
    /// <code>
    /// [System.Flags]
    /// public enum MovementCapabilities
    /// {
    ///     None = 0,
    ///     Walk = 1 &lt;&lt; 0,
    ///     Jump = 1 &lt;&lt; 1,
    ///     Swim = 1 &lt;&lt; 2,
    ///     Fly = 1 &lt;&lt; 3,
    /// }
    ///
    /// [WEnumToggleButtons(ButtonsPerRow = 3)]
    /// public MovementCapabilities unlockedAbilities;
    /// </code>
    /// Dropdown-backed fields:
    /// <code>
    /// [WEnumToggleButtons(ShowSelectNone = false)]
    /// [StringInList("Low", "Medium", "High")]
    /// public string difficulty;
    ///
    /// [WEnumToggleButtons(ButtonsPerRow = 4)]
    /// [IntDropdown(15, 30, 45, 60)]
    /// public int targetFrameRate;
    ///
    /// [WEnumToggleButtons]
    /// [ValueDropdown(typeof(LocalizationCatalogue), nameof(LocalizationCatalogue.GetKnownKeys), typeof(string))]
    /// public string localizationKey;
    /// </code>
    /// </example>
    public sealed class WEnumToggleButtonsAttribute : PropertyAttribute
    {
        /// <summary>
        /// Initializes the attribute with automatic row sizing.
        /// </summary>
        public WEnumToggleButtonsAttribute()
            : this(0) { }

        /// <summary>
        /// Initializes the attribute with a fixed number of buttons per row.
        /// </summary>
        /// <param name="buttonsPerRow">
        /// Number of buttons to render per row. Values below one fall back to automatic sizing.
        /// Use zero to keep the automatic layout behaviour while still enabling the optional toolbar controls.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="buttonsPerRow"/> is negative.</exception>
        public WEnumToggleButtonsAttribute(int buttonsPerRow)
        {
            if (buttonsPerRow < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(buttonsPerRow),
                    "Value cannot be negative."
                );
            }

            ButtonsPerRow = buttonsPerRow;
        }

        /// <summary>
        /// Gets the desired number of buttons per row.
        /// A value of zero indicates that the drawer should determine a sensible layout automatically.
        /// </summary>
        public int ButtonsPerRow { get; }

        /// <summary>
        /// Gets or sets a value indicating whether a quick action button for selecting every flag should be displayed.
        /// Only meaningful for <c>[Flags]</c> enums.
        /// </summary>
        public bool ShowSelectAll { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether a quick action button for clearing every flag should be displayed.
        /// Only meaningful for <c>[Flags]</c> enums.
        /// </summary>
        public bool ShowSelectNone { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether pagination may be applied when the option count exceeds the configured threshold.
        /// Disable when all options should always be visible regardless of their count.
        /// </summary>
        public bool EnablePagination { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of options displayed per page before pagination occurs.
        /// Values less than or equal to zero defer to the project-wide default stored in <c>UnityHelpersSettings</c>.
        /// </summary>
        public int PageSize { get; set; } = 0;
    }
}
