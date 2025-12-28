// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Editor.Tags
{
    using System;
    using UnityEditor;
    using UnityEngine;
    using UnityHelpers.Tags;

    [CustomEditor(typeof(AttributeMetadataCache))]
    public sealed class AttributeMetadataCacheEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Cache Utilities", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Purge & Refresh Cache"))
                {
                    PurgeAndRefreshCache();
                }
            }
        }

        private static void PurgeAndRefreshCache()
        {
            AttributeMetadataCache cache = AttributeMetadataCache.Instance;
            if (cache != null)
            {
                cache.SetMetadata(
                    Array.Empty<string>(),
                    Array.Empty<AttributeMetadataCache.TypeFieldMetadata>(),
                    Array.Empty<AttributeMetadataCache.RelationalTypeMetadata>(),
                    Array.Empty<AttributeMetadataCache.AutoLoadSingletonEntry>()
                );

                AssetDatabase.SaveAssets();
                Debug.Log("Attribute Metadata Cache purged.");
            }

            AttributeMetadataCacheGenerator.GenerateCache();
            Debug.Log("Attribute Metadata Cache refreshed.");
        }
    }
}
