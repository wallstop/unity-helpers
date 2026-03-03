# Editor Code Sample: Cache Invalidation Safety

<!-- parent: defensive-editor-programming.md -->

Code patterns for safely caching values in Unity Editor code with proper invalidation.

---

## Safe Cached Value Access

```csharp
// Safe cached value access
private SerializedProperty _cachedProperty;
private Object _cachedTarget;

private SerializedProperty GetProperty(SerializedObject serializedObject)
{
    if (serializedObject == null)
    {
        _cachedProperty = null;
        _cachedTarget = null;
        return null;
    }

    // Invalidate cache if target changed
    if (_cachedTarget != serializedObject.targetObject)
    {
        _cachedProperty = null;
        _cachedTarget = serializedObject.targetObject;
    }

    if (_cachedProperty == null)
    {
        _cachedProperty = serializedObject.FindProperty("_fieldName");
    }

    return _cachedProperty;
}
```

---

## Domain Reload Safe Caching

```csharp
// Handle domain reload
[InitializeOnLoad]
public static class EditorCache
{
    private static Dictionary<string, object> _cache;

    static EditorCache()
    {
        // Cache is cleared on domain reload - reinitialize
        _cache = new Dictionary<string, object>();
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode ||
            state == PlayModeStateChange.ExitingPlayMode)
        {
            // Clear cache when entering/exiting play mode
            _cache.Clear();
        }
    }

    public static bool TryGetCached<T>(string key, out T value)
    {
        value = default;
        if (_cache == null || string.IsNullOrEmpty(key))
        {
            return false;
        }

        if (_cache.TryGetValue(key, out object cached) && cached is T typed)
        {
            value = typed;
            return true;
        }

        return false;
    }
}
```

---

## Key Points

- Track the target object to detect when cache becomes invalid
- Clear cached properties when target changes
- Use `[InitializeOnLoad]` to reinitialize after domain reload
- Clear caches on play mode transitions
- Always null-check cache before access
- Validate keys before dictionary lookups

---

## See Also

- [SerializedProperty Safety](./editor-serialized-property-safety.md) - Safe property access patterns
- [Serialization Safety](./editor-serialization-safety.md) - Safe deserialization and EditorPrefs
- [Asset Operations Safety](./editor-asset-operations-safety.md) - Safe asset loading and creation
- [Event and Callback Safety](./editor-event-callback-safety.md) - Safe event handling
