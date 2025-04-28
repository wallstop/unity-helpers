namespace WallstopStudios.UnityHelpers.Editor.DataVisualizer.Components
{
    using System;
    using System.IO;
    using Core.Extension;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    public sealed class RenameAssetPopup : EditorWindow
    {
        private string _originalPath;
        private string _originalNameWithoutExtension;
        private Action<bool> _onCompleteCallback;
        private TextField _nameTextField;
        private Label _errorLabel;

        public static void ShowWindow(string assetPath, Action<bool> onComplete)
        {
            RenameAssetPopup window = GetWindow<RenameAssetPopup>(true, "Rename Asset", true);
            window.minSize = new Vector2(350, 100);
            window.maxSize = new Vector2(350, 100);
            window._originalPath = assetPath;
            window._originalNameWithoutExtension = Path.GetFileNameWithoutExtension(assetPath);
            window._onCompleteCallback = onComplete;
            window.ShowModalUtility();
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            root.Add(
                new Label("Enter new name (without extension):") { style = { marginBottom = 5 } }
            );

            _nameTextField = new TextField
            {
                value = _originalNameWithoutExtension,
                style = { marginBottom = 5 },
            };
            _nameTextField.schedule.Execute(() => _nameTextField.SelectAll()).ExecuteLater(50);
            root.Add(_nameTextField);

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

            VisualElement buttonContainer = new()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexEnd,
                    marginTop = 5,
                },
            };
            root.Add(buttonContainer);

            Button cancelButton = new(Cancel) { text = "Cancel", style = { marginRight = 5 } };
            buttonContainer.Add(cancelButton);

            Button renameButton = new(ConfirmRename) { text = "Rename" };
            buttonContainer.Add(renameButton);
        }

        private void ConfirmRename()
        {
            _errorLabel.style.display = DisplayStyle.None;
            string newName = _nameTextField.value;

            if (string.IsNullOrWhiteSpace(newName))
            {
                ShowError("Name cannot be empty.");
                return;
            }
            if (0 <= newName.IndexOfAny(Path.GetInvalidFileNameChars()))
            {
                ShowError("Name contains invalid characters.");
                return;
            }
            if (newName.Equals(_originalNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
            {
                ShowError("New name is the same as the old name.");
                return;
            }
            string directory = Path.GetDirectoryName(_originalPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                ShowError("Unable to determine directory.");
                return;
            }
            string newPath = Path.Combine(directory, newName + Path.GetExtension(_originalPath))
                .Replace('\\', '/');

            string validationError = AssetDatabase.ValidateMoveAsset(_originalPath, newPath);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                ShowError($"Invalid name: {validationError}");
                return;
            }

            string error = AssetDatabase.RenameAsset(_originalPath, newName);
            if (string.IsNullOrWhiteSpace(error))
            {
                ClosePopup(true);
            }
            else
            {
                this.LogError($"Asset rename failed: {error}");
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
            _onCompleteCallback?.Invoke(success);
            Close();
        }
    }
}
