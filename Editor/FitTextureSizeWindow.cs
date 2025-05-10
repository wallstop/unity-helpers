namespace WallstopStudios.UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using Core.Extension;
    using CustomEditors;
    using Object = UnityEngine.Object;

    public enum FitMode
    {
        GrowAndShrink = 0,
        GrowOnly = 1,
        ShrinkOnly = 2,
    }

    public sealed class FitTextureSizeWindow : EditorWindow
    {
        private FitMode _fitMode = FitMode.GrowAndShrink;

        [SerializeField]
        private List<Object> _textureSourcePaths = new();
        private Vector2 _scrollPosition = Vector2.zero;
        private SerializedObject _serializedObject;
        private SerializedProperty _textureSourcePathsProperty;
        private int _potentialChangeCount = -1;

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/Fit Texture Size", priority = -1)]
        public static void ShowWindow()
        {
            GetWindow<FitTextureSizeWindow>("Fit Texture Size");
        }

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _textureSourcePathsProperty = _serializedObject.FindProperty(
                nameof(_textureSourcePaths)
            );

            if (_textureSourcePaths is { Count: > 0 })
            {
                return;
            }

            _textureSourcePaths = new List<Object>();
            Object defaultFolder = AssetDatabase.LoadAssetAtPath<Object>("Assets/Sprites");
            if (defaultFolder == null)
            {
                return;
            }

            _textureSourcePaths.Add(defaultFolder);
            _serializedObject.Update();
        }

        private void OnGUI()
        {
            _serializedObject.Update();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            _fitMode = (FitMode)EditorGUILayout.EnumPopup("Fit Mode", _fitMode);

            EditorGUILayout.Space();
            PersistentDirectoryGUI.PathSelectorObjectArray(
                _textureSourcePathsProperty,
                nameof(FitTextureSizeWindow)
            );
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Calculate Potential Changes"))
            {
                _potentialChangeCount = CalculateTextureChanges(applyChanges: false);
                string message =
                    _potentialChangeCount >= 0
                        ? $"Calculation complete. {_potentialChangeCount} textures would be modified."
                        : "Calculation failed.";
                this.Log($"{message}");
            }

            if (_potentialChangeCount >= 0)
            {
                EditorGUILayout.HelpBox(
                    $"{_potentialChangeCount} textures would be modified with the current settings.",
                    MessageType.Info
                );
            }

            if (GUILayout.Button("Run Fit Texture Size"))
            {
                int actualChanges = CalculateTextureChanges(applyChanges: true);
                _potentialChangeCount = -1;
                string message =
                    actualChanges >= 0
                        ? $"Operation complete. {actualChanges} textures were modified."
                        : "Operation failed.";
                this.Log($"{message}");
            }

            EditorGUILayout.EndScrollView();
            _serializedObject.ApplyModifiedProperties();
        }

        private List<Texture2D> CollectTextures()
        {
            _textureSourcePaths ??= new List<Object>();
            HashSet<string> uniqueAssetPaths = new();
            List<string> searchPaths = new();

            foreach (Object sourceObject in _textureSourcePaths)
            {
                if (sourceObject == null)
                {
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath(sourceObject);
                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    continue;
                }

                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    _ = uniqueAssetPaths.Add(assetPath);
                }
                else
                {
                    this.LogWarn($"Skipping non-folder object: '{assetPath}'");
                }
            }

            if (!uniqueAssetPaths.Any())
            {
                if (_textureSourcePaths.Any(o => o != null))
                {
                    this.LogWarn($"No valid source folders found in the list.");
                }
                else
                {
                    this.Log($"No source folders specified. Searching entire 'Assets' folder.");
                    searchPaths.Add("Assets");
                }
            }
            else
            {
                searchPaths.AddRange(uniqueAssetPaths);
            }

            List<Texture2D> foundTextures = new();
            if (!searchPaths.Any())
            {
                return foundTextures.Distinct().OrderBy(texture => texture.name).ToList();
            }

            string[] guids = AssetDatabase.FindAssets("t:texture2D", searchPaths.ToArray());
            foreach (string assetGuid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuid);
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                {
                    foundTextures.Add(texture);
                }
            }

            return foundTextures.Distinct().OrderBy(texture => texture.name).ToList();
        }

        private int CalculateTextureChanges(bool applyChanges)
        {
            List<Texture2D> texturesToProcess = CollectTextures();

            if (texturesToProcess.Count <= 0)
            {
                this.Log($"No textures found in the specified paths.");
                return 0;
            }

            int changedCount = 0;
            List<TextureImporter> updatedImporters = new();
            if (applyChanges)
            {
                AssetDatabase.StartAssetEditing();
            }
            try
            {
                float totalAssets = texturesToProcess.Count;
                for (int i = 0; i < texturesToProcess.Count; i++)
                {
                    Texture2D texture = texturesToProcess[i];
                    string assetPath = AssetDatabase.GetAssetPath(texture);
                    float progress = (i + 1) / totalAssets;
                    string progressBarTitle = applyChanges
                        ? "Fitting Texture Size"
                        : "Calculating Changes";
                    bool cancel = EditorUtility.DisplayCancelableProgressBar(
                        progressBarTitle,
                        $"Checking: {Path.GetFileName(assetPath)} ({i + 1}/{texturesToProcess.Count})",
                        progress
                    );

                    if (cancel)
                    {
                        this.LogWarn($"Operation cancelled by user.");
                        return -1;
                    }

                    if (string.IsNullOrWhiteSpace(assetPath))
                    {
                        continue;
                    }

                    TextureImporter textureImporter =
                        AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (textureImporter == null)
                    {
                        continue;
                    }

                    textureImporter.GetSourceTextureWidthAndHeight(out int width, out int height);

                    float size = Mathf.Max(width, height);
                    int currentTextureSize = textureImporter.maxTextureSize;
                    int targetTextureSize = currentTextureSize;
                    bool needsChange = false;

                    if (_fitMode is FitMode.GrowAndShrink or FitMode.GrowOnly)
                    {
                        int tempSize = targetTextureSize;
                        while (tempSize < size)
                        {
                            tempSize <<= 1;
                        }
                        if (tempSize != targetTextureSize)
                        {
                            targetTextureSize = tempSize;
                            needsChange = true;
                        }
                    }

                    if (_fitMode is FitMode.GrowAndShrink or FitMode.ShrinkOnly)
                    {
                        int tempSize = targetTextureSize;
                        while (0 < tempSize >> 1 && size <= tempSize >> 1)
                        {
                            tempSize >>= 1;
                        }
                        if (tempSize != targetTextureSize)
                        {
                            targetTextureSize = tempSize;
                            needsChange = true;
                        }
                        else if (!needsChange && tempSize != currentTextureSize)
                        {
                            targetTextureSize = tempSize;
                            needsChange = true;
                        }
                    }

                    if (!needsChange || currentTextureSize == targetTextureSize)
                    {
                        continue;
                    }

                    changedCount++;
                    if (!applyChanges)
                    {
                        continue;
                    }

                    textureImporter.maxTextureSize = targetTextureSize;
                    updatedImporters.Add(textureImporter);

                    if (textureImporter.maxTextureSize == targetTextureSize)
                    {
                        continue;
                    }

                    this.LogError(
                        $"Failed to update {texture.name} to {targetTextureSize}, importer set size to {textureImporter.maxTextureSize}. Path: '{assetPath}'."
                    );

                    if (currentTextureSize == textureImporter.maxTextureSize)
                    {
                        changedCount--;
                        _ = updatedImporters.Remove(textureImporter);
                    }
                }
            }
            finally
            {
                if (applyChanges)
                {
                    AssetDatabase.StopAssetEditing();
                }
                EditorUtility.ClearProgressBar();

                if (applyChanges)
                {
                    foreach (TextureImporter importer in updatedImporters)
                    {
                        importer.SaveAndReimport();
                    }

                    if (changedCount != 0)
                    {
                        this.Log($"Updated {changedCount} textures.");
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                    else
                    {
                        this.Log($"No textures updated.");
                    }
                }
            }
            return changedCount;
        }
    }
#endif
}
