namespace Samples.UnityHelpers.UIToolkit.Editor
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    using WallstopStudios.UnityHelpers.Visuals.UIToolkit;

    /// <summary>
    /// Minimal EditorWindow that hosts the MultiFileSelectorElement.
    /// </summary>
    public sealed class MultiFileSelectorSampleWindow : EditorWindow
    {
        [MenuItem("Window/Unity Helpers/MultiFile Selector Sample")]
        public static void Open()
        {
            MultiFileSelectorSampleWindow window = GetWindow<MultiFileSelectorSampleWindow>();
            window.titleContent = new GUIContent("MultiFile Selector Sample");
            window.minSize = new Vector2(640f, 420f);
        }

        private void CreateGUI()
        {
            MultiFileSelectorElement selector = new MultiFileSelectorElement(
                initialPath: "Assets",
                filterExtensions: new[] { ".png", ".jpg", ".cs" },
                persistenceKey: "MultiFileSelectorSampleWindow"
            );
            selector.OnFilesSelected += OnFilesSelected;
            selector.OnCancelled += OnCancelled;
            rootVisualElement.Add(selector);
        }

        private void OnFilesSelected(List<string> files)
        {
            string first = files.Count > 0 ? files[0] : "<none>";
            Debug.Log($"Selected {files.Count} files. First: {first}");
        }

        private void OnCancelled()
        {
            Debug.Log("Selection cancelled.");
        }
    }
}
