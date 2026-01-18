// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// Test singleton used for verifying the fix for the AssetDatabase race condition
    /// where newly created assets were immediately deleted because LoadAssetAtPath
    /// returned null before the AssetDatabase had indexed the file.
    /// </summary>
    [ScriptableSingletonPath("CreatorTests/Race")]
    internal sealed class AssetDatabaseRaceSingleton
        : ScriptableObjectSingleton<AssetDatabaseRaceSingleton> { }
}
#endif
