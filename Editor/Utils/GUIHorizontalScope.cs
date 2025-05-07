namespace WallstopStudios.UnityHelpers.Editor.Utils
{
#if UNITY_EDITOR
    using System;
    using UnityEditor;

    public sealed class GUIHorizontalScope : IDisposable
    {
        public GUIHorizontalScope()
        {
            EditorGUILayout.BeginHorizontal();
        }

        public void Dispose()
        {
            EditorGUILayout.EndHorizontal();
        }
    }
#endif
}
