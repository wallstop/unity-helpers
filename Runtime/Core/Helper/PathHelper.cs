using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo(assemblyName: "WallstopStudios.UnityHelpers.Styles")]
[assembly: InternalsVisibleTo(assemblyName: "WallstopStudios.UnityHelpers.Editor")]

namespace WallstopStudios.UnityHelpers.Core.Helper
{
    internal static class PathHelper
    {
        public static string SanitizePath(this string path)
        {
            return path?.Replace('\\', '/');
        }
    }
}
