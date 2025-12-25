#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

    [ScriptableSingletonPath("CaseTest")]
    internal sealed class CaseMismatch : ScriptableObjectSingleton<CaseMismatch> { }
}
#endif
