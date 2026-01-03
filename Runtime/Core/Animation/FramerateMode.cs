// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Animation
{
    using System;

    /// <summary>
    /// Specifies how the framerate of an animation is determined.
    /// </summary>
    public enum FramerateMode
    {
        /// <summary>
        /// Invalid/unset state. Use <see cref="Constant"/> or <see cref="Curve"/> instead.
        /// </summary>
        [Obsolete("Use Constant or Curve instead of None.")]
        None = 0,

        /// <summary>
        /// The animation uses a single constant frames-per-second value for all frames.
        /// </summary>
        Constant = 1,

        /// <summary>
        /// The animation uses an AnimationCurve to define variable frames-per-second
        /// across the animation's normalized progress (0.0 = first frame, 1.0 = last frame).
        /// </summary>
        Curve = 2,
    }
}
