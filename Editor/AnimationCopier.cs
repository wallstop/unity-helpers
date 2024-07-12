namespace UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System;
    using System.IO;
    using Core.Attributes;
    using Core.Extension;
    using UnityEditor;
    using UnityEngine;
    using Utilities;

    public sealed class AnimationCopier : ScriptableWizard
    {
        private string _fullSourcePath;
        private string _fullDestinationPath;

        [ReadOnly]
        public string animationSourcePath;

        [ReadOnly]
        public string animationDestinationPath;

        [MenuItem("Tools/Unity Helpers/Animation Copier")]
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
                    "Select Animation Source Path", EditorUtilities.GetCurrentPathOfProjectWindow(), string.Empty);
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
                    "Select Animation Destination Path", EditorUtilities.GetCurrentPathOfProjectWindow(), string.Empty);
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

            if (string.IsNullOrEmpty(animationSourcePath) || string.IsNullOrEmpty(animationDestinationPath))
            {
                return;
            }

            foreach (string assetGuid in AssetDatabase.FindAssets("t:AnimationClip", new[] { animationSourcePath }))
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuid);

                AnimationClip animationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (animationClip == null)
                {
                    this.LogError("Invalid AnimationClip (null) found at path '{0}', skipping.", path);
                    continue;
                }

                string prefix = animationClip.name;
                string relativePath = path.Substring(animationSourcePath.Length);
                int animIndex = relativePath.LastIndexOf(prefix, StringComparison.OrdinalIgnoreCase);
                if (animIndex < 0)
                {
                    this.LogWarn("Unsupported animation at '{0}', expected to be prefixed by '{1}'.", path, prefix);
                    continue;
                }

                string partialPath = relativePath.Substring(0, animIndex);
                string outputPath = _fullDestinationPath + partialPath;

                if (!Directory.Exists(outputPath))
                {
                    _ = Directory.CreateDirectory(outputPath);
                }

                string destination = animationDestinationPath + partialPath + relativePath.Substring(animIndex);
                bool copySuccessful = AssetDatabase.CopyAsset(path, destination);
                if (copySuccessful)
                {
                    bool deleteSuccessful = AssetDatabase.DeleteAsset(path);
                    if (!deleteSuccessful)
                    {
                        this.LogError("Failed to delete asset at path '{0}'.", path);
                    }
                }
                else
                {
                    this.LogError("Failed to copy animation from '{0}' to '{1}'.", path, destination);
                }
            }
        }
    }
#endif
}