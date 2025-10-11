#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Editor.Utils
{
    using System;
    using UnityEditor;
    using UnityEngine;

    public static class EditorUi
    {
        // Manual suppression set by code/tests and automatic suppression inferred from environment/TestRunner
        private static bool _suppressManual;
        private static bool _suppressAuto;
        public static bool Suppress
        {
            get => _suppressManual || _suppressAuto || IsNUnitContextActive() || IsUnderTestStack();
            set => _suppressManual = value;
        }

        static EditorUi()
        {
            try
            {
                // Suppress only when actually running in non-interactive contexts
                // such as batch mode, the Unity Test Runner, or CI environments.
                _suppressAuto = Application.isBatchMode || IsInvokedByTestRunner() || IsCiEnv();
            }
            catch
            {
                _suppressAuto = false;
            }

#if UNITY_INCLUDE_TESTS
            // In the Unity Test Runner (EditMode or PlayMode), actively suppress dialogs while tests run.
            try
            {
                // Use fully-qualified names to avoid requiring using directives when the
                // test framework isn't available.
                var api = new UnityEditor.TestTools.TestRunner.Api.TestRunnerApi();
                api.RegisterCallbacks(new TestRunnerSuppressCallbacks());
            }
            catch
            {
                // Swallow any issues; if callbacks can't register, fallback to other detection.
            }
#endif
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

        // Reflection-based: detect NUnit context without compile-time dependency
        private static bool IsNUnitContextActive()
        {
            try
            {
                Type t = Type.GetType(
                    "NUnit.Framework.TestContext, nunit.framework",
                    throwOnError: false
                );
                if (t == null)
                {
                    // Fallback: scan loaded assemblies to locate NUnit
                    var asms = AppDomain.CurrentDomain.GetAssemblies();
                    for (int i = 0; i < asms.Length && t == null; i++)
                    {
                        var an = asms[i].GetName().Name;
                        if (
                            an != null
                            && an.IndexOf("nunit.framework", StringComparison.OrdinalIgnoreCase)
                                >= 0
                        )
                        {
                            t = asms[i].GetType("NUnit.Framework.TestContext", throwOnError: false);
                        }
                    }
                }
                if (t == null)
                    return false;
                var prop = t.GetProperty(
                    "CurrentContext",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
                );
                if (prop == null)
                    return false;
                var ctx = prop.GetValue(null, null);
                return ctx != null;
            }
            catch
            {
                return false;
            }
        }

        // Heuristic: scan callstack for known test runner/framework frames
        private static bool IsUnderTestStack()
        {
            try
            {
                var st = new System.Diagnostics.StackTrace();
                int frames = Math.Min(st.FrameCount, 20);
                for (int i = 0; i < frames; i++)
                {
                    var method = st.GetFrame(i)?.GetMethod();
                    var type = method?.DeclaringType;
                    string asm = type?.Assembly?.GetName()?.Name ?? string.Empty;
                    string full = type?.FullName ?? string.Empty;
                    if (
                        asm.IndexOf("nunit", StringComparison.OrdinalIgnoreCase) >= 0
                        || asm.IndexOf("UnityEditor.TestRunner", StringComparison.OrdinalIgnoreCase)
                            >= 0
                        || asm.IndexOf("UnityEditor.TestTools", StringComparison.OrdinalIgnoreCase)
                            >= 0
                        || asm.IndexOf("UnityEngine.TestRunner", StringComparison.OrdinalIgnoreCase)
                            >= 0
                    )
                    {
                        return true;
                    }
                    if (
                        full.IndexOf("NUnit", StringComparison.OrdinalIgnoreCase) >= 0
                        || full.IndexOf("UnityEditor.TestTools", StringComparison.OrdinalIgnoreCase)
                            >= 0
                        || full.IndexOf("UnityEngine.TestTools", StringComparison.OrdinalIgnoreCase)
                            >= 0
                        || full.IndexOf("TestRunner", StringComparison.OrdinalIgnoreCase) >= 0
                    )
                    {
                        return true;
                    }
                }
            }
            catch { }
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

#if UNITY_INCLUDE_TESTS
        // Callback tied into the Unity Test Runner lifecycle to gate UI prompts.
        private class TestRunnerSuppressCallbacks : UnityEditor.TestTools.TestRunner.Api.ICallbacks
        {
            public void RunStarted(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor testsToRun)
            {
                _suppressAuto = true;
            }

            public void RunFinished(UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor result)
            {
                // Restore to baseline automatic suppression (e.g., batch/CI), not forcibly enabling UI.
                _suppressAuto = Application.isBatchMode || IsInvokedByTestRunner() || IsCiEnv();
            }

            public void TestStarted(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor test) { }

            public void TestFinished(
                UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor result
            ) { }
        }
#endif
    }
}
#endif
