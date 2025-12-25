Logging – Tag Formatter Demo

Showcases `UnityLogTagFormatter`, the logging extension methods (`this.Log`, `this.LogWarn`, etc.), and how to register custom decorations plus runtime toggles.

How to use

- Open `Scenes/LoggingDemo.unity` and press Play.
- Use the on-screen controls to enable/disable global logging, toggle pretty output, and emit info/warn/error logs.
- Edit the `LoggingDemoController` fields (NPC callsign, status label, pretty toggle) to see how decorator registration affects output.

What it shows

- `[RuntimeInitializeOnLoadMethod]` bootstrap registering custom tag decorators (e.g., `npc` and `status=`).
- Runtime UI toggles calling `GlobalEnableLogging`, `DisableLogging`, and `SetGlobalLoggingEnabled`.
- Usage of `this.Log`, `this.LogWarn`, and `this.LogError` with custom formatting tags and pretty-mode toggles.

Built-in Format Tags

The formatter includes these decorations by default:

| Tag | Aliases | Effect | Example |
|-----|---------|--------|---------|
| Bold | `b`, `bold`, `!` | `<b>text</b>` | `$"{value:b}"` |
| Italic | `i`, `italic`, `_` | `<i>text</i>` | `$"{value:i}"` |
| Color | `#hex`, `#name`, `color=value` | `<color=X>text</color>` | `$"{value:#red}"` or `$"{value:color=FF0000}"` |
| Size | `size=N`, or just `N` | `<size=N>text</size>` | `$"{value:size=18}"` or `$"{value:24}"` |
| JSON | `json` | Serializes object to JSON | `$"{obj:json}"` |

Combine multiple tags with commas: `$"{value:b,#red,24}"` produces bold, red, size-24 text.

Color names use Unity's built-in Color properties (red, green, blue, cyan, magenta, yellow, white, black, gray, etc.).

Example: Custom Decorator Registration

```csharp
using WallstopStudios.UnityHelpers.Core.Helper.Logging;

public static class LoggingBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        UnityLogTagFormatter formatter = new();

        // Simple exact-match decorator
        formatter.AddDecoration(
            match: "npc",
            format: value => $"<color=cyan>[NPC]</color> {value}",
            tag: "NPC"
        );

        // Predicate-based decorator for dynamic tags
        formatter.AddDecoration(
            predicate: tag => tag.StartsWith("status="),
            format: (tag, value) =>
            {
                string status = tag.Substring("status=".Length);
                return $"<b>[{status}]</b> {value}";
            },
            tag: "Status",
            priority: 0,
            editorOnly: true
        );
    }
}
```

Example: Using the Formatter

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Extension;

public class Enemy : MonoBehaviour
{
    void Start()
    {
        // Extension method logs with context
        this.Log($"Enemy spawned at {transform.position:b}");
        this.LogWarn($"Health low: {health:#red,b}");
        this.LogError($"Critical error: {error:!,#FF0000}");

        // With pretty mode (adds timestamp, thread, component info)
        this.Log($"Debug info: {data:json}", pretty: true);
    }
}
```

Pretty Mode Output Format

When `pretty: true`, logs include contextual information:

```text
12.34|GameObjectName[ComponentType]|Your message here
```

For background threads:

```text
12.34|worker#5|GameObjectName[ComponentType]|Your message here
```
