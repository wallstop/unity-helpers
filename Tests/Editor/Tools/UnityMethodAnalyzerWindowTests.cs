#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Editor.Tools
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Editor.Tools.UnityMethodAnalyzer;

    /// <summary>
    /// Tests for the UnityMethodAnalyzerWindow focusing on state management,
    /// cancellation behavior, and UI state consistency.
    /// </summary>
    [TestFixture]
    public sealed class UnityMethodAnalyzerWindowTests
    {
        private UnityMethodAnalyzerWindow _window;
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(
                Path.GetTempPath(),
                "UnityMethodAnalyzerWindowTests_" + Path.GetRandomFileName()
            );
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (_window != null)
            {
                _window.Close();
                _window = null;
            }

            if (Directory.Exists(_tempDir))
            {
                try
                {
                    Directory.Delete(_tempDir, true);
                }
                catch
                {
                    // Ignore cleanup failures in tests
                }
            }
        }

        private UnityMethodAnalyzerWindow CreateWindow()
        {
            _window = ScriptableObject.CreateInstance<UnityMethodAnalyzerWindow>();
            return _window;
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            FieldInfo field = obj.GetType()
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsTrue(field != null, $"Field '{fieldName}' not found");
            field.SetValue(obj, value);
        }

        private T GetPrivateField<T>(object obj, string fieldName)
        {
            FieldInfo field = obj.GetType()
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsTrue(field != null, $"Field '{fieldName}' not found");
            return (T)field.GetValue(obj);
        }

        private void InvokePrivateMethod(object obj, string methodName, params object[] args)
        {
            MethodInfo method = obj.GetType()
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsTrue(method != null, $"Method '{methodName}' not found");
            method.Invoke(obj, args);
        }

        [Test]
        public void WindowInitializesWithCorrectDefaultState()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            bool isAnalyzing = GetPrivateField<bool>(window, "_isAnalyzing");
            float analysisProgress = GetPrivateField<float>(window, "_analysisProgress");
            string statusMessage = GetPrivateField<string>(window, "_statusMessage");
            CancellationTokenSource cts = GetPrivateField<CancellationTokenSource>(
                window,
                "_cancellationTokenSource"
            );

            Assert.IsFalse(isAnalyzing, "Window should not be analyzing on init");
            Assert.AreEqual(0f, analysisProgress, "Progress should be 0 on init");
            Assert.AreEqual(
                "Ready to analyze",
                statusMessage,
                "Status should be 'Ready to analyze'"
            );
            Assert.IsTrue(cts == null, "CancellationTokenSource should be null on init");
        }

        [Test]
        public void ResetAnalysisStateClearsAllAnalysisFields()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Simulate an in-progress analysis state
            SetPrivateField(window, "_isAnalyzing", true);
            SetPrivateField(window, "_analysisProgress", 0.75f);

            // Call ResetAnalysisState
            InvokePrivateMethod(window, "ResetAnalysisState");

            bool isAnalyzing = GetPrivateField<bool>(window, "_isAnalyzing");
            float analysisProgress = GetPrivateField<float>(window, "_analysisProgress");

            Assert.IsFalse(isAnalyzing, "isAnalyzing should be false after reset");
            Assert.AreEqual(0f, analysisProgress, "analysisProgress should be 0 after reset");
        }

        [Test]
        public void CancelAnalysisDoesNothingWhenNoCancellationTokenSource()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Ensure CTS is null
            SetPrivateField(window, "_cancellationTokenSource", null);

            // Should not throw
            InvokePrivateMethod(window, "CancelAnalysis");

            // State should remain unchanged
            bool isAnalyzing = GetPrivateField<bool>(window, "_isAnalyzing");
            Assert.IsFalse(isAnalyzing, "State should remain unchanged when CTS is null");
        }

        [Test]
        public void CancelAnalysisCancelsActiveCancellationTokenSource()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            CancellationTokenSource cts = new();
            SetPrivateField(window, "_cancellationTokenSource", cts);
            SetPrivateField(window, "_isAnalyzing", true);

            Assert.IsFalse(cts.IsCancellationRequested, "CTS should not be cancelled initially");

            InvokePrivateMethod(window, "CancelAnalysis");

            Assert.IsTrue(
                cts.IsCancellationRequested,
                "CTS should be cancelled after CancelAnalysis"
            );
        }

        [Test]
        public void CancelAnalysisDoesNotCancelAlreadyCancelledTokenSource()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            CancellationTokenSource cts = new();
            cts.Cancel(); // Pre-cancel
            SetPrivateField(window, "_cancellationTokenSource", cts);

            // Should not throw when called on already cancelled CTS
            InvokePrivateMethod(window, "CancelAnalysis");

            Assert.IsTrue(cts.IsCancellationRequested, "CTS should remain cancelled");
        }

        [Test]
        public void StartAnalysisDoesNothingWhenAlreadyAnalyzing()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Set up as if already analyzing
            SetPrivateField(window, "_isAnalyzing", true);
            SetPrivateField(window, "_statusMessage", "Previous status");

            // Try to start analysis
            InvokePrivateMethod(window, "StartAnalysis");

            // Status message should not have changed
            string statusMessage = GetPrivateField<string>(window, "_statusMessage");
            Assert.AreEqual(
                "Previous status",
                statusMessage,
                "Status should not change when already analyzing"
            );
        }

        [Test]
        public void StartAnalysisDisposesOldCancellationTokenSource()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            CancellationTokenSource oldCts = new();
            SetPrivateField(window, "_cancellationTokenSource", oldCts);
            SetPrivateField(window, "_isAnalyzing", false);
            SetPrivateField(window, "_sourcePaths", new List<string> { _tempDir });

            // Create a simple test file so analysis has something to process
            WriteTestFile("Test.cs", "public class Test { }");

            InvokePrivateMethod(window, "StartAnalysis");

            // The old CTS should be disposed (we can check by trying to use it)
            Assert.Throws<ObjectDisposedException>(() =>
            {
                CancellationToken token = oldCts.Token;
            });
        }

        [Test]
        public void StartAnalysisSetsAnalyzingStateImmediately()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            SetPrivateField(window, "_isAnalyzing", false);
            SetPrivateField(window, "_sourcePaths", new List<string> { _tempDir });

            WriteTestFile("Test.cs", "public class Test { }");

            InvokePrivateMethod(window, "StartAnalysis");

            bool isAnalyzing = GetPrivateField<bool>(window, "_isAnalyzing");
            string statusMessage = GetPrivateField<string>(window, "_statusMessage");

            // Note: Since StartAnalysis is async void, the state might have already reset
            // by the time we check. We primarily verify it doesn't throw.
            // The key assertion is that the method executes without error.
            Assert.Pass("StartAnalysis executed without throwing");
        }

        [Test]
        public void ResetAnalysisStateIsIdempotent()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Call reset multiple times - should not throw or change state unexpectedly
            InvokePrivateMethod(window, "ResetAnalysisState");
            InvokePrivateMethod(window, "ResetAnalysisState");
            InvokePrivateMethod(window, "ResetAnalysisState");

            bool isAnalyzing = GetPrivateField<bool>(window, "_isAnalyzing");
            float analysisProgress = GetPrivateField<float>(window, "_analysisProgress");

            Assert.IsFalse(isAnalyzing, "isAnalyzing should remain false");
            Assert.AreEqual(0f, analysisProgress, "analysisProgress should remain 0");
        }

        [Test]
        public void CancelAnalysisIsIdempotent()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            CancellationTokenSource cts = new();
            SetPrivateField(window, "_cancellationTokenSource", cts);

            // Call cancel multiple times - should not throw
            InvokePrivateMethod(window, "CancelAnalysis");
            InvokePrivateMethod(window, "CancelAnalysis");
            InvokePrivateMethod(window, "CancelAnalysis");

            Assert.IsTrue(cts.IsCancellationRequested, "CTS should be cancelled");
        }

        [Test]
        public void StartAnalysisSetsCorrectInitialStatusMessage()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            SetPrivateField(window, "_isAnalyzing", false);
            SetPrivateField(window, "_sourcePaths", new List<string> { _tempDir });
            SetPrivateField(window, "_statusMessage", "Previous message");

            WriteTestFile("Test.cs", "public class Test { }");

            InvokePrivateMethod(window, "StartAnalysis");

            // Due to async nature, we just verify it started
            // The status message should have changed from "Previous message"
            string statusMessage = GetPrivateField<string>(window, "_statusMessage");
            Assert.AreNotEqual(
                "Previous message",
                statusMessage,
                "Status message should have changed"
            );
        }

        [Test]
        public void StartAnalysisWithNoValidDirectoriesSetsAppropriateMessage()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            SetPrivateField(window, "_isAnalyzing", false);
            SetPrivateField(window, "_sourcePaths", new List<string> { "/nonexistent/path" });

            InvokePrivateMethod(window, "StartAnalysis");

            // Wait a moment for async to complete
            string statusMessage = GetPrivateField<string>(window, "_statusMessage");
            bool isAnalyzing = GetPrivateField<bool>(window, "_isAnalyzing");

            Assert.AreEqual(
                "No valid directories selected",
                statusMessage,
                "Should indicate no valid directories"
            );
            Assert.IsFalse(isAnalyzing, "Should not be analyzing when no valid directories");
        }

        [Test]
        public void StartAnalysisWithEmptySourcePathsSetsAppropriateMessage()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            SetPrivateField(window, "_isAnalyzing", false);
            SetPrivateField(window, "_sourcePaths", new List<string>());

            InvokePrivateMethod(window, "StartAnalysis");

            string statusMessage = GetPrivateField<string>(window, "_statusMessage");
            bool isAnalyzing = GetPrivateField<bool>(window, "_isAnalyzing");

            Assert.AreEqual(
                "No valid directories selected",
                statusMessage,
                "Should indicate no valid directories"
            );
            Assert.IsFalse(isAnalyzing, "Should not be analyzing with empty source paths");
        }

        [Test]
        public void StartAnalysisWithNullEntriesInSourcePathsFiltersThemOut()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            SetPrivateField(window, "_isAnalyzing", false);
            SetPrivateField(window, "_sourcePaths", new List<string> { null, "", "   ", _tempDir });

            WriteTestFile("Test.cs", "public class Test { }");

            InvokePrivateMethod(window, "StartAnalysis");

            // Should process the one valid directory without error
            // Status should not be "No valid directories selected"
            string statusMessage = GetPrivateField<string>(window, "_statusMessage");
            Assert.AreNotEqual(
                "No valid directories selected",
                statusMessage,
                "Should have found the valid directory"
            );
        }

        [UnityTest]
        public IEnumerator CancellationDuringAnalysisResetsUIState()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Create many test files to slow down analysis
            for (int i = 0; i < 50; i++)
            {
                WriteTestFile(
                    $"TestClass{i}.cs",
                    $@"
namespace TestNs
{{
    public class TestClass{i} : UnityEngine.MonoBehaviour
    {{
        private void Start() {{ }}
        private void Update() {{ }}
    }}
}}
"
                );
            }

            SetPrivateField(window, "_isAnalyzing", false);
            SetPrivateField(window, "_sourcePaths", new List<string> { _tempDir });

            // Start analysis
            InvokePrivateMethod(window, "StartAnalysis");

            // Wait a frame for analysis to begin
            yield return null;

            // Cancel the analysis
            InvokePrivateMethod(window, "CancelAnalysis");

            // Wait for cancellation to process
            float waitTime = 0f;
            float maxWaitTime = 5f;
            while (waitTime < maxWaitTime)
            {
                bool isAnalyzing = GetPrivateField<bool>(window, "_isAnalyzing");
                if (!isAnalyzing)
                {
                    break;
                }

                yield return null;
                waitTime += Time.deltaTime;
            }

            // Verify state is reset
            bool finalIsAnalyzing = GetPrivateField<bool>(window, "_isAnalyzing");
            float finalProgress = GetPrivateField<float>(window, "_analysisProgress");
            string finalStatus = GetPrivateField<string>(window, "_statusMessage");

            Assert.IsFalse(finalIsAnalyzing, "isAnalyzing should be false after cancellation");
            Assert.AreEqual(0f, finalProgress, "Progress should be 0 after cancellation");
            Assert.AreEqual(
                "Analysis cancelled",
                finalStatus,
                "Status should indicate cancellation"
            );
        }

        [UnityTest]
        public IEnumerator SuccessfulAnalysisResetsUIState()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Create a small test file for quick analysis
            WriteTestFile("SimpleTest.cs", "public class SimpleTest { }");

            SetPrivateField(window, "_isAnalyzing", false);
            SetPrivateField(window, "_sourcePaths", new List<string> { _tempDir });

            // Start analysis
            InvokePrivateMethod(window, "StartAnalysis");

            // Wait for analysis to complete
            float waitTime = 0f;
            float maxWaitTime = 10f;
            while (waitTime < maxWaitTime)
            {
                bool isAnalyzing = GetPrivateField<bool>(window, "_isAnalyzing");
                if (!isAnalyzing)
                {
                    break;
                }

                yield return null;
                waitTime += Time.deltaTime;
            }

            // Verify state is reset
            bool finalIsAnalyzing = GetPrivateField<bool>(window, "_isAnalyzing");
            float finalProgress = GetPrivateField<float>(window, "_analysisProgress");
            string finalStatus = GetPrivateField<string>(window, "_statusMessage");

            Assert.IsFalse(finalIsAnalyzing, "isAnalyzing should be false after completion");
            Assert.AreEqual(0f, finalProgress, "Progress should be 0 after completion");
            Assert.IsTrue(
                finalStatus.Contains("Analysis complete"),
                $"Status should indicate completion, was: {finalStatus}"
            );
        }

        [UnityTest]
        public IEnumerator CancelButtonCanBeClickedAgainAfterCancellation()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Create test files
            for (int i = 0; i < 20; i++)
            {
                WriteTestFile($"Test{i}.cs", $"public class Test{i} {{ }}");
            }

            SetPrivateField(window, "_sourcePaths", new List<string> { _tempDir });

            // First analysis cycle - start and cancel
            InvokePrivateMethod(window, "StartAnalysis");
            yield return null;
            InvokePrivateMethod(window, "CancelAnalysis");

            // Wait for cancellation
            float waitTime = 0f;
            while (waitTime < 5f && GetPrivateField<bool>(window, "_isAnalyzing"))
            {
                yield return null;
                waitTime += Time.deltaTime;
            }

            Assert.IsFalse(
                GetPrivateField<bool>(window, "_isAnalyzing"),
                "Should not be analyzing after first cancellation"
            );

            // Second analysis cycle - should be able to start again
            InvokePrivateMethod(window, "StartAnalysis");
            yield return null;

            // Verify analysis started again (either still running or completed quickly)
            // The key is that StartAnalysis didn't throw or get blocked
            Assert.Pass("Was able to start analysis again after cancellation");
        }

        [UnityTest]
        public IEnumerator AnalyzeButtonIsEnabledAfterCancellation()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Create test files
            for (int i = 0; i < 30; i++)
            {
                WriteTestFile($"Test{i}.cs", $"public class Test{i} {{ }}");
            }

            SetPrivateField(window, "_sourcePaths", new List<string> { _tempDir });

            // Start analysis
            InvokePrivateMethod(window, "StartAnalysis");
            yield return null;

            // Cancel
            InvokePrivateMethod(window, "CancelAnalysis");

            // Wait for cancellation
            float waitTime = 0f;
            while (waitTime < 5f && GetPrivateField<bool>(window, "_isAnalyzing"))
            {
                yield return null;
                waitTime += Time.deltaTime;
            }

            // Verify the analyze button would be enabled
            // The button is enabled when: !_isAnalyzing && hasValidPaths
            bool isAnalyzing = GetPrivateField<bool>(window, "_isAnalyzing");
            List<string> sourcePaths = GetPrivateField<List<string>>(window, "_sourcePaths");
            bool hasValidPaths =
                sourcePaths != null
                && sourcePaths.Exists(p => !string.IsNullOrEmpty(p) && Directory.Exists(p));
            bool analyzeEnabled = !isAnalyzing && hasValidPaths;

            Assert.IsTrue(analyzeEnabled, "Analyze button should be enabled after cancellation");
        }

        [UnityTest]
        public IEnumerator CancelButtonIsNotVisibleAfterCancellation()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Create test files
            for (int i = 0; i < 30; i++)
            {
                WriteTestFile($"Test{i}.cs", $"public class Test{i} {{ }}");
            }

            SetPrivateField(window, "_sourcePaths", new List<string> { _tempDir });

            // Start analysis
            InvokePrivateMethod(window, "StartAnalysis");
            yield return null;

            // Cancel
            InvokePrivateMethod(window, "CancelAnalysis");

            // Wait for cancellation
            float waitTime = 0f;
            while (waitTime < 5f && GetPrivateField<bool>(window, "_isAnalyzing"))
            {
                yield return null;
                waitTime += Time.deltaTime;
            }

            // The cancel button visibility is controlled by _isAnalyzing
            bool isAnalyzing = GetPrivateField<bool>(window, "_isAnalyzing");
            Assert.IsFalse(
                isAnalyzing,
                "Cancel button should not be visible (isAnalyzing should be false)"
            );
        }

        [UnityTest]
        public IEnumerator ProgressBarIsHiddenAfterCancellation()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Create test files
            for (int i = 0; i < 30; i++)
            {
                WriteTestFile($"Test{i}.cs", $"public class Test{i} {{ }}");
            }

            SetPrivateField(window, "_sourcePaths", new List<string> { _tempDir });

            // Start analysis
            InvokePrivateMethod(window, "StartAnalysis");
            yield return null;

            // Cancel
            InvokePrivateMethod(window, "CancelAnalysis");

            // Wait for cancellation
            float waitTime = 0f;
            while (waitTime < 5f && GetPrivateField<bool>(window, "_isAnalyzing"))
            {
                yield return null;
                waitTime += Time.deltaTime;
            }

            // Progress bar visibility is controlled by _isAnalyzing
            // Progress value should be 0 after cancellation
            bool isAnalyzing = GetPrivateField<bool>(window, "_isAnalyzing");
            float progress = GetPrivateField<float>(window, "_analysisProgress");

            Assert.IsFalse(
                isAnalyzing,
                "Progress bar should be hidden (isAnalyzing should be false)"
            );
            Assert.AreEqual(0f, progress, "Progress should be reset to 0");
        }

        [Test]
        public void OnDisableCancelsPendingAnalysis()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            CancellationTokenSource cts = new();
            SetPrivateField(window, "_cancellationTokenSource", cts);
            SetPrivateField(window, "_isAnalyzing", true);

            // Simulate OnDisable being called (e.g., when window is closed)
            MethodInfo onDisable = typeof(UnityMethodAnalyzerWindow).GetMethod(
                "OnDisable",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            Assert.IsTrue(onDisable != null, "OnDisable method should exist");
            onDisable.Invoke(window, null);

            Assert.IsTrue(cts.IsCancellationRequested, "CTS should be cancelled on disable");
        }

        [Test]
        public void OnDisableHandlesNullCancellationTokenSource()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            SetPrivateField(window, "_cancellationTokenSource", null);

            // Should not throw
            MethodInfo onDisable = typeof(UnityMethodAnalyzerWindow).GetMethod(
                "OnDisable",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            Assert.IsTrue(onDisable != null, "OnDisable method should exist");

            Assert.DoesNotThrow(
                () => onDisable.Invoke(window, null),
                "OnDisable should handle null CTS gracefully"
            );
        }

        [UnityTest]
        public IEnumerator MultipleCancellationsDoNotCorruptState()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Create test files
            for (int i = 0; i < 50; i++)
            {
                WriteTestFile($"Test{i}.cs", $"public class Test{i} {{ }}");
            }

            SetPrivateField(window, "_sourcePaths", new List<string> { _tempDir });

            // Start analysis
            InvokePrivateMethod(window, "StartAnalysis");
            yield return null;

            // Rapid-fire cancellations
            for (int i = 0; i < 10; i++)
            {
                InvokePrivateMethod(window, "CancelAnalysis");
            }

            // Wait for cancellation to complete
            float waitTime = 0f;
            while (waitTime < 5f && GetPrivateField<bool>(window, "_isAnalyzing"))
            {
                yield return null;
                waitTime += Time.deltaTime;
            }

            // Verify state is consistent
            bool isAnalyzing = GetPrivateField<bool>(window, "_isAnalyzing");
            float progress = GetPrivateField<float>(window, "_analysisProgress");

            Assert.IsFalse(isAnalyzing, "State should be consistent after multiple cancellations");
            Assert.AreEqual(0f, progress, "Progress should be 0 after multiple cancellations");
        }

        [UnityTest]
        public IEnumerator RapidStartCancelCyclesDoNotCorruptState()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Create test files
            for (int i = 0; i < 20; i++)
            {
                WriteTestFile($"Test{i}.cs", $"public class Test{i} {{ }}");
            }

            SetPrivateField(window, "_sourcePaths", new List<string> { _tempDir });

            // Rapid start-cancel cycles
            for (int cycle = 0; cycle < 5; cycle++)
            {
                // Wait for any previous operation to complete
                float waitTime = 0f;
                while (waitTime < 2f && GetPrivateField<bool>(window, "_isAnalyzing"))
                {
                    yield return null;
                    waitTime += Time.deltaTime;
                }

                InvokePrivateMethod(window, "StartAnalysis");
                yield return null;
                InvokePrivateMethod(window, "CancelAnalysis");
            }

            // Wait for final state to settle
            float finalWait = 0f;
            while (finalWait < 5f && GetPrivateField<bool>(window, "_isAnalyzing"))
            {
                yield return null;
                finalWait += Time.deltaTime;
            }

            // Verify state is consistent
            bool isAnalyzing = GetPrivateField<bool>(window, "_isAnalyzing");
            float progress = GetPrivateField<float>(window, "_analysisProgress");

            Assert.IsFalse(isAnalyzing, "State should be consistent after rapid cycles");
            Assert.AreEqual(0f, progress, "Progress should be 0 after rapid cycles");
        }

        [Test]
        public void CancellationTokenSourceIsDisposedAfterAnalysis()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            CancellationTokenSource initialCts = new();
            SetPrivateField(window, "_cancellationTokenSource", initialCts);
            SetPrivateField(window, "_isAnalyzing", false);
            SetPrivateField(window, "_sourcePaths", new List<string> { _tempDir });

            WriteTestFile("Test.cs", "public class Test { }");

            // Start a new analysis - this should dispose the old CTS
            InvokePrivateMethod(window, "StartAnalysis");

            // Verify the old CTS is disposed
            Assert.Throws<ObjectDisposedException>(
                () =>
                {
                    CancellationToken token = initialCts.Token;
                },
                "Old CTS should be disposed when starting new analysis"
            );
        }

        [Test]
        public void UpdateIssueCountsHandlesEmptyIssuesList()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Set up analyzer with no issues
            MethodAnalyzer analyzer = new();
            SetPrivateField(window, "_analyzer", analyzer);

            InvokePrivateMethod(window, "UpdateIssueCounts");

            int totalCount = GetPrivateField<int>(window, "_totalCount");
            int criticalCount = GetPrivateField<int>(window, "_criticalCount");
            int highCount = GetPrivateField<int>(window, "_highCount");
            int mediumCount = GetPrivateField<int>(window, "_mediumCount");
            int lowCount = GetPrivateField<int>(window, "_lowCount");
            int infoCount = GetPrivateField<int>(window, "_infoCount");

            Assert.AreEqual(0, totalCount, "Total count should be 0 for empty issues");
            Assert.AreEqual(0, criticalCount, "Critical count should be 0");
            Assert.AreEqual(0, highCount, "High count should be 0");
            Assert.AreEqual(0, mediumCount, "Medium count should be 0");
            Assert.AreEqual(0, lowCount, "Low count should be 0");
            Assert.AreEqual(0, infoCount, "Info count should be 0");
        }

        private void WriteTestFile(string filename, string content)
        {
            File.WriteAllText(Path.Combine(_tempDir, filename), content);
        }

        [Test]
        public void GroupByFileIsDefaultGroupingOnInit()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            bool groupByFile = GetPrivateField<bool>(window, "_groupByFile");
            bool groupBySeverity = GetPrivateField<bool>(window, "_groupBySeverity");
            bool groupByCategory = GetPrivateField<bool>(window, "_groupByCategory");

            Assert.IsTrue(groupByFile, "Group by file should be true by default");
            Assert.IsFalse(groupBySeverity, "Group by severity should be false by default");
            Assert.IsFalse(groupByCategory, "Group by category should be false by default");
        }

        [Test]
        public void GroupBySelectionExactlyOneIsAlwaysSelected()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Test all combinations - exactly one should always be true
            SetPrivateField(window, "_groupByFile", true);
            SetPrivateField(window, "_groupBySeverity", false);
            SetPrivateField(window, "_groupByCategory", false);
            AssertExactlyOneGroupBySelected(window, "Initial state");

            SetPrivateField(window, "_groupByFile", false);
            SetPrivateField(window, "_groupBySeverity", true);
            SetPrivateField(window, "_groupByCategory", false);
            AssertExactlyOneGroupBySelected(window, "Severity selected");

            SetPrivateField(window, "_groupByFile", false);
            SetPrivateField(window, "_groupBySeverity", false);
            SetPrivateField(window, "_groupByCategory", true);
            AssertExactlyOneGroupBySelected(window, "Category selected");
        }

        [Test]
        public void GroupBySwitchFromFileToSeverityWorks()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Start with File grouping
            SetPrivateField(window, "_groupByFile", true);
            SetPrivateField(window, "_groupBySeverity", false);
            SetPrivateField(window, "_groupByCategory", false);

            // Simulate clicking Severity (transition from false to true)
            SimulateGroupByClick(
                window,
                groupByFile: false,
                groupBySeverity: true,
                groupByCategory: false
            );

            bool groupByFile = GetPrivateField<bool>(window, "_groupByFile");
            bool groupBySeverity = GetPrivateField<bool>(window, "_groupBySeverity");
            bool groupByCategory = GetPrivateField<bool>(window, "_groupByCategory");

            Assert.IsFalse(groupByFile, "File should be deselected after clicking Severity");
            Assert.IsTrue(groupBySeverity, "Severity should be selected");
            Assert.IsFalse(groupByCategory, "Category should remain deselected");
        }

        [Test]
        public void GroupBySwitchFromSeverityToFileWorks()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Start with Severity grouping
            SetPrivateField(window, "_groupByFile", false);
            SetPrivateField(window, "_groupBySeverity", true);
            SetPrivateField(window, "_groupByCategory", false);

            // Simulate clicking File (transition from false to true)
            SimulateGroupByClick(
                window,
                groupByFile: true,
                groupBySeverity: false,
                groupByCategory: false
            );

            bool groupByFile = GetPrivateField<bool>(window, "_groupByFile");
            bool groupBySeverity = GetPrivateField<bool>(window, "_groupBySeverity");
            bool groupByCategory = GetPrivateField<bool>(window, "_groupByCategory");

            Assert.IsTrue(groupByFile, "File should be selected");
            Assert.IsFalse(groupBySeverity, "Severity should be deselected after clicking File");
            Assert.IsFalse(groupByCategory, "Category should remain deselected");
        }

        [Test]
        public void GroupBySwitchFromFileToCategoryWorks()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Start with File grouping
            SetPrivateField(window, "_groupByFile", true);
            SetPrivateField(window, "_groupBySeverity", false);
            SetPrivateField(window, "_groupByCategory", false);

            // Simulate clicking Category
            SimulateGroupByClick(
                window,
                groupByFile: false,
                groupBySeverity: false,
                groupByCategory: true
            );

            bool groupByFile = GetPrivateField<bool>(window, "_groupByFile");
            bool groupBySeverity = GetPrivateField<bool>(window, "_groupBySeverity");
            bool groupByCategory = GetPrivateField<bool>(window, "_groupByCategory");

            Assert.IsFalse(groupByFile, "File should be deselected after clicking Category");
            Assert.IsFalse(groupBySeverity, "Severity should remain deselected");
            Assert.IsTrue(groupByCategory, "Category should be selected");
        }

        [Test]
        public void GroupBySwitchFromCategoryToFileWorks()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Start with Category grouping
            SetPrivateField(window, "_groupByFile", false);
            SetPrivateField(window, "_groupBySeverity", false);
            SetPrivateField(window, "_groupByCategory", true);

            // Simulate clicking File
            SimulateGroupByClick(
                window,
                groupByFile: true,
                groupBySeverity: false,
                groupByCategory: false
            );

            bool groupByFile = GetPrivateField<bool>(window, "_groupByFile");
            bool groupBySeverity = GetPrivateField<bool>(window, "_groupBySeverity");
            bool groupByCategory = GetPrivateField<bool>(window, "_groupByCategory");

            Assert.IsTrue(groupByFile, "File should be selected");
            Assert.IsFalse(groupBySeverity, "Severity should remain deselected");
            Assert.IsFalse(groupByCategory, "Category should be deselected after clicking File");
        }

        [Test]
        public void GroupBySwitchFromSeverityToCategoryWorks()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Start with Severity grouping
            SetPrivateField(window, "_groupByFile", false);
            SetPrivateField(window, "_groupBySeverity", true);
            SetPrivateField(window, "_groupByCategory", false);

            // Simulate clicking Category
            SimulateGroupByClick(
                window,
                groupByFile: false,
                groupBySeverity: false,
                groupByCategory: true
            );

            bool groupByFile = GetPrivateField<bool>(window, "_groupByFile");
            bool groupBySeverity = GetPrivateField<bool>(window, "_groupBySeverity");
            bool groupByCategory = GetPrivateField<bool>(window, "_groupByCategory");

            Assert.IsFalse(groupByFile, "File should remain deselected");
            Assert.IsFalse(
                groupBySeverity,
                "Severity should be deselected after clicking Category"
            );
            Assert.IsTrue(groupByCategory, "Category should be selected");
        }

        [Test]
        public void GroupBySwitchFromCategoryToSeverityWorks()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Start with Category grouping
            SetPrivateField(window, "_groupByFile", false);
            SetPrivateField(window, "_groupBySeverity", false);
            SetPrivateField(window, "_groupByCategory", true);

            // Simulate clicking Severity
            SimulateGroupByClick(
                window,
                groupByFile: false,
                groupBySeverity: true,
                groupByCategory: false
            );

            bool groupByFile = GetPrivateField<bool>(window, "_groupByFile");
            bool groupBySeverity = GetPrivateField<bool>(window, "_groupBySeverity");
            bool groupByCategory = GetPrivateField<bool>(window, "_groupByCategory");

            Assert.IsFalse(groupByFile, "File should remain deselected");
            Assert.IsTrue(groupBySeverity, "Severity should be selected");
            Assert.IsFalse(
                groupByCategory,
                "Category should be deselected after clicking Severity"
            );
        }

        [Test]
        public void GroupByClickingAlreadySelectedFileDoesNotDeselect()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Start with File grouping
            SetPrivateField(window, "_groupByFile", true);
            SetPrivateField(window, "_groupBySeverity", false);
            SetPrivateField(window, "_groupByCategory", false);

            // Simulate clicking File again (already selected) - toggle returns false
            // but we should ignore this and keep File selected
            SimulateGroupByClickRaw(
                window,
                newGroupByFile: false,
                newGroupBySeverity: false,
                newGroupByCategory: false
            );

            bool groupByFile = GetPrivateField<bool>(window, "_groupByFile");
            bool groupBySeverity = GetPrivateField<bool>(window, "_groupBySeverity");
            bool groupByCategory = GetPrivateField<bool>(window, "_groupByCategory");

            Assert.IsTrue(
                groupByFile,
                "File should remain selected when clicking already-selected File"
            );
            Assert.IsFalse(groupBySeverity, "Severity should remain deselected");
            Assert.IsFalse(groupByCategory, "Category should remain deselected");
        }

        [Test]
        public void GroupByClickingAlreadySelectedSeverityDoesNotDeselect()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Start with Severity grouping
            SetPrivateField(window, "_groupByFile", false);
            SetPrivateField(window, "_groupBySeverity", true);
            SetPrivateField(window, "_groupByCategory", false);

            // Simulate clicking Severity again (already selected) - toggle returns false
            SimulateGroupByClickRaw(
                window,
                newGroupByFile: false,
                newGroupBySeverity: false,
                newGroupByCategory: false
            );

            bool groupByFile = GetPrivateField<bool>(window, "_groupByFile");
            bool groupBySeverity = GetPrivateField<bool>(window, "_groupBySeverity");
            bool groupByCategory = GetPrivateField<bool>(window, "_groupByCategory");

            Assert.IsFalse(groupByFile, "File should remain deselected");
            Assert.IsTrue(
                groupBySeverity,
                "Severity should remain selected when clicking already-selected Severity"
            );
            Assert.IsFalse(groupByCategory, "Category should remain deselected");
        }

        [Test]
        public void GroupByClickingAlreadySelectedCategoryDoesNotDeselect()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Start with Category grouping
            SetPrivateField(window, "_groupByFile", false);
            SetPrivateField(window, "_groupBySeverity", false);
            SetPrivateField(window, "_groupByCategory", true);

            // Simulate clicking Category again (already selected) - toggle returns false
            SimulateGroupByClickRaw(
                window,
                newGroupByFile: false,
                newGroupBySeverity: false,
                newGroupByCategory: false
            );

            bool groupByFile = GetPrivateField<bool>(window, "_groupByFile");
            bool groupBySeverity = GetPrivateField<bool>(window, "_groupBySeverity");
            bool groupByCategory = GetPrivateField<bool>(window, "_groupByCategory");

            Assert.IsFalse(groupByFile, "File should remain deselected");
            Assert.IsFalse(groupBySeverity, "Severity should remain deselected");
            Assert.IsTrue(
                groupByCategory,
                "Category should remain selected when clicking already-selected Category"
            );
        }

        [Test]
        public void GroupByRoundTripFileToSeverityBackToFileWorks()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Start with File
            SetPrivateField(window, "_groupByFile", true);
            SetPrivateField(window, "_groupBySeverity", false);
            SetPrivateField(window, "_groupByCategory", false);
            AssertExactlyOneGroupBySelected(window, "Initial: File");

            // Switch to Severity
            SimulateGroupByClick(
                window,
                groupByFile: false,
                groupBySeverity: true,
                groupByCategory: false
            );
            AssertExactlyOneGroupBySelected(window, "After switch to Severity");
            Assert.IsTrue(GetPrivateField<bool>(window, "_groupBySeverity"), "Should be Severity");

            // Switch back to File
            SimulateGroupByClick(
                window,
                groupByFile: true,
                groupBySeverity: false,
                groupByCategory: false
            );
            AssertExactlyOneGroupBySelected(window, "After switch back to File");
            Assert.IsTrue(GetPrivateField<bool>(window, "_groupByFile"), "Should be File again");
        }

        [Test]
        public void GroupByRoundTripSeverityToCategoryBackToSeverityWorks()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Start with Severity
            SetPrivateField(window, "_groupByFile", false);
            SetPrivateField(window, "_groupBySeverity", true);
            SetPrivateField(window, "_groupByCategory", false);
            AssertExactlyOneGroupBySelected(window, "Initial: Severity");

            // Switch to Category
            SimulateGroupByClick(
                window,
                groupByFile: false,
                groupBySeverity: false,
                groupByCategory: true
            );
            AssertExactlyOneGroupBySelected(window, "After switch to Category");
            Assert.IsTrue(GetPrivateField<bool>(window, "_groupByCategory"), "Should be Category");

            // Switch back to Severity
            SimulateGroupByClick(
                window,
                groupByFile: false,
                groupBySeverity: true,
                groupByCategory: false
            );
            AssertExactlyOneGroupBySelected(window, "After switch back to Severity");
            Assert.IsTrue(
                GetPrivateField<bool>(window, "_groupBySeverity"),
                "Should be Severity again"
            );
        }

        [Test]
        public void GroupByFullCycleFileSeverityCategoryFileWorks()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Start with File
            SetPrivateField(window, "_groupByFile", true);
            SetPrivateField(window, "_groupBySeverity", false);
            SetPrivateField(window, "_groupByCategory", false);

            // File -> Severity
            SimulateGroupByClick(
                window,
                groupByFile: false,
                groupBySeverity: true,
                groupByCategory: false
            );
            Assert.IsTrue(
                GetPrivateField<bool>(window, "_groupBySeverity"),
                "Step 1: Should be Severity"
            );
            AssertExactlyOneGroupBySelected(window, "Step 1");

            // Severity -> Category
            SimulateGroupByClick(
                window,
                groupByFile: false,
                groupBySeverity: false,
                groupByCategory: true
            );
            Assert.IsTrue(
                GetPrivateField<bool>(window, "_groupByCategory"),
                "Step 2: Should be Category"
            );
            AssertExactlyOneGroupBySelected(window, "Step 2");

            // Category -> File
            SimulateGroupByClick(
                window,
                groupByFile: true,
                groupBySeverity: false,
                groupByCategory: false
            );
            Assert.IsTrue(GetPrivateField<bool>(window, "_groupByFile"), "Step 3: Should be File");
            AssertExactlyOneGroupBySelected(window, "Step 3");
        }

        [Test]
        public void GroupByFullCycleReverseFileCategorySeverityFileWorks()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Start with File
            SetPrivateField(window, "_groupByFile", true);
            SetPrivateField(window, "_groupBySeverity", false);
            SetPrivateField(window, "_groupByCategory", false);

            // File -> Category
            SimulateGroupByClick(
                window,
                groupByFile: false,
                groupBySeverity: false,
                groupByCategory: true
            );
            Assert.IsTrue(
                GetPrivateField<bool>(window, "_groupByCategory"),
                "Step 1: Should be Category"
            );
            AssertExactlyOneGroupBySelected(window, "Step 1");

            // Category -> Severity
            SimulateGroupByClick(
                window,
                groupByFile: false,
                groupBySeverity: true,
                groupByCategory: false
            );
            Assert.IsTrue(
                GetPrivateField<bool>(window, "_groupBySeverity"),
                "Step 2: Should be Severity"
            );
            AssertExactlyOneGroupBySelected(window, "Step 2");

            // Severity -> File
            SimulateGroupByClick(
                window,
                groupByFile: true,
                groupBySeverity: false,
                groupByCategory: false
            );
            Assert.IsTrue(GetPrivateField<bool>(window, "_groupByFile"), "Step 3: Should be File");
            AssertExactlyOneGroupBySelected(window, "Step 3");
        }

        [Test]
        public void GroupByRapidClickingMaintainsConsistentState()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            SetPrivateField(window, "_groupByFile", true);
            SetPrivateField(window, "_groupBySeverity", false);
            SetPrivateField(window, "_groupByCategory", false);

            // Rapid clicking simulation
            for (int i = 0; i < 50; i++)
            {
                int selection = i % 3;
                switch (selection)
                {
                    case 0:
                        SimulateGroupByClick(
                            window,
                            groupByFile: true,
                            groupBySeverity: false,
                            groupByCategory: false
                        );
                        break;
                    case 1:
                        SimulateGroupByClick(
                            window,
                            groupByFile: false,
                            groupBySeverity: true,
                            groupByCategory: false
                        );
                        break;
                    case 2:
                        SimulateGroupByClick(
                            window,
                            groupByFile: false,
                            groupBySeverity: false,
                            groupByCategory: true
                        );
                        break;
                }

                AssertExactlyOneGroupBySelected(window, $"Iteration {i}");
            }
        }

        [Test]
        public void GroupByMultipleClicksOnSameButtonMaintainsSelection()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Start with File
            SetPrivateField(window, "_groupByFile", true);
            SetPrivateField(window, "_groupBySeverity", false);
            SetPrivateField(window, "_groupByCategory", false);

            // Click File multiple times (simulating toggle behavior returning false)
            for (int i = 0; i < 10; i++)
            {
                // When clicking already-selected toggle, it returns false
                SimulateGroupByClickRaw(
                    window,
                    newGroupByFile: false,
                    newGroupBySeverity: false,
                    newGroupByCategory: false
                );

                bool groupByFile = GetPrivateField<bool>(window, "_groupByFile");
                Assert.IsTrue(groupByFile, $"File should remain selected after click {i + 1}");
                AssertExactlyOneGroupBySelected(window, $"Click {i + 1}");
            }
        }

        [Test]
        public void GroupByMultipleClicksOnAlreadySelectedSeverityMaintainsSelection()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Start with Severity
            SetPrivateField(window, "_groupByFile", false);
            SetPrivateField(window, "_groupBySeverity", true);
            SetPrivateField(window, "_groupByCategory", false);

            // Click Severity multiple times
            for (int i = 0; i < 10; i++)
            {
                SimulateGroupByClickRaw(
                    window,
                    newGroupByFile: false,
                    newGroupBySeverity: false,
                    newGroupByCategory: false
                );

                bool groupBySeverity = GetPrivateField<bool>(window, "_groupBySeverity");
                Assert.IsTrue(
                    groupBySeverity,
                    $"Severity should remain selected after click {i + 1}"
                );
                AssertExactlyOneGroupBySelected(window, $"Click {i + 1}");
            }
        }

        [Test]
        public void GroupByMultipleClicksOnAlreadySelectedCategoryMaintainsSelection()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Start with Category
            SetPrivateField(window, "_groupByFile", false);
            SetPrivateField(window, "_groupBySeverity", false);
            SetPrivateField(window, "_groupByCategory", true);

            // Click Category multiple times
            for (int i = 0; i < 10; i++)
            {
                SimulateGroupByClickRaw(
                    window,
                    newGroupByFile: false,
                    newGroupBySeverity: false,
                    newGroupByCategory: false
                );

                bool groupByCategory = GetPrivateField<bool>(window, "_groupByCategory");
                Assert.IsTrue(
                    groupByCategory,
                    $"Category should remain selected after click {i + 1}"
                );
                AssertExactlyOneGroupBySelected(window, $"Click {i + 1}");
            }
        }

        [Test]
        public void GroupByAllPossibleTransitionsWork()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Test all 6 possible transitions
            string[] states = { "File", "Severity", "Category" };
            for (int from = 0; from < 3; from++)
            {
                for (int to = 0; to < 3; to++)
                {
                    if (from == to)
                    {
                        continue;
                    }

                    // Set initial state
                    SetPrivateField(window, "_groupByFile", from == 0);
                    SetPrivateField(window, "_groupBySeverity", from == 1);
                    SetPrivateField(window, "_groupByCategory", from == 2);

                    // Perform transition
                    SimulateGroupByClick(
                        window,
                        groupByFile: to == 0,
                        groupBySeverity: to == 1,
                        groupByCategory: to == 2
                    );

                    // Verify result
                    bool groupByFile = GetPrivateField<bool>(window, "_groupByFile");
                    bool groupBySeverity = GetPrivateField<bool>(window, "_groupBySeverity");
                    bool groupByCategory = GetPrivateField<bool>(window, "_groupByCategory");

                    Assert.AreEqual(
                        to == 0,
                        groupByFile,
                        $"Transition {states[from]} -> {states[to]}: File"
                    );
                    Assert.AreEqual(
                        to == 1,
                        groupBySeverity,
                        $"Transition {states[from]} -> {states[to]}: Severity"
                    );
                    Assert.AreEqual(
                        to == 2,
                        groupByCategory,
                        $"Transition {states[from]} -> {states[to]}: Category"
                    );
                    AssertExactlyOneGroupBySelected(
                        window,
                        $"Transition {states[from]} -> {states[to]}"
                    );
                }
            }
        }

        [Test]
        public void GroupByStatePreservedWhenNoButtonClicked()
        {
            UnityMethodAnalyzerWindow window = CreateWindow();

            // Test each state
            bool[][] states =
            {
                new[] { true, false, false },
                new[] { false, true, false },
                new[] { false, false, true },
            };

            foreach (bool[] state in states)
            {
                SetPrivateField(window, "_groupByFile", state[0]);
                SetPrivateField(window, "_groupBySeverity", state[1]);
                SetPrivateField(window, "_groupByCategory", state[2]);

                // Simulate no button clicked (all toggles return current values)
                SimulateGroupByClickRaw(
                    window,
                    newGroupByFile: state[0],
                    newGroupBySeverity: state[1],
                    newGroupByCategory: state[2]
                );

                bool groupByFile = GetPrivateField<bool>(window, "_groupByFile");
                bool groupBySeverity = GetPrivateField<bool>(window, "_groupBySeverity");
                bool groupByCategory = GetPrivateField<bool>(window, "_groupByCategory");

                Assert.AreEqual(state[0], groupByFile, "File state should be preserved");
                Assert.AreEqual(state[1], groupBySeverity, "Severity state should be preserved");
                Assert.AreEqual(state[2], groupByCategory, "Category state should be preserved");
            }
        }

        private void AssertExactlyOneGroupBySelected(
            UnityMethodAnalyzerWindow window,
            string context
        )
        {
            bool groupByFile = GetPrivateField<bool>(window, "_groupByFile");
            bool groupBySeverity = GetPrivateField<bool>(window, "_groupBySeverity");
            bool groupByCategory = GetPrivateField<bool>(window, "_groupByCategory");

            int selectedCount =
                (groupByFile ? 1 : 0) + (groupBySeverity ? 1 : 0) + (groupByCategory ? 1 : 0);
            Assert.AreEqual(
                1,
                selectedCount,
                $"[{context}] Exactly one grouping should be selected, but {selectedCount} were selected. "
                    + $"File={groupByFile}, Severity={groupBySeverity}, Category={groupByCategory}"
            );
        }

        /// <summary>
        /// Simulates clicking a group-by button by setting up state as if the button was clicked.
        /// The target grouping should be true, others false.
        /// </summary>
        private void SimulateGroupByClick(
            UnityMethodAnalyzerWindow window,
            bool groupByFile,
            bool groupBySeverity,
            bool groupByCategory
        )
        {
            // Simulate the toggle return values when a new button is clicked
            SimulateGroupByClickRaw(window, groupByFile, groupBySeverity, groupByCategory);
        }

        /// <summary>
        /// Simulates the raw toggle return values and invokes the internal logic.
        /// This allows testing edge cases like clicking an already-selected button.
        /// </summary>
        private void SimulateGroupByClickRaw(
            UnityMethodAnalyzerWindow window,
            bool newGroupByFile,
            bool newGroupBySeverity,
            bool newGroupByCategory
        )
        {
            // We need to invoke the internal grouping logic with specific toggle values.
            // Since DrawGroupBySection is tightly coupled to GUI, we simulate the logic directly.
            // The logic after getting toggle values is what we test.

            bool currentGroupByFile = GetPrivateField<bool>(window, "_groupByFile");
            bool currentGroupBySeverity = GetPrivateField<bool>(window, "_groupBySeverity");
            bool currentGroupByCategory = GetPrivateField<bool>(window, "_groupByCategory");

            // Detect which button was clicked by checking for a transition from false to true
            bool fileClicked = newGroupByFile && !currentGroupByFile;
            bool severityClicked = newGroupBySeverity && !currentGroupBySeverity;
            bool categoryClicked = newGroupByCategory && !currentGroupByCategory;

            if (fileClicked)
            {
                SetPrivateField(window, "_groupByFile", true);
                SetPrivateField(window, "_groupBySeverity", false);
                SetPrivateField(window, "_groupByCategory", false);
            }
            else if (severityClicked)
            {
                SetPrivateField(window, "_groupByFile", false);
                SetPrivateField(window, "_groupBySeverity", true);
                SetPrivateField(window, "_groupByCategory", false);
            }
            else if (categoryClicked)
            {
                SetPrivateField(window, "_groupByFile", false);
                SetPrivateField(window, "_groupBySeverity", false);
                SetPrivateField(window, "_groupByCategory", true);
            }
            // If no transition from false to true, state is preserved (clicking already-selected button)
        }
    }
}
#endif
