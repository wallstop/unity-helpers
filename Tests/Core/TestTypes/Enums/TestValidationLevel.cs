// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes.Enums
{
    using System;

    /// <summary>
    /// Test enum representing different validation severity levels for validation-related tests.
    /// </summary>
    public enum TestValidationLevel
    {
        /// <summary>
        /// Unknown or unset validation level.
        /// </summary>
        [Obsolete("Use a specific TestValidationLevel value instead of Unknown.")]
        Unknown = 0,

        /// <summary>
        /// Informational level validation message.
        /// </summary>
        Info = 1,

        /// <summary>
        /// Warning level validation message.
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Error level validation message.
        /// </summary>
        Error = 3,

        /// <summary>
        /// Critical level validation message.
        /// </summary>
        Critical = 4,
    }
}
