namespace WallstopStudios.UnityHelpers.Editor.DataVisualizer.Components
{
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    public sealed class DataVisualizerSettingsPopup : EditorWindow
    {
        internal DataVisualizerSettings _settings;
        internal Action _onCloseCallback;

        public static void ShowWindow(DataVisualizerSettings settingsToEdit, Action onCloseCallback)
        {
            DataVisualizerSettingsPopup window = CreateInstance<DataVisualizerSettingsPopup>();
            window.titleContent = new GUIContent("Data Visualizer Settings");
            window._settings = settingsToEdit;
            window._onCloseCallback = onCloseCallback;
            window.minSize = new Vector2(370, 130);
            window.maxSize = new Vector2(370, 130);
            window.ShowUtility();
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

            Toggle prefsToggle = new("Use EditorPrefs for State:")
            {
                value = _settings.UseEditorPrefsForState,
                tooltip =
                    "If checked, window state is saved globally in EditorPrefs.\nIf unchecked, state is saved within the DataVisualizerSettings asset file.",
            };
            prefsToggle.RegisterValueChangedCallback(evt =>
            {
                if (_settings.UseEditorPrefsForState != evt.newValue)
                {
                    _settings.UseEditorPrefsForState = evt.newValue;
                    EditorUtility.SetDirty(_settings);
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
