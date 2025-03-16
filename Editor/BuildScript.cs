namespace UnityHelpers.Editor
{
#if UNITY_EDITOR
    using UnityEditor;
    using System.IO;
    using UnityEngine;

    // Needed for build
    public static class BuildScript
    {
        [MenuItem("Build/Build Unity Package")]
        public static void BuildLinux()
        {
            Debug.Log($"Project Path: {Application.dataPath}");

            string[] scenes = { "Editor/Scenes/SampleScene.unity" };

            const string buildPath = "Builds/UnityHelpers";
            if (!Directory.Exists(buildPath))
            {
                Directory.CreateDirectory(buildPath);
            }

            BuildPipeline.BuildPlayer(
                scenes,
                buildPath + "/UnityHelpers.x86_64",
                BuildTarget.StandaloneLinux64,
                BuildOptions.None
            );
        }
    }
#endif
}
