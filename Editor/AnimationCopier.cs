namespace WallstopStudios.UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEngine;
    using Core.Extension;

    public sealed class AnimationCopier : ScriptableWizard
    {
        private string _fullSourcePath;
        private string _fullDestinationPath;

        [SerializeField]
        private string _animationSourcePathRelative;

        [SerializeField]
        private string _animationDestinationPathRelative;

        private int _totalToCopy;
        private int _duplicates;
        private int _newAnimations;

        private void OnEnable()
        {
            if (
                string.IsNullOrWhiteSpace(_fullSourcePath)
                && string.IsNullOrWhiteSpace(_animationSourcePathRelative)
            )
            {
                _fullSourcePath = Path.GetFullPath(Path.Combine(Application.dataPath, "Sprites"));
                _animationSourcePathRelative = GetRelativeAssetPath(_fullSourcePath);
                if (_animationSourcePathRelative == null)
                {
                    this.LogError(
                        $"Default source path 'Assets/Sprites' is outside the project's Assets folder."
                    );
                    _fullSourcePath = Application.dataPath;
                    _animationSourcePathRelative = "Assets";
                }
            }
            else if (!string.IsNullOrWhiteSpace(_animationSourcePathRelative))
            {
                _fullSourcePath = Path.GetFullPath(
                    Path.Combine(
                        Application.dataPath.Replace("Assets", ""),
                        _animationSourcePathRelative
                    )
                );
            }

            if (
                string.IsNullOrWhiteSpace(_fullDestinationPath)
                && string.IsNullOrWhiteSpace(_animationDestinationPathRelative)
            )
            {
                _fullDestinationPath = Path.GetFullPath(
                    Path.Combine(Application.dataPath, "Animations")
                );
                _animationDestinationPathRelative = GetRelativeAssetPath(_fullDestinationPath);
                if (_animationDestinationPathRelative == null)
                {
                    this.LogError(
                        $"Default destination path 'Assets/Animations' is outside the project's Assets folder."
                    );
                    _fullDestinationPath = Application.dataPath;
                    _animationDestinationPathRelative = "Assets";
                }
            }
            else if (!string.IsNullOrWhiteSpace(_animationDestinationPathRelative))
            {
                _fullDestinationPath = Path.GetFullPath(
                    Path.Combine(
                        Application.dataPath.Replace("Assets", ""),
                        _animationDestinationPathRelative
                    )
                );
            }

            CalculateAnimationCounts();
        }

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/Animation Copier", priority = -2)]
        public static void CopyAnimations()
        {
            _ = DisplayWizard<AnimationCopier>("Animation Copier", "Copy");
        }

        protected override bool DrawWizardGUI()
        {
            EditorGUILayout.LabelField("Source Path (Read Only):", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(_animationSourcePathRelative ?? "Not Set");
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Set Animation Source Path"))
            {
                string sourcePath = EditorUtility.OpenFolderPanel(
                    "Select Animation Source Path",
                    Directory.Exists(_fullSourcePath) ? _fullSourcePath : Application.dataPath,
                    string.Empty
                );

                if (!string.IsNullOrWhiteSpace(sourcePath))
                {
                    string relativePath = GetRelativeAssetPath(sourcePath);
                    if (relativePath != null)
                    {
                        _fullSourcePath = sourcePath;
                        _animationSourcePathRelative = relativePath;
                        CalculateAnimationCounts();
                        Repaint();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog(
                            "Invalid Path",
                            "The selected source path must be inside the project's 'Assets' folder.",
                            "OK"
                        );
                    }
                }
            }

            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Destination Path (Read Only):", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(_animationDestinationPathRelative ?? "Not Set");
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Set Animation Destination Path"))
            {
                string destPath = EditorUtility.OpenFolderPanel(
                    "Select Animation Destination Path",
                    Directory.Exists(_fullDestinationPath)
                        ? _fullDestinationPath
                        : Application.dataPath,
                    string.Empty
                );

                if (!string.IsNullOrWhiteSpace(destPath))
                {
                    string relativePath = GetRelativeAssetPath(destPath);
                    if (relativePath != null)
                    {
                        _fullDestinationPath = destPath;
                        _animationDestinationPathRelative = relativePath;
                        CalculateAnimationCounts();
                        Repaint();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog(
                            "Invalid Path",
                            "The selected destination path must be inside the project's 'Assets' folder.",
                            "OK"
                        );
                    }
                }
            }

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Summary:", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.LabelField("Animations Found in Source:", _totalToCopy.ToString());
            EditorGUILayout.LabelField(
                "Existing at Destination (Overwrite):",
                _duplicates.ToString()
            );
            EditorGUILayout.LabelField("New Animations to Copy:", _newAnimations.ToString());
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Separator();

            isValid =
                !string.IsNullOrWhiteSpace(_animationSourcePathRelative)
                && !string.IsNullOrWhiteSpace(_animationDestinationPathRelative)
                && Directory.Exists(_fullSourcePath)
                && Directory.Exists(_fullDestinationPath);

            return true;
        }

        private void OnWizardCreate()
        {
            if (
                string.IsNullOrWhiteSpace(_fullSourcePath)
                || string.IsNullOrWhiteSpace(_fullDestinationPath)
                || string.IsNullOrWhiteSpace(_animationSourcePathRelative)
                || string.IsNullOrWhiteSpace(_animationDestinationPathRelative)
            )
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Source or Destination path is not set.",
                    "OK"
                );
                return;
            }

            if (!Directory.Exists(_fullSourcePath))
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    $"Source path does not exist:\n{_fullSourcePath}",
                    "OK"
                );
                return;
            }

            if (!Directory.Exists(Path.GetDirectoryName(_fullDestinationPath)))
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    $"Base destination path does not exist or is invalid:\n{_fullDestinationPath}",
                    "OK"
                );
                return;
            }

            CalculateAnimationCounts();
            if (_totalToCopy == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Animations",
                    $"No AnimationClips found in the source path:\n{_animationSourcePathRelative}",
                    "OK"
                );
                return;
            }

            string confirmationMessage =
                $"Found {_totalToCopy} animations in '{_animationSourcePathRelative}'.\n"
                + $"- {_newAnimations} will be newly copied.\n"
                + $"- {_duplicates} already exist and will be overwritten at '{_animationDestinationPathRelative}'.\n\n"
                + "The original animations in the source folder will be DELETED after successful copy.\n\n"
                + "Proceed?";

            if (
                !EditorUtility.DisplayDialog(
                    "Confirm Animation Copy & Delete",
                    confirmationMessage,
                    "Yes, Copy and Delete",
                    "Cancel"
                )
            )
            {
                this.Log($"Animation copy cancelled by user.");
                return;
            }

            int processed = 0;
            int copyErrors = 0;
            int deleteErrors = 0;

            string[] assetGuids = AssetDatabase.FindAssets(
                "t:AnimationClip",
                new[] { _animationSourcePathRelative }
            );

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string assetGuid in assetGuids)
                {
                    string sourceAssetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                    if (string.IsNullOrWhiteSpace(sourceAssetPath))
                    {
                        continue;
                    }

                    AnimationClip animationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                        sourceAssetPath
                    );
                    if (animationClip == null)
                    {
                        this.LogError(
                            $"[AnimationCopier] Invalid AnimationClip (null) found at path '{sourceAssetPath}', skipping."
                        );
                        continue;
                    }

                    string animationFileName = Path.GetFileName(sourceAssetPath);
                    string directoryName = Path.GetDirectoryName(sourceAssetPath);
                    if (string.IsNullOrWhiteSpace(directoryName))
                    {
                        this.LogWarn($"Failed to find directory name for '{sourceAssetPath}'.");
                        continue;
                    }
                    string sourceDirectoryRelative = directoryName.Replace(
                        Path.DirectorySeparatorChar,
                        '/'
                    );

                    string relativeSubPath;
                    if (
                        sourceDirectoryRelative.StartsWith(
                            _animationSourcePathRelative,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        int startIndex = _animationSourcePathRelative.Length;
                        if (
                            startIndex < sourceDirectoryRelative.Length
                            && sourceDirectoryRelative[startIndex] == '/'
                        )
                        {
                            startIndex++;
                        }
                        relativeSubPath = sourceDirectoryRelative.Substring(startIndex);
                    }
                    else
                    {
                        this.LogWarn(
                            $"[AnimationCopier] Asset '{sourceAssetPath}' is somehow outside expected source '{_animationSourcePathRelative}'. Skipping copy for this asset."
                        );
                        continue;
                    }

                    string outputDirectory = Path.Combine(_fullDestinationPath, relativeSubPath)
                        .Replace(Path.DirectorySeparatorChar, '/');

                    string destinationAssetPath = Path.Combine(
                            _animationDestinationPathRelative,
                            relativeSubPath,
                            animationFileName
                        )
                        .Replace(Path.DirectorySeparatorChar, '/');

                    if (!Directory.Exists(outputDirectory))
                    {
                        try
                        {
                            Directory.CreateDirectory(outputDirectory);
                        }
                        catch (Exception ex)
                        {
                            this.LogError(
                                $"[AnimationCopier] Failed to create directory '{outputDirectory}'. Error: {ex.Message}. Skipping animation '{animationFileName}'."
                            );
                            copyErrors++;
                            continue;
                        }
                    }

                    bool copySuccessful = AssetDatabase.CopyAsset(
                        sourceAssetPath,
                        destinationAssetPath
                    );
                    if (copySuccessful)
                    {
                        bool deleteSuccessful = AssetDatabase.DeleteAsset(sourceAssetPath);
                        if (!deleteSuccessful)
                        {
                            this.LogError(
                                $"[AnimationCopier] Failed to delete original asset at path '{sourceAssetPath}'."
                            );
                            deleteErrors++;
                        }
                        processed++;
                    }
                    else
                    {
                        this.LogError(
                            $"[AnimationCopier] Failed to copy animation from '{sourceAssetPath}' to '{destinationAssetPath}'."
                        );
                        copyErrors++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            this.Log(
                $"[AnimationCopier] Finished processing. Successfully copied and deleted: {processed}. Copy errors: {copyErrors}. Delete errors: {deleteErrors}."
            );

            EditorUtility.DisplayDialog(
                "Animation Copy Complete",
                $"Processed: {processed} animations.\n"
                    + $"Copy Errors: {copyErrors}\n"
                    + $"Delete Errors: {deleteErrors}\n\n"
                    + "See console for details.",
                "OK"
            );
        }

        private static string GetRelativeAssetPath(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return null;
            }

            fullPath = fullPath.Replace(Path.DirectorySeparatorChar, '/');
            int assetIndex = fullPath.IndexOf("Assets/", StringComparison.Ordinal);
            if (assetIndex >= 0)
            {
                return fullPath.Substring(assetIndex);
            }

            if (
                Path.GetFileName(fullPath).Equals("Assets", StringComparison.OrdinalIgnoreCase)
                && Directory.Exists(fullPath)
            )
            {
                return "Assets";
            }
            return null;
        }

        private void CalculateAnimationCounts()
        {
            _totalToCopy = 0;
            _duplicates = 0;
            _newAnimations = 0;

            if (
                string.IsNullOrWhiteSpace(_animationSourcePathRelative)
                || string.IsNullOrWhiteSpace(_animationDestinationPathRelative)
                || !Directory.Exists(_fullSourcePath)
            )
            {
                return;
            }

            string[] assetGuids = AssetDatabase.FindAssets(
                "t:AnimationClip",
                new[] { _animationSourcePathRelative }
            );

            _totalToCopy = assetGuids.Length;

            foreach (string assetGuid in assetGuids)
            {
                string sourceAssetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                if (string.IsNullOrWhiteSpace(sourceAssetPath))
                {
                    continue;
                }

                string animationFileName = Path.GetFileName(sourceAssetPath);
                string directoryName = Path.GetDirectoryName(sourceAssetPath);
                if (string.IsNullOrWhiteSpace(directoryName))
                {
                    this.LogError($"Failed to find directory of '{sourceAssetPath}'.");
                    continue;
                }
                string sourceDirectoryRelative = directoryName.Replace(
                    Path.DirectorySeparatorChar,
                    '/'
                );

                string relativeSubPath;
                if (
                    sourceDirectoryRelative.StartsWith(
                        _animationSourcePathRelative,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    int startIndex = _animationSourcePathRelative.Length;
                    if (
                        startIndex < sourceDirectoryRelative.Length
                        && sourceDirectoryRelative[startIndex] == '/'
                    )
                    {
                        startIndex++;
                    }
                    relativeSubPath = sourceDirectoryRelative.Substring(startIndex);
                }
                else
                {
                    this.LogWarn(
                        $"Asset '{sourceAssetPath}' found outside specified source '{_animationSourcePathRelative}'. Skipping count check for this asset."
                    );
                    continue;
                }

                string destinationAssetPath = Path.Combine(
                        _animationDestinationPathRelative,
                        relativeSubPath,
                        animationFileName
                    )
                    .Replace(Path.DirectorySeparatorChar, '/');

                if (AssetDatabase.LoadAssetAtPath<AnimationClip>(destinationAssetPath) != null)
                {
                    _duplicates++;
                }
                else
                {
                    _newAnimations++;
                }
            }
        }
    }
#endif
}
