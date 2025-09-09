namespace WallstopStudios.UnityHelpers.Editor.Utils
{
#if UNITY_EDITOR
    using System;
    using System.Reflection;
    using UnityEditor;

    public static class EditorUtilities
    {
        public static string GetCurrentPathOfProjectWindow()
        {
            Type projectWindowUtilType = typeof(ProjectWindowUtil);
            MethodInfo getActiveFolderPath = projectWindowUtilType.GetMethod(
                "GetActiveFolderPath",
                BindingFlags.Static | BindingFlags.NonPublic
            );
            object obj = getActiveFolderPath?.Invoke(null, Array.Empty<object>());
            return obj?.ToString() ?? string.Empty;
        }
    }
#endif
}
