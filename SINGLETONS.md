# Singleton Utilities (Runtime + ScriptableObject)

This package includes two lightweight, production‑ready singleton helpers that make global access patterns safe, consistent, and testable:

- `RuntimeSingleton<T>` — a component singleton that ensures one instance exists in play mode, optionally persists across scenes, and self‑initializes when first accessed.
- `ScriptableObjectSingleton<T>` — a configuration/data singleton backed by a single asset under `Resources/`, with an editor auto‑creator to keep assets present and correctly placed.

> ODIN compatibility: When Odin Inspector is present (`ODIN_INSPECTOR` defined), these types derive from `SerializedMonoBehaviour` / `SerializedScriptableObject` for richer serialization. Without Odin, they fall back to Unity base types. No code changes required.

Contents
- ODIN Compatibility
- When To Use / Not To Use
- RuntimeSingleton<T>
  - Lifecycle diagram, examples, pitfalls
- ScriptableObjectSingleton<T>
  - Lookup + auto‑creator diagrams, examples, tips
- Scenarios & Guidance
- Troubleshooting

<a id="odin-compatibility"></a>
## ODIN Compatibility

- With Odin installed (symbol `ODIN_INSPECTOR`), base classes inherit from `SerializedMonoBehaviour` and `SerializedScriptableObject` to enable serialization of complex types (dictionaries, polymorphic fields) with Odin drawers.
- Without Odin, bases inherit from Unity’s `MonoBehaviour`/`ScriptableObject` with no behavior change.

<a id="when-to-use"></a>
## When To Use

- `RuntimeSingleton<T>`
  - Cross‑scene services (thread dispatcher, audio router, global managers).
  - Utility components that should always be available via `T.Instance`.
  - Creating the instance on demand when not found in the scene.

- `ScriptableObjectSingleton<T>`
  - Global settings/configuration (graphics, audio, feature flags).
  - Data that should be edited as an asset and loaded via `Resources`.
  - Consistent project setup for teams (auto‑created asset on editor load).

<a id="when-not-to-use"></a>
## When Not To Use

- Prefer DI/service locators for heavily decoupled architectures requiring multiple implementations per environment, or for test seams where global state is undesirable.
- Avoid `RuntimeSingleton<T>` for ephemeral, per‑scene logic or objects that should be duplicated in additive scenes.
- Avoid `ScriptableObjectSingleton<T>` for save data or level‑specific data that should not live in Resources or should have multiple instances.

<a id="runtime-singleton"></a>
## `RuntimeSingleton<T>` Overview

- Access via `T.Instance` (creates a new `GameObject` named `"<Type>-Singleton"` and adds `T` if none exists; otherwise finds an existing active instance).
- `HasInstance` lets you check for an existing instance without creating one.
- `Preserve` (virtual, default `true`) controls `DontDestroyOnLoad`.
- Handles duplicate detection and cleans up instance reference on destroy. Instance is cleared on domain reload before scene load.

Example: Simple service

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Utils;

public sealed class GameServices : RuntimeSingleton<GameServices>
{
    // Disable cross‑scene persistence if desired
    protected override bool Preserve => false;

    public void Log(string message)
    {
        Debug.Log($"[GameServices] {message}");
    }
}

// Usage from anywhere
GameServices.Instance.Log("Hello world");
```

ODIN note: With Odin installed, the class inherits `SerializedMonoBehaviour`, enabling dictionaries and other complex serialized types.

Common pitfalls:
- If an inactive instance exists in the scene, `Instance` won’t find it (search excludes inactive objects) and will create a new one.
- If two active instances exist, the newer one logs an error and destroys itself.
- If `Preserve` is `true`, the instance is detached and marked `DontDestroyOnLoad`.

Lifecycle diagram:
```
T.Instance ─┬─ Has _instance? ──▶ return
            │
            ├─ Find active T in scene? ──▶ set _instance, return
            │
            └─ Create GameObject("T-Singleton") + Add T
                 └─ Awake(): assign _instance, if Preserve: DontDestroyOnLoad
                         └─ Start(): if duplicate, log + destroy self
```

Notes:
- To avoid creation during a sensitive frame, place a pre‑made instance in your bootstrap scene.
- For scene‑local managers, override `Preserve => false`.

<a id="scriptableobject-singleton"></a>
## `ScriptableObjectSingleton<T>` Overview

- Access via `T.Instance` (lazy‑loads from `Resources/` using either a custom path or the type name; warns if multiple assets found and chooses the first by name).
- `HasInstance` indicates whether the lazy value exists and is not null.
- Optional `[ScriptableSingletonPath("Sub/Folder")]` to control the `Resources` subfolder.
- Editor utility auto‑creates and relocates assets: see the “ScriptableObject Singleton Creator” in the Editor Tools Guide.

Example: Settings asset

```csharp
using WallstopStudios.UnityHelpers.Utils;
using WallstopStudios.UnityHelpers.Core.Attributes;

[ScriptableSingletonPath("Settings/Audio")]
public sealed class AudioSettings : ScriptableObjectSingleton<AudioSettings>
{
    public float musicVolume = 0.8f;
    public bool enableSpatialAudio = true;
}

// Usage at runtime
float vol = AudioSettings.Instance.musicVolume;
```

ODIN note: With Odin installed, the class inherits `SerializedScriptableObject`, so you can safely serialize complex collections without custom drawers.

Asset management tips:
- Place the asset under `Assets/Resources/` (or under the path from `[ScriptableSingletonPath]`).
- The Editor’s “ScriptableObject Singleton Creator” runs on load to create missing assets and move misplaced ones. It also supports a test‑assembly toggle used by our test suite.

Lookup order diagram:
```
Instance access:
  [1] Resources.LoadAll<T>(custom path from [ScriptableSingletonPath])
  [2] if none: Resources.Load<T>(type name)
  [3] if none: Resources.LoadAll<T>(root)
  [4] if multiple: warn + pick first by name (sorted)
```

Auto‑creator flow (Editor):
```
On editor load:
  - Scan all ScriptableObjectSingleton<T> types
  - For each non-abstract type:
      - Determine Resources path (attribute or type name)
      - Ensure folder under Assets/Resources
      - If asset exists elsewhere: move to target path
      - Else: create new asset at target path
  - Save & Refresh if changes
```

Asset structure diagram:
```
Default (no attribute):
Assets/
  Resources/
    AudioSettings.asset         // type name

With [ScriptableSingletonPath("Settings/Audio")]:
Assets/
  Resources/
    Settings/
      Audio/
        AudioSettings.asset
```

<a id="scenarios"></a>
## Scenarios & Guidance

- Global dispatcher: See `UnityMainThreadDispatcher` which derives from `RuntimeSingleton<UnityMainThreadDispatcher>`.
- Global data caches or registries: Use `ScriptableObjectSingleton<T>` so data lives in a single editable asset and loads fast.
- Cross‑scene managers: Keep `Preserve = true` to avoid duplicates across scene loads.

<a id="troubleshooting"></a>
## Troubleshooting

## Best Practices

- Prefer placing a pre-made runtime singleton in a bootstrap scene when construction order matters; avoid first-access implicit creation during critical frames.
- For scene-local managers, override `Preserve => false` to prevent cross-scene persistence.
- Keep exactly one singleton asset under `Resources/` for each `ScriptableObjectSingleton<T>`; let the auto-creator relocate any strays.
- Use `[ScriptableSingletonPath]` to group related settings; avoid deep nesting that hurts discoverability.
- With ODIN installed, take advantage of `Serialized*` bases for complex serialized fields; without Odin, keep fields Unity-serializable.

- Multiple ScriptableObject assets found: a warning is logged and the first by name is used. Resolve by keeping only one asset in Resources or by letting the auto‑creator relocate the correct one.
- `Instance` returns null for ScriptableObject: Ensure the asset exists under `Resources/` and the type name or custom path matches.
- Domain reloads: Both singletons clear cached instances before scene load.

## Related Docs

- Editor tool: [ScriptableObject Singleton Creator](EDITOR_TOOLS_GUIDE.md#scriptableobject-singleton-creator).
- Tests: `Tests/Runtime/Utils/RuntimeSingletonTests.cs` and `Tests/Editor/Utils/ScriptableObjectSingletonTests.cs`.
