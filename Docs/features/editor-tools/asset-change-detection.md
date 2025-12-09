# Asset Change Detection

**Automatically respond to asset creation and deletion events.**

The `[DetectAssetChanged]` attribute allows you to annotate methods that should execute automatically when specific asset types are created or deleted in the Unity Editor. Perfect for cache invalidation, auto-configuration, validation, and maintaining derived data.

---

## Table of Contents

- [Basic Usage](#basic-usage)
- [Attribute Parameters](#attribute-parameters)
- [Method Signatures](#method-signatures)
- [Inheritance Support](#inheritance-support)
- [Asset Change Context](#asset-change-context)
- [Best Practices](#best-practices)
- [Examples](#examples)

---

## Basic Usage

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class SpriteCache : MonoBehaviour
{
    private static Dictionary<string, Sprite> _cache = new();

    [DetectAssetChanged(typeof(Sprite), AssetChangeFlags.Created | AssetChangeFlags.Deleted)]
    private static void OnSpriteChanged(Sprite sprite)
    {
        if (sprite != null)
        {
            Debug.Log($"Sprite changed: {sprite.name}");
            InvalidateCache();
        }
    }

    private static void InvalidateCache()
    {
        _cache.Clear();
    }
}
```

> **Visual Reference**
>
> ![Asset change detection workflow diagram](../../images/editor-tools/asset-change-detection-flow.png)
> _Automatic method invocation when assets are created or deleted_

---

## Attribute Parameters

```csharp
[DetectAssetChanged(
    Type assetType,                          // Type of asset to monitor (required)
    AssetChangeFlags flags,                  // Created, Deleted, or both (required)
    DetectAssetChangedOptions options = None // IncludeAssignableTypes for inheritance
)]
```

### AssetChangeFlags

```csharp
[Flags]
public enum AssetChangeFlags
{
    None = 0,
    Created = 1 << 0,     // Trigger on asset creation
    Deleted = 1 << 1,     // Trigger on asset deletion
}
```

### DetectAssetChangedOptions

```csharp
[Flags]
public enum DetectAssetChangedOptions
{
    None = 0,
    IncludeAssignableTypes = 1 << 0,  // Also trigger for derived types
}
```

---

## Method Signatures

The attribute supports four method signatures:

### 1. No Parameters (Fire-and-Forget)

```csharp
[DetectAssetChanged(typeof(ScriptableObject), AssetChangeFlags.Created)]
private static void OnScriptableObjectCreated()
{
    Debug.Log("A ScriptableObject was created");
}
```

**When to use:** Simple notifications that don't need asset details

---

### 2. Asset Parameter (Most Common)

```csharp
[DetectAssetChanged(typeof(Material), AssetChangeFlags.Deleted)]
private void OnMaterialDeleted(Material material)
{
    Debug.Log($"Material deleted: {material?.name ?? "null"}");
    RemoveFromCache(material);
}
```

**When to use:** Need the asset reference to perform updates

**Note:** Asset parameter will be `null` for deletion events (asset no longer exists)

---

### 3. Asset Path Parameter

```csharp
[DetectAssetChanged(typeof(Texture2D), AssetChangeFlags.Created)]
private static void OnTextureCreated(string assetPath)
{
    Debug.Log($"New texture at: {assetPath}");
    ValidateTextureSettings(assetPath);
}
```

**When to use:** Need the file path for AssetDatabase operations

---

### 4. Full Context (Advanced)

```csharp
[DetectAssetChanged(typeof(AudioClip), AssetChangeFlags.Created | AssetChangeFlags.Deleted)]
private void OnAudioClipChanged(AssetChangeContext context)
{
    Debug.Log($"AudioClip {context.AssetPath}: {context.Flags}");

    if (context.Asset != null)
    {
        ProcessAudioClip((AudioClip)context.Asset);
    }
}
```

**When to use:** Need both the asset reference and path, or want to handle multiple flags in one method

---

## Inheritance Support

By default, the attribute only triggers for exact type matches. Use `IncludeAssignableTypes` to include derived types:

```csharp
// Triggers for ScriptableObject and ALL derived types
[DetectAssetChanged(
    typeof(ScriptableObject),
    AssetChangeFlags.Created,
    DetectAssetChangedOptions.IncludeAssignableTypes
)]
private static void OnAnyScriptableObjectCreated(ScriptableObject obj)
{
    Debug.Log($"ScriptableObject created: {obj.GetType().Name}");
}

// Only triggers for exact Material type (not derived classes)
[DetectAssetChanged(typeof(Material), AssetChangeFlags.Created)]
private static void OnExactMaterialCreated(Material mat)
{
    Debug.Log("Material (exact type) created");
}
```

---

## Asset Change Context

The `AssetChangeContext` struct provides complete information about the change:

```csharp
public readonly struct AssetChangeContext
{
    public readonly Object Asset;           // The asset (null for deletions)
    public readonly string AssetPath;       // Full asset path
    public readonly AssetChangeFlags Flags; // Created or Deleted
}
```

---

## Best Practices

### Performance Considerations

1. **Keep methods fast** - They run synchronously during asset import
2. **Avoid heavy operations** - Consider deferring work with `EditorApplication.delayCall`
3. **Use static methods when possible** - Faster invocation, no instance required

### Design Patterns

```csharp
// ✅ GOOD: Static method for global cache
[DetectAssetChanged(typeof(Sprite), AssetChangeFlags.Created | AssetChangeFlags.Deleted)]
private static void OnSpriteChanged()
{
    SpriteManager.InvalidateCache();
}

// ✅ GOOD: Instance method for component-specific logic
[DetectAssetChanged(typeof(AudioClip), AssetChangeFlags.Created)]
private void OnAudioClipCreated(AudioClip clip)
{
    if (clip.name.StartsWith(audioPrefix))
    {
        RegisterClip(clip);
    }
}

// ⚠️ CAUTION: Expensive operation during import
[DetectAssetChanged(typeof(Texture2D), AssetChangeFlags.Created)]
private static void OnTextureCreated(Texture2D texture)
{
    // Heavy processing - consider deferring
    ProcessTexture(texture);
}
```

### Avoiding Reentrant Issues

```csharp
[DetectAssetChanged(typeof(Material), AssetChangeFlags.Created)]
private static void OnMaterialCreated(string assetPath)
{
    // ❌ BAD: Creating assets during asset processing can cause loops
    // AssetDatabase.CreateAsset(newMaterial, "Assets/Generated.mat");

    // ✅ GOOD: Defer asset creation
    EditorApplication.delayCall += () =>
    {
        AssetDatabase.CreateAsset(newMaterial, "Assets/Generated.mat");
    };
}
```

---

## Examples

### Cache Invalidation

```csharp
public class TextureAtlas : ScriptableObject
{
    private static List<Texture2D> _cachedTextures;

    [DetectAssetChanged(typeof(Texture2D), AssetChangeFlags.Created | AssetChangeFlags.Deleted)]
    private static void OnTextureChanged()
    {
        _cachedTextures = null; // Invalidate cache
    }
}
```

### Auto-Configuration

```csharp
public class MaterialValidator : MonoBehaviour
{
    [DetectAssetChanged(typeof(Material), AssetChangeFlags.Created)]
    private static void ValidateNewMaterial(Material material, string assetPath)
    {
        if (material.shader.name == "Standard")
        {
            // Apply project-wide defaults
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Glossiness", 0.5f);
            EditorUtility.SetDirty(material);
        }
    }
}
```

### Derived Type Monitoring

```csharp
public abstract class GameData : ScriptableObject { }

public class DataRegistry : MonoBehaviour
{
    private static HashSet<GameData> _allData = new();

    [DetectAssetChanged(
        typeof(GameData),
        AssetChangeFlags.Created,
        DetectAssetChangedOptions.IncludeAssignableTypes
    )]
    private static void RegisterGameData(GameData data)
    {
        _allData.Add(data);
        Debug.Log($"Registered: {data.GetType().Name}");
    }

    [DetectAssetChanged(
        typeof(GameData),
        AssetChangeFlags.Deleted,
        DetectAssetChangedOptions.IncludeAssignableTypes
    )]
    private static void UnregisterGameData(string assetPath)
    {
        // Asset is already deleted, use path for cleanup
        Debug.Log($"Unregistered: {assetPath}");
    }
}
```

---

## Implementation Details

The `DetectAssetChangeProcessor` (Editor assembly) automatically:

1. Scans for methods decorated with `[DetectAssetChanged]`
2. Registers callbacks with Unity's `AssetPostprocessor`
3. Invokes methods when matching assets change
4. Handles null checks and error cases
5. Supports both Edit Mode and Play Mode

**Threading:** All callbacks execute on the main thread during asset processing

**Timing:** Methods are called after Unity completes asset import/deletion

---

## Troubleshooting

### Method Not Called

- Ensure the method is in a type that Unity can discover (not in a generic class)
- Check that the asset type matches exactly (unless using `IncludeAssignableTypes`)
- Verify the asset change flags match the operation (Created vs Deleted)

### Performance Issues

- Profile with Unity Profiler during asset import
- Consider deferring work with `EditorApplication.delayCall`
- Use `static` methods to avoid unnecessary instance lookups

### Null Reference Exceptions

- Remember: asset parameter is `null` for deletion events
- Always null-check when handling `AssetChangeFlags.Deleted`

---

## Related Features

- [Attribute Metadata Cache Generator](./attribute-metadata-cache-generator.md) - Caches attribute metadata for fast lookup
- [ScriptableObject Singleton Creator](./scriptableobject-singleton-creator.md) - Auto-creates singleton assets
- [Inspector Attributes](../inspector/inspector-overview.md) - Other custom inspector features
