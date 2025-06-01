namespace WallstopStudios.UnityHelpers.Editor.AssetProcessors
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Extension;
    using Core.Helper;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public sealed class SpriteLabelProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        )
        {
            bool anyChanged = !Helpers.CachedLabels.Any();
            InitializeCacheIfNeeded();

            foreach (string path in importedAssets)
            {
                if (
                    !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    && !path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                    && !path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                )
                {
                    continue;
                }

                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null || ti.textureType != TextureImporterType.Sprite)
                {
                    continue;
                }

                Object mainObj = AssetDatabase.LoadMainAssetAtPath(path);
                if (mainObj == null)
                {
                    continue;
                }

                string[] newLabels = AssetDatabase.GetLabels(mainObj);
                if (
                    !Helpers.CachedLabels.TryGetValue(path, out string[] oldLabels)
                    || !AreEqual(oldLabels, newLabels)
                )
                {
                    Debug.Log(
                        $"[SpriteLabelProcessor] Labels changed on '{path}': {FormatLabels(oldLabels)} → {FormatLabels(newLabels)}"
                    );

                    string[] updated = new string[newLabels.Length];
                    Array.Copy(newLabels, updated, newLabels.Length);
                    anyChanged = true;
                    Helpers.CachedLabels[path] = updated;
                }
            }

            if (anyChanged)
            {
                Helpers.AllSpriteLabels = Helpers
                    .CachedLabels.Values.SelectMany(x => x)
                    .Distinct()
                    .Ordered()
                    .ToArray();
            }
        }

        private static void InitializeCacheIfNeeded()
        {
            _ = Helpers.GetAllSpriteLabelNames();
        }

        private static bool AreEqual(string[] a, string[] b)
        {
            if (a == null && b == null)
            {
                return true;
            }

            if (a == null || b == null)
            {
                return false;
            }

            if (a.Length != b.Length)
            {
                return false;
            }

            HashSet<string> setA = new(a, StringComparer.OrdinalIgnoreCase);
            HashSet<string> setB = new(b, StringComparer.OrdinalIgnoreCase);
            return setA.SetEquals(setB);
        }

        private static string FormatLabels(string[] arr)
        {
            if (arr == null || arr.Length == 0)
            {
                return "(none)";
            }

            return string.Join(", ", arr);
        }
    }
#endif
}
