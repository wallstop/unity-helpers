UI Toolkit – MultiFile Selector (Editor)

A minimal EditorWindow showcasing `MultiFileSelectorElement` for fast in-Editor multi-file selection with breadcrumbs, filtering, virtualization, and persistence.

How to use

- Import the sample, then open: `Window > Unity Helpers > MultiFile Selector Sample`.
- Pick extensions and select multiple files; results are logged to the Console.

What it shows

- Creating and adding `MultiFileSelectorElement` to an EditorWindow.
- Hooks for allocation-free `OnFilesSelectedReadOnly` (and legacy `OnFilesSelected`) plus `OnCancelled`.
- Persistence of last directory and search filter across sessions.

Features

Core Functionality:

- Virtualized ListView for large directories (stays responsive with thousands of files)
- Folder-first ordering with live search filtering
- HashSet-backed selection with Select All / Clear / Invert helpers
- Clickable breadcrumbs and double-click directory navigation
- Constrained to Unity project Assets folder

Events:

- `OnFilesSelectedReadOnly` - Zero-allocation callback with `IReadOnlyCollection<string>`
- `OnFilesSelected` (legacy) - Allocates a new `List<string>` copy
- `OnCancelled` - Invoked when user cancels without confirming

Persistence System:

The selector can persist the last directory and search filter across sessions using `EditorPrefs` (or `PlayerPrefs` at runtime).

```csharp
// Enable persistence with a unique key
var selector = new MultiFileSelectorElement(
    initialPath: "Assets/Animations",
    filterExtensions: new[] { ".anim" },
    persistenceKey: "AnimationViewer"  // Enables persistence
);
```

- **persistenceKey**: Optional unique identifier for this selector instance
- When provided, the selector remembers the last visited directory and search text
- Scoped persistence prevents conflicts between different selectors

Cleanup stale entries (useful for editor tools with many selectors):

```csharp
// Remove entries older than 30 days
MultiFileSelectorElement.CleanupStalePersistenceEntries(TimeSpan.FromDays(30));
```

Example: Basic Usage

```csharp
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using WallstopStudios.UnityHelpers.Visuals.UIToolkit;

public sealed class AnimationPickerWindow : EditorWindow
{
    [MenuItem("Window/Animation Picker")]
    public static void ShowWindow() => GetWindow<AnimationPickerWindow>();

    void CreateGUI()
    {
        MultiFileSelectorElement selector = new(
            initialPath: "Assets",
            filterExtensions: new[] { ".anim", ".controller" },
            persistenceKey: "AnimationPicker"
        );

        selector.OnFilesSelectedReadOnly += files =>
        {
            Debug.Log($"Selected {files.Count} animation files");
            foreach (string path in files)
            {
                Debug.Log($"  - {path}");
            }
        };

        selector.OnCancelled += () => Close();

        rootVisualElement.Add(selector);
    }
}
```

When to Use

Use `MultiFileSelectorElement` when you need:

- In-UI multi-file selection (native file dialog is too limited)
- Custom selection semantics or filtering
- Persistence of user's last location
- Zero-allocation file selection callbacks
