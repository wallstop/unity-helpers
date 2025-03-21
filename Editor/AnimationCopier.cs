namespace UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System;
    using System.IO;
    using Core.Attributes;
    using Core.Extension;
    using UnityEditor;
    using UnityEngine;
    using Utils;

    public sealed class AnimationCopier : ScriptableWizard
    {
        private string _fullSourcePath;
        private string _fullDestinationPath;

        [DxReadOnly]
        public string animationSourcePath;

        [DxReadOnly]
        public string animationDestinationPath;

        private void OnEnable()
        {
            if (string.IsNullOrWhiteSpace(_fullSourcePath))
            {
                _fullSourcePath = $"{Application.dataPath}/Sprites";
                int assetIndex = _fullSourcePath.IndexOf("Assets", StringComparison.Ordinal);
                if (0 <= assetIndex)
                {
                    animationSourcePath = _fullSourcePath.Substring(assetIndex);
                }
            }

            if (string.IsNullOrWhiteSpace(_fullDestinationPath))
            {
                _fullDestinationPath = $"{Application.dataPath}/Animations";
                int assetIndex = _fullDestinationPath.IndexOf("Assets", StringComparison.Ordinal);
                if (0 <= assetIndex)
                {
                    animationDestinationPath = _fullDestinationPath.Substring(assetIndex);
                }
            }
        }

        [MenuItem("Tools/Unity Helpers/Animation Copier", priority = -2)]
        public static void CopyAnimations()
        {
            _ = DisplayWizard<AnimationCopier>("Animation Copier", "Copy");
        }

        protected override bool DrawWizardGUI()
        {
            bool returnValue = base.DrawWizardGUI();

            if (GUILayout.Button("Set Animation Source Path"))
            {
                string sourcePath = EditorUtility.OpenFolderPanel(
                    "Select Animation Source Path",
                    EditorUtilities.GetCurrentPathOfProjectWindow(),
                    string.Empty
                );
                int assetIndex = sourcePath?.IndexOf("Assets", StringComparison.Ordinal) ?? -1;
                if (assetIndex < 0)
                {
                    return false;
                }

                _fullSourcePath = animationSourcePath = sourcePath ?? string.Empty;
                animationSourcePath = animationSourcePath.Substring(assetIndex);
                return true;
            }

            if (GUILayout.Button("Set Animation Destination Path"))
            {
                string sourcePath = EditorUtility.OpenFolderPanel(
                    "Select Animation Destination Path",
                    EditorUtilities.GetCurrentPathOfProjectWindow(),
                    string.Empty
                );
                int assetIndex = sourcePath?.IndexOf("Assets", StringComparison.Ordinal) ?? -1;
                if (assetIndex < 0)
                {
                    return false;
                }

                _fullDestinationPath = animationDestinationPath = sourcePath ?? string.Empty;
                animationDestinationPath = animationDestinationPath.Substring(assetIndex);
                return true;
            }

            return returnValue;
        }

        private void OnWizardCreate()
        {
            if (string.IsNullOrEmpty(_fullSourcePath) || string.IsNullOrEmpty(_fullDestinationPath))
            {
                return;
            }

            if (
                string.IsNullOrEmpty(animationSourcePath)
                || string.IsNullOrEmpty(animationDestinationPath)
            )
            {
                return;
            }

            int processed = 0;
            foreach (
                string assetGuid in AssetDatabase.FindAssets(
                    "t:AnimationClip",
                    new[] { animationSourcePath }
                )
            )
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuid);

                AnimationClip animationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (animationClip == null)
                {
                    this.LogError(
                        "Invalid AnimationClip (null) found at path '{0}', skipping.",
                        path
                    );
                    continue;
                }

                string prefix = animationClip.name;
                string relativePath = path.Substring(animationSourcePath.Length);
                int prefixIndex = relativePath.LastIndexOf(
                    prefix,
                    StringComparison.OrdinalIgnoreCase
                );
                if (prefixIndex < 0)
                {
                    this.LogWarn(
                        "Unsupported animation at '{0}', expected to be prefixed by '{1}'.",
                        path,
                        prefix
                    );
                    continue;
                }

                string partialPath = relativePath.Substring(0, prefixIndex);
                string outputPath = _fullDestinationPath + partialPath;

                if (!Directory.Exists(outputPath))
                {
                    _ = Directory.CreateDirectory(outputPath);
                }

                string destination =
                    animationDestinationPath + partialPath + relativePath.Substring(prefixIndex);
                bool copySuccessful = AssetDatabase.CopyAsset(path, destination);
                if (copySuccessful)
                {
                    bool deleteSuccessful = AssetDatabase.DeleteAsset(path);
                    if (!deleteSuccessful)
                    {
                        this.LogError("Failed to delete asset at path '{0}'.", path);
                    }

                    ++processed;
                }
                else
                {
                    this.LogError(
                        "Failed to copy animation from '{0}' to '{1}'.",
                        path,
                        destination
                    );
                }
            }

            this.Log($"Processed {processed} AnimationClips.");
        }
    }
#endif
}
