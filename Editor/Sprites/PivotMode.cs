// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Editor.Sprites
{
#if UNITY_EDITOR
    using System;

    /// <summary>
    /// Defines pivot point positions for extracted sprites.
    /// </summary>
    public enum PivotMode
    {
        /// <summary>
        /// Obsolete value. Use <see cref="Center"/> for default centered pivot.
        /// </summary>
        [Obsolete("Use Center for default centered pivot.")]
        None = 0,

        /// <summary>
        /// Center pivot point (0.5, 0.5).
        /// </summary>
        Center = 1,

        /// <summary>
        /// Bottom-left corner pivot point (0, 0).
        /// </summary>
        BottomLeft = 2,

        /// <summary>
        /// Top-left corner pivot point (0, 1).
        /// </summary>
        TopLeft = 3,

        /// <summary>
        /// Bottom-right corner pivot point (1, 0).
        /// </summary>
        BottomRight = 4,

        /// <summary>
        /// Top-right corner pivot point (1, 1).
        /// </summary>
        TopRight = 5,

        /// <summary>
        /// Left center pivot point (0, 0.5).
        /// </summary>
        LeftCenter = 6,

        /// <summary>
        /// Right center pivot point (1, 0.5).
        /// </summary>
        RightCenter = 7,

        /// <summary>
        /// Top center pivot point (0.5, 1).
        /// </summary>
        TopCenter = 8,

        /// <summary>
        /// Bottom center pivot point (0.5, 0).
        /// </summary>
        BottomCenter = 9,

        /// <summary>
        /// Custom pivot point using the specified custom pivot coordinates.
        /// </summary>
        Custom = 10,
    }
#endif
}
