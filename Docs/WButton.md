# WButton Inspector Buttons

`WButtonAttribute` exposes arbitrary methods as actionable buttons inside the Unity inspector, mirroring Odin's button concept while integrating with Unity Helpers' styling and pagination infrastructure.

## Applying the Attribute

```csharp
using WallstopStudios.UnityHelpers.Core.Attributes;

public sealed class ExampleBehaviour : MonoBehaviour
{
    [WButton] // Uses the method name as button label.
    public void ResetPosition()
    {
        transform.position = Vector3.zero;
    }

    [WButton("Download Data", drawOrder: -2, historyCapacity: 3)]
    public async Task<string> FetchAsync(int id, CancellationToken token)
    {
        // long running work...
        return $"Fetched {id}";
    }
}
```

- **Display name** (optional) customises the button text. When omitted the method name is nicified.
- **Draw order** controls placement: values `>= -1` render above the default inspector, lower values render below it. Groups are paginated by draw order.
- **History capacity** overrides how many results are retained (defaults to the package-wide setting).

## Parameters & Input

- Primitive types, enums, `UnityEngine.Object` references, vectors, colours, quaternions, rects, and arrays are rendered inline for editing.
- Optional parameters honour C# default values. Nullable parameters expose a toggle for setting `null`.
- `params` arrays are supported through a size field and per-element editors.
- Unsupported types fall back to a JSON text box; the inspector attempts to deserialize input before invocation.
- Parameters of type `CancellationToken` are auto-injected and hidden from the UI. They enable the cancel button during long-running invocations.

## Priority Palette & Colours

- Every button can declare a `priority` string. The inspector resolves that key against the **WButton Priority Colors** palette in _Project Settings → Wallstop Studios → Unity Helpers_.
- Palette entries now store both the button background colour and the text colour. New priorities auto-pick a complimentary hue and a readable text colour, but you can tailor either value to match your branding.
- When a button does not specify a priority (or when the key is missing), the drawer falls back to the `Default` palette entry. Colours are applied directly to the button surface, not the surrounding container, so the priority scheme remains distinct.

## Execution Behaviour

- Methods execute on the main thread. Reflection delegates are cached for performance.
- Return types classify execution:
  - `void` – instant execution with success/failure messages.
  - `IEnumerator` – scheduled via an editor coroutine runner with cancellation support.
  - `Task` / `Task<T>` and `ValueTask` / `ValueTask<T>` – tracked asynchronously, marshalled back to the main thread on completion.
- When the return type produces a value, the most recent result (per target instance) is recorded. `UnityEngine.Object` references are shown as read-only object fields; other values use readable summaries or JSON.
- Errors add entries to the history list and log detailed exceptions to the console.

## Pagination & Styling

- Button groups are paginated using the **WButton Page Size** setting under _Project Settings → Wallstop Studios → Unity Helpers_.
- The same settings panel also exposes the history depth. Both values are clamped to sensible bounds.
- A fallback inspector (`WButtonInspector`) applies to all objects without a bespoke custom editor. Custom editors can opt-in by calling:

```csharp
private readonly Dictionary<int, WButtonPaginationState> pagination = new();

public override void OnInspectorGUI()
{
    WButtonGUI.DrawButtons(this, WButtonPlacement.Top, pagination);
    // editor UI...
    WButtonGUI.DrawButtons(this, WButtonPlacement.Bottom, pagination);
    WButtonInvocationController.ProcessTriggeredMethods(triggered); // optional manual handling
}
```

## Result History

- Each method maintains a per-target history ring buffer using the configured capacity (or attribute override).
- Entries display status (`[OK]`, `[ERR]`, `[CANCEL]`), a timestamp, and summary text. Errors include exception type and message.
- The cancel button terminates all in-flight invocations for that method across the current selection (when a cancellation token is available).

## Notes & Limitations

- Methods with `ref`, `out`, or pointer parameters are ignored (a warning is emitted).
- Generic methods are unsupported.
- When multiple objects are inspected simultaneously, parameter edits are applied to all targets; mixed values show the standard inspector "–" indicator.
- Ensure long-running logic that touches Unity objects still marshals back to the main thread as needed.
