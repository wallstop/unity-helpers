namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    public static class DirectoryHelper
    {
        public static void EnsureDirectoryExists(string relativeDirectoryPath)
        {
            if (string.IsNullOrWhiteSpace(relativeDirectoryPath))
            {
                return;
            }

#if UNITY_EDITOR
            if (!relativeDirectoryPath.StartsWith("Assets/"))
            {
                if (relativeDirectoryPath.Equals("Assets", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                Debug.LogError(
                    $"Attempted to create directory outside of Assets: '{relativeDirectoryPath}'"
                );
                throw new ArgumentException(
                    "Cannot create directories outside the Assets folder using AssetDatabase.",
                    nameof(relativeDirectoryPath)
                );
            }

            if (AssetDatabase.IsValidFolder(relativeDirectoryPath))
            {
                return;
            }

            string parentPath = Path.GetDirectoryName(relativeDirectoryPath).SanitizePath();
            if (
                string.IsNullOrWhiteSpace(parentPath)
                || parentPath.Equals("Assets", StringComparison.OrdinalIgnoreCase)
            )
            {
                string folderNameToCreate = Path.GetFileName(relativeDirectoryPath);
                if (
                    !string.IsNullOrWhiteSpace(folderNameToCreate)
                    && !AssetDatabase.IsValidFolder(relativeDirectoryPath)
                )
                {
                    AssetDatabase.CreateFolder("Assets", folderNameToCreate);
                }
                return;
            }

            EnsureDirectoryExists(parentPath);
            string currentFolderName = Path.GetFileName(relativeDirectoryPath);
            if (
                !string.IsNullOrWhiteSpace(currentFolderName)
                && !AssetDatabase.IsValidFolder(relativeDirectoryPath)
            )
            {
                AssetDatabase.CreateFolder(parentPath, currentFolderName);
                Debug.Log($"Created folder: {relativeDirectoryPath}");
            }
#else
            Directory.CreateDirectory(relativeDirectoryPath);
#endif
        }

        public static string GetCallerScriptDirectory([CallerFilePath] string sourceFilePath = "")
        {
            return string.IsNullOrWhiteSpace(sourceFilePath)
                ? string.Empty
                : Path.GetDirectoryName(sourceFilePath);
        }

        public static string FindPackageRootPath(string startDirectory)
        {
            return FindRootPath(
                startDirectory,
                path => File.Exists(Path.Combine(path, "package.json"))
            );
        }

        public static string FindRootPath(
            string startDirectory,
            Func<string, bool> terminalCondition
        )
        {
            string currentPath = startDirectory;
            while (!string.IsNullOrWhiteSpace(currentPath))
            {
                try
                {
                    if (terminalCondition(currentPath))
                    {
                        DirectoryInfo directoryInfo = new(currentPath);
                        if (!directoryInfo.Exists)
                        {
                            return currentPath;
                        }

                        return directoryInfo.FullName;
                    }
                }
                catch
                {
                    return currentPath;
                }

                try
                {
                    string parentPath = Path.GetDirectoryName(currentPath);
                    if (string.Equals(parentPath, currentPath, StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    currentPath = parentPath;
                }
                catch
                {
                    return currentPath;
                }
            }

            return string.Empty;
        }

        public static string FindAbsolutePathToDirectory(string directory)
        {
            string scriptDirectory = GetCallerScriptDirectory();
            if (string.IsNullOrEmpty(scriptDirectory))
            {
                return string.Empty;
            }

            string packageRootAbsolute = FindPackageRootPath(scriptDirectory);
            if (string.IsNullOrEmpty(packageRootAbsolute))
            {
                return string.Empty;
            }

            string targetPathAbsolute = Path.Combine(
                packageRootAbsolute,
                directory.Replace('/', Path.DirectorySeparatorChar)
            );

            return AbsoluteToUnityRelativePath(targetPathAbsolute);
        }

        public static string AbsoluteToUnityRelativePath(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                return string.Empty;
            }

            absolutePath = absolutePath.Replace('\\', '/');
            string projectRoot = Application.dataPath.Replace('\\', '/');

            projectRoot = Path.GetDirectoryName(projectRoot)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                return string.Empty;
            }

            if (absolutePath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                // +1 to remove the leading slash only if projectRoot doesn't end with one
                int startIndex = projectRoot.EndsWith("/", StringComparison.OrdinalIgnoreCase)
                    ? projectRoot.Length
                    : projectRoot.Length + 1;
                return absolutePath.Length > startIndex ? absolutePath[startIndex..] : string.Empty;
            }
            if (absolutePath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                int startIndex = projectRoot.EndsWith("/", StringComparison.OrdinalIgnoreCase)
                    ? projectRoot.Length
                    : projectRoot.Length + 1;
                if (startIndex < absolutePath.Length)
                {
                    return "Assets/" + absolutePath[startIndex..];
                }

                return "Assets";
            }

            return string.Empty;
        }
    }
}
