# Skill: Editor Singleton Patterns

<!-- trigger: singleton, ScriptableObjectSingleton, asset creation, ClearInstance, singleton path | Singleton asset management patterns for Editor code | Core -->

**Trigger**: When working with `ScriptableObjectSingleton` assets in Editor code, including creation, caching, path validation, and find-or-create patterns.

---

## When to Use

- Creating or loading `ScriptableObjectSingleton` assets
- Implementing find-or-create patterns for singleton assets
- Referencing `ScriptableSingletonPath` attribute paths in Editor code
- Debugging singleton instance resolution failures

## When NOT to Use

- Runtime singleton patterns (MonoBehaviour singletons)
- Non-singleton `ScriptableObject` assets
- Editor code that only reads singleton values without creating them

---

## Singleton Path Consistency (CRITICAL)

When Editor code references `ScriptableObjectSingleton` asset paths, hardcoded paths **must match** the `[ScriptableSingletonPath]` attribute on the singleton class. Path drift between the attribute and hardcoded constants is a common source of silent failures.

**Bug pattern**: The `ScriptableSingletonPath` attribute is updated but hardcoded paths in Editor generators or tests are not. The singleton resolves via `Instance` (which reads the attribute), but Editor code creates/loads from the wrong path.

**Prevention**: Write a regression test that validates hardcoded path constants against the `ScriptableSingletonPath` attribute:

```csharp
[Test]
public void CachePathsMatchScriptableSingletonPathAttribute()
{
    Attribute[] attributes = Attribute.GetCustomAttributes(
        typeof(MySingleton),
        typeof(ScriptableSingletonPathAttribute)
    );
    Assert.AreEqual(1, attributes.Length);

    ScriptableSingletonPathAttribute pathAttribute =
        (ScriptableSingletonPathAttribute)attributes[0];
    string resourcesPath = pathAttribute.resourcesPath;

    string expectedAssetPath =
        $"Assets/Resources/{resourcesPath}/{nameof(MySingleton)}.asset";
    Assert.AreEqual(expectedAssetPath, HardcodedAssetPath);
}
```

**Rule**: When changing a `ScriptableSingletonPath` attribute value, search the entire codebase for the old path string and update all references.

---

## Asset Creation Pattern (CRITICAL)

When creating assets via `AssetDatabase.CreateAsset()` in Editor code, always follow this three-step pattern from `ScriptableObjectSingletonMetadataUtility.LoadOrCreateMetadataAsset()`:

```csharp
ScriptableObject instance = ScriptableObject.CreateInstance<MyType>();
using (AssetDatabaseBatchHelper.PauseBatch())
{
    try
    {
        AssetDatabase.CreateAsset(instance, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
    }
    catch (Exception ex)
    {
        Debug.LogWarning($"Failed to create asset: {ex.Message}");
        if (instance != null)
        {
            Object.DestroyImmediate(instance);
        }
        return null;
    }
}
```

**Three required elements:**

1. **`PauseBatch()`** — Temporarily exits any active `AssetDatabase.StartAssetEditing()` batch scope. Without this, `CreateAsset` may silently fail when called from within a batch.
2. **`SaveAssets()`** — Writes the asset to disk immediately.
3. **`ImportAsset(path, ForceSynchronousImport)`** — Forces the AssetDatabase to recognize the new file. Without this, `LoadAssetAtPath()` may return null for the newly created asset, especially during test execution.

**Bug pattern**: Calling `CreateAsset` without `ImportAsset(ForceSynchronousImport)` creates the file on disk but the AssetDatabase may not index it immediately. Subsequent `LoadAssetAtPath` calls return null, causing silent failures in tests and during editor initialization.

---

## Lazy Singleton Caching After Asset Creation (CRITICAL)

When code accesses `ScriptableObjectSingleton<T>.Instance` and the asset doesn't exist yet, the `Lazy<T>` caches `null` permanently. If the asset is subsequently created, `Instance` still returns the stale `null`.

**Bug pattern**: Code calls `Instance` as a fallback lookup, gets `null`, creates the asset, but forgets to clear the `Lazy<T>` cache. All subsequent `Instance` calls return `null`.

**Prevention**: After creating a new singleton asset, always call `ClearInstance()`:

```csharp
// After creating the asset successfully
ScriptableObjectSingleton<MyType>.ClearInstance();
```

`ClearInstance()` is safe to call unconditionally — it is a no-op if `Instance` was never accessed.

---

## Singleton Instance Path Validation (CRITICAL)

When Editor code uses `ScriptableObjectSingleton<T>.Instance` in a find-or-create pattern (e.g., `GetOrCreateCache()`), always validate that the returned instance is at the expected asset path before using it. `Instance` uses `Resources.FindObjectsOfTypeAll<T>()` as a fallback, which discovers objects at ANY path—including backup copies, test fixtures, or objects created by other tools. Without path validation, the code may silently return an object at the wrong location, bypassing asset creation at the correct path.

```csharp
// WRONG - Instance may return object at wrong path
cache = MySingleton.Instance;
if (cache != null) return cache;  // BUG: may be at wrong path

// CORRECT - Validate path before accepting Instance result
cache = MySingleton.Instance;
if (cache != null)
{
    string instancePath = AssetDatabase.GetAssetPath(cache);
    if (string.Equals(instancePath, expectedPath, StringComparison.OrdinalIgnoreCase))
    {
        return cache;
    }
    // Fall through to creation at correct path
}
```

---

## Related Skills

- [defensive-editor-programming](./defensive-editor-programming.md) - Overview of all defensive editor patterns
- [editor-multi-object-editing](./editor-multi-object-editing.md) - Multi-object editing and undo patterns
- [editor-api-rules](./editor-api-rules.md) - Forbidden APIs and value handling rules
- [create-editor-tool](./create-editor-tool.md) - Editor tool creation patterns
