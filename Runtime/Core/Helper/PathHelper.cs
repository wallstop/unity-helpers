namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;

    public static class PathHelper
    {
        public static string Sanitize(string path)
        {
            return SanitizePath(path);
        }

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
