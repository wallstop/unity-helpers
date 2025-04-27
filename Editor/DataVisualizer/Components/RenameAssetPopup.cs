namespace WallstopStudios.UnityHelpers.Editor.DataVisualizer.Components
{
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    // For Action

    public class RenameAssetPopup : EditorWindow
    {
        private string _originalPath;
        private string _originalNameWithoutExtension;
        private Action<bool> _onCompleteCallback; // Action<wasSuccessful>
        private TextField _nameTextField;
        private Label _errorLabel;

        // Method to open the window
        public static void ShowWindow(string assetPath, Action<bool> onComplete)
        {
            RenameAssetPopup window = GetWindow<RenameAssetPopup>(true, "Rename Asset", true); // Create modal, utility window
            window.minSize = new Vector2(350, 100);
            window.maxSize = new Vector2(350, 100);
            window._originalPath = assetPath;
            window._originalNameWithoutExtension = Path.GetFileNameWithoutExtension(assetPath);
            window._onCompleteCallback = onComplete;
            // window.ShowModalUtility(); // ShowUtility opens non-modal, use GetWindow(utility=true) for modal-like focus
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            // Instructions
            root.Add(
                new Label("Enter new name (without extension):") { style = { marginBottom = 5 } }
            );

            // Text Field
            _nameTextField = new TextField
            {
                value = _originalNameWithoutExtension,
                style = { marginBottom = 5 },
            };
            // Select text initially for easy replacement
            _nameTextField.schedule.Execute(() => _nameTextField.SelectAll()).ExecuteLater(50);
            root.Add(_nameTextField);

            // Error Label
            _errorLabel = new Label
            {
                name = "error-label",
                style =
                {
                    color = Color.red,
                    height = 18,
                    display = DisplayStyle.None,
                },
            };
            root.Add(_errorLabel);

            // Buttons Container
            var buttonContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexEnd,
                    marginTop = 5,
                },
            };
            root.Add(buttonContainer);

            // Cancel Button
            var cancelButton = new Button(Cancel) { text = "Cancel", style = { marginRight = 5 } };
            buttonContainer.Add(cancelButton);

            // Rename Button
            var renameButton = new Button(ConfirmRename) { text = "Rename" };
            buttonContainer.Add(renameButton);
        }

        private void ConfirmRename()
        {
            _errorLabel.style.display = DisplayStyle.None; // Hide previous error
            string newName = _nameTextField.value;

            // --- Validation ---
            if (string.IsNullOrWhiteSpace(newName))
            {
                ShowError("Name cannot be empty.");
                return;
            }
            // Check for invalid filename characters (basic check)
            if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                ShowError("Name contains invalid characters.");
                return;
            }
            if (newName.Equals(_originalNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
            {
                ShowError("New name is the same as the old name.");
                return;
            }
            // --- End Validation ---


            // --- Check Uniqueness ---
            string directory = Path.GetDirectoryName(_originalPath);
            string newPath = Path.Combine(directory, newName + Path.GetExtension(_originalPath))
                .Replace('\\', '/'); // Add extension back

            string validationError = AssetDatabase.ValidateMoveAsset(_originalPath, newPath);

            if (!string.IsNullOrEmpty(validationError))
            {
                // Name collision or other validation error
                ShowError($"Invalid name: {validationError}");
                return;
            }

            // --- Perform Rename ---
            string error = AssetDatabase.RenameAsset(_originalPath, newName);
            if (string.IsNullOrEmpty(error)) // Rename successful if error string is empty/null
            {
                Debug.Log($"Asset renamed successfully to: {newName}");
                // AssetDatabase.SaveAssets(); // Good practice to save after rename
                // AssetDatabase.Refresh(); // Refresh might be needed

                // Close window and invoke callback
                ClosePopup(true);
            }
            else
            {
                Debug.LogError($"Asset rename failed: {error}");
                ShowError($"Failed to rename: {error}");
            }
        }

        private void ShowError(string message)
        {
            _errorLabel.text = message;
            _errorLabel.style.display = DisplayStyle.Flex;
        }

        private void Cancel()
        {
            ClosePopup(false);
        }

        private void ClosePopup(bool success)
        {
            _onCompleteCallback?.Invoke(success); // Notify main window
            this.Close(); // Close this popup window
        }

        // Ensure callback is invoked even if window is closed manually
        private void OnDestroy()
        {
            // Check if callback exists and maybe hasn't been called yet? Difficult to track perfectly.
            // Usually, just letting it close is fine. Callback is primarily for success/cancel paths.
        }
    }
}
