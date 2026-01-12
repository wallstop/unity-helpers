// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.Validation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Validates that test files follow naming conventions.
    /// Specifically checks for underscores in:
    /// - Test method names
    /// - TestName attribute values
    /// - SetName() method calls
    /// </summary>
    [TestFixture]
    [Category("Fast")]
    [Category("Validation")]
    public sealed class TestNamingConventionTests
    {
        private static readonly string[] TestAssemblyPrefixes =
        {
            "WallstopStudios.UnityHelpers.Tests",
        };

        private static readonly Regex TestNameAttributePattern = new(
            @"TestName\s*=\s*""([^""]+)""",
            RegexOptions.Compiled
        );

        private static readonly Regex SetNameMethodPattern = new(
            @"\.SetName\s*\(\s*""([^""]+)""",
            RegexOptions.Compiled
        );

        private static readonly Regex UnderscorePattern = new(@"_", RegexOptions.Compiled);

        /// <summary>
        /// Verifies that test method names do not contain underscores.
        /// </summary>
        [Test]
        public void TestMethodNamesDoNotContainUnderscores()
        {
            List<string> violations = new();
            List<Assembly> testAssemblies = GetTestAssemblies();

            foreach (Assembly assembly in testAssemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }

                foreach (Type type in types)
                {
                    if (type == null)
                    {
                        continue;
                    }

                    MethodInfo[] methods;
                    try
                    {
                        methods = type.GetMethods(
                            BindingFlags.Public
                                | BindingFlags.NonPublic
                                | BindingFlags.Instance
                                | BindingFlags.Static
                        );
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (MethodInfo method in methods)
                    {
                        if (!IsTestMethod(method))
                        {
                            continue;
                        }

                        if (UnderscorePattern.IsMatch(method.Name))
                        {
                            violations.Add(
                                $"Method '{type.FullName}.{method.Name}' contains underscores. "
                                    + "Use PascalCase without underscores."
                            );
                        }
                    }
                }
            }

            Assert.IsEmpty(
                violations,
                $"Found {violations.Count} test method(s) with underscores in their names:\n"
                    + string.Join("\n", violations)
            );
        }

        /// <summary>
        /// Verifies that TestName attribute values do not contain underscores.
        /// Scans source files directly for comprehensive detection.
        /// </summary>
        [Test]
        public void TestNameAttributeValuesDoNotContainUnderscores()
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
                Assert.Inconclusive("Tests directory not found at: " + testsPath);
                return;
            }

            List<string> violations = new();
            string[] testFiles = Directory.GetFiles(testsPath, "*.cs", SearchOption.AllDirectories);

            foreach (string filePath in testFiles)
            {
                if (filePath.EndsWith(".meta"))
                {
                    continue;
                }

                string[] lines;
                try
                {
                    lines = File.ReadAllLines(filePath);
                }
                catch
                {
                    continue;
                }

                string relativePath = GetRelativePath(filePath, packagePath);

                for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                {
                    string line = lines[lineIndex];
                    MatchCollection matches = TestNameAttributePattern.Matches(line);

                    foreach (Match match in matches)
                    {
                        string testName = match.Groups[1].Value;
                        if (UnderscorePattern.IsMatch(testName))
                        {
                            int lineNumber = lineIndex + 1;
                            violations.Add(
                                $"{relativePath}:{lineNumber}: TestName=\"{testName}\" contains underscores. "
                                    + "Use dot notation (e.g., \"Input.Null.ReturnsFalse\") or PascalCase."
                            );
                        }
                    }
                }
            }

            Assert.IsEmpty(
                violations,
                $"Found {violations.Count} TestName value(s) with underscores:\n"
                    + string.Join("\n", violations)
            );
        }

        /// <summary>
        /// Verifies that SetName() method call values do not contain underscores.
        /// Scans source files directly for comprehensive detection.
        /// </summary>
        [Test]
        public void SetNameMethodValuesDoNotContainUnderscores()
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
                Assert.Inconclusive("Tests directory not found at: " + testsPath);
                return;
            }

            List<string> violations = new();
            string[] testFiles = Directory.GetFiles(testsPath, "*.cs", SearchOption.AllDirectories);

            foreach (string filePath in testFiles)
            {
                if (filePath.EndsWith(".meta"))
                {
                    continue;
                }

                string[] lines;
                try
                {
                    lines = File.ReadAllLines(filePath);
                }
                catch
                {
                    continue;
                }

                string relativePath = GetRelativePath(filePath, packagePath);

                for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                {
                    string line = lines[lineIndex];
                    MatchCollection matches = SetNameMethodPattern.Matches(line);

                    foreach (Match match in matches)
                    {
                        string setNameValue = match.Groups[1].Value;
                        if (UnderscorePattern.IsMatch(setNameValue))
                        {
                            int lineNumber = lineIndex + 1;
                            violations.Add(
                                $"{relativePath}:{lineNumber}: SetName(\"{setNameValue}\") contains underscores. "
                                    + "Use dot notation (e.g., \"Input.Null.ReturnsFalse\") or PascalCase."
                            );
                        }
                    }
                }
            }

            Assert.IsEmpty(
                violations,
                $"Found {violations.Count} SetName() value(s) with underscores:\n"
                    + string.Join("\n", violations)
            );
        }

        /// <summary>
        /// Verifies that TestCaseSource method names do not contain underscores.
        /// </summary>
        [Test]
        public void TestCaseSourceMethodNamesDoNotContainUnderscores()
        {
            List<string> violations = new();
            List<Assembly> testAssemblies = GetTestAssemblies();

            foreach (Assembly assembly in testAssemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }

                foreach (Type type in types)
                {
                    if (type == null)
                    {
                        continue;
                    }

                    MethodInfo[] methods;
                    try
                    {
                        methods = type.GetMethods(
                            BindingFlags.Public
                                | BindingFlags.NonPublic
                                | BindingFlags.Instance
                                | BindingFlags.Static
                        );
                    }
                    catch
                    {
                        continue;
                    }

                    HashSet<string> testCaseSourceNames = new();
                    foreach (MethodInfo method in methods)
                    {
                        object[] attributes;
                        try
                        {
                            attributes = method.GetCustomAttributes(true);
                        }
                        catch
                        {
                            continue;
                        }

                        foreach (object attribute in attributes)
                        {
                            Type attributeType = attribute.GetType();
                            if (attributeType.Name == "TestCaseSourceAttribute")
                            {
                                PropertyInfo sourceNameProperty = attributeType.GetProperty(
                                    "SourceName"
                                );
                                if (sourceNameProperty != null)
                                {
                                    object value = sourceNameProperty.GetValue(attribute);
                                    if (
                                        value is string sourceName
                                        && !string.IsNullOrEmpty(sourceName)
                                    )
                                    {
                                        testCaseSourceNames.Add(sourceName);
                                    }
                                }
                            }
                        }
                    }

                    foreach (string sourceName in testCaseSourceNames)
                    {
                        if (UnderscorePattern.IsMatch(sourceName))
                        {
                            violations.Add(
                                $"TestCaseSource method '{type.FullName}.{sourceName}' contains underscores. "
                                    + "Use PascalCase without underscores."
                            );
                        }
                    }
                }
            }

            Assert.IsEmpty(
                violations,
                $"Found {violations.Count} TestCaseSource method name(s) with underscores:\n"
                    + string.Join("\n", violations)
            );
        }

        /// <summary>
        /// Comprehensive test that reports all naming convention violations at once.
        /// </summary>
        [Test]
        public void AllTestNamingConventionsAreFollowed()
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
                Assert.Inconclusive("Tests directory not found at: " + testsPath);
                return;
            }

            StringBuilder violations = new();
            int violationCount = 0;

            List<Assembly> testAssemblies = GetTestAssemblies();
            foreach (Assembly assembly in testAssemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }

                foreach (Type type in types)
                {
                    if (type == null)
                    {
                        continue;
                    }

                    MethodInfo[] methods;
                    try
                    {
                        methods = type.GetMethods(
                            BindingFlags.Public
                                | BindingFlags.NonPublic
                                | BindingFlags.Instance
                                | BindingFlags.Static
                        );
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (MethodInfo method in methods)
                    {
                        if (!IsTestMethod(method))
                        {
                            continue;
                        }

                        if (UnderscorePattern.IsMatch(method.Name))
                        {
                            violations.AppendLine($"  - Method: {type.FullName}.{method.Name}");
                            violationCount++;
                        }
                    }
                }
            }

            string[] testFiles = Directory.GetFiles(testsPath, "*.cs", SearchOption.AllDirectories);
            foreach (string filePath in testFiles)
            {
                if (filePath.EndsWith(".meta"))
                {
                    continue;
                }

                string[] lines;
                try
                {
                    lines = File.ReadAllLines(filePath);
                }
                catch
                {
                    continue;
                }

                string relativePath = GetRelativePath(filePath, packagePath);

                for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                {
                    string line = lines[lineIndex];
                    int lineNumber = lineIndex + 1;

                    MatchCollection testNameMatches = TestNameAttributePattern.Matches(line);
                    foreach (Match match in testNameMatches)
                    {
                        string testName = match.Groups[1].Value;
                        if (UnderscorePattern.IsMatch(testName))
                        {
                            violations.AppendLine(
                                $"  - {relativePath}:{lineNumber}: TestName=\"{testName}\""
                            );
                            violationCount++;
                        }
                    }

                    MatchCollection setNameMatches = SetNameMethodPattern.Matches(line);
                    foreach (Match match in setNameMatches)
                    {
                        string setNameValue = match.Groups[1].Value;
                        if (UnderscorePattern.IsMatch(setNameValue))
                        {
                            violations.AppendLine(
                                $"  - {relativePath}:{lineNumber}: SetName(\"{setNameValue}\")"
                            );
                            violationCount++;
                        }
                    }
                }
            }

            Assert.IsEmpty(
                violations.ToString(),
                $"Found {violationCount} naming convention violation(s) (underscores not allowed):\n"
                    + violations
            );
        }

        private static List<Assembly> GetTestAssemblies()
        {
            List<Assembly> testAssemblies = new();
            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in loadedAssemblies)
            {
                string assemblyName = assembly.GetName().Name;
                foreach (string prefix in TestAssemblyPrefixes)
                {
                    if (assemblyName.StartsWith(prefix))
                    {
                        testAssemblies.Add(assembly);
                        break;
                    }
                }
            }

            return testAssemblies;
        }

        private static bool IsTestMethod(MethodInfo method)
        {
            object[] attributes;
            try
            {
                attributes = method.GetCustomAttributes(true);
            }
            catch
            {
                return false;
            }

            foreach (object attribute in attributes)
            {
                string attributeName = attribute.GetType().Name;
                if (
                    attributeName == "TestAttribute"
                    || attributeName == "TestCaseAttribute"
                    || attributeName == "TestCaseSourceAttribute"
                    || attributeName == "UnityTestAttribute"
                )
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetPackagePath()
        {
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

            Assembly thisAssembly = typeof(TestNamingConventionTests).Assembly;
            string assemblyLocation = thisAssembly.Location;
            if (!string.IsNullOrEmpty(assemblyLocation))
            {
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

            string dataPath = Application.dataPath;
            string projectRoot = Path.GetDirectoryName(dataPath);

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

        private static string GetRelativePath(string fullPath, string basePath)
        {
            if (string.IsNullOrEmpty(basePath))
            {
                return fullPath;
            }

            string normalizedFull = Path.GetFullPath(fullPath).Replace('\\', '/');
            string normalizedBase = Path.GetFullPath(basePath).Replace('\\', '/');

            if (!normalizedBase.EndsWith("/"))
            {
                normalizedBase += "/";
            }

            if (normalizedFull.StartsWith(normalizedBase))
            {
                return normalizedFull.Substring(normalizedBase.Length);
            }

            return fullPath;
        }
    }
}
