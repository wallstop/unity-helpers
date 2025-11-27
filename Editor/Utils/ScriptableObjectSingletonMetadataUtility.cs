namespace WallstopStudios.UnityHelpers.Editor.Utils
{
#if UNITY_EDITOR
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Utils;

    internal static class ScriptableObjectSingletonMetadataUtility
    {
        internal static ScriptableObjectSingletonMetadata LoadOrCreateMetadataAsset()
        {
            ScriptableObjectSingletonMetadata metadata =
                AssetDatabase.LoadAssetAtPath<ScriptableObjectSingletonMetadata>(
                    ScriptableObjectSingletonMetadata.AssetPath
                );
            if (metadata != null)
            {
                return metadata;
            }

            ScriptableObjectSingletonMetadata created =
                ScriptableObject.CreateInstance<ScriptableObjectSingletonMetadata>();
            EnsureResourcesFolder();
            bool editingInterrupted = TryStopAssetEditing();
            try
            {
                AssetDatabase.CreateAsset(created, ScriptableObjectSingletonMetadata.AssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(ScriptableObjectSingletonMetadata.AssetPath);
                return created;
            }
            finally
            {
                if (editingInterrupted)
                {
                    AssetDatabase.StartAssetEditing();
                }
            }
        }

        internal static void UpdateEntry(
            Type type,
            string resourcesLoadPath,
            string resourcesPath,
            string assetGuid
        )
        {
            ScriptableObjectSingletonMetadata metadata = LoadOrCreateMetadataAsset();
            ScriptableObjectSingletonMetadata.Entry entry = new()
            {
                assemblyQualifiedTypeName = type.AssemblyQualifiedName,
                resourcesLoadPath = resourcesLoadPath,
                resourcesPath = resourcesPath,
                assetGuid = assetGuid,
            };
            metadata.SetOrUpdateEntry(entry);
            EditorUtility.SetDirty(metadata);
        }

        private static void EnsureResourcesFolder()
        {
            string assetPath = ScriptableObjectSingletonMetadata.AssetPath;
            string directory = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(directory))
            {
                string[] segments = directory.Split('/', StringSplitOptions.RemoveEmptyEntries);
                string current = segments[0];
                for (int i = 1; i < segments.Length; i++)
                {
                    string next = $"{current}/{segments[i]}";
                    if (!AssetDatabase.IsValidFolder(next))
                    {
                        AssetDatabase.CreateFolder(current, segments[i]);
                    }
                    current = next;
                }
            }
        }

        private static bool TryStopAssetEditing()
        {
            try
            {
                AssetDatabase.StopAssetEditing();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
#endif
}
