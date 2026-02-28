# Editor Code Sample: Serialization Safety

<!-- parent: defensive-editor-programming.md -->

Code patterns for safely handling serialization and deserialization in Unity Editor code.

---

## Defensive Deserialization

```csharp
// Defensive deserialization
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

---

## Validate After Deserialization

Implement Unity's `ISerializationCallbackReceiver` interface to hook into the deserialization lifecycle. The `OnAfterDeserialize` method is called by Unity immediately after deserializing the object, making it the ideal place to validate and repair potentially corrupt data.

```csharp
// Implement ISerializationCallbackReceiver to validate after deserialization
// OnAfterDeserialize is called by Unity after the object is deserialized
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

---

## EditorPrefs Safety

```csharp
// Safe EditorPrefs access
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
    catch (Exception ex)
    {
        Debug.LogWarning($"[EditorPrefs] Failed to get key '{key}': {ex.Message}");
        return defaultValue;
    }
}
```

---

## Key Points

- Always provide fallback values for deserialization failures
- Use try-catch around JsonUtility.FromJson
- Implement `ISerializationCallbackReceiver.OnAfterDeserialize` to repair corrupt data
- Remove null entries from collections after deserialization
- Clamp numeric values to valid ranges
- Generate missing IDs or required values
- Validate EditorPrefs keys before access
- Return default values on any EditorPrefs exception

---

## See Also

- [SerializedProperty Safety](./editor-serialized-property-safety.md) - Safe property access patterns
- [Asset Operations Safety](./editor-asset-operations-safety.md) - Safe asset loading and creation
- [Cache Invalidation Safety](./editor-cache-invalidation-safety.md) - Proper cache management
- [Event and Callback Safety](./editor-event-callback-safety.md) - Safe event handling
