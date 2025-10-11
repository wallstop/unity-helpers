#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Editor.Utils
{
    using System;
    using UnityEditor;

    public static class EditorUi
    {
        private static bool _suppress;
        public static bool Suppress
        {
            get => _suppress;
            set => _suppress = value;
        }

        static EditorUi()
        {
            try
            {
#if UNITY_INCLUDE_TESTS
                _suppress = true;
#else
                _suppress = Application.isBatchMode || IsInvokedByTestRunner() || IsCiEnv();
#endif
            }
            catch
            {
                _suppress = false;
            }
        }

        private static bool IsCiEnv()
        {
            // Common CI/test env hints
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("UNITY_CI"))
                || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"))
                || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("UNITY_TESTS"));
        }

        private static bool IsInvokedByTestRunner()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; ++i)
            {
                string a = args[i];
                if (
                    a.IndexOf("runTests", StringComparison.OrdinalIgnoreCase) >= 0
                    || a.IndexOf("testResults", StringComparison.OrdinalIgnoreCase) >= 0
                    || a.IndexOf("testPlatform", StringComparison.OrdinalIgnoreCase) >= 0
                )
                {
                    return true;
                }
            }
            return false;
        }

        public static bool Confirm(
            string title,
            string message,
            string ok,
            string cancel,
            bool defaultWhenSuppressed = true
        )
        {
            if (Suppress)
            {
                return defaultWhenSuppressed;
            }
            return EditorUtility.DisplayDialog(title, message, ok, cancel);
        }

        public static void Info(string title, string message)
        {
            if (Suppress)
            {
                return;
            }
            EditorUtility.DisplayDialog(title, message, "OK");
        }

        public static void ShowProgress(string title, string info, float progress)
        {
            if (Suppress)
            {
                return;
            }
            EditorUtility.DisplayProgressBar(title, info, progress);
        }

        public static bool CancelableProgress(string title, string info, float progress)
        {
            if (Suppress)
            {
                return false;
            }
            return EditorUtility.DisplayCancelableProgressBar(title, info, progress);
        }

        public static void ClearProgress()
        {
            EditorUtility.ClearProgressBar();
        }

        public static string OpenFilePanel(string title, string directory, string extension)
        {
            if (Suppress)
            {
                return string.Empty;
            }
            return EditorUtility.OpenFilePanel(title, directory, extension);
        }

        public static string OpenFolderPanel(string title, string directory, string defaultName)
        {
            if (Suppress)
            {
                return string.Empty;
            }
            return EditorUtility.OpenFolderPanel(title, directory, defaultName);
        }
    }
}
#endif
