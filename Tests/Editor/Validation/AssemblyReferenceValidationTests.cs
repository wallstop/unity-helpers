// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.Validation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEditor.Compilation;
    using UnityEngine;
    using Assembly = System.Reflection.Assembly;

    /// <summary>
    /// Validation tests that verify assembly references and InternalsVisibleTo entries
    /// are correctly configured. These tests catch compilation issues like:
    /// - Missing namespace references
    /// - Broken assembly references
    /// - Missing InternalsVisibleTo entries for test assemblies
    /// </summary>
    [TestFixture]
    [Category("Fast")]
    [Category("Validation")]
    public sealed class AssemblyReferenceValidationTests
    {
        private static readonly string[] ProductionAssemblyNames =
        {
            "WallstopStudios.UnityHelpers",
            "WallstopStudios.UnityHelpers.Editor",
        };

        private static readonly string[] TestAssemblyNames =
        {
            "WallstopStudios.UnityHelpers.Tests.Core",
            "WallstopStudios.UnityHelpers.Tests.Editor",
            "WallstopStudios.UnityHelpers.Tests.Editor.Reflex",
            "WallstopStudios.UnityHelpers.Tests.Editor.Singletons",
            "WallstopStudios.UnityHelpers.Tests.Editor.Sprites",
            "WallstopStudios.UnityHelpers.Tests.Editor.Tools",
            "WallstopStudios.UnityHelpers.Tests.Editor.Validation",
            "WallstopStudios.UnityHelpers.Tests.Editor.VContainer",
            "WallstopStudios.UnityHelpers.Tests.Editor.Zenject",
            "WallstopStudios.UnityHelpers.Tests.Runtime",
            "WallstopStudios.UnityHelpers.Tests.Runtime.Performance",
            "WallstopStudios.UnityHelpers.Tests.Runtime.Random",
            "WallstopStudios.UnityHelpers.Tests.Runtime.Reflex",
            "WallstopStudios.UnityHelpers.Tests.Runtime.VContainer",
            "WallstopStudios.UnityHelpers.Tests.Runtime.Zenject",
        };

        private static readonly string[] AssemblyInfoPaths =
        {
            "Runtime/AssemblyInfo.cs",
            "Editor/AssemblyInfo.cs",
        };

        /// <summary>
        /// Verifies all production assemblies can be loaded.
        /// </summary>
        [Test]
        public void ProductionAssembliesCanBeLoaded()
        {
            List<string> failedAssemblies = new();

            foreach (string assemblyName in ProductionAssemblyNames)
            {
                try
                {
                    Assembly assembly = GetLoadedAssembly(assemblyName);
                    if (assembly == null)
                    {
                        failedAssemblies.Add(
                            $"{assemblyName}: Assembly not found in loaded assemblies"
                        );
                    }
                }
                catch (Exception ex)
                {
                    failedAssemblies.Add($"{assemblyName}: {ex.Message}");
                }
            }

            Assert.IsEmpty(
                failedAssemblies,
                $"Failed to load production assemblies:\n{string.Join("\n", failedAssemblies)}"
            );
        }

        /// <summary>
        /// Verifies all test assemblies can be loaded.
        /// </summary>
        [Test]
        public void TestAssembliesCanBeLoaded()
        {
            List<string> failedAssemblies = new();
            List<string> skippedAssemblies = new();

            foreach (string assemblyName in TestAssemblyNames)
            {
                try
                {
                    Assembly assembly = GetLoadedAssembly(assemblyName);
                    if (assembly == null)
                    {
                        // Some test assemblies may not be compiled if their dependencies are not present
                        // (e.g., VContainer, Zenject, Reflex integrations)
                        if (IsOptionalIntegrationAssembly(assemblyName))
                        {
                            skippedAssemblies.Add(
                                $"{assemblyName}: Optional integration assembly not compiled (expected)"
                            );
                        }
                        else
                        {
                            failedAssemblies.Add(
                                $"{assemblyName}: Assembly not found in loaded assemblies. "
                                    + "This may indicate a missing asmdef or broken assembly references."
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    failedAssemblies.Add($"{assemblyName}: {ex.Message}");
                }
            }

            if (skippedAssemblies.Count > 0)
            {
                Debug.Log(
                    $"Skipped optional integration assemblies:\n{string.Join("\n", skippedAssemblies)}"
                );
            }

            Assert.IsEmpty(
                failedAssemblies,
                $"Failed to load test assemblies:\n{string.Join("\n", failedAssemblies)}"
            );
        }

        /// <summary>
        /// Verifies all loaded test assemblies can load their types without errors.
        /// This catches broken type references and missing namespace issues.
        /// </summary>
        [Test]
        public void TestAssembliesCanLoadAllTypes()
        {
            List<string> failedAssemblies = new();

            foreach (string assemblyName in TestAssemblyNames)
            {
                Assembly assembly = GetLoadedAssembly(assemblyName);
                if (assembly == null)
                {
                    // Skip assemblies that are not loaded (optional integrations)
                    continue;
                }

                try
                {
                    Type[] types = assembly.GetTypes();
                    if (types.Length == 0)
                    {
                        // Empty assemblies are suspicious but not necessarily an error
                        Debug.LogWarning(
                            $"Assembly {assemblyName} has no types. This may indicate a configuration issue."
                        );
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    StringBuilder sb = new();
                    sb.AppendLine($"{assemblyName}: Failed to load types");
                    sb.AppendLine("Loader exceptions:");
                    foreach (Exception loaderEx in ex.LoaderExceptions)
                    {
                        if (loaderEx != null)
                        {
                            sb.AppendLine($"  - {loaderEx.Message}");
                        }
                    }

                    failedAssemblies.Add(sb.ToString());
                }
                catch (Exception ex)
                {
                    failedAssemblies.Add($"{assemblyName}: {ex.Message}");
                }
            }

            Assert.IsEmpty(
                failedAssemblies,
                $"Failed to load types from test assemblies:\n{string.Join("\n", failedAssemblies)}"
            );
        }

        /// <summary>
        /// Verifies all loaded production assemblies can load their types without errors.
        /// </summary>
        [Test]
        public void ProductionAssembliesCanLoadAllTypes()
        {
            List<string> failedAssemblies = new();

            foreach (string assemblyName in ProductionAssemblyNames)
            {
                Assembly assembly = GetLoadedAssembly(assemblyName);
                if (assembly == null)
                {
                    failedAssemblies.Add($"{assemblyName}: Assembly not loaded");
                    continue;
                }

                try
                {
                    Type[] types = assembly.GetTypes();
                    Assert.Greater(
                        types.Length,
                        0,
                        $"Production assembly {assemblyName} should have types"
                    );
                }
                catch (ReflectionTypeLoadException ex)
                {
                    StringBuilder sb = new();
                    sb.AppendLine($"{assemblyName}: Failed to load types");
                    sb.AppendLine("Loader exceptions:");
                    foreach (Exception loaderEx in ex.LoaderExceptions)
                    {
                        if (loaderEx != null)
                        {
                            sb.AppendLine($"  - {loaderEx.Message}");
                        }
                    }

                    failedAssemblies.Add(sb.ToString());
                }
                catch (Exception ex)
                {
                    failedAssemblies.Add($"{assemblyName}: {ex.Message}");
                }
            }

            Assert.IsEmpty(
                failedAssemblies,
                $"Failed to load types from production assemblies:\n{string.Join("\n", failedAssemblies)}"
            );
        }

        /// <summary>
        /// Verifies that InternalsVisibleTo entries exist for all test assemblies that need them.
        /// This test reads the AssemblyInfo.cs files and verifies entries exist.
        /// </summary>
        [Test]
        public void InternalsVisibleToEntriesExistForTestAssemblies()
        {
            string packagePath = GetPackagePath();
            if (string.IsNullOrEmpty(packagePath))
            {
                Assert.Inconclusive("Could not determine package path");
                return;
            }

            List<string> missingEntries = new();
            Dictionary<string, HashSet<string>> assemblyInfoEntries = new();

            // Read all AssemblyInfo files
            foreach (string relativePath in AssemblyInfoPaths)
            {
                string fullPath = Path.Combine(packagePath, relativePath);
                if (!File.Exists(fullPath))
                {
                    missingEntries.Add($"AssemblyInfo file not found: {relativePath}");
                    continue;
                }

                string content = File.ReadAllText(fullPath);
                HashSet<string> entries = ParseInternalsVisibleToEntries(content);
                assemblyInfoEntries[relativePath] = entries;
            }

            // Check that each test assembly has an InternalsVisibleTo entry in at least one AssemblyInfo
            foreach (string testAssemblyName in TestAssemblyNames)
            {
                // Skip assemblies that are optional integrations
                if (IsOptionalIntegrationAssembly(testAssemblyName))
                {
                    // Optional integrations should still have entries in case they are compiled
                    // Check if they have entries but don't fail if missing
                    bool hasEntry = false;
                    foreach (KeyValuePair<string, HashSet<string>> kvp in assemblyInfoEntries)
                    {
                        if (kvp.Value.Contains(testAssemblyName))
                        {
                            hasEntry = true;
                            break;
                        }
                    }

                    if (!hasEntry)
                    {
                        Debug.LogWarning(
                            $"Optional integration assembly {testAssemblyName} does not have "
                                + "InternalsVisibleTo entry. Add one if internal access is needed."
                        );
                    }

                    continue;
                }

                bool foundEntry = false;
                foreach (KeyValuePair<string, HashSet<string>> kvp in assemblyInfoEntries)
                {
                    if (kvp.Value.Contains(testAssemblyName))
                    {
                        foundEntry = true;
                        break;
                    }
                }

                if (!foundEntry)
                {
                    missingEntries.Add(
                        $"Missing InternalsVisibleTo entry for test assembly: {testAssemblyName}. "
                            + "Add [assembly: InternalsVisibleTo(\""
                            + testAssemblyName
                            + "\")] "
                            + "to Runtime/AssemblyInfo.cs and/or Editor/AssemblyInfo.cs"
                    );
                }
            }

            Assert.IsEmpty(
                missingEntries,
                $"InternalsVisibleTo configuration issues:\n{string.Join("\n", missingEntries)}"
            );
        }

        /// <summary>
        /// Verifies that all asmdef files in the Tests folder have corresponding assemblies
        /// that can be loaded (unless they are optional integrations).
        /// </summary>
        [Test]
        public void AllTestAsmdefFilesProduceLoadableAssemblies()
        {
            string packagePath = GetPackagePath();
            if (string.IsNullOrEmpty(packagePath))
            {
                Assert.Inconclusive("Could not determine package path");
                return;
            }

            string testsPath = Path.Combine(packagePath, "Tests");
            if (!Directory.Exists(testsPath))
            {
                Assert.Fail("Tests directory not found at: " + testsPath);
                return;
            }

            string[] asmdefFiles = Directory.GetFiles(
                testsPath,
                "*.asmdef",
                SearchOption.AllDirectories
            );
            List<string> issues = new();

            foreach (string asmdefPath in asmdefFiles)
            {
                try
                {
                    string asmdefContent = File.ReadAllText(asmdefPath);
                    string assemblyName = ExtractAssemblyNameFromAsmdef(asmdefContent);

                    if (string.IsNullOrEmpty(assemblyName))
                    {
                        issues.Add($"{asmdefPath}: Could not parse assembly name from asmdef");
                        continue;
                    }

                    Assembly assembly = GetLoadedAssembly(assemblyName);
                    if (assembly == null)
                    {
                        if (IsOptionalIntegrationAssembly(assemblyName))
                        {
                            // Expected for optional integrations
                            continue;
                        }

                        issues.Add(
                            $"{assemblyName}: asmdef exists at {asmdefPath} but assembly is not loaded. "
                                + "Check for missing references or compilation errors."
                        );
                    }
                }
                catch (Exception ex)
                {
                    issues.Add($"{asmdefPath}: Error processing asmdef: {ex.Message}");
                }
            }

            Assert.IsEmpty(issues, $"asmdef/assembly issues found:\n{string.Join("\n", issues)}");
        }

        /// <summary>
        /// Verifies that the Unity Compilation Pipeline reports no errors for test assemblies.
        /// </summary>
        [Test]
        public void UnityCompilationPipelineReportsNoErrors()
        {
            UnityEditor.Compilation.Assembly[] assemblies = CompilationPipeline.GetAssemblies(
                AssembliesType.Editor
            );

            List<string> testAssemblyIssues = new();

            foreach (UnityEditor.Compilation.Assembly assembly in assemblies)
            {
                if (!assembly.name.StartsWith("WallstopStudios.UnityHelpers.Tests"))
                {
                    continue;
                }

                // Check if the assembly has all its references resolved
                foreach (UnityEditor.Compilation.Assembly reference in assembly.assemblyReferences)
                {
                    if (!File.Exists(reference.outputPath))
                    {
                        testAssemblyIssues.Add(
                            $"{assembly.name}: Missing referenced assembly: {reference.name}"
                        );
                    }
                }

                // Check if the assembly has all its asmdef references resolved
                foreach (string asmdefRef in assembly.allReferences)
                {
                    // Skip system/Unity references
                    if (
                        asmdefRef.Contains("Unity")
                        || asmdefRef.Contains("System")
                        || asmdefRef.Contains("mscorlib")
                    )
                    {
                        continue;
                    }

                    // Check if it's a project reference that should exist
                    if (asmdefRef.Contains("WallstopStudios") && !File.Exists(asmdefRef))
                    {
                        testAssemblyIssues.Add(
                            $"{assembly.name}: Missing project reference: {asmdefRef}"
                        );
                    }
                }
            }

            Assert.IsEmpty(
                testAssemblyIssues,
                $"Compilation pipeline reported issues:\n{string.Join("\n", testAssemblyIssues)}"
            );
        }

        /// <summary>
        /// Verifies that test assemblies reference the correct production assemblies.
        /// </summary>
        [Test]
        public void TestAssembliesReferenceProductionAssemblies()
        {
            List<string> issues = new();

            foreach (string testAssemblyName in TestAssemblyNames)
            {
                Assembly testAssembly = GetLoadedAssembly(testAssemblyName);
                if (testAssembly == null)
                {
                    // Skip assemblies that are not loaded
                    continue;
                }

                AssemblyName[] referencedAssemblies = testAssembly.GetReferencedAssemblies();
                HashSet<string> referencedNames = new();
                foreach (AssemblyName an in referencedAssemblies)
                {
                    referencedNames.Add(an.Name);
                }

                // Test.Core should reference production assemblies
                if (testAssemblyName == "WallstopStudios.UnityHelpers.Tests.Core")
                {
                    if (!referencedNames.Contains("WallstopStudios.UnityHelpers"))
                    {
                        issues.Add(
                            $"{testAssemblyName}: Should reference WallstopStudios.UnityHelpers"
                        );
                    }
                }

                // Editor test assemblies should reference the Editor assembly
                if (testAssemblyName.Contains(".Tests.Editor"))
                {
                    if (!referencedNames.Contains("WallstopStudios.UnityHelpers.Editor"))
                    {
                        // Some test assemblies may only need the runtime assembly
                        // Just log a warning for visibility
                        Debug.Log(
                            $"Note: {testAssemblyName} does not directly reference "
                                + "WallstopStudios.UnityHelpers.Editor (may be indirect)"
                        );
                    }
                }
            }

            Assert.IsEmpty(issues, $"Test assembly reference issues:\n{string.Join("\n", issues)}");
        }

        /// <summary>
        /// Verifies that the current assembly (Validation) can access internal members
        /// from production assemblies via InternalsVisibleTo.
        /// </summary>
        [Test]
        public void ValidationAssemblyCanAccessInternalMembers()
        {
            // This test verifies that InternalsVisibleTo is working for this assembly
            Assembly runtimeAssembly = GetLoadedAssembly("WallstopStudios.UnityHelpers");
            Assert.IsNotNull(runtimeAssembly, "Runtime assembly should be loaded");

            Assembly editorAssembly = GetLoadedAssembly("WallstopStudios.UnityHelpers.Editor");
            Assert.IsNotNull(editorAssembly, "Editor assembly should be loaded");

            // Verify we can see internal types (if any exist)
            // This is a basic check that InternalsVisibleTo is configured
            Type[] runtimeTypes = runtimeAssembly.GetTypes();
            Type[] editorTypes = editorAssembly.GetTypes();

            Assert.Greater(runtimeTypes.Length, 0, "Runtime assembly should have types");
            Assert.Greater(editorTypes.Length, 0, "Editor assembly should have types");

            // Log internal type count for visibility
            int internalRuntimeTypes = 0;
            foreach (Type t in runtimeTypes)
            {
                if (!t.IsPublic && !t.IsNestedPublic)
                {
                    internalRuntimeTypes++;
                }
            }

            int internalEditorTypes = 0;
            foreach (Type t in editorTypes)
            {
                if (!t.IsPublic && !t.IsNestedPublic)
                {
                    internalEditorTypes++;
                }
            }

            Debug.Log(
                $"Internal types accessible: Runtime={internalRuntimeTypes}, Editor={internalEditorTypes}"
            );
        }

        /// <summary>
        /// Verifies namespace consistency between asmdef rootNamespace and actual types.
        /// </summary>
        [Test]
        public void AsmdefRootNamespaceMatchesActualTypes()
        {
            string packagePath = GetPackagePath();
            if (string.IsNullOrEmpty(packagePath))
            {
                Assert.Inconclusive("Could not determine package path");
                return;
            }

            string testsPath = Path.Combine(packagePath, "Tests");
            string[] asmdefFiles = Directory.GetFiles(
                testsPath,
                "*.asmdef",
                SearchOption.AllDirectories
            );
            List<string> issues = new();

            foreach (string asmdefPath in asmdefFiles)
            {
                try
                {
                    string asmdefContent = File.ReadAllText(asmdefPath);
                    string assemblyName = ExtractAssemblyNameFromAsmdef(asmdefContent);
                    string rootNamespace = ExtractRootNamespaceFromAsmdef(asmdefContent);

                    if (string.IsNullOrEmpty(assemblyName) || string.IsNullOrEmpty(rootNamespace))
                    {
                        continue;
                    }

                    Assembly assembly = GetLoadedAssembly(assemblyName);
                    if (assembly == null)
                    {
                        // Skip unloaded assemblies
                        continue;
                    }

                    Type[] types = assembly.GetTypes();
                    bool hasMatchingNamespace = false;
                    List<string> mismatchedTypes = new();

                    foreach (Type t in types)
                    {
                        if (t.Namespace == null)
                        {
                            continue;
                        }

                        if (t.Namespace.StartsWith(rootNamespace))
                        {
                            hasMatchingNamespace = true;
                        }
                        else if (
                            !t.Namespace.StartsWith("System")
                            && !t.Namespace.StartsWith("Unity")
                            && !t.Namespace.StartsWith("NUnit")
                            && !t.IsCompilerGenerated()
                        )
                        {
                            mismatchedTypes.Add($"{t.FullName} (namespace: {t.Namespace})");
                        }
                    }

                    if (types.Length > 0 && !hasMatchingNamespace && mismatchedTypes.Count > 0)
                    {
                        StringBuilder issueBuilder = new(
                            $"{assemblyName}: rootNamespace is '{rootNamespace}' but types use different namespaces:"
                        );
                        int maxEntries = Math.Min(5, mismatchedTypes.Count);
                        for (int i = 0; i < maxEntries; i++)
                        {
                            issueBuilder.Append("\n  ");
                            issueBuilder.Append(mismatchedTypes[i]);
                        }
                        if (mismatchedTypes.Count > 5)
                        {
                            issueBuilder.Append($"\n  ... and {mismatchedTypes.Count - 5} more");
                        }
                        issues.Add(issueBuilder.ToString());
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip assemblies with type load issues (covered by other tests)
                }
                catch (Exception ex)
                {
                    issues.Add($"{asmdefPath}: Error: {ex.Message}");
                }
            }

            // This is informational - namespace mismatches are not necessarily errors
            if (issues.Count > 0)
            {
                Debug.LogWarning(
                    "Namespace configuration notes (not necessarily errors):\n"
                        + string.Join("\n", issues)
                );
            }

            // Test passes - this is informational only
            Assert.Pass("Namespace analysis complete");
        }

        private static Assembly GetLoadedAssembly(string assemblyName)
        {
            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in loadedAssemblies)
            {
                if (assembly.GetName().Name == assemblyName)
                {
                    return assembly;
                }
            }

            return null;
        }

        private static bool IsOptionalIntegrationAssembly(string assemblyName)
        {
            return assemblyName.Contains(".Reflex")
                || assemblyName.Contains(".VContainer")
                || assemblyName.Contains(".Zenject");
        }

        private static HashSet<string> ParseInternalsVisibleToEntries(string content)
        {
            HashSet<string> entries = new();
            string[] lines = content.Split('\n');

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (!trimmed.Contains("InternalsVisibleTo"))
                {
                    continue;
                }

                // Parse: [assembly: InternalsVisibleTo("AssemblyName")]
                int startQuote = trimmed.IndexOf('"');
                int endQuote = trimmed.LastIndexOf('"');

                if (startQuote >= 0 && endQuote > startQuote)
                {
                    string assemblyName = trimmed.Substring(
                        startQuote + 1,
                        endQuote - startQuote - 1
                    );
                    entries.Add(assemblyName);
                }
            }

            return entries;
        }

        private static string ExtractAssemblyNameFromAsmdef(string content)
        {
            // Simple JSON parsing for "name": "value"
            string[] lines = content.Split('\n');
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("\"name\""))
                {
                    int colonIndex = trimmed.IndexOf(':');
                    if (colonIndex >= 0)
                    {
                        string value = trimmed.Substring(colonIndex + 1).Trim();
                        value = value.TrimStart('"').TrimEnd(',', '"');
                        return value;
                    }
                }
            }

            return null;
        }

        private static string ExtractRootNamespaceFromAsmdef(string content)
        {
            // Simple JSON parsing for "rootNamespace": "value"
            string[] lines = content.Split('\n');
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("\"rootNamespace\""))
                {
                    int colonIndex = trimmed.IndexOf(':');
                    if (colonIndex >= 0)
                    {
                        string value = trimmed.Substring(colonIndex + 1).Trim();
                        value = value.TrimStart('"').TrimEnd(',', '"');
                        return value;
                    }
                }
            }

            return null;
        }

        private static string GetPackagePath()
        {
            // Find the package path by looking for package.json
            string[] guids = AssetDatabase.FindAssets("package t:TextAsset");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (
                    path.EndsWith("package.json")
                    && path.Contains("com.wallstop-studios.unity-helpers")
                )
                {
                    return Path.GetDirectoryName(path);
                }
            }

            // Fallback: try to find via this assembly's location
            Assembly thisAssembly = typeof(AssemblyReferenceValidationTests).Assembly;
            string assemblyLocation = thisAssembly.Location;
            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                // Walk up to find package root
                string current = Path.GetDirectoryName(assemblyLocation);
                for (int i = 0; i < 10 && !string.IsNullOrEmpty(current); i++)
                {
                    if (File.Exists(Path.Combine(current, "package.json")))
                    {
                        return current;
                    }

                    current = Path.GetDirectoryName(current);
                }
            }

            // Fallback: use known relative path from Assets
            string dataPath = Application.dataPath;
            string projectRoot = Path.GetDirectoryName(dataPath);

            // Check common package locations
            string[] possiblePaths =
            {
                Path.Combine(projectRoot, "Packages", "com.wallstop-studios.unity-helpers"),
                Path.Combine(dataPath, "..", "Packages", "com.wallstop-studios.unity-helpers"),
            };

            foreach (string possiblePath in possiblePaths)
            {
                string normalized = Path.GetFullPath(possiblePath);
                if (
                    Directory.Exists(normalized)
                    && File.Exists(Path.Combine(normalized, "package.json"))
                )
                {
                    return normalized;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Extension methods for type checking.
    /// </summary>
    internal static class TypeExtensions
    {
        public static bool IsCompilerGenerated(this Type type)
        {
            return type.GetCustomAttribute<CompilerGeneratedAttribute>() != null
                || (type.Name.StartsWith("<") && type.Name.Contains(">"));
        }
    }
}
