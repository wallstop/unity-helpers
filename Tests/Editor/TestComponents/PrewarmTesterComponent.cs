namespace WallstopStudios.UnityHelpers.Tests.Editor.TestComponents
{
#if UNITY_EDITOR
    using UnityEngine;

    // Minimal editor-side definition so Editor tests can reference the type
    // without depending on runtime test assemblies.
    public sealed class PrewarmTesterComponent : MonoBehaviour { }
#endif
}
