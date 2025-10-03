namespace WallstopStudios.UnityHelpers.Tests.Performance
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;

    internal static class BenchmarkReadmeUpdater
    {
        private const string ReadmeFileName = "README.md";

        public static void UpdateSection(string sectionName, IEnumerable<string> lines)
        {
            if (Helpers.IsRunningInContinuousIntegration)
            {
                return;
            }

            string readmePath = ResolveReadmePath();
            if (string.IsNullOrWhiteSpace(readmePath) || !File.Exists(readmePath))
            {
                return;
            }

            string startToken = $"<!-- {sectionName}_START -->";
            string endToken = $"<!-- {sectionName}_END -->";

            string readmeText = File.ReadAllText(readmePath);
            int startIndex = readmeText.IndexOf(startToken, StringComparison.Ordinal);
            int endIndex = readmeText.IndexOf(endToken, StringComparison.Ordinal);
            if (startIndex < 0 || endIndex < 0 || endIndex < startIndex)
            {
                return;
            }

            string newline = readmeText.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

            List<string> contentLines = lines?.ToList() ?? new List<string>();

            string replacement =
                startToken + newline + string.Join(newline, contentLines) + newline + endToken;

            string existing = readmeText.Substring(
                startIndex,
                endIndex + endToken.Length - startIndex
            );
            if (string.Equals(existing, replacement, StringComparison.Ordinal))
            {
                return;
            }

            string updated =
                readmeText.Substring(0, startIndex)
                + replacement
                + readmeText[(endIndex + endToken.Length)..];
            File.WriteAllText(readmePath, updated);
        }

        private static string ResolveReadmePath()
        {
            string startDirectory = DirectoryHelper.GetCallerScriptDirectory();
            string root = DirectoryHelper.FindPackageRootPath(startDirectory);
            if (string.IsNullOrWhiteSpace(root))
            {
                return string.Empty;
            }

            return Path.Combine(root, ReadmeFileName);
        }
    }
}
