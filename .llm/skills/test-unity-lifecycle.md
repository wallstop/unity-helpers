# Skill: Test Unity Lifecycle

<!-- trigger: test, track, destroy, cleanup, lifecycle | Track(), DestroyImmediate, object cleanup | Core -->

**Trigger**: When managing Unity object lifecycle in tests, including Track(), DestroyImmediate, and object cleanup.

---

## When to Use

Use this skill when:

- Creating Unity objects in tests (GameObjects, Components, Editors)
- Understanding the Track() pattern for automatic cleanup
- Troubleshooting UNH001, UNH002, or UNH003 lint errors
- Writing tests that intentionally destroy objects

For general test creation, see [create-test](./create-test.md).
For Odin-specific testing, see [test-odin-drawers](./test-odin-drawers.md).

---

## CRITICAL: Track All Unity Objects

**MANDATORY**: All Unity objects created in tests MUST be tracked for automatic cleanup. The lint script `scripts/lint-tests.ps1` enforces these rules.

### Lint Rules Enforced

| Rule     | Description                                                                                |
| -------- | ------------------------------------------------------------------------------------------ |
| `UNH001` | Avoid direct `DestroyImmediate`/`Destroy` in tests; track object and let teardown clean up |
| `UNH002` | Unity object allocation must be tracked: wrap with `Track()`                               |
| `UNH003` | Test class creates Unity objects but doesn't inherit from `CommonTestBase`                 |

---

## MANDATORY: Run Lint After EVERY Test Change

> **CRITICAL**: Run the test lifecycle linter IMMEDIATELY after ANY modification to test files. Do NOT wait until the end of your task.

```bash
pwsh -NoProfile -File scripts/lint-tests.ps1
```

This linter is also run by the pre-push git hook. Failing to run it locally will result in rejected pushes.

---

## Required Pattern: Track All Unity Objects

**ALWAYS** wrap Unity object creation with `Track()`:

```csharp
// ✅ CORRECT - Objects tracked for automatic cleanup
public sealed class MyDrawerTests : CommonTestBase
{
    [Test]
    public void DrawerCreatesEditorSuccessfully()
    {
        MyTarget target = CreateScriptableObject<MyTarget>();
        Editor editor = Track(Editor.CreateEditor(target));

        Assert.IsTrue(editor != null);
    }
}
```

---

## Forbidden Pattern: Manual DestroyImmediate

**NEVER** use try-finally blocks with `DestroyImmediate` for cleanup:

```csharp
// ❌ FORBIDDEN - Manual cleanup causes UNH001 lint errors
Editor editor = Editor.CreateEditor(target);
try
{
    editor.OnInspectorGUI();
}
finally
{
    UnityEngine.Object.DestroyImmediate(editor);  // UNH001 violation!
}

// ✅ CORRECT - Track() handles cleanup automatically
Editor editor = Track(Editor.CreateEditor(target));
editor.OnInspectorGUI();
```

---

## Track Methods Reference

| Method                        | Use For                                              |
| ----------------------------- | ---------------------------------------------------- |
| `CreateScriptableObject<T>()` | Creating test `ScriptableObject` targets             |
| `NewGameObject(name)`         | Creating test `GameObject` instances                 |
| `Track(obj)`                  | Any Unity object (`Editor`, `Material`, `Texture2D`) |
| `TrackDisposable(disposable)` | `IDisposable` resources                              |
| `TrackAssetPath(path)`        | Created asset files that need deletion               |
| `_trackedObjects.Remove(obj)` | Remove from tracking after intentional destroy       |

---

## Exception: Using `// UNH-SUPPRESS` Comments

The `// UNH-SUPPRESS` comment tells the linter to skip checking that specific line. Use it **ONLY** when:

1. **Testing destroy behavior** — Intentionally destroying objects to verify error handling
2. **Testing destroyed state** — Verifying code handles destroyed objects gracefully
3. **Testing cleanup edge cases** — Ensuring cleanup code doesn't double-destroy

### UNH-SUPPRESS Syntax

Place the comment on the **same line** as the `DestroyImmediate` call:

```csharp
// ✅ CORRECT - Comment on same line
UnityEngine.Object.DestroyImmediate(target); // UNH-SUPPRESS: Test verifies behavior after target destroyed

// ✅ CORRECT - With explanation
Object.DestroyImmediate(target); // UNH-SUPPRESS: Intentionally destroy to test null handling

// ❌ WRONG - Comment on different line (will NOT suppress)
// UNH-SUPPRESS: This won't work
UnityEngine.Object.DestroyImmediate(target);
```

### Complete Example: Testing Destroyed Object Handling

```csharp
[Test]
public void InspectorHandlesDestroyedTargetGracefully()
{
    MyTarget target = CreateScriptableObject<MyTarget>();
    Editor editor = Track(Editor.CreateEditor(target));

    editor.OnInspectorGUI();

    UnityEngine.Object.DestroyImmediate(target); // UNH-SUPPRESS: Test verifies behavior after target destroyed
    _trackedObjects.Remove(target); // Remove from tracking to prevent double-destroy in teardown

    Assert.DoesNotThrow(() => editor.OnInspectorGUI());
}
```

### When NOT to Use UNH-SUPPRESS

```csharp
// ❌ WRONG - Don't use suppress for normal cleanup
try
{
    editor.OnInspectorGUI();
}
finally
{
    UnityEngine.Object.DestroyImmediate(editor); // UNH-SUPPRESS  <-- DON'T DO THIS
}

// ✅ CORRECT - Use Track() instead
Editor editor = Track(Editor.CreateEditor(target));
editor.OnInspectorGUI();
// Cleanup handled automatically by CommonTestBase
```

---

## Async Test Pattern

For `[UnityTest]` with `IEnumerator`, still use `Track()`:

```csharp
[UnityTest]
public IEnumerator OnInspectorGuiDoesNotThrowForTarget()
{
    MyTarget target = CreateScriptableObject<MyTarget>();
    Editor editor = Track(Editor.CreateEditor(target));
    bool completed = false;
    Exception caught = null;

    yield return TestIMGUIExecutor.Run(() =>
    {
        try
        {
            editor.OnInspectorGUI();
            completed = true;
        }
        catch (Exception ex)
        {
            caught = ex;
        }
    });

    Assert.IsTrue(caught == null);
    Assert.IsTrue(completed);
}
```

---

## Fix Workflow

1. Make a test file change
2. Run `pwsh -NoProfile -File scripts/lint-tests.ps1`
3. Fix any `UNH001`, `UNH002`, or `UNH003` errors
4. Re-run linter to confirm fix
5. Only then proceed to next change

### Common Fixes

| Error    | Fix                                                                                           |
| -------- | --------------------------------------------------------------------------------------------- |
| `UNH001` | Remove `DestroyImmediate`; use `Track()` OR add `// UNH-SUPPRESS` if testing destroy behavior |
| `UNH002` | Wrap object creation with `Track()`: `Track(new GameObject())`                                |
| `UNH003` | Add `: CommonTestBase` or `: EditorCommonTestBase` to test class                              |

---

## CommonTestBase Inheritance

Tests that create Unity objects must inherit from `CommonTestBase`:

```csharp
// ✅ CORRECT
public sealed class MyTests : CommonTestBase
{
    [Test]
    public void MyTest()
    {
        GameObject obj = NewGameObject("Test");
        // Automatically cleaned up
    }
}

// ❌ WRONG - UNH003 violation
public sealed class MyTests
{
    [Test]
    public void MyTest()
    {
        GameObject obj = new GameObject("Test"); // UNH002 + UNH003
    }
}
```

---

## Related Skills

- [create-test](./create-test.md) — General test creation guidelines
- [test-odin-drawers](./test-odin-drawers.md) — Odin Inspector drawer testing
- [validate-before-commit](./validate-before-commit.md) — Pre-commit validation workflow
