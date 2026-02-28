# Editor Code Sample: Safe Asset Operations

<!-- parent: defensive-editor-programming.md -->

Code patterns for safely working with AssetDatabase and asset operations in Unity Editor.

---

## Safe Asset Loading

```csharp
// Safe asset loading
public static T LoadAssetSafe<T>(string path) where T : Object
{
    if (string.IsNullOrEmpty(path))
    {
        return null;
    }

    T asset = AssetDatabase.LoadAssetAtPath<T>(path);

    if (asset == null)
    {
        Debug.LogWarning($"[AssetLoader] Failed to load asset at: {path}");
    }

    return asset;
}
```

---

## Safe Asset Iteration

```csharp
// Safe asset iteration
public void ProcessSelectedAssets()
{
    Object[] selection = Selection.objects;
    if (selection == null || selection.Length == 0)
    {
        return;
    }

    for (int i = 0; i < selection.Length; i++)
    {
        Object obj = selection[i];
        if (obj == null)
        {
            continue;
        }

        string path = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(path))
        {
            continue;
        }

        ProcessAsset(path);
    }
}
```

---

## Safe Asset Creation

```csharp
// Safe asset creation with directory validation
public static bool TryCreateAsset<T>(T asset, string path, out string error) where T : Object
{
    error = null;

    if (asset == null)
    {
        error = "Asset is null";
        return false;
    }

    if (string.IsNullOrEmpty(path))
    {
        error = "Path is null or empty";
        return false;
    }

    string directory = Path.GetDirectoryName(path);
    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
    {
        try
        {
            Directory.CreateDirectory(directory);
        }
        catch (Exception ex)
        {
            error = $"Failed to create directory: {ex.Message}";
            return false;
        }
    }

    try
    {
        AssetDatabase.CreateAsset(asset, path);
        return true;
    }
    catch (Exception ex)
    {
        error = $"Failed to create asset: {ex.Message}";
        return false;
    }
}
```

---

## Key Points

- Validate paths before any AssetDatabase operation
- Selection.objects may be null or contain null entries
- AssetDatabase.GetAssetPath may return empty string
- Wrap asset creation in try-catch for exception safety
- Create directories before creating assets
- Use TryX pattern with out error for clear failure reporting

---

## See Also

- [SerializedProperty Safety](./editor-serialized-property-safety.md) - Safe property access patterns
- [Serialization Safety](./editor-serialization-safety.md) - Safe deserialization and EditorPrefs
- [Cache Invalidation Safety](./editor-cache-invalidation-safety.md) - Proper cache management
- [Event and Callback Safety](./editor-event-callback-safety.md) - Safe event handling
