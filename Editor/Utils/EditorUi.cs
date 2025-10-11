#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Editor.Utils
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using UnityEditor;
    using UnityEditor.TestTools.TestRunner.Api;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;

    public static class EditorUi
    {
        // Manual suppression set by code/tests and automatic suppression inferred from environment/TestRunner
        private static bool _suppressManual;
        private static bool _suppressAuto;
        public static bool Suppress
        {
            // Only suppress when explicitly requested or when we know
            // the environment is non-interactive (batch/CI) or tests are actively running.
            // Avoid heuristic stack/NUnit checks that can trip when Test Runner is merely open.
            get => _suppressManual || _suppressAuto;
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
            // Keep a static reference so callbacks remain registered and not GC'd.
            // This avoids losing RunStarted/RunFinished notifications mid-session.
            _testRunnerApi = null;
            // In the Unity Test Runner (EditMode or PlayMode), actively suppress dialogs while tests run.
            try
            {
                // Use fully-qualified names to avoid requiring using directives when the
                // test framework isn't available.
                TestRunnerApi api =
                    ScriptableObject.CreateInstance<UnityEditor.TestTools.TestRunner.Api.TestRunnerApi>();
                api.RegisterCallbacks(new TestRunnerSuppressCallbacks());
                _testRunnerApi = api;
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
                Type t = ReflectionHelpers.TryResolveType(
                    "NUnit.Framework.TestContext, nunit.framework"
                );
                if (t == null)
                {
                    foreach (Assembly asm in ReflectionHelpers.GetAllLoadedAssemblies())
                    {
                        string an = asm.GetName().Name;
                        if (
                            !string.IsNullOrEmpty(an)
                            && an.IndexOf("nunit.framework", StringComparison.OrdinalIgnoreCase)
                                >= 0
                        )
                        {
                            t = asm.GetType("NUnit.Framework.TestContext", throwOnError: false);
                            if (t != null)
                            {
                                break;
                            }
                        }
                    }
                }
                if (t == null)
                {
                    return false;
                }

                PropertyInfo prop = t.GetProperty(
                    "CurrentContext",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
                );
                if (prop == null)
                {
                    return false;
                }

                object ctx = prop.GetValue(null, null);
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
                StackTrace st = new();
                int frames = Math.Min(st.FrameCount, 20);
                for (int i = 0; i < frames; i++)
                {
                    MethodBase method = st.GetFrame(i)?.GetMethod();
                    Type type = method?.DeclaringType;
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

        // Hold reference to the TestRunnerApi instance while editor is open.
        private static UnityEditor.TestTools.TestRunner.Api.TestRunnerApi _testRunnerApi;
#endif
    }
}
#endif
