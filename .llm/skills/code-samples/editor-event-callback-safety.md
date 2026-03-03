# Editor Code Sample: Event and Callback Safety

<!-- parent: defensive-editor-programming.md -->

Code patterns for safely handling events and callbacks in Unity Editor code.

---

## Safe Event Invocation

```csharp
// Safe event invocation
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

---

## Safe Multi-cast Delegate Invocation

```csharp
// Safe multi-cast delegate invocation
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

---

## Safe Editor Callback Registration

```csharp
// Safe callback registration with cleanup
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

## Key Points

- Copy delegate to local variable before invoking (thread safety)
- Wrap event handler invocation in try-catch
- Never let subscriber exceptions crash the publisher
- Unsubscribe before subscribing to prevent double subscription
- Always unsubscribe in OnDisable
- Validate `this == null` in callbacks (Unity object may be destroyed)
- Use GetInvocationList for multi-cast delegates to handle individual failures

---

## See Also

- [SerializedProperty Safety](./editor-serialized-property-safety.md) - Safe property access patterns
- [Serialization Safety](./editor-serialization-safety.md) - Safe deserialization and EditorPrefs
- [Asset Operations Safety](./editor-asset-operations-safety.md) - Safe asset loading and creation
- [Cache Invalidation Safety](./editor-cache-invalidation-safety.md) - Proper cache management
