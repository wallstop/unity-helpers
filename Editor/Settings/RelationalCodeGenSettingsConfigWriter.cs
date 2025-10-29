namespace WallstopStudios.UnityHelpers.Editor.Settings
{
    using System.IO;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.CodeGen;

    internal static class RelationalCodeGenSettingsConfigWriter
    {
        private const string ConfigDirectory = "Assets/WallstopStudios/Settings";
        private const string ConfigAssetPath =
            ConfigDirectory + "/relational_codegen_defaults.json";

        internal static void WriteConfig(RelationalCodeGenSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            EnsureDirectory();

            SerializableDefaults payload = new SerializableDefaults
            {
                sibling = settings.SiblingDefault.ToString(),
                parent = settings.ParentDefault.ToString(),
                child = settings.ChildDefault.ToString(),
            };

            string json = JsonUtility.ToJson(payload, prettyPrint: true);
            File.WriteAllText(ConfigAssetPath, json);

            AssetDatabase.ImportAsset(ConfigAssetPath);
        }

        private static void EnsureDirectory()
        {
            if (!Directory.Exists(ConfigDirectory))
            {
                Directory.CreateDirectory(ConfigDirectory);
                AssetDatabase.ImportAsset(ConfigDirectory);
            }
        }

        [System.Serializable]
        private struct SerializableDefaults
        {
            public string sibling;
            public string parent;
            public string child;
        }
    }
}
