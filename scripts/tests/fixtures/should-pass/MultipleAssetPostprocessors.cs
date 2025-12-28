// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

using UnityEditor;
using UnityEngine;

// This file should PASS: AssetImporter-related classes don't have the one-class-per-file restriction
public class MultipleAssetPostprocessorsA : AssetPostprocessor
{
    private void OnPreprocessTexture() { }
}

public class MultipleAssetPostprocessorsB : AssetPostprocessor
{
    private void OnPreprocessModel() { }
}
