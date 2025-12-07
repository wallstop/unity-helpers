namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    /// <summary>
    /// Helpers for creating and resolving directories in Unity projects.
    /// </summary>
    /// <remarks>
    /// Editor paths are expected to be under the <c>Assets/</c> folder. Provides conversions between absolute and Unity-relative paths.
    /// </remarks>
    public static class DirectoryHelper
    {
        /// <summary>
        /// Ensures a directory exists in the project. In the editor, the directory must be inside <c>Assets/</c> and is created via <c>AssetDatabase</c>.
        /// </summary>
        /// <param name="relativeDirectoryPath">Unity relative path (e.g., <c>Assets/MyFolder/Sub</c>).</param>
        /// <exception cref="ArgumentException">Thrown if attempting to create a directory outside of <c>Assets/</c> in the editor.</exception>
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

            // First, ensure the folder exists on disk. This prevents Unity's internal
            // "Moving file failed" modal dialog when CreateAsset tries to move a temp file
            // to a destination folder that doesn't exist.
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (!string.IsNullOrEmpty(projectRoot))
            {
                string absoluteDirectory = Path.Combine(projectRoot, relativeDirectoryPath);
                try
                {
                    if (!Directory.Exists(absoluteDirectory))
                    {
                        Directory.CreateDirectory(absoluteDirectory);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"DirectoryHelper: Failed to create directory on disk '{absoluteDirectory}': {ex.Message}"
                    );
                }
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

        /// <summary>
        /// Gets the directory of the calling source file (useful for locating package-relative content).
        /// </summary>
        public static string GetCallerScriptDirectory([CallerFilePath] string sourceFilePath = "")
        {
            return string.IsNullOrWhiteSpace(sourceFilePath)
                ? string.Empty
                : Path.GetDirectoryName(sourceFilePath);
        }

        /// <summary>
        /// Walks up the directory tree until a folder containing <c>package.json</c> is found.
        /// </summary>
        public static string FindPackageRootPath(string startDirectory)
        {
            return FindRootPath(
                startDirectory,
                path => File.Exists(Path.Combine(path, "package.json"))
            );
        }

        /// <summary>
        /// Walks up the directory tree until <paramref name="terminalCondition"/> returns true.
        /// </summary>
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

        /// <summary>
        /// Resolves an absolute path to a directory relative to the package root and returns a Unity-relative path.
        /// </summary>
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

        /// <summary>
        /// Converts an absolute OS path to a Unity-relative path (e.g., <c>Assets/...</c>), or empty string if outside the project.
        /// </summary>
        public static string AbsoluteToUnityRelativePath(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                return string.Empty;
            }

            absolutePath = absolutePath.SanitizePath();
            string projectRoot = Application.dataPath.SanitizePath();

            projectRoot = Path.GetDirectoryName(projectRoot)?.SanitizePath();
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

            return string.Empty;
        }
    }
}
