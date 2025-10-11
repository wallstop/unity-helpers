namespace WallstopStudios.UnityHelpers.Tests.Attributes
{
#if UNITY_EDITOR
    using UnityEngine;

    // Minimal editor-side definition so Editor tests can reference the type
    // without depending on runtime test assemblies.
    public sealed class PrewarmTesterComponent : MonoBehaviour { }
#endif
}
