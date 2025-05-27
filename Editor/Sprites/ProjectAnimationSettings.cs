namespace WallstopStudios.UnityHelpers.Editor.Sprites
{
#if UNITY_EDITOR
    using UnityEngine;

    public sealed class ProjectAnimationSettings : ScriptableObject
    {
        public string lastAnimationPath = "Assets";

        private static ProjectAnimationSettings _instance;
        private const string SettingsPath = "Assets/Editor/ProjectAnimationSettings.asset";

        public static ProjectAnimationSettings Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<ProjectAnimationSettings>(
                    SettingsPath
                );

                if (_instance != null)
                {
                    return _instance;
                }

                _instance = CreateInstance<ProjectAnimationSettings>();
                if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Editor"))
                {
                    UnityEditor.AssetDatabase.CreateFolder("Assets", "Editor");
                }
                UnityEditor.AssetDatabase.CreateAsset(_instance, SettingsPath);
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
                Debug.Log("Created ProjectAnimationSettings at: " + SettingsPath);
                return _instance;
            }
        }

        public void Save()
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}
