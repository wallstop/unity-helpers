// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes.Enums
{
    using System;

    /// <summary>
    /// Test enum representing different attribute types for attribute-related test scenarios.
    /// </summary>
    public enum TestAttributeType
    {
        /// <summary>
        /// Unknown or unset attribute type.
        /// </summary>
        [Obsolete("Use a specific TestAttributeType value instead of Unknown.")]
        Unknown = 0,

        /// <summary>
        /// Represents a string-type attribute.
        /// </summary>
        String = 1,

        /// <summary>
        /// Represents an integer-type attribute.
        /// </summary>
        Integer = 2,

        /// <summary>
        /// Represents a float-type attribute.
        /// </summary>
        Float = 3,

        /// <summary>
        /// Represents a boolean-type attribute.
        /// </summary>
        Boolean = 4,

        /// <summary>
        /// Represents an object-type attribute.
        /// </summary>
        Object = 5,
    }
}
