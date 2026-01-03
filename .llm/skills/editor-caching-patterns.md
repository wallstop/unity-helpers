# Skill: Editor Caching Patterns

<!-- trigger: editor cache, inspector cache, drawer cache, LRU | Caching strategies for Editor code | Core -->

**Trigger**: When implementing caching in Unity Editor tools, inspectors, or property drawers to minimize allocations.

---

## When to Use This Skill

Use this skill when:

- Creating or modifying PropertyDrawers, Custom Inspectors, or EditorWindows
- Optimizing editor code that runs frequently (every frame)
- Implementing bounded caches with eviction policies
- Sharing cache resources across multiple editor components

---

## Why Caching Matters

Inspectors and drawers run **every frame**. Without caching:

- `new GUIContent()` allocates every frame
- String operations create garbage
- Dictionary lookups may create allocations
- Texture creation is expensive

---

## Basic Caching Patterns

### Static Caches

```csharp
// CORRECT - Static caches
private static readonly Dictionary<string, float> HeightCache = new(StringComparer.Ordinal);
private static readonly GUIContent ReusableContent = new();

// CORRECT - Pool expensive objects
private static readonly WallstopGenericPool<List<string>> ListPool = new(
    () => new List<string>(),
    onRelease: list => list.Clear()
);

// INCORRECT - Allocates every frame
public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
{
    GUIContent content = new GUIContent("Label"); // Allocation!
    List<string> items = new List<string>();       // Allocation!
}
```

### Reusable GUIContent

```csharp
private static readonly GUIContent ReusableContent = new();

public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
{
    // Reuse instead of allocating new
    ReusableContent.text = "My Label";
    ReusableContent.tooltip = "My Tooltip";
    ReusableContent.image = null;

    EditorGUI.LabelField(position, ReusableContent);
}
```

---

## Shared Caches (DRY Principle)

When multiple drawers/inspectors need the same cache, use `EditorCacheHelper`:

```csharp
// CORRECT - Use shared cache helper
using WallstopStudios.UnityHelpers.Editor.Core.Helper;

// In any drawer:
string display = EditorCacheHelper.GetCachedIntString(index);
string pageLabel = EditorCacheHelper.GetPaginationLabel(page, total);
Texture2D solidTex = EditorCacheHelper.GetSolidTexture(color);

// INCORRECT - Duplicating caches across multiple files
// Drawer1.cs
private static readonly Dictionary<int, string> IntToStringCache = new();

// Drawer2.cs (duplicate!)
private static readonly Dictionary<int, string> IntToStringCache = new();
```

### Available EditorCacheHelper Methods

| Method                                              | Purpose                                     |
| --------------------------------------------------- | ------------------------------------------- |
| `GetCachedIntString(int)`                           | Cached integer-to-string conversion         |
| `GetPaginationLabel(int page, int total)`           | Cached "Page X / Y" strings                 |
| `GetSolidTexture(Color)`                            | Cached 1x1 solid color textures             |
| `AddToBoundedCache<K,V>(cache, key, value, max)`    | Add to bounded LRU cache with eviction      |
| `TryGetFromBoundedLRUCache<K,V>(cache, key, out v)` | Get from LRU cache (updates access order)   |

---

## Bounded LRU Caching Pattern

For custom bounded caches, use `EditorCacheHelper` LRU methods to prevent unbounded memory growth:

```csharp
private static readonly Dictionary<string, MyValue> MyCache = new();
private const int MaxCacheSize = 500;

public static MyValue GetOrCreate(string key)
{
    // LRU read - updates access order so frequently-used items stay cached
    if (EditorCacheHelper.TryGetFromBoundedLRUCache(MyCache, key, out MyValue cached))
    {
        return cached;
    }

    MyValue value = CreateValue(key);

    // LRU add - evicts least-recently-used when at capacity
    EditorCacheHelper.AddToBoundedCache(MyCache, key, value, MaxCacheSize);

    return value;
}
```

### LRU vs FIFO

**LRU (Least Recently Used)** is preferred over FIFO because it keeps frequently-accessed items in cache longer. Both reads and writes update an item's "recency", preventing hot items from being evicted.

### When to Use Bounded Caches

| Use Case                  | Recommendation                         |
| ------------------------- | -------------------------------------- |
| Integer-to-string         | Unbounded (limited key space)          |
| Path-to-asset lookups     | Bounded - paths can grow unboundedly   |
| Type-to-metadata          | Unbounded (limited types in project)   |
| User input caching        | Bounded - unpredictable input space    |
| Color-to-texture          | Bounded (many possible colors)         |

---

## Cache Invalidation Patterns

### Target-Based Invalidation

```csharp
private Object _lastTarget;
private SerializedProperty _cachedProperty;

private SerializedProperty GetProperty(SerializedObject so)
{
    if (so == null || so.targetObject == null)
    {
        _cachedProperty = null;
        _lastTarget = null;
        return null;
    }

    if (_lastTarget != so.targetObject)
    {
        _cachedProperty = null;
        _lastTarget = so.targetObject;
    }

    if (_cachedProperty == null)
    {
        _cachedProperty = so.FindProperty("_fieldName");
    }

    return _cachedProperty;
}
```

### Time-Based Invalidation

```csharp
private static readonly Dictionary<string, (float timestamp, object value)> TimeCache = new();
private const float CacheLifetimeSeconds = 5f;

public static T GetCachedOrCompute<T>(string key, Func<T> compute)
{
    float now = (float)EditorApplication.timeSinceStartup;

    if (TimeCache.TryGetValue(key, out var entry))
    {
        if (now - entry.timestamp < CacheLifetimeSeconds)
        {
            return (T)entry.value;
        }
    }

    T result = compute();
    TimeCache[key] = (now, result);
    return result;
}
```

### Domain Reload Handling

Static caches persist across domain reloads in some configurations. Handle this:

```csharp
[InitializeOnLoadMethod]
private static void ClearCachesOnDomainReload()
{
    EditorApplication.quitting += ClearAllCaches;
    AssemblyReloadEvents.beforeAssemblyReload += ClearAllCaches;
}

private static void ClearAllCaches()
{
    HeightCache.Clear();
    TypeCache.Clear();
    // Clear other static caches
}
```

---

## USS/UXML Style Loading

Style loading should also use caching:

```csharp
namespace WallstopStudios.UnityHelpers.Editor.Styles
{
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    using WallstopStudios.UnityHelpers.Core.Helper;

    public static class MyStyleLoader
    {
        private const string StylesRelativePath = "Editor/Styles/MyComponent/";
        private const string StylesFileName = "MyStyles.uss";

        private static StyleSheet _stylesStyleSheet;
        private static bool _initialized;

        public static StyleSheet Styles
        {
            get
            {
                EnsureInitialized();
                return _stylesStyleSheet;
            }
        }

        public static bool IsProSkin => EditorGUIUtility.isProSkin;

        public static void ApplyStyles(VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            EnsureInitialized();

            if (_stylesStyleSheet != null)
            {
                element.styleSheets.Add(_stylesStyleSheet);
            }
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            string stylesPath = DirectoryHelper.GetPackagePath(StylesRelativePath + StylesFileName);
            _stylesStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(stylesPath);
        }
    }
#endif
}
```

USS files go in `Editor/Styles/` with `.uss` extension.

---

## Common Editor Patterns

### Progress Bar for Long Operations

```csharp
private void ProcessAssets(List<string> assetPaths)
{
    int total = assetPaths.Count;
    try
    {
        for (int i = 0; i < total; i++)
        {
            string path = assetPaths[i];

            if (EditorUtility.DisplayCancelableProgressBar(
                "Processing Assets",
                $"Processing {Path.GetFileName(path)} ({i + 1}/{total})",
                (float)i / total))
            {
                break; // User cancelled
            }

            ProcessSingleAsset(path);
        }
    }
    finally
    {
        EditorUtility.ClearProgressBar();
    }
}
```

### Undo Support

```csharp
// Record object before modification
Undo.RecordObject(targetObject, "Descriptive Undo Name");
targetObject.someField = newValue;

// For multiple objects
Undo.RecordObjects(serializedObject.targetObjects, "Change Multiple");

// Group multiple operations
Undo.SetCurrentGroupName("Complex Operation");
int undoGroup = Undo.GetCurrentGroup();
// ... multiple changes ...
Undo.CollapseUndoOperations(undoGroup);

// Register newly created objects
GameObject newObj = new GameObject("Created Object");
Undo.RegisterCreatedObjectUndo(newObj, "Create Object");
```

### AssetDatabase Refresh

```csharp
// After modifying assets on disk
AssetDatabase.Refresh();

// With import options
AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

// Import specific asset
AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

// Save all assets
AssetDatabase.SaveAssets();
```

### Drag and Drop

```csharp
private void HandleDragAndDrop(Rect dropArea)
{
    Event evt = Event.current;

    switch (evt.type)
    {
        case EventType.DragUpdated:
        case EventType.DragPerform:
        {
            if (!dropArea.Contains(evt.mousePosition))
            {
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                foreach (Object draggedObject in DragAndDrop.objectReferences)
                {
                    string path = AssetDatabase.GetAssetPath(draggedObject);
                    // Process dropped object
                }
            }

            evt.Use();
            break;
        }
    }
}
```

### User Dialogs (with Suppression)

```csharp
private bool ConfirmAction(string message)
{
    if (SuppressUserPrompts)
    {
        return true; // Auto-confirm in batch/test mode
    }

    return EditorUtility.DisplayDialog(
        "Confirm Action",
        message,
        "Yes",
        "No"
    );
}

private string SelectFolder()
{
    if (SuppressUserPrompts)
    {
        return null;
    }

    return EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
}
```

---

## Performance Profiling Tips

### Measuring Cache Effectiveness

```csharp
#if UNITY_EDITOR && DEVELOPMENT_BUILD
private static int _cacheHits;
private static int _cacheMisses;

public static void LogCacheStats()
{
    float hitRate = _cacheHits / (float)(_cacheHits + _cacheMisses) * 100;
    Debug.Log($"Cache hit rate: {hitRate:F1}% ({_cacheHits} hits, {_cacheMisses} misses)");
}
#endif
```

### Using Unity Profiler

```csharp
using Unity.Profiling;

private static readonly ProfilerMarker DrawerMarker = new("MyDrawer.OnGUI");

public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
{
    using (DrawerMarker.Auto())
    {
        // Drawing code
    }
}
```

---

## Related Skills

- [create-editor-tool](./create-editor-tool.md) - EditorWindows and Custom Inspectors
- [create-property-drawer](./create-property-drawer.md) - PropertyDrawer creation
- [high-performance-csharp](./high-performance-csharp.md) - General performance patterns
- [use-pooling](./use-pooling.md) - Object pooling strategies
- [memory-allocation-traps](./memory-allocation-traps.md) - Common allocation pitfalls
