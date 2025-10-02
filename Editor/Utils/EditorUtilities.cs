namespace WallstopStudios.UnityHelpers.Editor.Utils
{
#if UNITY_EDITOR
    using System;
    using System.Reflection;
    using Core.Helper;
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
            object obj =
                getActiveFolderPath != null
                    ? ReflectionHelpers.InvokeStaticMethod(getActiveFolderPath)
                    : null;
            return obj?.ToString() ?? string.Empty;
        }
    }
#endif
}
