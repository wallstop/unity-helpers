namespace WallstopStudios.UnityHelpers.Visuals.UIToolkit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.UIElements;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Helper;

    public sealed class MultiFileSelectorElement : VisualElement
    {
        private const string DefaultRootPath = "Assets";

        public event Action<List<string>> OnFilesSelected;
        public event Action OnCancelled;

        private readonly TextField _pathField;
        private readonly Button _upButton;
        private readonly VisualElement _fileListContent;
        private readonly HashSet<string> _fileExtensionsToDisplay;
        private readonly List<string> _selectedFilePaths = new();

        private string _currentDirectory;

        public MultiFileSelectorElement(string initialPath, string[] filterExtensions)
        {
            _fileExtensionsToDisplay =
                filterExtensions?.Select(ext => ext).ToHashSet(StringComparer.OrdinalIgnoreCase)
                ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            style.position = Position.Absolute;
            style.left = style.top = style.right = style.bottom = 0;
            style.backgroundColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f, 0.9f));
            style.paddingLeft = style.paddingRight = style.paddingTop = style.paddingBottom = 20;
            style.alignItems = Align.Center;
            style.justifyContent = Justify.Center;

            VisualElement contentBox = new()
            {
                style =
                {
                    width = new Length(80, LengthUnit.Percent),
                    height = new Length(80, LengthUnit.Percent),
                    maxWidth = 700,
                    maxHeight = 500,
                    backgroundColor = new StyleColor(Color.gray),
                    paddingLeft = 10,
                    paddingRight = 10,
                    paddingTop = 10,
                    paddingBottom = 10,
                    flexDirection = FlexDirection.Column,
                    borderTopLeftRadius = 5,
                    borderTopRightRadius = 5,
                    borderBottomLeftRadius = 5,
                    borderBottomRightRadius = 5,
                },
            };
            Add(contentBox);

            VisualElement headerControls = new()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom = 5,
                    alignItems = Align.Center,
                },
            };
            Button assetsFolderButton = new(() => NavigateTo(DefaultRootPath))
            {
                text = "Assets",
                style = { width = 60, marginRight = 5 },
            };
            _upButton = new Button(NavigateUp)
            {
                text = "Up",
                style = { width = 40, marginRight = 5 },
            };
            _pathField = new TextField(null)
            {
                isReadOnly = true,
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    marginRight = 5,
                },
            };

            headerControls.Add(assetsFolderButton);
            headerControls.Add(_upButton);
            headerControls.Add(_pathField);
            contentBox.Add(headerControls);

            // TODO: Custom styling
            ScrollView fileListView = new(ScrollViewMode.Vertical)
            {
                style =
                {
                    flexGrow = 1,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderBottomColor = Color.black,
                    borderTopColor = Color.black,
                    borderLeftColor = Color.black,
                    borderRightColor = Color.black,
                    marginBottom = 5,
                    backgroundColor = Color.white * 0.15f,
                },
            };
            _fileListContent = new VisualElement();
            fileListView.Add(_fileListContent);
            contentBox.Add(fileListView);

            VisualElement footerControls = new()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexEnd,
                    marginTop = 5,
                },
            };
            Button cancelButton = new(() => OnCancelled?.Invoke())
            {
                text = "Cancel",
                style = { marginRight = 10 },
            };
            Button confirmButton = new(ConfirmSelection) { text = "Add Selected" };
            footerControls.Add(cancelButton);
            footerControls.Add(confirmButton);
            contentBox.Add(footerControls);

            string validInitialPath = initialPath;
            if (
                string.IsNullOrEmpty(validInitialPath)
                || !Directory.Exists(Path.Combine(Application.dataPath, "..", validInitialPath))
            )
            {
                validInitialPath = DefaultRootPath;
            }
            NavigateTo(validInitialPath);
        }

        private void NavigateTo(string path)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
            string projectRootPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            if (!fullPath.StartsWith(projectRootPath, StringComparison.OrdinalIgnoreCase))
            {
                fullPath = projectRootPath;
            }

            string assetsFullPath = Path.GetFullPath(Application.dataPath);
            if (
                DefaultRootPath == "Assets"
                && !fullPath.StartsWith(assetsFullPath, StringComparison.OrdinalIgnoreCase)
                && fullPath.StartsWith(projectRootPath)
            )
            {
                fullPath = assetsFullPath;
            }

            if (!Directory.Exists(fullPath))
            {
                Debug.LogWarning($"Directory does not exist: {fullPath}. Resetting to Assets.");
                _currentDirectory = assetsFullPath;
            }
            else
            {
                _currentDirectory = fullPath;
            }

            if (_currentDirectory.Equals(projectRootPath, StringComparison.OrdinalIgnoreCase))
            {
                _pathField.SetValueWithoutNotify("Project Root");
            }
            else if (
                _currentDirectory.StartsWith(projectRootPath, StringComparison.OrdinalIgnoreCase)
            )
            {
                _pathField.SetValueWithoutNotify(
                    _currentDirectory.Substring(projectRootPath.Length + 1).SanitizePath()
                );
            }
            else
            {
                _pathField.SetValueWithoutNotify(_currentDirectory.SanitizePath());
            }

            PopulateFileList();
            DirectoryInfo parentDirectory = Directory.GetParent(_currentDirectory);
            _upButton.SetEnabled(
                !_currentDirectory.Equals(assetsFullPath, StringComparison.OrdinalIgnoreCase)
                    && parentDirectory != null
                    && parentDirectory.FullName.Length >= projectRootPath.Length
            );
        }

        private void NavigateUp()
        {
            if (string.IsNullOrEmpty(_currentDirectory))
            {
                return;
            }

            DirectoryInfo directoryParent = Directory.GetParent(_currentDirectory);
            if (directoryParent != null)
            {
                string projectRootPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

                if (
                    directoryParent.FullName.Length < projectRootPath.Length
                    || _currentDirectory.Equals(
                        Path.GetFullPath(Application.dataPath),
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    Debug.Log("Cannot navigate above Assets folder.");
                    NavigateTo(DefaultRootPath);
                    return;
                }
                NavigateTo(directoryParent.FullName.Substring(projectRootPath.Length + 1));
            }
        }

        private void PopulateFileList()
        {
            _fileListContent.Clear();
            try
            {
                foreach (string dirPath in Directory.GetDirectories(_currentDirectory).Ordered())
                {
                    string dirName = Path.GetFileName(dirPath);
                    Button dirButton = new(() =>
                        NavigateTo(
                            dirPath.Substring(
                                Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Length
                                    + 1
                            )
                        )
                    )
                    {
                        text = $"📁 {dirName}",
                        style = { unityTextAlign = TextAnchor.MiddleLeft, marginBottom = 1 },
                    };
                    _fileListContent.Add(dirButton);
                }

                foreach (string filePath in Directory.GetFiles(_currentDirectory).Ordered())
                {
                    if (_fileExtensionsToDisplay.Count > 0)
                    {
                        string extension = Path.GetExtension(filePath);
                        if (!_fileExtensionsToDisplay.Contains(extension))
                        {
                            continue;
                        }
                    }

                    VisualElement fileItem = new()
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Row,
                            alignItems = Align.Center,
                            marginBottom = 1,
                        },
                    };
                    Toggle toggle = new()
                    {
                        value = _selectedFilePaths.Contains(filePath),
                        style = { marginRight = 5 },
                    };
                    toggle.RegisterValueChangedCallback(evt =>
                    {
                        if (evt.newValue)
                        {
                            _selectedFilePaths.Add(filePath);
                        }
                        else
                        {
                            _selectedFilePaths.Remove(filePath);
                        }
                    });

                    Label label = new(Path.GetFileName(filePath));
                    label.RegisterCallback<PointerDownEvent, Toggle>(
                        (_, context) => context.value = !context.value,
                        toggle
                    );

                    fileItem.Add(toggle);
                    fileItem.Add(label);
                    _fileListContent.Add(fileItem);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error accessing path {_currentDirectory}: {ex.Message}");
                _fileListContent.Add(new Label($"Error reading directory: {ex.Message}"));
            }
        }

        private void ConfirmSelection()
        {
            OnFilesSelected?.Invoke(_selectedFilePaths.ToList());
        }

        public void ResetAndShow(string newInitialPath)
        {
            _selectedFilePaths.Clear();
            NavigateTo(newInitialPath);
        }
    }
}
