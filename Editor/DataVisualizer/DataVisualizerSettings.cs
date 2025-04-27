namespace WallstopStudios.UnityHelpers.Editor.DataVisualizer
{
    using UnityEditor; // Needed for CreateAssetMenu in older Unity versions if not auto-imported
    using UnityEngine;
    using UnityEngine.Serialization;

    // Place this attribute if you want to be able to create it manually via Assets > Create menu
    [CreateAssetMenu(
        fileName = "DataVisualizerSettings",
        menuName = "DataVisualizer/Data Visualizer Settings",
        order = 1
    )]
    public class DataVisualizerSettings : ScriptableObject
    {
        public const string DefaultDataFolderPath = "Assets/Data";

        public string DataFolderPath => _dataFolderPath;

        [Tooltip(
            "Path relative to the project root (e.g., Assets/Data) where DataObject assets might be located or created."
        )]
        [SerializeField]
        internal string _dataFolderPath = DefaultDataFolderPath;

        // Add future settings here
        // public Font TitleFont;
        // public int MaxItemsToShow;

        private void OnValidate()
        {
            if (!Application.isEditor || !Application.isPlaying)
            {
                if (string.IsNullOrWhiteSpace(_dataFolderPath))
                {
                    _dataFolderPath = DefaultDataFolderPath;
                }

                _dataFolderPath = _dataFolderPath.Replace('\\', '/');
            }
        }
    }
}
