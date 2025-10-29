namespace WallstopStudios.UnityHelpers.Editor.Settings
{
    using System.IO;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.CodeGen;

    internal sealed class RelationalCodeGenSettingsProvider : SettingsProvider
    {
        private const string SettingsProviderPath = "Project/Wallstop Studios/Relational CodeGen";
        private const string AssetFolderPath = "Assets/WallstopStudios/Settings/Resources";
        private const string AssetPath = AssetFolderPath + "/RelationalCodeGenSettings.asset";

        private RelationalCodeGenSettingsProvider()
            : base(SettingsProviderPath, SettingsScope.Project)
        {
            label = "Relational CodeGen";
        }

        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            return new RelationalCodeGenSettingsProvider();
        }

        public override void OnGUI(string searchContext)
        {
            RelationalCodeGenSettings settings = GetOrCreateSettings();
            SerializedObject serializedSettings = new SerializedObject(settings);

            serializedSettings.Update();

            EditorGUILayout.HelpBox(
                "Defaults apply when attribute CodeGenPreference is set to Inherit.",
                MessageType.Info
            );

            EditorGUILayout.PropertyField(
                serializedSettings.FindProperty("siblingDefault"),
                new GUIContent("Sibling Default")
            );
            EditorGUILayout.PropertyField(
                serializedSettings.FindProperty("parentDefault"),
                new GUIContent("Parent Default")
            );
            EditorGUILayout.PropertyField(
                serializedSettings.FindProperty("childDefault"),
                new GUIContent("Child Default")
            );

            if (serializedSettings.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                RelationalCodeGenUtility.ClearCachedSettings();
                RelationalCodeGenSettingsConfigWriter.WriteConfig(settings);
            }
        }

        private static RelationalCodeGenSettings GetOrCreateSettings()
        {
            RelationalCodeGenSettings settings =
                AssetDatabase.LoadAssetAtPath<RelationalCodeGenSettings>(AssetPath);

            if (settings != null)
            {
                return settings;
            }

            EnsureFolderHierarchy();

            settings = ScriptableObject.CreateInstance<RelationalCodeGenSettings>();
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();

            RelationalCodeGenUtility.ClearCachedSettings();
            RelationalCodeGenSettingsConfigWriter.WriteConfig(settings);

            return settings;
        }

        private static void EnsureFolderHierarchy()
        {
            string[] segments = AssetFolderPath.Split('/');
            string current = segments[0];

            for (int i = 1; i < segments.Length; ++i)
            {
                string next = Path.Combine(current, segments[i]).Replace('\\', '/');
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }
    }
}
