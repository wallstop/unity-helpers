namespace WallstopStudios.UnityHelpers.Editor.DataVisualizer.Components
{
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    public sealed class DataVisualizerSettingsPopup : EditorWindow
    {
        private DataVisualizerSettings _settings;
        private Action _onCloseCallback;
        private bool _settingsChanged = false;
        private TextField _dataFolderPathDisplay; // Keep field reference if needed by SelectDataFolder

        public static void ShowWindow(DataVisualizerSettings settingsToEdit, Action onCloseCallback)
        {
            DataVisualizerSettingsPopup window = CreateInstance<DataVisualizerSettingsPopup>();
            window.titleContent = new GUIContent("Data Visualizer Settings");
            window._settings = settingsToEdit;
            window._onCloseCallback = onCloseCallback;
            window.minSize = new Vector2(370, 130);
            window.maxSize = new Vector2(370, 130);
            window.ShowModalUtility();
        }

        public static DataVisualizerSettingsPopup CreateAndConfigureInstance(
            DataVisualizerSettings settingsToEdit,
            Action onCloseCallback
        )
        {
            DataVisualizerSettingsPopup window = CreateInstance<DataVisualizerSettingsPopup>();
            // Configure the instance BEFORE it's shown
            window.titleContent = new GUIContent("Data Visualizer Settings");
            window._settings = settingsToEdit;
            window._onCloseCallback = onCloseCallback;
            window.minSize = new Vector2(370, 130); // Keep size constraints
            window.maxSize = new Vector2(370, 130);
            return window; // Return the ready-to-show instance
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.style.paddingBottom = 10;
            root.style.paddingTop = 10;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;

            if (_settings == null)
            {
                root.Add(new Label("Error: Settings object reference is missing."));
                return;
            }

            _settingsChanged = false; // Reset flag
            Toggle prefsToggle = new("Use Settings Asset for State:")
            {
                value = _settings.PersistStateInSettingsAsset,
            };
            prefsToggle.RegisterValueChangedCallback(evt =>
            {
                if (_settings.PersistStateInSettingsAsset != evt.newValue)
                {
                    _settings.PersistStateInSettingsAsset = evt.newValue;
                    EditorUtility.SetDirty(_settings);
                    _settingsChanged = true;
                }
            });
            root.Add(prefsToggle);

            VisualElement dataFolderContainer = new()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginTop = 10,
                },
            };
            root.Add(dataFolderContainer);

            dataFolderContainer.Add(
                new Label("Data Folder:") { style = { width = 80, flexShrink = 0 } }
            );

            TextField dataFolderPathDisplay = new()
            {
                value = _settings.DataFolderPath,
                isReadOnly = true,
                name = "data-folder-display",
                style =
                {
                    flexGrow = 1,
                    marginLeft = 5,
                    marginRight = 5,
                },
            };
            dataFolderContainer.Add(dataFolderPathDisplay);

            Button selectFolderButton = new(() => SelectDataFolder(dataFolderPathDisplay))
            {
                text = "Select...",
                style = { flexShrink = 0 },
            };
            dataFolderContainer.Add(selectFolderButton);

            VisualElement buttonContainer = new()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexEnd,
                    marginTop = 15,
                },
            };
            root.Add(buttonContainer);
            Button closeButton = new(CloseWindowAndNotify) { text = "Close" };
            buttonContainer.Add(closeButton);
        }

        private void SelectDataFolder(TextField displayField)
        {
            if (_settings == null)
            {
                return;
            }

            string currentRelativePath = displayField.value;
            string projectRoot = Path.GetFullPath(Directory.GetCurrentDirectory())
                .Replace('\\', '/');
            string currentFullPath = Path.Combine(projectRoot, currentRelativePath);
            string startDir = Directory.Exists(currentFullPath)
                ? currentFullPath
                : Application.dataPath;

            string selectedAbsolutePath = EditorUtility.OpenFolderPanel(
                "Select Data Folder",
                startDir,
                ""
            );

            if (!string.IsNullOrWhiteSpace(selectedAbsolutePath))
            {
                string projectAssetsPath = Path.GetFullPath(Application.dataPath)
                    .Replace('\\', '/');
                selectedAbsolutePath = Path.GetFullPath(selectedAbsolutePath).Replace('\\', '/');

                if (
                    !selectedAbsolutePath.StartsWith(
                        projectAssetsPath,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    EditorUtility.DisplayDialog(
                        "Invalid Folder",
                        "The selected folder must be inside the project's 'Assets' directory.",
                        "OK"
                    );
                    return;
                }

                string relativePath = "Assets";
                if (projectAssetsPath.Length < selectedAbsolutePath.Length)
                {
                    relativePath += selectedAbsolutePath.Substring(projectAssetsPath.Length);
                }

                if (
                    !string.Equals(_settings.DataFolderPath, relativePath, StringComparison.Ordinal)
                )
                {
                    _settings._dataFolderPath = relativePath;
                    EditorUtility.SetDirty(_settings);
                    _settingsChanged = true;
                    displayField.value = _settings.DataFolderPath;
                }
            }
        }

        private void CloseWindowAndNotify()
        {
            Close();
        }

        private void OnDestroy()
        {
            _onCloseCallback?.Invoke();
        }
    }
}
