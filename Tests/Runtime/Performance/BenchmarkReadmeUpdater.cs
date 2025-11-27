namespace WallstopStudios.UnityHelpers.Tests.Runtime.Performance
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WallstopStudios.UnityHelpers.Core.Helper;

    internal static class BenchmarkReadmeUpdater
    {
        private const string ReadmeFileName = "README.md";

        public static void UpdateSection(string sectionName, IEnumerable<string> lines)
        {
            UpdateSection(sectionName, lines, ReadmeFileName);
        }

        public static void UpdateSection(
            string sectionName,
            IEnumerable<string> lines,
            string fileName
        )
        {
            if (Helpers.IsRunningInContinuousIntegration)
            {
                return;
            }

            string filePath = ResolveFilePath(fileName);
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return;
            }

            string startToken = $"<!-- {sectionName}_START -->";
            string endToken = $"<!-- {sectionName}_END -->";

            string fileText = File.ReadAllText(filePath);
            int startIndex = fileText.IndexOf(startToken, StringComparison.Ordinal);
            int endIndex = fileText.IndexOf(endToken, StringComparison.Ordinal);
            if (startIndex < 0 || endIndex < 0 || endIndex < startIndex)
            {
                return;
            }

            string newline = fileText.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

            List<string> contentLines = lines?.ToList() ?? new List<string>();

            string replacement =
                startToken + newline + string.Join(newline, contentLines) + newline + endToken;

            string existing = fileText.Substring(
                startIndex,
                endIndex + endToken.Length - startIndex
            );
            if (string.Equals(existing, replacement, StringComparison.Ordinal))
            {
                return;
            }

            string updated =
                fileText.Substring(0, startIndex)
                + replacement
                + fileText[(endIndex + endToken.Length)..];
            File.WriteAllText(filePath, updated);
        }

        private static string ResolveFilePath(string fileName)
        {
            string startDirectory = DirectoryHelper.GetCallerScriptDirectory();
            string root = DirectoryHelper.FindPackageRootPath(startDirectory);
            if (string.IsNullOrWhiteSpace(root))
            {
                return string.Empty;
            }

            return Path.Combine(root, fileName);
        }
    }
}
