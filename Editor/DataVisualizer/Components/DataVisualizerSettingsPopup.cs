namespace WallstopStudios.UnityHelpers.Editor.DataVisualizer.Components
{
    using System; // For Action
    using System.IO;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    // Use the same namespace as your main window for easier access if needed
    namespace UnityHelpers.Editor
    {
        public class DataVisualizerSettingsPopup : EditorWindow
        {
            internal DataVisualizerSettings _settings;
            internal Action _onCloseCallback; // Callback when window closes

            private bool _settingsChanged = false; // Flag to track if changes were made

            // Static method to create and show the modal window
            public static void ShowWindow(
                DataVisualizerSettings settingsToEdit,
                Action onCloseCallback
            )
            {
                DataVisualizerSettingsPopup window = CreateInstance<DataVisualizerSettingsPopup>();
                window.titleContent = new GUIContent("Data Visualizer Settings"); // Set window title
                window._settings = settingsToEdit; // Inject the settings object!
                window._onCloseCallback = onCloseCallback; // Inject the callback
                window.minSize = new Vector2(370, 130); // Adjust size as needed
                window.maxSize = new Vector2(370, 130);
                window.ShowUtility(); // Use this for a floating utility window feel
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

                _settingsChanged = false; // Reset changed flag on GUI creation

                // Title (Optional, window has title)
                // root.Add(new Label("Settings") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 10 } });

                // --- Use EditorPrefs Toggle ---
                var prefsToggle = new Toggle("Use EditorPrefs for State:")
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
                        EditorUtility.SetDirty(_settings); // Mark dirty immediately
                        _settingsChanged = true;
                        // Migration logic should happen back in the main window AFTER this one closes.
                        // We just record that the setting changed.
                    }
                });
                root.Add(prefsToggle);

                // --- Data Folder Setting ---
                var dataFolderContainer = new VisualElement
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

                // Display field (use TextField set to readonly for easier styling/copying)
                var dataFolderPathDisplay = new TextField
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

                var selectFolderButton = new Button(() => SelectDataFolder(dataFolderPathDisplay))
                { // Pass display field to update
                    text = "Select...",
                    style = { flexShrink = 0 },
                };
                dataFolderContainer.Add(selectFolderButton);

                // --- Close Button ---
                var buttonContainer = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        justifyContent = Justify.FlexEnd,
                        marginTop = 15,
                    },
                };
                root.Add(buttonContainer);
                var closeButton = new Button(CloseWindowAndNotify) { text = "Close" };
                buttonContainer.Add(closeButton);
            }

            // Folder Selection Logic (Now inside this window)
            private void SelectDataFolder(TextField displayField) // Takes display field to update it
            {
                if (_settings == null)
                    return;

                string currentRelativePath = displayField.value; // Use current value from display
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

                if (!string.IsNullOrEmpty(selectedAbsolutePath))
                {
                    string projectAssetsPath = Path.GetFullPath(Application.dataPath)
                        .Replace('\\', '/');
                    selectedAbsolutePath = Path.GetFullPath(selectedAbsolutePath)
                        .Replace('\\', '/');

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
                    if (selectedAbsolutePath.Length > projectAssetsPath.Length)
                    {
                        // Get path part after Application.dataPath ends
                        relativePath += selectedAbsolutePath.Substring(projectAssetsPath.Length);
                    }

                    if (_settings.DataFolderPath != relativePath)
                    {
                        _settings._dataFolderPath = relativePath;
                        EditorUtility.SetDirty(_settings); // Mark dirty
                        _settingsChanged = true; // Flag that settings changed
                        displayField.value = _settings.DataFolderPath; // Update display immediately
                        Debug.Log(
                            $"Data folder updated (in memory) to: {_settings.DataFolderPath}"
                        );
                    }
                }
            }

            // Close button action
            private void CloseWindowAndNotify()
            {
                // Intentionally don't call AssetDatabase.SaveAssets here.
                // Let the main window handle saving and migration *after* getting notified.
                this.Close();
            }

            // Called when the window is closed (by button or 'X')
            private void OnDestroy()
            {
                // Notify the main window that settings might have changed.
                // The main window will then decide whether to save/migrate/refresh.
                _onCloseCallback?.Invoke();
                Debug.Log("Settings Popup closed.");
            }
        }
    } // End Namespace
}
