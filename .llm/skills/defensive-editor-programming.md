# Skill: Defensive Editor Programming

<!-- trigger: editor defensive, serializedproperty, asset safety, editor validation | Editor code - handle Unity Editor edge cases | Core -->

**Trigger**: When writing Unity Editor code (custom inspectors, property drawers, editor windows, asset processors). Editor code requires additional defensive patterns beyond standard defensive programming.

---

## When to Use This Skill

Use this skill when:

- Creating custom PropertyDrawers or Editors
- Working with SerializedProperty and SerializedObject
- Loading or manipulating assets via AssetDatabase
- Caching editor state that may be invalidated by Unity reloads
- Handling serialization in editor contexts
- Writing event/callback handlers in editor tools

---

## Why Editor Code Needs Extra Defense

Editor code is especially vulnerable because:

- User can modify inspector values at any time
- SerializedProperty may reference destroyed objects
- Assets may be deleted mid-operation
- Unity reload (domain reload, assembly reload) may invalidate cached state
- Selection can change during multi-frame operations
- Undo/Redo can restore unexpected states

---

## SerializedProperty Safety

### Safe Property Access

```csharp
// ✅ Safe property access
public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
{
    if (property == null)
    {
        return;
    }

    if (property.serializedObject == null || property.serializedObject.targetObject == null)
    {
        EditorGUI.LabelField(position, label, new GUIContent("(Missing Object)"));
        return;
    }

    EditorGUI.BeginProperty(position, label, property);
    try
    {
        // Draw property...
    }
    finally
    {
        EditorGUI.EndProperty();
    }
}
```

### Safe Property Height Calculation

```csharp
public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
{
    if (property == null || property.serializedObject == null)
    {
        return EditorGUIUtility.singleLineHeight;
    }

    // Calculate actual height...
    return EditorGUIUtility.singleLineHeight;
}
```

### Safe Nested Property Access

```csharp
// ✅ Safe nested property access
private SerializedProperty GetNestedProperty(SerializedProperty parent, string path)
{
    if (parent == null || string.IsNullOrEmpty(path))
    {
        return null;
    }

    SerializedProperty nested = parent.FindPropertyRelative(path);
    // FindPropertyRelative returns null if not found - no exception
    return nested;
}
```

---

## Safe Asset Operations

### Safe Asset Loading

```csharp
// ✅ Safe asset loading
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

### Safe Asset Iteration

```csharp
// ✅ Safe asset iteration
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

### Safe Asset Creation

```csharp
// ✅ Safe asset creation with directory validation
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

## Cache Invalidation Safety

### Safe Cached Value Access

```csharp
// ✅ Safe cached value access
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

### Domain Reload Safe Caching

```csharp
// ✅ Handle domain reload
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

## Serialization Resilience in Editor

### Defensive Deserialization

```csharp
// ✅ Defensive deserialization
public T DeserializeSafe<T>(string json) where T : class, new()
{
    if (string.IsNullOrEmpty(json))
    {
        return new T();
    }

    try
    {
        T result = JsonUtility.FromJson<T>(json);
        return result ?? new T();
    }
    catch (Exception ex)
    {
        Debug.LogWarning($"[Serialization] Failed to deserialize {typeof(T).Name}: {ex.Message}");
        return new T();
    }
}
```

### Validate After Deserialization

```csharp
// ✅ Validate after deserialization
public void OnAfterDeserialize()
{
    // Repair potentially corrupt data
    if (_items == null)
    {
        _items = new List<Item>();
    }

    // Remove null entries that may have resulted from missing references
    _items.RemoveAll(item => item == null);

    // Clamp values to valid ranges
    _health = Mathf.Clamp(_health, 0, _maxHealth);

    // Ensure required references
    if (string.IsNullOrEmpty(_id))
    {
        _id = System.Guid.NewGuid().ToString();
    }
}
```

### EditorPrefs Safety

```csharp
// ✅ Safe EditorPrefs access
public static T GetEditorPref<T>(string key, T defaultValue)
{
    if (string.IsNullOrEmpty(key))
    {
        return defaultValue;
    }

    try
    {
        if (typeof(T) == typeof(string))
        {
            return (T)(object)EditorPrefs.GetString(key, (string)(object)defaultValue);
        }
        if (typeof(T) == typeof(int))
        {
            return (T)(object)EditorPrefs.GetInt(key, (int)(object)defaultValue);
        }
        if (typeof(T) == typeof(float))
        {
            return (T)(object)EditorPrefs.GetFloat(key, (float)(object)defaultValue);
        }
        if (typeof(T) == typeof(bool))
        {
            return (T)(object)EditorPrefs.GetBool(key, (bool)(object)defaultValue);
        }

        return defaultValue;
    }
    catch
    {
        return defaultValue;
    }
}
```

---

## Event/Callback Safety in Editor

### Safe Event Invocation

```csharp
// ✅ Safe event invocation
public void RaiseValueChanged(int newValue)
{
    if (OnValueChanged == null)
    {
        return;
    }

    // Copy delegate to avoid race conditions
    Action<int> handler = OnValueChanged;

    try
    {
        handler.Invoke(newValue);
    }
    catch (Exception ex)
    {
        // Never let subscriber exceptions crash the publisher
        Debug.LogError($"[{nameof(MyClass)}] Exception in OnValueChanged handler: {ex}");
    }
}
```

### Safe Multi-cast Delegate Invocation

```csharp
// ✅ Safe multi-cast delegate invocation
public void RaiseEvent()
{
    Delegate[] handlers = OnEvent?.GetInvocationList();
    if (handlers == null)
    {
        return;
    }

    for (int i = 0; i < handlers.Length; i++)
    {
        try
        {
            ((Action)handlers[i]).Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{nameof(MyClass)}] Exception in event handler: {ex}");
        }
    }
}
```

### Safe Editor Callback Registration

```csharp
// ✅ Safe callback registration with cleanup
public class MyEditorWindow : EditorWindow
{
    private void OnEnable()
    {
        // Always unsubscribe first to prevent double subscription
        Selection.selectionChanged -= OnSelectionChanged;
        Selection.selectionChanged += OnSelectionChanged;

        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChanged;
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnSelectionChanged()
    {
        // Validate window state before processing
        if (this == null)
        {
            return;
        }

        Repaint();
    }
}
```

---

## Consistent Display Label Normalization

When editor UI renders a fallback label (e.g., `(Option N)` for empty strings), **every** code path that references that label must apply the same normalization — not just rendering, but also search, filter, suggestion, and selection. Centralize the fallback in a single helper (e.g., `GetNormalizedDisplayLabel`) and never use the raw label in comparison paths.

**Rule:** If any code path transforms a value for display, audit all other paths that read the same value and apply the identical transform. This covers rendering, search/filter, autocomplete/suggestion, selection/resolution, and keyboard navigation labels.

---

## Quick Checklist for Editor Code

Before submitting editor code, verify:

- [ ] SerializedProperty null-checked before access
- [ ] SerializedObject.targetObject validated
- [ ] Asset paths validated before AssetDatabase operations
- [ ] Selection validated (may be empty or contain nulls)
- [ ] Cache invalidation handles domain reload
- [ ] Cache invalidation handles target object changes
- [ ] Event handlers wrapped in try-catch
- [ ] Event subscriptions cleaned up in OnDisable
- [ ] Deserialization handles corrupt/missing data
- [ ] EditorPrefs access handles missing keys
- [ ] Display label fallbacks applied consistently across all code paths

---

## Related Skills

- [defensive-programming](./defensive-programming.md) - Core defensive patterns for all code
- [create-editor-tool](./create-editor-tool.md) - Editor tool creation patterns
- [create-test](./create-test.md) - Test edge cases and error conditions
- [create-property-drawer](./create-property-drawer.md) - PropertyDrawer patterns including display label normalization
