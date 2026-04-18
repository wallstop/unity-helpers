# Skill: Editor Undo Complete Policy

<!-- trigger: editor undo, undo support, undo policy, undo tiers, importer undo | Complete undo policy for editor tooling with enforceable scope boundaries | Core -->

**Trigger**: When adding or modifying any editor code path that mutates objects, serialized properties, importers, or assets.

---

## Goal

Keep editor tooling undo-safe by default, and explicit about operations Unity cannot fully reverse.

## Tier Model

Use this model to classify every mutation path:

1. Tier A (Blocking)

- Mutation is expected to be undoable and must record undo.
- Examples: `SerializedProperty` value changes, direct `UnityEngine.Object` field mutation in custom editors/property drawers, menu callback writes.
- Enforcement: missing undo is a blocker.

1. Tier B (Warning)

- Mutation path is complex and requires manual review.
- Examples: deferred callback chains, mixed direct mutation + importer settings in multi-step flows.
- Enforcement: warning plus review note.

1. Tier C (Out-of-guarantee)

- Full reversal is not guaranteed by Unity Undo semantics alone.
- Examples: file I/O side effects, broad reimport flows, destructive batch writes.
- Enforcement: document limitation and do not claim full reversal.

## Required Patterns

1. Record before mutate

```csharp
Undo.RecordObject(targetObject, "Apply Change");
```

1. Multi-object edits

```csharp
Undo.RecordObjects(serializedObject.targetObjects, "Apply Change");
```

1. SerializedProperty workflows

- Use `serializedObject.Update()` before reading/writing.
- Use `ApplyModifiedProperties()` after mutation.

1. Menu callback writes

- Record undo inside callback before mutation.
- Re-acquire serialized property if needed, then apply.

## Importer Guidance

For importer mutation paths:

- Record undo before changing importer settings.
- Be explicit that reimport/file system side effects may not be fully reversible by Undo alone.
- Prefer clear operation names so undo history is understandable.

## Anti-Patterns

- Mutating values during render pass without change guards.
- Writing directly in `OnGUI` loops every frame.
- Performing mutation callbacks with no undo recording.
- Claiming "full undo" for side-effectful file/reimport operations.

## Review Checklist

For each changed editor mutation path:

1. Is the mutation tiered (A/B/C)?
2. If Tier A, is undo recorded before mutation?
3. Are serialized object update/apply steps correct?
4. Is behavior covered by undo/redo regression tests when feasible?
5. If Tier C, is limitation documented where users/devs will see it?

## Related Skills

- [editor-multi-object-editing](./editor-multi-object-editing.md)
- [odin-undo-safety](./odin-undo-safety.md)
- [defensive-editor-programming](./defensive-editor-programming.md)
