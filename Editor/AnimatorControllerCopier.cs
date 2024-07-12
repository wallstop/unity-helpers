﻿namespace UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System;
    using System.IO;
    using Core.Attributes;
    using Core.Extension;
    using UnityEditor;
    using UnityEngine;
    using Utils;

    public sealed class AnimatorControllerCopier : ScriptableWizard
    {
        private string _fullSourcePath;
        private string _fullDestinationPath;

        [ReadOnly]
        public string controllerSourcePath;

        [ReadOnly]
        public string controllerDestinationpath;

        [MenuItem("Tools/Unity Helpers/Animator Controller Copier")]
        public static void CopyAnimations()
        {
            _ = DisplayWizard<AnimatorControllerCopier>("Animator Controller Copier", "Copy");
        }

        protected override bool DrawWizardGUI()
        {
            bool returnValue = base.DrawWizardGUI();

            if (GUILayout.Button("Set Animator Controller Source Path"))
            {
                string sourcePath = EditorUtility.OpenFolderPanel(
                    "Select Animator Controller Source Path", EditorUtilities.GetCurrentPathOfProjectWindow(),
                    string.Empty);
                int assetIndex = sourcePath?.IndexOf("Assets", StringComparison.Ordinal) ?? -1;
                if (assetIndex < 0)
                {
                    return false;
                }

                _fullSourcePath = controllerSourcePath = sourcePath ?? string.Empty;
                controllerSourcePath = controllerSourcePath.Substring(assetIndex);
                return true;
            }

            if (GUILayout.Button("Set Animator Controller Destination Path"))
            {
                string sourcePath = EditorUtility.OpenFolderPanel(
                    "Select Animator Controller Destination Path", EditorUtilities.GetCurrentPathOfProjectWindow(),
                    string.Empty);
                int assetIndex = sourcePath?.IndexOf("Assets", StringComparison.Ordinal) ?? -1;
                if (assetIndex < 0)
                {
                    return false;
                }

                _fullDestinationPath = controllerDestinationpath = sourcePath ?? string.Empty;
                controllerDestinationpath = controllerDestinationpath.Substring(assetIndex);
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

            if (string.IsNullOrEmpty(controllerSourcePath) || string.IsNullOrEmpty(controllerDestinationpath))
            {
                return;
            }

            int processed = 0;
            foreach (string assetGuid in AssetDatabase.FindAssets(
                         "t:AnimatorController", new[] { controllerSourcePath }))
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuid);

                RuntimeAnimatorController animatorController =
                    AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
                if (animatorController == null)
                {
                    this.LogError("Invalid Animator Controller (null) found at path '{0}', skipping.", path);
                    continue;
                }

                string prefix = animatorController.name;
                string relativePath = path.Substring(controllerSourcePath.Length);
                int prefixIndex = relativePath.LastIndexOf(prefix, StringComparison.OrdinalIgnoreCase);
                if (prefixIndex < 0)
                {
                    this.LogWarn(
                        "Unsupported AnimatorController at '{0}', expected to be prefixed by '{1}'.", path, prefix);
                    continue;
                }

                string partialPath = relativePath.Substring(0, prefixIndex);
                string outputPath = _fullDestinationPath + partialPath;

                if (!Directory.Exists(outputPath))
                {
                    _ = Directory.CreateDirectory(outputPath);
                }

                string destination = controllerDestinationpath + partialPath + relativePath.Substring(prefixIndex);
                bool copySuccessful = AssetDatabase.CopyAsset(path, destination);
                if (copySuccessful)
                {
                    bool deleteSuccessful = AssetDatabase.DeleteAsset(path);
                    if (!deleteSuccessful)
                    {
                        this.LogError("Failed to delete Animator Controller asset at path '{0}'.", path);
                    }

                    ++processed;
                }
                else
                {
                    this.LogError("Failed to copy Animator Controller from '{0}' to '{1}'.", path, destination);
                }
            }

            this.Log($"Processed {processed} Animator Controllers.");
        }
    }
#endif
}