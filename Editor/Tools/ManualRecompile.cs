namespace WallstopStudios.UnityHelpers.Editor.Tools
{
    using System;
    using UnityEditor;
    using UnityEditor.Compilation;
    using UnityEditor.ShortcutManagement;
    using UnityEngine;

    /// <summary>
    /// Provides menu and shortcut access to trigger Unity's script recompilation pipeline.
    /// </summary>
    public static class ManualRecompile
    {
        private const string ShortcutId = "Wallstop Studios/Request Script Compilation";
        private const string MenuItemPath =
            "Tools/Wallstop Studios/Unity Helpers/Request Script Compilation";
        private const string LogPrefix = "[Unity Helpers]";

        private static readonly ImportAssetOptions RefreshOptions =
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport;

        private static Func<bool> isCompilationPendingEvaluator = () =>
            EditorApplication.isCompiling;

        internal static Func<bool> IsCompilationPendingEvaluator
        {
            get => isCompilationPendingEvaluator;
            set
            {
                if (value != null)
                {
                    isCompilationPendingEvaluator = value;
                    return;
                }

                isCompilationPendingEvaluator = () => EditorApplication.isCompiling;
            }
        }

        internal static bool SkipCompilationRequestForTests { get; set; }

        internal static Action AssetsRefreshedForTests { get; set; }

        internal static Action CompilationRequestedForTests { get; set; }

        [MenuItem(MenuItemPath)]
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
            if (IsCompilationPending())
            {
                Debug.Log(
                    $"{LogPrefix} Script compilation already in progress; manual request skipped."
                );
                return;
            }

            AssetDatabase.Refresh(RefreshOptions);
            AssetsRefreshedForTests?.Invoke();

            bool skipCompilation = SkipCompilationRequestForTests;
            SkipCompilationRequestForTests = false;

            if (skipCompilation)
            {
                Debug.Log(
                    $"{LogPrefix} Asset database refreshed; compilation request skipped (tests)."
                );
                return;
            }

            CompilationRequestedForTests?.Invoke();
            CompilationPipeline.RequestScriptCompilation();
            Debug.Log($"{LogPrefix} Refreshed assets and requested script compilation.");
        }

        private static bool IsCompilationPending()
        {
            return isCompilationPendingEvaluator();
        }
    }
}
