#if UNITY_EDITOR

namespace WallstopStudios.UnityHelpers.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Ensures Roslyn analyzers/source generators are available to the Unity compiler by copying the required
    /// assemblies into Assets and appending the appropriate analyzer references to csc.rsp.
    /// Mirrors the approach used in DxMessaging so local development and package consumption behave identically.
    /// </summary>
    [InitializeOnLoad]
    public static class SetupCscRsp
    {
        private const string PackageName = "com.wallstop-studios.unity-helpers";
        private const string AnalyzerDestinationDirectory =
            "Assets/Plugins/Editor/WallstopStudios.UnityHelpers/";
        private const string AnalyzerFolderRelative = "Editor/Analyzers/";

        private static readonly string RspFilePath = Path.Combine(
                Application.dataPath,
                "..",
                "csc.rsp"
            )
            .Replace("\\", "/");

        private static readonly string[] RequiredDllNames =
        {
            "WallstopStudios.UnityHelpers.SourceGenerators.dll",
            "Microsoft.CodeAnalysis.dll",
            "Microsoft.CodeAnalysis.CSharp.dll",
            "System.Collections.Immutable.dll",
            "System.Reflection.Metadata.dll",
            "System.Runtime.CompilerServices.Unsafe.dll",
            "System.Text.Json.dll",
        };

        private static readonly HashSet<string> ExistingAssetDlls = new(
            StringComparer.OrdinalIgnoreCase
        );

        static SetupCscRsp()
        {
            EditorApplication.delayCall += EnsureAnalyzerAssembliesInAssets;
            EditorApplication.delayCall += EnsureCscRspIncludesAnalyzers;
        }

        private static void EnsureAnalyzerAssembliesInAssets()
        {
            ExistingAssetDlls.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:DefaultAsset", new[] { "Assets" }))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!assetPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!assetPath.Contains("Assets/Plugins", StringComparison.OrdinalIgnoreCase))
                {
                    ExistingAssetDlls.Add(Path.GetFileName(assetPath));
                }
            }

            string[] analyzerSourceDirectories = GetAnalyzerSourceDirectories().ToArray();

            bool copiedAny = false;
            foreach (
                string dllName in RequiredDllNames.Where(name => !ExistingAssetDlls.Contains(name))
            )
            {
                foreach (string directory in analyzerSourceDirectories)
                {
                    string sourcePath = Path.Combine(directory, dllName);
                    if (!File.Exists(sourcePath))
                    {
                        continue;
                    }

                    try
                    {
                        if (!Directory.Exists(AnalyzerDestinationDirectory))
                        {
                            Directory.CreateDirectory(AnalyzerDestinationDirectory);
                            AssetDatabase.Refresh();
                        }

                        string destinationPath =
                            AnalyzerDestinationDirectory.TrimEnd('/') + "/" + dllName;

                        bool needsCopy = FilesDiffer(sourcePath, destinationPath);
                        if (needsCopy)
                        {
                            File.Copy(sourcePath, destinationPath, overwrite: true);
                            AssetDatabase.ImportAsset(destinationPath);
                            copiedAny = true;
                        }

                        if (
                            string.Equals(
                                dllName,
                                "WallstopStudios.UnityHelpers.SourceGenerators.dll",
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        {
                            Object mainAsset = AssetDatabase.LoadMainAssetAtPath(destinationPath);
                            if (mainAsset != null)
                            {
                                AssetDatabase.SetLabels(mainAsset, new[] { "RoslynAnalyzer" });
                            }
                        }

                        if (
                            needsCopy
                            && AssetImporter.GetAtPath(destinationPath) is PluginImporter importer
                        )
                        {
                            importer.SetCompatibleWithAnyPlatform(false);
                            importer.SetExcludeFromAnyPlatform("Editor", false);
                            importer.SetExcludeFromAnyPlatform("Standalone", false);
                            importer.SaveAndReimport();
                        }
                        if (!needsCopy)
                        {
                            // Already identical; nothing else to do.
                        }
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError(
                            $"Failed to copy analyzer dependency '{dllName}' to Assets. {exception}"
                        );
                    }

                    break;
                }
            }

            if (copiedAny)
            {
                AssetDatabase.Refresh();
            }
        }

        private static bool FilesDiffer(string sourcePath, string destinationPath)
        {
            if (!File.Exists(destinationPath))
            {
                return true;
            }

            FileInfo sourceInfo = new(sourcePath);
            FileInfo destInfo = new(destinationPath);
            if (sourceInfo.Length != destInfo.Length)
            {
                return true;
            }

            using FileStream sourceStream = File.OpenRead(sourcePath);
            using FileStream destStream = File.OpenRead(destinationPath);
            using SHA256 sha256 = SHA256.Create();
            byte[] sourceHash = sha256.ComputeHash(sourceStream);
            byte[] destHash = sha256.ComputeHash(destStream);
            return !sourceHash.SequenceEqual(destHash);
        }

        private static void EnsureCscRspIncludesAnalyzers()
        {
            try
            {
                if (!File.Exists(RspFilePath))
                {
                    File.WriteAllText(RspFilePath, string.Empty);
                    AssetDatabase.ImportAsset("csc.rsp");
                }

                string rspContent = File.ReadAllText(RspFilePath);
                bool modified = false;
                foreach (string analyzerArgument in GetAnalyzerArguments())
                {
                    if (rspContent.Contains(analyzerArgument, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    File.AppendAllText(RspFilePath, analyzerArgument + Environment.NewLine);
                    modified = true;
                }

                if (modified)
                {
                    AssetDatabase.ImportAsset("csc.rsp");
                    Debug.Log("Updated csc.rsp with Unity Helpers analyzer references.");
                }
            }
            catch (IOException ioException)
            {
                Debug.LogError($"Failed to update csc.rsp: {ioException}");
            }
        }

        private static IEnumerable<string> GetAnalyzerSourceDirectories()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string packageDirectory = Path.Combine(
                projectRoot,
                "Packages",
                PackageName,
                AnalyzerFolderRelative
            );
            if (Directory.Exists(packageDirectory))
            {
                yield return packageDirectory.SanitizePath();
            }

            string packageCacheRoot = Path.Combine(projectRoot, "Library", "PackageCache");
            if (Directory.Exists(packageCacheRoot))
            {
                foreach (
                    string directory in Directory.EnumerateDirectories(
                        packageCacheRoot,
                        $"{PackageName}*",
                        SearchOption.TopDirectoryOnly
                    )
                )
                {
                    string analyzerDirectory = Path.Combine(directory, AnalyzerFolderRelative);
                    if (Directory.Exists(analyzerDirectory))
                    {
                        yield return analyzerDirectory.SanitizePath();
                    }
                }
            }
        }

        private static IEnumerable<string> GetAnalyzerArguments()
        {
            foreach (string directory in GetAnalyzerSourceDirectories())
            {
                yield return $"-a:\"{directory}\"";
            }
        }
    }
}
#endif
