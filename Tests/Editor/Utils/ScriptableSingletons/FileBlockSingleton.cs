#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

    [ScriptableSingletonPath("CreatorTests/FileBlock")]
    internal sealed class FileBlockSingleton : ScriptableObjectSingleton<FileBlockSingleton> { }
}
#endif
