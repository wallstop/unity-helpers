// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.TestComponents
{
#if UNITY_EDITOR
    using UnityEngine;

    // Minimal editor-side definition so Editor tests can reference the type
    // without depending on runtime test assemblies.
    public sealed class PrewarmTesterComponent : MonoBehaviour { }
#endif
}
