namespace WallstopStudios.UnityHelpers.Editor.Tools
{
    using UnityEditor;
    using UnityEditor.Compilation;
    using UnityEditor.ShortcutManagement;
    using UnityEngine;

    /// <summary>
    /// Provides menu and shortcut access to trigger Unity's script recompilation pipeline.
    /// </summary>
    public static class ManualRecompile
    {
        private const string ShortcutId =
            "Wallstop Studios/Unity Helpers/Request Script Compilation";

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/Request Script Compilation")]
        public static void RequestFromMenu()
        {
            Request();
        }

        [Shortcut(ShortcutId, KeyCode.R, ShortcutModifiers.Alt | ShortcutModifiers.Action)]
        public static void RequestFromShortcut()
        {
            Request();
        }

        private static void Request()
        {
            CompilationPipeline.RequestScriptCompilation();
        }
    }
}
