namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;

    /// <summary>
    /// Utilities for normalizing and sanitizing file paths for Unity projects.
    /// </summary>
    /// <remarks>
    /// Uses forward slashes to ensure consistency across platforms and AssetDatabase APIs.
    /// </remarks>
    public static class PathHelper
    {
        /// <summary>
        /// Normalizes directory separators to forward slashes.
        /// </summary>
        /// <param name="path">Any file system path or Unity relative path.</param>
        /// <returns>The input path with all backslashes replaced by forward slashes; null if input is null.</returns>
        public static string Sanitize(string path)
        {
            return SanitizePath(path);
        }

        /// <summary>
        /// Normalizes directory separators to forward slashes (internal helper).
        /// </summary>
        internal static string SanitizePath(this string path)
        {
            if (path == null)
            {
                return null;
            }

            // Avoid unnecessary allocation if path already has forward slashes only
            if (path.IndexOf('\\', StringComparison.Ordinal) < 0)
            {
                return path;
            }

            return path.Replace('\\', '/');
        }
    }
}
