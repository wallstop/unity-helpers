namespace WallstopStudios.UnityHelpers.Editor.AssetProcessors
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using Core.Extension;
    using Core.Helper;
    using UnityEditor;
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
            if (Helpers.IsRunningInBatchMode || Helpers.IsRunningInContinuousIntegration)
            {
                return;
            }

            bool anyChanged = Helpers.CachedLabels.Count == 0;

            foreach (string path in importedAssets)
            {
                if (!path.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
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
                if (newLabels.Length != 0)
                {
                    if (
                        !Helpers.CachedLabels.TryGetValue(path, out string[] oldLabels)
                        || !AreEqual(oldLabels, newLabels)
                    )
                    {
                        anyChanged = true;
                        if (newLabels.Length == 0)
                        {
                            Helpers.CachedLabels.Remove(path);
                            continue;
                        }

                        Helpers.CachedLabels[path] = newLabels;
                    }
                }
                else if (Helpers.CachedLabels.ContainsKey(path))
                {
                    anyChanged = true;
                    Helpers.CachedLabels.Remove(path);
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

            return a.ToImmutableHashSet(StringComparer.Ordinal).SetEquals(b);
        }
    }
#endif
}
