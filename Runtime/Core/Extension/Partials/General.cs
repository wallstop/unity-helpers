// ReSharper disable once CheckNamespace
namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using UnityEngine;

    /// <summary>
    /// General-purpose helpers such as JSON formatting, input filtering, and scene membership checks.
    /// </summary>
    public static partial class UnityExtensions
    {
        /// <summary>
        /// Converts a Vector3 to a JSON-formatted string representation.
        /// </summary>
        public static string ToJsonString(this Vector3 vector)
        {
            return FormattableString.Invariant($"{{{vector.x}, {vector.y}, {vector.z}}}");
        }

        /// <summary>
        /// Converts a Vector2 to a JSON-formatted string representation.
        /// </summary>
        public static string ToJsonString(this Vector2 vector)
        {
            return FormattableString.Invariant($"{{{vector.x}, {vector.y}}}");
        }

        /// <summary>
        /// Determines if a Vector2 represents insignificant input (noise) below a threshold.
        /// </summary>
        public static bool IsNoise(this Vector2 inputVector, float threshold = 0.2f)
        {
            float limit = Mathf.Abs(threshold);
            return Mathf.Abs(inputVector.x) <= limit && Mathf.Abs(inputVector.y) <= limit;
        }

        /// <summary>
        /// Determines if a GameObject is in the DontDestroyOnLoad scene.
        /// </summary>
        public static bool IsDontDestroyOnLoad(this GameObject gameObjectToCheck)
        {
            if (gameObjectToCheck == null)
            {
                return false;
            }

            return string.Equals(
                gameObjectToCheck.scene.name,
                "DontDestroyOnLoad",
                StringComparison.Ordinal
            );
        }
    }
}
